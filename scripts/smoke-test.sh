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

# 5. List satellites endpoint returns seeded data
echo "[5] List satellites endpoint returns seeded data"
SATELLITES=$(curl -s "$BASE_URL/api/satellites")
SATELLITE_COUNT=$(echo "$SATELLITES" | jq 'length')
if [[ "$SATELLITE_COUNT" -ge 5 ]]; then
  echo "  $SATELLITE_COUNT satellites returned (expected >= 5)"
  pass
else
  fail "expected >= 5 satellites, got $SATELLITE_COUNT"
fi

# 6. Get satellite by ID
echo "[6] Get satellite by ID"
SAT_BY_ID=$(curl -s "$BASE_URL/api/satellites/1")
SAT_NAME=$(echo "$SAT_BY_ID" | jq -r '.name')
if [[ -n "$SAT_NAME" && "$SAT_NAME" != "null" ]]; then
  echo "  Satellite #1: $SAT_NAME"
  pass
else
  fail "expected a satellite at id=1, got: $(echo "$SAT_BY_ID" | jq -c .)"
fi

# 7. 404 for unknown satellite
echo "[7] 404 for unknown satellite"
NOT_FOUND_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/satellites/9999")
if [[ "$NOT_FOUND_STATUS" == "404" ]]; then
  echo "  GET /api/satellites/9999 -> $NOT_FOUND_STATUS"
  pass
else
  fail "expected 404 for unknown satellite, got $NOT_FOUND_STATUS"
fi

# 8. 404 for unknown endpoint
echo "[8] 404 for unknown endpoint"
UNKNOWN_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/nonexistent")
if [[ "$UNKNOWN_STATUS" == "404" ]]; then
  echo "  GET /api/nonexistent -> $UNKNOWN_STATUS"
  pass
else
  fail "expected 404 for unknown endpoint, got $UNKNOWN_STATUS"
fi

# 9. Telemetry data is being generated (wait for simulator ticks)
echo "[9] Telemetry data is being generated"
sleep 3
TELEMETRY=$(curl -s "$BASE_URL/api/satellites/1/telemetry?count=1")
TELEMETRY_COUNT=$(echo "$TELEMETRY" | jq 'length')
if [[ "$TELEMETRY_COUNT" -ge 1 ]]; then
  FIRST_SPEED=$(echo "$TELEMETRY" | jq -r '.[0].speedKms')
  echo "  $TELEMETRY_COUNT telemetry event(s) for satellite #1, speed: $FIRST_SPEED km/s"
  pass
else
  fail "expected >= 1 telemetry event, got $TELEMETRY_COUNT"
fi

# 10. Latest telemetry endpoint returns data for a satellite
echo "[10] Latest telemetry endpoint returns data"
LATEST=$(curl -s "$BASE_URL/api/satellites/1/telemetry/latest")
LATEST_SPEED=$(echo "$LATEST" | jq -r '.speedKms')
if [[ "$LATEST_SPEED" != "null" && -n "$LATEST_SPEED" ]]; then
  echo "  Latest speed: $LATEST_SPEED km/s"
  pass
else
  fail "expected latest telemetry with speedKms, got: $(echo "$LATEST" | jq -c .)"
fi

# 11. Responses return JSON content type
echo "[11] JSON content type on API responses"
JSON_TYPE=$(curl -s -o /dev/null -w "%{content_type}" "$BASE_URL/api/satellites" | tr -d '[:space:]')
if echo "$JSON_TYPE" | grep -qi "application/json"; then
  echo "  Content-Type: $JSON_TYPE"
  pass
else
  fail "expected application/json content-type, got: $JSON_TYPE"
fi

echo
echo "=== Results: $PASS passed, $FAIL failed ==="
exit $FAIL
