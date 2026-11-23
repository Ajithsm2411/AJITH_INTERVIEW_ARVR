/**
 * LipSyncAnimator maps real-time audio amplitude and phonemes to 3D avatar mouth morphs
 */
export class LipSyncAnimator {
  constructor(mouthMesh) {
    this.mouthMesh = mouthMesh;
    this.targetOpen = 0.0;
    this.currentOpen = 0.0;
  }

  setLoudness(loudness) {
    this.targetOpen = Math.min(1.0, loudness * 2.8);
  }

  setViseme(viseme) {
    if (viseme && viseme.mouthOpen !== undefined) {
      this.targetOpen = viseme.mouthOpen;
    }
  }

  update(dt = 0.016) {
    // Smooth interpolation for natural lip flutter
    this.currentOpen += (this.targetOpen - this.currentOpen) * (dt * 18.0);

    if (this.mouthMesh) {
      // Scale vertically to open/close mouth
      const scaleY = 0.05 + this.currentOpen * 0.12;
      this.mouthMesh.scale.set(1.0, scaleY, 1.0);
    }
  }
}
