import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.js';

/**
 * VR Seated Comfort & Teleportation Controller
 */
export class VRTeleportation {
  constructor(dolly, camera) {
    this.dolly = dolly;
    this.camera = camera;
    this.candidateSeat = new THREE.Vector3(0, 0, -1.25);
    this.interviewerSeat = new THREE.Vector3(0, 0, 1.25);
  }

  resetToCandidateSeat() {
    this.dolly.position.copy(this.candidateSeat);
    this.dolly.rotation.set(0, 0, 0); // Facing interviewer (+Z)
  }

  resetToInterviewerSeat() {
    this.dolly.position.copy(this.interviewerSeat);
    this.dolly.rotation.set(0, Math.PI, 0); // Facing candidate (-Z)
  }
}
