import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.js';

/**
 * 3D Executive Boardroom Environment
 */
export class SpatialEnvironment {
  constructor(scene) {
    this.scene = scene;
    this.tableMesh = null;
    this.buildRoom();
  }

  buildRoom() {
    // 1. Flooring (Warm executive acoustic carpet)
    const floorGeo = new THREE.PlaneGeometry(16, 16);
    const floorMat = new THREE.MeshStandardMaterial({
      color: 0x181e28,
      roughness: 0.8,
      metalness: 0.1
    });
    const floor = new THREE.Mesh(floorGeo, floorMat);
    floor.rotation.x = -Math.PI / 2;
    floor.receiveShadow = true;
    this.scene.add(floor);

    // 2. Ceiling with acoustic architectural panel
    const ceilGeo = new THREE.PlaneGeometry(16, 16);
    const ceilMat = new THREE.MeshStandardMaterial({ color: 0x121620, roughness: 0.9 });
    const ceiling = new THREE.Mesh(ceilGeo, ceilMat);
    ceiling.position.y = 3.6;
    ceiling.rotation.x = Math.PI / 2;
    this.scene.add(ceiling);

    // 3. Walls
    const wallMat = new THREE.MeshStandardMaterial({ color: 0x1f2735, roughness: 0.7 });

    // Back wall (behind interviewer)
    const backWall = new THREE.Mesh(new THREE.PlaneGeometry(16, 3.6), wallMat);
    backWall.position.set(0, 1.8, 5);
    backWall.rotation.y = Math.PI;
    this.scene.add(backWall);

    // Front wall (behind candidate)
    const frontWall = new THREE.Mesh(new THREE.PlaneGeometry(16, 3.6), wallMat);
    frontWall.position.set(0, 1.8, -5);
    this.scene.add(frontWall);

    // Side walls
    const leftWall = new THREE.Mesh(new THREE.PlaneGeometry(10, 3.6), wallMat);
    leftWall.position.set(-6, 1.8, 0);
    leftWall.rotation.y = Math.PI / 2;
    this.scene.add(leftWall);

    const rightWall = new THREE.Mesh(new THREE.PlaneGeometry(10, 3.6), wallMat);
    rightWall.position.set(6, 1.8, 0);
    rightWall.rotation.y = -Math.PI / 2;
    this.scene.add(rightWall);

    // 4. Executive Conference Table (Frosted Glass & Walnut)
    const tableTopGeo = new THREE.BoxGeometry(1.6, 0.06, 2.4);
    const tableTopMat = new THREE.MeshStandardMaterial({
      color: 0x1a2332,
      roughness: 0.2,
      metalness: 0.8,
      transparent: true,
      opacity: 0.92
    });
    this.tableMesh = new THREE.Mesh(tableTopGeo, tableTopMat);
    this.tableMesh.position.set(0, 0.73, 0);
    this.tableMesh.castShadow = true;
    this.tableMesh.receiveShadow = true;
    this.scene.add(this.tableMesh);

    // Table pedestal legs
    const legMat = new THREE.MeshStandardMaterial({ color: 0x0f141d, metalness: 0.9, roughness: 0.3 });
    const leg1 = new THREE.Mesh(new THREE.CylinderGeometry(0.08, 0.08, 0.73, 16), legMat);
    leg1.position.set(0, 0.365, -0.8);
    this.scene.add(leg1);

    const leg2 = new THREE.Mesh(new THREE.CylinderGeometry(0.08, 0.08, 0.73, 16), legMat);
    leg2.position.set(0, 0.365, 0.8);
    this.scene.add(leg2);

    // 5. Lighting
    const ambientLight = new THREE.AmbientLight(0xdde6f0, 0.6);
    this.scene.add(ambientLight);

    // Overhead soft spotlight over conference table
    const spotLight = new THREE.SpotLight(0xfff5e6, 2.5);
    spotLight.position.set(0, 3.4, 0);
    spotLight.target = this.tableMesh;
    spotLight.angle = Math.PI / 3;
    spotLight.penumbra = 0.5;
    spotLight.castShadow = true;
    this.scene.add(spotLight);

    // Subtle cyan accent strip
    const accentLight = new THREE.PointLight(0x00e5ff, 1.2, 8);
    accentLight.position.set(0, 2.8, 4.2);
    this.scene.add(accentLight);
  }
}
