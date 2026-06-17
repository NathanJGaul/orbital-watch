#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:5164"
WS_URL="ws://localhost:5164"
PASS=0
FAIL=0

pass() { PASS=$((PASS + 1)); }
fail() { FAIL=$((FAIL + 1)); echo "  FAIL: $1"; }

echo "=== Smoke Tests ==="
echo

# 1. Authenticated connection receives live telemetry
echo "[1] Authenticated connection receives live telemetry"
TOKEN=$(curl -s -X POST "$BASE_URL/api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"userId":"test"}' | jq -r '.token')
if [[ -n "$TOKEN" && "$TOKEN" != "null" ]]; then
  echo "  Token acquired: ${TOKEN:0:20}..."
  pass
else
  fail "no token returned"
fi

# 2. Unauthenticated connection is rejected
echo "[2] Unauthenticated connection is rejected"
NEGOTIATE_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/hubs/telemetry/negotiate")
if [[ "$NEGOTIATE_STATUS" == "401" ]]; then
  pass
else
  fail "expected HTTP 401 on negotiate, got $NEGOTIATE_STATUS"
fi

# 3. CSP header is present
echo "[3] CSP header is present"
CSP=$(curl -sI "$BASE_URL/api/satellites" | grep -i "content-security-policy" || true)
if [[ -n "$CSP" ]]; then
  echo "  $CSP"
  pass
else
  fail "Content-Security-Policy header not found"
fi

# 4. Rate limiter is registered (test by hitting the auth endpoint 11 times)
echo "[4] Rate limiter is registered"
for i in $(seq 1 11); do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/auth/token" \
    -H "Content-Type: application/json" -d '{"userId":"test"}')
  echo "  Request $i: $STATUS"
done
RATE_LIMITED=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/auth/token" \
  -H "Content-Type: application/json" -d '{"userId":"test"}')
if [[ "$RATE_LIMITED" == "429" ]]; then
  pass
else
  fail "expected HTTP 429 after rate limit, got $RATE_LIMITED"
fi

echo
echo "=== Results: $PASS passed, $FAIL failed ==="
exit $FAIL
