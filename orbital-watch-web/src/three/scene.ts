import * as THREE from "three";
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls.js";

const SCENE_EARTH_RADIUS = 5;

export interface GlobeScene {
  scene: THREE.Scene;
  camera: THREE.PerspectiveCamera;
  renderer: THREE.WebGLRenderer;
  controls: OrbitControls;
  earth: THREE.Mesh;
  dispose: () => void;
}

export function createGlobeScene(canvas: HTMLCanvasElement): GlobeScene {
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x0a0a0e);

  // Camera: field of view 45 deg, aspect ratio from canvas, near/far clipping planes
  const camera = new THREE.PerspectiveCamera(
    45,
    canvas.clientWidth / canvas.clientHeight,
    0.1,
    1000,
  );
  camera.position.set(0, 0, 15);

  const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
  renderer.setSize(canvas.clientWidth, canvas.clientHeight);
  renderer.setPixelRatio(window.devicePixelRatio);

  // Lighting: ambient (fills shadows so the dark side of Each isn't pure black)
  // + directional (simulates sunlight, casts the day/night terminator)
  const ambient = new THREE.AmbientLight(0xffffff, 0.4);
  scene.add(ambient);

  const sunlight = new THREE.DirectionalLight(0xffffff, 1.2);
  sunlight.position.set(10, 5, 10);
  scene.add(sunlight);

  // Earth sphere
  const textureLoader = new THREE.TextureLoader();
  const earthTexture = textureLoader.load("/textures/earth_daymap.jpg");

  const earthGeometry = new THREE.SphereGeometry(SCENE_EARTH_RADIUS, 64, 64);
  const earthMaterial = new THREE.MeshStandardMaterial({
    map: earthTexture,
    roughness: 0.8,
    metalness: 0.1,
  });
  const earth = new THREE.Mesh(earthGeometry, earthMaterial);
  scene.add(earth);

  // OrbitalControls: mouse drag to rotate, scroll to zoom, right-drag to planes
  const controls = new OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true;
  controls.dampingFactor = 0.05;
  controls.minDistance = 7;
  controls.maxDistance = 40;

  function handleResize() {
    const width = canvas.clientWidth;
    const height = canvas.clientHeight;
    camera.aspect = width / height;
    camera.updateProjectionMatrix();
    renderer.setSize(width, height);
  }
  window.addEventListener("resize", handleResize);

  function dispose() {
    window.removeEventListener("resize", handleResize);
    controls.dispose();
    earthGeometry.dispose();
    earthMaterial.dispose();
    renderer.dispose();
  }

  return { scene, camera, renderer, controls, earth, dispose };
}

export { SCENE_EARTH_RADIUS };
