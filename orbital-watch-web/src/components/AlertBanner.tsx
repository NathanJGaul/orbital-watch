import { useTelemetryStore } from "../store/telemetryStore";

const SEVERITY_STYLES: Record<string, { bg: string; border: string }> = {
  Low: { bg: "#fef9c3", border: "#ca8a04" },
  Medium: { bg: "#fed7aa", border: "#ea580c" },
  High: { bg: "#fecaca", border: "#dc2626" },
  Critical: { bg: "#fca5a5", border: "#991b1b" },
};

export function AlertBanner() {
  const alerts = useTelemetryStore((s) => s.alerts);
  const removeAlert = useTelemetryStore((s) => s.removeAlert);

  if (alerts.length === 0) return null;

  return (
    <div className="alert-banner-container">
      {alerts.map((alert, i) => {
        const style = SEVERITY_STYLES[alert.severity] ?? SEVERITY_STYLES.Medium;
        return (
          <div
            key={`${alert.primarySatelliteId}-${alert.secondarySatelliteId}-${i}`}
            className="alert-banner"
            style={{ backgroundColor: style.bg, borderColor: style.border }}
          >
            <strong>{alert.severity} Conjunction</strong> — Satellite{" "}
            {alert.primarySatelliteId} and Satellite{" "}
            {alert.secondarySatelliteId} at {alert.missDistanceKm.toFixed(1)} km
            <button
              onClick={() => removeAlert(alert)}
              aria-label="Dismiss alert"
            >
              ✕
            </button>
          </div>
        );
      })}
    </div>
  );
}
