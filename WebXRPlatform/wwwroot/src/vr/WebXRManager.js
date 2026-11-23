import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.js';

/**
 * WebXR Manager handling session lifecycle, Quest 3 / Vive controllers,
 * laser pointer raycasting, trigger interaction, and seated eye-level rig.
 */
export class WebXRManager {
  constructor(renderer, camera, scene) {
    this.renderer = renderer;
    this.camera = camera;
    this.scene = scene;
    this.isSupported = false;
    this.xrSession = null;
    this.controllers = [];
    this.controllerGrips = [];
    this.raycaster = new THREE.Raycaster();
    this.tempMatrix = new THREE.Matrix4();
    this.activeSelect = false;

    this.onSessionStarted = null;
    this.onSessionEnded = null;
    this.onControllerSelect = null; // (raycaster, isPressed)

    // VR Rig hierarchy: dolly -> camera (Seated eye level at 1.20m, candidate position at z = -1.25)
    this.dolly = new THREE.Group();
    this.dolly.position.set(0, 0, -1.25);
    this.scene.add(this.dolly);
    this.dolly.add(this.camera);

    // Reticle cursor dot on raycast hit
    const reticleGeo = new THREE.RingGeometry(0.008, 0.016, 24);
    const reticleMat = new THREE.MeshBasicMaterial({ color: 0x00e5ff, side: THREE.DoubleSide });
    this.reticle = new THREE.Mesh(reticleGeo, reticleMat);
    this.reticle.visible = false;
    this.scene.add(this.reticle);

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
        this.reticle.visible = false;
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
      const mat = new THREE.LineBasicMaterial({ color: 0x00e5ff, transparent: true, opacity: 0.8 });
      const line = new THREE.Line(geom, mat);
      controller.add(line);

      controller.addEventListener('selectstart', () => {
        this.activeSelect = true;
      });

      controller.addEventListener('selectend', () => {
        this.activeSelect = false;
      });

      this.dolly.add(controller);
      this.controllers.push(controller);
    }
  }

  /**
   * Called per frame in render loop to perform controller raycasting
   * @param {SpatialVirtualScreen3D} virtualScreen
   */
  update(virtualScreen = null) {
    if (!this.renderer.xr.isPresenting || this.controllers.length === 0) {
      return;
    }

    let hitResult = null;
    for (const controller of this.controllers) {
      this.tempMatrix.identity().extractRotation(controller.matrixWorld);
      this.raycaster.ray.origin.setFromMatrixPosition(controller.matrixWorld);
      this.raycaster.ray.direction.set(0, 0, -1).applyMatrix4(this.tempMatrix);

      if (virtualScreen) {
        const hit = virtualScreen.handleRaycast(this.raycaster, this.activeSelect);
        if (hit && hit.hitPoint) {
          hitResult = hit;
          this.reticle.visible = true;
          this.reticle.position.copy(hit.hitPoint);
          this.reticle.position.z += 0.002;
          this.reticle.lookAt(this.camera.position);
          break;
        }
      }
    }

    if (!hitResult) {
      this.reticle.visible = false;
    }
  }

  setSeatedPosition(x, y, z) {
    this.dolly.position.set(x, y, z);
  }
}

