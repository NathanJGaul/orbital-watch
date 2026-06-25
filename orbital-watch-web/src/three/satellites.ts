import * as THREE from "three";
import { latLonAltToVector3 } from "./coordinates";
import type { TelemetryEvent, Satellite } from "../types/telemetry";

const REGIME_COLORS_3D: Record<string, number> = {
  LEO: 0x3b82f6,
  MEO: 0x8b5cf6,
  GEO: 0x10b981,
  HEO: 0xf59e0b,
};

const SATELLITE_SIZE = 0.08;
const TRAIL_LENGTH = 60; // number of historical positions kept per satellite

interface SatelliteMesh {
  mesh: THREE.Mesh;
  trail: THREE.Line;
  trailPositions: Float32Array;
  trailWriteIndex: number;
  trailFilled: number;
  baseScale: number; // used for the conjunction pulse animation
}

export class SatelliteLayer {
  private group: THREE.Group;
  private satellites: Map<number, SatelliteMesh> = new Map();
  private conjunctionLines: THREE.Line[] = [];
  private conjunctionTimeouts: Set<ReturnType<typeof setTimeout>> = new Set();

  constructor(scene: THREE.Scene) {
    this.group = new THREE.Group();
    scene.add(this.group);
  }

  // Call once, then the satellite catalog loads, to create meshes for each satellite.
  initSatellites(satellites: Satellite[]) {
    for (const sat of satellites) {
      if (this.satellites.has(sat.id)) continue;

      const color = REGIME_COLORS_3D[sat.orbitalRegime] ?? 0xffffff;

      // Satellite marker: small sphere
      const geometry = new THREE.SphereGeometry(SATELLITE_SIZE, 16, 16);
      const material = new THREE.MeshBasicMaterial({ color });
      const mesh = new THREE.Mesh(geometry, material);
      this.group.add(mesh);

      // Trail: a Line with a pre-allocated buffer.
      // Pre-allocating avoids creating a new BufferGeometry objects every frame.
      const trailPositions = new Float32Array(TRAIL_LENGTH * 3); // x,y,z per point
      const trailGeometry = new THREE.BufferGeometry();
      trailGeometry.setAttribute("position", new THREE.BufferAttribute(trailPositions, 3));
      const trailMaterial = new THREE.LineBasicMaterial({
        color,
        transparent: true,
        opacity: 0.4,
      });
      const trail = new THREE.Line(trailGeometry, trailMaterial);
      this.group.add(trail);

      this.satellites.set(sat.id, {
        mesh,
        trail,
        trailPositions,
        trailWriteIndex: 0,
        trailFilled: 0,
        baseScale: 1,
      });
    }
  }

  // Call on every telemetry update to reposition a satellite and extend its trail
  updatePosition(event: TelemetryEvent) {
    const sat = this.satellites.get(event.satelliteId);
    if (!sat) return;

    const pos = latLonAltToVector3(event.latitudeDeg, event.longitudeDeg, event.altitudeKm);
    sat.mesh.position.copy(pos);

    // Ring buffer: overwrite the oldest position with the new one
    const idx = sat.trailWriteIndex;
    sat.trailPositions[idx * 3] = pos.x;
    sat.trailPositions[idx * 3 + 1] = pos.y;
    sat.trailPositions[idx * 3 + 2] = pos.z;

    sat.trailWriteIndex = (idx + 1) % TRAIL_LENGTH;
    sat.trailFilled = Math.min(sat.trailFilled + 1, TRAIL_LENGTH);

    // Tell Three.js the buffer changed - without this, the GPU keep the old data
    const posAttr = sat.trail.geometry.getAttribute("position") as THREE.BufferAttribute;
    posAttr.needsUpdate = true;

    // Only draw the points we've actually filled (avoid drawing zeros at startup)
    sat.trail.geometry.setDrawRange(0, sat.trailFilled);
  }

  // Briefly pulse a satellite's mesh - called when a conjunction alert fires
  pulse(satelliteId: number) {
    const sat = this.satellites.get(satelliteId);
    if (!sat) return;
    sat.baseScale = 2.5; // animate() will lerp this back down to 1
  }

  // Call every frame to animate any active pulses
  animate() {
    for (const sat of this.satellites.values()) {
      if (sat.baseScale > 1) {
        sat.baseScale = THREE.MathUtils.lerp(sat.baseScale, 1, 0.08);
        sat.mesh.scale.setScalar(sat.baseScale);
      }
    }
  }

  // Draw a temporary line between two satellites, removed after 'durationMs'
  showConjunctionLine(satelliteIdA: number, satelliteIdB: number, durationMs = 4000) {
    const a = this.satellites.get(satelliteIdA);
    const b = this.satellites.get(satelliteIdB);
    if (!a || !b) return;

    const positions = new Float32Array([
      a.mesh.position.x,
      a.mesh.position.y,
      a.mesh.position.z,
      b.mesh.position.x,
      b.mesh.position.y,
      b.mesh.position.z,
    ]);

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute("position", new THREE.BufferAttribute(positions, 3));

    const material = new THREE.LineBasicMaterial({
      color: 0xff3333,
      linewidth: 2,
    });

    const line = new THREE.Line(geometry, material);
    this.group.add(line);
    this.conjunctionLines.push(line);

    this.pulse(satelliteIdA);
    this.pulse(satelliteIdB);

    const timeoutId = setTimeout(() => {
      this.conjunctionTimeouts.delete(timeoutId);
      this.group.remove(line);
      geometry.dispose();
      material.dispose();
      this.conjunctionLines = this.conjunctionLines.filter((l) => l !== line);
    }, durationMs);
    this.conjunctionTimeouts.add(timeoutId);
  }

  dispose() {
    for (const timeoutId of this.conjunctionTimeouts) {
      clearTimeout(timeoutId);
    }
    this.conjunctionTimeouts.clear();

    for (const line of this.conjunctionLines) {
      this.group.remove(line);
      line.geometry.dispose();
      (line.material as THREE.Material).dispose();
    }
    this.conjunctionLines = [];

    for (const sat of this.satellites.values()) {
      sat.mesh.geometry.dispose();
      (sat.mesh.material as THREE.Material).dispose();
      sat.trail.geometry.dispose();
      (sat.trail.material as THREE.Material).dispose();
    }
    this.satellites.clear();
  }
}
