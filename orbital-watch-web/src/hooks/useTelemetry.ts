import { useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import { useTelemetryStore } from "../store/telemetryStore";
import { getToken } from "../lib/auth";
import type { TelemetryEvent, ConjunctionAlert } from "../types/telemetry";

export function useTelemetry(satelliteIds: number[]) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  const updateTelemetry = useTelemetryStore((state) => state.updateTelemetry);
  const addAlert = useTelemetryStore((state) => state.addAlert);
  const removeAlert = useTelemetryStore((state) => state.removeAlert);
  const setConnectionStatus = useTelemetryStore(
    (state) => state.setConnectionStatus,
  );

  useEffect(() => {
    // Don't connect if we dont have a token yet (user hasn't logged in)
    const token = getToken();
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/telemetry", {
        accessTokenFactory: () => getToken() ?? "",
      })
      .withAutomaticReconnect([0, 2000, 5000, 10_000, 30_000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connectionRef.current = connection;

    // Register handlers before starting the connection
    connection.on("TelemetryUpdate", (event: TelemetryEvent) =>
      updateTelemetry(event),
    );
    connection.on("ConjunctionAlert", (alert: ConjunctionAlert) => {
      addAlert(alert);

      setTimeout(() => removeAlert(alert), 10_000);
    });
    connection.on("Subscribed", (satelliteId: number) => {
      console.log(`Subscribed to satellite ${satelliteId}`);
    });

    // Connection lifecycle events
    connection.onreconnecting(() => setConnectionStatus("reconnecting"));
    connection.onreconnected(async () => {
      setConnectionStatus("connected");
      // Re-subscribe to all satellites
      for (const id of satelliteIds) {
        await connection.invoke("SubscribeToSatellite", id);
      }
    });

    connection.onclose(() => setConnectionStatus("disconnected"));

    // Start connection
    setConnectionStatus("connecting");
    connection
      .start()
      .then(async () => {
        setConnectionStatus("connected");
        for (const id of satelliteIds) {
          await connection.invoke("SubscribeToSatellite", id);
        }
      })
      .catch((err) => {
        console.error("SignalR connection failed", err);
        setConnectionStatus("disconnected");
      });

    // Cleanup on unmount
    return () => {
      connection.stop();
      connectionRef.current = null;
    };
  }, [satelliteIds]);
}
