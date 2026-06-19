import { create } from "zustand";
import type {
  Satellite,
  TelemetryEvent,
  ConjunctionAlert,
  ConnectionStatus,
} from "../types/telemetry";

interface TelemetryState {
  // Static catalog data, fetched once on load
  satellites: Satellite[];
  satSatellites: (satellites: Satellite[]) => void;

  // Live telemetry data, keyed by satellite ID - updated on every SignalR message
  latestTelemetry: Record<number, TelemetryEvent>; // O(1) lookup compared to O(n) array lookup
  updateTelemetry: (event: TelemetryEvent) => void;

  // Active conjunction alerts (auto-expire after 10s, handled by the component)
  alerts: ConjunctionAlert[];
  addAlert: (alert: ConjunctionAlert) => void;
  removeAlert: (alert: ConjunctionAlert) => void;

  // SignalR connection status surfaced in the UI
  connectionStatus: ConnectionStatus;
  setConnectionStatus: (status: ConnectionStatus) => void;
}

export const useTelemetryStore = create<TelemetryState>((set) => ({
  satellites: [],
  setSatellites: (satellites) => set({ satellites }),

  latestTelemetry: {},
  updateTelemetry: (event) =>
    set((state) => ({
      latestTelemetry: {
        ...state.latestTelemetry,
        [event.satelliteId]: event,
      },
    })),

  alerts: [],
  addAlert: (alert) =>
    set((state) => ({
      alerts: [...state.alerts, alert],
    })),
  removeAlert: (alert) =>
    set((state) => ({
      alerts: state.alerts.filter((a) => a !== alert),
    })),

  connectionStatus: "disconnected",
  setConnectionStatus: (status) => set({ connectionStatus: status }),
}));
