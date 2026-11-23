import { InterviewerAvatar } from './InterviewerAvatar.js';
import { CandidateAvatar } from './CandidateAvatar.js';
import { AvatarState } from '../../../shared/types/index.js';

export class AvatarManager {
  constructor(scene) {
    this.scene = scene;
    this.interviewer = new InterviewerAvatar(scene);
    this.candidate = new CandidateAvatar(scene);
  }

  setInterviewerState(state) {
    this.interviewer.setState(state);
  }

  setCandidateState(state) {
    this.candidate.setState(state);
  }

  setCandidateFirstPersonVR(active) {
    this.candidate.setFirstPersonVR(active);
  }

  setInterviewerViseme(viseme) {
    this.interviewer.lipSync.setViseme(viseme);
  }

  setCandidateLoudness(loudness) {
    this.candidate.lipSync.setLoudness(loudness);
  }

  update(dt) {
    this.interviewer.update(dt);
    this.candidate.update(dt);
  }
}
