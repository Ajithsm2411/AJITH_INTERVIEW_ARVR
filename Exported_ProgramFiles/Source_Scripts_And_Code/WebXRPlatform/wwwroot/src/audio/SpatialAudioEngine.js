/**
 * Spatial Audio Engine utilizing Web Audio PannerNode for 3D positional immersion
 */
export class SpatialAudioEngine {
  constructor() {
    this.audioContext = null;
    this.interviewerPanner = null;
    this.masterGain = null;
    this.isMuted = false;
    this.volume = 1.0;
  }

  init() {
    if (this.audioContext) return;
    this.audioContext = new (window.AudioContext || window.webkitAudioContext)();

    this.masterGain = this.audioContext.createGain();
    this.masterGain.gain.setValueAtTime(this.volume, this.audioContext.currentTime);
    this.masterGain.connect(this.audioContext.destination);

    // 3D PannerNode for Dr. Sarah Vance seated across the table (0, 1.20m, 1.25m)
    this.interviewerPanner = this.audioContext.createPanner();
    this.interviewerPanner.panningModel = 'HRTF';
    this.interviewerPanner.distanceModel = 'inverse';
    this.interviewerPanner.refDistance = 1.0;
    this.interviewerPanner.maxDistance = 10.0;
    this.interviewerPanner.rolloffFactor = 1.0;
    this.interviewerPanner.coneInnerAngle = 90;
    this.interviewerPanner.coneOuterAngle = 180;
    this.interviewerPanner.coneOuterGain = 0.2;

    this.interviewerPanner.positionX.setValueAtTime(0.0, this.audioContext.currentTime);
    this.interviewerPanner.positionY.setValueAtTime(1.20, this.audioContext.currentTime);
    this.interviewerPanner.positionZ.setValueAtTime(1.25, this.audioContext.currentTime);

    this.interviewerPanner.connect(this.masterGain);
  }

  setVolume(vol) {
    this.volume = Math.max(0.0, Math.min(1.0, vol));
    if (this.masterGain && !this.isMuted) {
      this.masterGain.gain.setValueAtTime(this.volume, this.audioContext.currentTime);
    }
  }

  toggleMute() {
    this.isMuted = !this.isMuted;
    if (this.masterGain) {
      this.masterGain.gain.setValueAtTime(this.isMuted ? 0.0 : this.volume, this.audioContext.currentTime);
    }
    return this.isMuted;
  }

  updateListenerPose(x, y, z, forwardX, forwardY, forwardZ, upX = 0, upY = 1, upZ = 0) {
    if (!this.audioContext) return;
    const listener = this.audioContext.listener;
    if (listener.positionX) {
      listener.positionX.setValueAtTime(x, this.audioContext.currentTime);
      listener.positionY.setValueAtTime(y, this.audioContext.currentTime);
      listener.positionZ.setValueAtTime(z, this.audioContext.currentTime);
      listener.forwardX.setValueAtTime(forwardX, this.audioContext.currentTime);
      listener.forwardY.setValueAtTime(forwardY, this.audioContext.currentTime);
      listener.forwardZ.setValueAtTime(forwardZ, this.audioContext.currentTime);
      listener.upX.setValueAtTime(upX, this.audioContext.currentTime);
      listener.upY.setValueAtTime(upY, this.audioContext.currentTime);
      listener.upZ.setValueAtTime(upZ, this.audioContext.currentTime);
    }
  }

  playSpatialTone(frequency = 440, duration = 0.2) {
    this.init();
    if (this.audioContext.state === 'suspended') this.audioContext.resume();

    const osc = this.audioContext.createOscillator();
    const gain = this.audioContext.createGain();

    osc.frequency.setValueAtTime(frequency, this.audioContext.currentTime);
    gain.gain.setValueAtTime(0.3, this.audioContext.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.001, this.audioContext.currentTime + duration);

    osc.connect(gain);
    gain.connect(this.interviewerPanner);

    osc.start();
    osc.stop(this.audioContext.currentTime + duration);
  }
}

export const spatialAudio = new SpatialAudioEngine();
export default spatialAudio;
