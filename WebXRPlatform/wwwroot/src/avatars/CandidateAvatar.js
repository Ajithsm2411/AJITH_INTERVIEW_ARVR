import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.js';
import { LipSyncAnimator } from './LipSyncAnimator.js';
import { AvatarState } from '../../../shared/types/index.js';

/**
 * 3D Avatar for Candidate: Alex Mercer
 */
export class CandidateAvatar {
  constructor(scene) {
    this.scene = scene;
    this.root = new THREE.Group();
    this.state = AvatarState.IDLE;
    this.lipSync = null;

    this.head = null;
    this.torso = null;
    this.leftArm = null;
    this.rightArm = null;
    this.mouth = null;
    this.isFirstPersonVR = false;

    this.time = 0;
    this.buildAvatar();
  }

  buildAvatar() {
    this.root.position.set(0, 0, -1.25); // Seated in candidate chair
    this.root.rotation.y = 0; // Facing Dr. Vance (+Z)

    // Chair
    const chairMat = new THREE.MeshStandardMaterial({ color: 0x141b24, roughness: 0.6 });
    const seat = new THREE.Mesh(new THREE.BoxGeometry(0.65, 0.1, 0.65), chairMat);
    seat.position.set(0, 0.45, 0);
    this.root.add(seat);

    const backrest = new THREE.Mesh(new THREE.BoxGeometry(0.65, 0.9, 0.1), chairMat);
    backrest.position.set(0, 0.95, -0.28);
    this.root.add(backrest);

    // Torso (Candidate casual oxford shirt)
    const shirtMat = new THREE.MeshStandardMaterial({ color: 0x2e4255, roughness: 0.6 });
    this.torso = new THREE.Mesh(new THREE.BoxGeometry(0.48, 0.65, 0.28), shirtMat);
    this.torso.position.set(0, 0.85, 0);
    this.root.add(this.torso);

    // Head (1.20m eye level)
    const skinMat = new THREE.MeshStandardMaterial({ color: 0xdbab8e, roughness: 0.5 });
    this.head = new THREE.Group();
    this.head.position.set(0, 1.25, 0);

    const headMesh = new THREE.Mesh(new THREE.SphereGeometry(0.14, 24, 24), skinMat);
    this.head.add(headMesh);

    // Hair
    const hairMat = new THREE.MeshStandardMaterial({ color: 0x221a14, roughness: 0.8 });
    const hair = new THREE.Mesh(new THREE.SphereGeometry(0.15, 24, 24), hairMat);
    hair.position.set(0, 0.04, -0.02);
    this.head.add(hair);

    // Mouth
    const mouthMat = new THREE.MeshBasicMaterial({ color: 0x772222 });
    this.mouth = new THREE.Mesh(new THREE.BoxGeometry(0.06, 0.02, 0.02), mouthMat);
    this.mouth.position.set(0, -0.05, 0.135);
    this.head.add(this.mouth);

    this.root.add(this.head);
    this.lipSync = new LipSyncAnimator(this.mouth);

    // Arms
    this.leftArm = new THREE.Mesh(new THREE.CylinderGeometry(0.06, 0.05, 0.45), shirtMat);
    this.leftArm.position.set(-0.28, 0.75, 0.1);
    this.leftArm.rotation.x = Math.PI / 4;
    this.root.add(this.leftArm);

    this.rightArm = new THREE.Mesh(new THREE.CylinderGeometry(0.06, 0.05, 0.45), shirtMat);
    this.rightArm.position.set(0.28, 0.75, 0.1);
    this.rightArm.rotation.x = Math.PI / 4;
    this.root.add(this.rightArm);

    this.scene.add(this.root);
  }

  setFirstPersonVR(active) {
    this.isFirstPersonVR = active;
    // Head and neck culling prevents clipping inside head while showing body/arms
    if (this.head) {
      this.head.visible = !active;
    }
  }

  setState(newState) {
    this.state = newState;
  }

  update(dt = 0.016) {
    this.time += dt;
    this.lipSync.update(dt);

    const breath = Math.sin(this.time * 2.2) * 0.004;
    this.torso.position.y = 0.85 + breath;

    if (this.state === AvatarState.SPEAKING) {
      // Conversational gesturing across table
      this.rightArm.rotation.x = Math.PI / 4 + Math.sin(this.time * 5.0) * 0.15;
      this.leftArm.rotation.x = Math.PI / 4 + Math.cos(this.time * 4.0) * 0.10;
    } else {
      this.rightArm.rotation.x = Math.PI / 4;
      this.leftArm.rotation.x = Math.PI / 4;
    }
  }
}
