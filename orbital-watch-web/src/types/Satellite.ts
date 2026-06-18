export interface Satellite {
    id: number
    name: string
    noradId: string
    orbitalRegime: 'LEO' | 'MEO' | 'GEO' | 'HEO'
    owner: string
    isActive: boolean
}

export interface Telemetry {
    id: number
    satelliteId: number
    timestampt: string      // ISO 8601 from .NET DateTime
    altitudeDeg: number
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
    severity: 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL'
}

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting'