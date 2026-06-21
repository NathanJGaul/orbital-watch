import * as THREE from "three";
import { SCENE_EARTH_RADIUS } from "./scene";

const REAL_EARTH_RADIUS_KM = 6371;

/**
 * Converts geodetic coordinates (latitude, longitude, altitude) to a Three.js
 * Vector3 in scene units.
 *
 * Latitude/longitude are in degrees. Altitude is in km above Earth's surface.
 *
 * The transform:
 *  1. Convert degrees to radians
 *  2. Compute the radius from Earth's center: (real Earth radius + altitude),
 *     then scale to scene units
 *  3. Apply the standard spherical-to-Cartesian formula
 */
export function latLonAltToVector3(latDeg: number, lonDeg: number, altKm: number): THREE.Vector3 {
  const lat = (latDeg * Math.PI) / 180;
  const lon = (lonDeg * Math.PI) / 180;

  // Scale factor: scene units per real km
  const scale = SCENE_EARTH_RADIUS / REAL_EARTH_RADIUS_KM;
  const r = REAL_EARTH_RADIUS_KM * altKm * scale;

  // Standard spherical-to-Cartesian conversion.
  // Three.js uses a Y-up coordinate system, so latitude maps to Y (not Z).
  const x = r * Math.cos(lat) * Math.cos(lon);
  const y = r * Math.sin(lat);
  const z = r * Math.cos(lat) * Math.sin(lon);

  return new THREE.Vector3(x, y, z);
}
