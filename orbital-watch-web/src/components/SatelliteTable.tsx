import { useTelemetryStore } from '../store/telemetryStore'

const REGIME_COLORS: Record<string, string> = {
    LEO: '#3b82f6',
    MEO: '#8b5cf6',
    GEO: '#10b981',
    HEO: '#f59e0b',
}

export function SatelliteTable() {
    const satellites      = useTelemetryStore((s) => s.satellites)
    const latestTelemetry = useTelemetryStore((s) => s.latestTelemetry)
    const alerts          = useTelemetryStore((s) => s.alerts)

    // Build a set of satellite IDs currently involved in an active conjunction
    const alertedIds = new Set<number>()
    for (const alert of alerts) {
        alertedIds.add(alert.primarySatelliteId)
        alertedIds.add(alert.secondarySatelliteId)
    }

    return (
        <table className="satellite-table">
            <thead>
            <tr>
                <th>Satellite</th>
                <th>Regime</th>
                <th>Lat</th>
                <th>Lon</th>
                <th>Alt (km)</th>
                <th>Speed (km/s)</th>
                <th>Last update</th>
            </tr>
            </thead>
            <tbody>
            {satellites.map((sat) => {
                const telemetry = latestTelemetry[sat.id]
                const isAlerted = alertedIds.has(sat.id)

                return (
                    <tr key={sat.id} className={isAlerted ? 'row-alert' : ''}>
                        <td>
                <span
                    className="regime-dot"
                    style={{ backgroundColor: REGIME_COLORS[sat.orbitalRegime] }}
                />
                            {sat.name}
                        </td>
                        <td>{sat.orbitalRegime}</td>
                        <td>{telemetry ? telemetry.latitudeDeg.toFixed(2) : '—'}</td>
                        <td>{telemetry ? telemetry.longitudeDeg.toFixed(2) : '—'}</td>
                        <td>{telemetry ? telemetry.altitudeKm.toFixed(1) : '—'}</td>
                        <td>{telemetry ? telemetry.speedKms.toFixed(3) : '—'}</td>
                        <td>
                            {telemetry
                                ? new Date(telemetry.timestamp).toLocaleTimeString()
                                : 'waiting...'}
                        </td>
                    </tr>
                )
            })}
            </tbody>
        </table>
    )
}