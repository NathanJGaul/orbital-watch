export interface Satellite {
    id: number
    name: string
    noradId: string
    orbitalRegime: 'LEO' | 'MEO' | 'GEO' | 'HEO'
    owner: string
    isActive: boolean
}

export interface TelemetryEvent {
    id: number
    satelliteId: number
    timestamp: string      // ISO 8601 from .NET DateTime
    altitudeKm: number
    longitudeDeg: number
    latitudeDeg: number
    velocityXKms: number
    velocityYKms: number
    velocityZKms: number
    speedKms: number
}

export interface ConjunctionAlert {
    primarySatelliteId: number
    secondarySatelliteId: number
    missDistanceKm: number
    detectedAt: string
    severity: 'Low' | 'Medium' | 'High' | 'Critical'
}

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting'
