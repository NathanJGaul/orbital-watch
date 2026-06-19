import { useEffect, useState } from 'react'
import { LoginGate } from './components/LoginGate'
import { SatelliteTable } from './components/SatelliteTable'
import { AlertBanner } from './components/AlertBanner'
import { useTelemetry } from './hooks/useTelemetry'
import { useTelemetryStore } from './store/telemetryStore'
import type { Satellite } from './types/telemetry'
import './App.css'

// Hardcoded for this project — Part 01 seeds satellites with IDs 1-5
const SATELLITE_IDS = [1, 2, 3, 4, 5]

function Dashboard() {
  const setSatellites     = useTelemetryStore((s) => s.setSatellites)
  const connectionStatus  = useTelemetryStore((s) => s.connectionStatus)

  // Establish the SignalR connection and subscriptions
  useTelemetry(SATELLITE_IDS)

  // Fetch the satellite catalog once on mount
  useEffect(() => {
    fetch('/api/satellites')
        .then((res) => res.json())
        .then((data: Satellite[]) => setSatellites(data))
        .catch((err) => console.error('Failed to load satellites:', err))
  }, [setSatellites])

  return (
      <div className="dashboard">
        <header>
          <h1>Orbital Watch</h1>
          <span className={`status-pill status-${connectionStatus}`}>
          {connectionStatus}
        </span>
        </header>
        <AlertBanner />
        <SatelliteTable />
      </div>
  )
}

function App() {
  const [loggedIn, setLoggedIn] = useState(false)

  if (!loggedIn) {
    return <LoginGate onLoggedIn={() => setLoggedIn(true)} />
  }

  return <Dashboard />
}

export default App