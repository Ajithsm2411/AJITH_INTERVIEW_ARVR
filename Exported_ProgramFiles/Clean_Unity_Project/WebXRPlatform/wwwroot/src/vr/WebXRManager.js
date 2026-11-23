import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.js';

/**
 * WebXR Manager handling session lifecycle, controller polling, and non-VR fallback (Spec 14 & 16)
 */
export class WebXRManager {
  constructor(renderer, camera, scene) {
    this.renderer = renderer;
    this.camera = camera;
    this.scene = scene;
    this.isSupported = false;
    this.xrSession = null;
    this.controllers = [];
    this.onSessionStarted = null;
    this.onSessionEnded = null;

    // VR Rig hierarchy: rig -> cameraOffset -> camera (Seated eye level at 1.20m)
    this.dolly = new THREE.Group();
    this.dolly.position.set(0, 0, -1.25); // Candidate chair position
    this.scene.add(this.dolly);
    this.dolly.add(this.camera);

    this.checkSupport();
  }

  async checkSupport() {
    if ('xr' in navigator) {
      try {
        this.isSupported = await navigator.xr.isSessionSupported('immersive-vr');
      } catch (err) {
        this.isSupported = false;
      }
    } else {
      this.isSupported = false;
    }
    console.log(`[WebXRManager] WebXR immersive-vr supported: ${this.isSupported}`);
    return this.isSupported;
  }

  async enterVR() {
    if (!this.isSupported) {
      console.warn('[WebXRManager] WebXR immersive-vr is not supported on this device/browser. Using Desktop 3D Preview.');
      return false;
    }

    try {
      const sessionInit = { optionalFeatures: ['local-floor', 'bounded-floor', 'hand-tracking'] };
      const session = await navigator.xr.requestSession('immersive-vr', sessionInit);
      this.renderer.xr.setSession(session);
      this.xrSession = session;

      this._setupControllers();

      session.addEventListener('end', () => {
        this.xrSession = null;
        if (this.onSessionEnded) this.onSessionEnded();
      });

      if (this.onSessionStarted) this.onSessionStarted();
      return true;
    } catch (err) {
      console.error('[WebXRManager] Failed to start WebXR session:', err);
      return false;
    }
  }

  _setupControllers() {
    this.controllers = [];
    for (let i = 0; i < 2; i++) {
      const controller = this.renderer.xr.getController(i);

      // Add laser pointer line
      const geom = new THREE.BufferGeometry().setFromPoints([
        new THREE.Vector3(0, 0, 0),
        new THREE.Vector3(0, 0, -3)
      ]);
      const mat = new THREE.LineBasicMaterial({ color: 0x00e5ff });
      const line = new THREE.Line(geom, mat);
      controller.add(line);

      this.dolly.add(controller);
      this.controllers.push(controller);
    }
  }

  setSeatedPosition(x, y, z) {
    this.dolly.position.set(x, y, z);
  }
}
