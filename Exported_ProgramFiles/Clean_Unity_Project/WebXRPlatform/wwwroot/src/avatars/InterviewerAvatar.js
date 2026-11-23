import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.js';
import { LipSyncAnimator } from './LipSyncAnimator.js';
import { AvatarState } from '../../../shared/types/index.js';

/**
 * 3D Avatar for Interviewer: Dr. Sarah Vance
 */
export class InterviewerAvatar {
  constructor(scene) {
    this.scene = scene;
    this.root = new THREE.Group();
    this.state = AvatarState.IDLE;
    this.lipSync = null;

    // Body part references for procedural animation
    this.head = null;
    this.torso = null;
    this.leftArm = null;
    this.rightArm = null;
    this.mouth = null;

    this.time = 0;
    this.buildAvatar();
  }

  buildAvatar() {
    this.root.position.set(0, 0, 1.25); // Seated across the table
    this.root.rotation.y = Math.PI; // Facing candidate at (0, 0, -1.25)

    // Chair (Executive mesh)
    const chairMat = new THREE.MeshStandardMaterial({ color: 0x111622, roughness: 0.6 });
    const seat = new THREE.Mesh(new THREE.BoxGeometry(0.65, 0.1, 0.65), chairMat);
    seat.position.set(0, 0.45, 0);
    this.root.add(seat);

    const backrest = new THREE.Mesh(new THREE.BoxGeometry(0.65, 0.9, 0.1), chairMat);
    backrest.position.set(0, 0.95, 0.28);
    this.root.add(backrest);

    // Torso (Executive navy blazer)
    const blazerMat = new THREE.MeshStandardMaterial({ color: 0x1b2838, roughness: 0.7 });
    this.torso = new THREE.Mesh(new THREE.BoxGeometry(0.48, 0.65, 0.28), blazerMat);
    this.torso.position.set(0, 0.85, 0);
    this.root.add(this.torso);

    // Head & Neck (Eye level at 1.20m)
    const skinMat = new THREE.MeshStandardMaterial({ color: 0xe0b59b, roughness: 0.5 });
    this.head = new THREE.Group();
    this.head.position.set(0, 1.25, 0);

    const headMesh = new THREE.Mesh(new THREE.SphereGeometry(0.14, 24, 24), skinMat);
    this.head.add(headMesh);

    // Hair
    const hairMat = new THREE.MeshStandardMaterial({ color: 0x3d2314, roughness: 0.8 });
    const hair = new THREE.Mesh(new THREE.SphereGeometry(0.15, 24, 24), hairMat);
    hair.position.set(0, 0.04, -0.02);
    this.head.add(hair);

    // Eyes
    const eyeMat = new THREE.MeshBasicMaterial({ color: 0x224455 });
    const leftEye = new THREE.Mesh(new THREE.SphereGeometry(0.02, 12, 12), eyeMat);
    leftEye.position.set(-0.045, 0.02, 0.13);
    this.head.add(leftEye);

    const rightEye = new THREE.Mesh(new THREE.SphereGeometry(0.02, 12, 12), eyeMat);
    rightEye.position.set(0.045, 0.02, 0.13);
    this.head.add(rightEye);

    // Mouth
    const mouthMat = new THREE.MeshBasicMaterial({ color: 0x883333 });
    this.mouth = new THREE.Mesh(new THREE.BoxGeometry(0.06, 0.02, 0.02), mouthMat);
    this.mouth.position.set(0, -0.05, 0.135);
    this.head.add(this.mouth);

    this.root.add(this.head);
    this.lipSync = new LipSyncAnimator(this.mouth);

    // Arms
    this.leftArm = new THREE.Mesh(new THREE.CylinderGeometry(0.06, 0.05, 0.45), blazerMat);
    this.leftArm.position.set(-0.28, 0.75, 0.1);
    this.leftArm.rotation.x = Math.PI / 4;
    this.root.add(this.leftArm);

    this.rightArm = new THREE.Mesh(new THREE.CylinderGeometry(0.06, 0.05, 0.45), blazerMat);
    this.rightArm.position.set(0.28, 0.75, 0.1);
    this.rightArm.rotation.x = Math.PI / 4;
    this.root.add(this.rightArm);

    this.scene.add(this.root);
  }

  setState(newState) {
    this.state = newState;
  }

  update(dt = 0.016) {
    this.time += dt;
    this.lipSync.update(dt);

    // Subtle natural breathing
    const breath = Math.sin(this.time * 2.0) * 0.005;
    this.torso.position.y = 0.85 + breath;

    // State-based behavioral animations
    switch (this.state) {
      case AvatarState.SPEAKING:
        // Gesturing & expressive head movement
        this.head.rotation.y = Math.sin(this.time * 3.5) * 0.08;
        this.head.rotation.x = Math.cos(this.time * 2.5) * 0.05;
        this.rightArm.rotation.x = Math.PI / 4 + Math.sin(this.time * 4.0) * 0.12;
        break;

      case AvatarState.LISTENING:
        // Attentive conversational nods
        this.head.rotation.x = 0.05 + Math.sin(this.time * 2.2) * 0.08;
        this.head.rotation.y = 0.0;
        this.rightArm.rotation.x = Math.PI / 4;
        break;

      case AvatarState.THINKING:
        // Thoughtful head tilt & chin pose
        this.head.rotation.z = 0.08;
        this.head.rotation.x = -0.06;
        this.rightArm.rotation.x = Math.PI / 2.5; // Hand near chin
        break;

      default:
        // Natural idle
        this.head.rotation.set(0, Math.sin(this.time * 0.8) * 0.03, 0);
        this.rightArm.rotation.x = Math.PI / 4;
        break;
    }
  }
}
