import { useEffect, useRef } from "react";
import { createGlobeScene } from "../three/scene";
import { SatelliteLayer } from "../three/satellites";
import { useTelemetryStore } from "../store/telemetryStore";

export function Globe() {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const layerRef = useRef<SatelliteLayer>(null);

  const satellites = useTelemetryStore((s) => s.satellites);
  const latestTelemetry = useTelemetryStore((s) => s.latestTelemetry);
  const alerts = useTelemetryStore((s) => s.alerts);

  // one time scene setup
  useEffect(() => {
    if (!canvasRef.current) return;

    const globeScene = createGlobeScene(canvasRef.current);
    const layer = new SatelliteLayer(globeScene.scene);
    layerRef.current = layer;

    let animationFrameId: number;

    function animate() {
      globeScene.controls.update(); // required when enableDamping is true
      layer.animate(); // step any pulse animations
      globeScene.renderer.render(globeScene.scene, globeScene.camera);
      animationFrameId = requestAnimationFrame(animate);
    }
    animate();

    return () => {
      cancelAnimationFrame(animationFrameId);
      layer.dispose();
      globeScene.dispose();
    };
  }, []);

  // Initialize satellite meshes once the catalog is loaded
  useEffect(() => {
    if (satellites.length > 0 && layerRef.current) {
      layerRef.current.initSatellites(satellites);
    }
  }, [satellites]);

  // Reposition satellites on every telemetry update
  useEffect(() => {
    if (!layerRef.current) return;
    for (const event of Object.values(latestTelemetry)) {
      layerRef.current.updatePosition(event);
    }
  }, [latestTelemetry]);

  // Draw conjunction lines when alerts fires
  useEffect(() => {
    if (!layerRef.current) return;
    for (const alert of alerts) {
      layerRef.current.showConjunctionLine(alert.primarySatelliteId, alert.secondarySatelliteId);
    }
  }, [alerts.length]);

  return <canvas ref={canvasRef} className="globe-canvas" />;
}
