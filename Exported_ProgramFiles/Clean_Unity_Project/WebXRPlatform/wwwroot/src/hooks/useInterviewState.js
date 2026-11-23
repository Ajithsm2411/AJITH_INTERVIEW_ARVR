import { DialoguePhase, AvatarState } from '../../../shared/types/index.js';
import { apiClient } from '../services/apiClient.js';
import { audioPlayback } from '../audio/AudioPlaybackManager.js';

export class InterviewStateMachine {
  constructor(avatarManager, speechController) {
    this.avatars = avatarManager;
    this.speech = speechController;

    this.phase = DialoguePhase.IDLE;
    this.currentSession = null;
    this.currentQuestion = null;
    this.lastAnswer = null;
    this.lastEvaluation = null;
    this.finalReport = null;

    this.isPaused = false;
    this.elapsedSeconds = 0;
    this.durationSeconds = 900; // 15 mins default
    this.listeners = new Map();

    this._setupSpeechBridge();
  }

  _setupSpeechBridge() {
    this.speech.mic.onLoudnessUpdate = (loudness, isSpeaking) => {
      if (this.phase === DialoguePhase.CANDIDATE_RECORDING) {
        this.avatars.setCandidateLoudness(loudness);
        this.emit('loudness', { loudness, isSpeaking });
      }
    };

    this.speech.mic.onSilenceDetected = () => {
      if (this.phase === DialoguePhase.CANDIDATE_RECORDING) {
        console.log('[StateMachine] Silence detected (>1.8s). Auto-submitting spoken answer.');
        this.submitSpokenAnswer();
      }
    };

    this.speech.onTranscriptUpdate = (trans) => {
      this.emit('transcript', trans);
    };
  }

  setPhase(newPhase) {
    this.phase = newPhase;
    this.emit('phaseChange', newPhase);
  }

  async startInterview(sessionConfig) {
    // Step 1 -> Step 2
    this.setPhase(DialoguePhase.INTERVIEWER_SPEAKING);
    this.avatars.setInterviewerState(AvatarState.SPEAKING);
    this.avatars.setCandidateState(AvatarState.LISTENING);

    const sessionRes = await apiClient.createSession(sessionConfig);
    this.currentSession = sessionRes.session;
    this.durationSeconds = (sessionRes.session.duration_minutes || 15) * 60;

    const startRes = await apiClient.startSession(this.currentSession.id);
    this.currentQuestion = startRes.question;

    audioPlayback.playUIChime();

    // Dr. Vance speaks Question 1
    this.speech.mic.setInterviewerSpeaking(true);
    audioPlayback.speak(this.currentQuestion.question_text, (viseme) => {
      this.avatars.setInterviewerViseme(viseme);
    });

    audioPlayback.onSpeechEnd = () => {
      this.speech.mic.setInterviewerSpeaking(false);
      this.beginCandidateRecording();
    };
  }

  beginCandidateRecording() {
    // Step 3: Candidate Spoken Answer
    this.setPhase(DialoguePhase.CANDIDATE_RECORDING);
    this.avatars.setInterviewerState(AvatarState.LISTENING);
    this.avatars.setCandidateState(AvatarState.SPEAKING);
    this.speech.startListening();
  }

  async submitSpokenAnswer(textFallback = '') {
    if (this.phase !== DialoguePhase.CANDIDATE_RECORDING) return;

    // Step 4: Answer Delivery & Evaluation
    this.setPhase(DialoguePhase.CANDIDATE_DELIVERING);
    const { transcript, audioBlob } = await this.speech.stopListening();
    const finalTranscript = textFallback || transcript || 'I focused on building high-reliability distributed services with automated testing.';

    this.avatars.setCandidateState(AvatarState.SPEAKING);
    this.avatars.setInterviewerState(AvatarState.THINKING);

    const answerRes = await apiClient.submitAnswer({
      questionId: this.currentQuestion.id,
      transcriptText: finalTranscript,
      speechDurationSeconds: 4.5,
      isTypedFallback: Boolean(textFallback)
    });

    this.lastAnswer = answerRes.answer;
    this.lastEvaluation = answerRes.evaluation;

    // Interviewer acknowledges response
    this.setPhase(DialoguePhase.INTERVIEWER_ACKNOWLEDGING);
    this.avatars.setInterviewerState(AvatarState.SPEAKING);
    this.avatars.setCandidateState(AvatarState.LISTENING);

    this.speech.mic.setInterviewerSpeaking(true);
    audioPlayback.speak(answerRes.interviewerReaction.text, (viseme) => {
      this.avatars.setInterviewerViseme(viseme);
    });

    audioPlayback.onSpeechEnd = () => {
      this.speech.mic.setInterviewerSpeaking(false);
      this.advanceToNextQuestionOrFinish();
    };
  }

  async advanceToNextQuestionOrFinish() {
    // Step 5: Check session time
    if (this.elapsedSeconds >= this.durationSeconds) {
      await this.finishInterview();
      return;
    }

    this.setPhase(DialoguePhase.INTERVIEWER_SPEAKING);
    this.avatars.setInterviewerState(AvatarState.SPEAKING);
    this.avatars.setCandidateState(AvatarState.LISTENING);

    const nextRes = await apiClient.getNextQuestion(this.currentSession.id);
    this.currentQuestion = nextRes.question;

    audioPlayback.playUIChime();

    this.speech.mic.setInterviewerSpeaking(true);
    audioPlayback.speak(this.currentQuestion.question_text, (viseme) => {
      this.avatars.setInterviewerViseme(viseme);
    });

    audioPlayback.onSpeechEnd = () => {
      this.speech.mic.setInterviewerSpeaking(false);
      this.beginCandidateRecording();
    };
  }

  async finishInterview() {
    // Step 6: Conclude & generate report
    this.setPhase(DialoguePhase.SESSION_CONCLUDED);
    this.avatars.setInterviewerState(AvatarState.INTERVIEW_COMPLETED);
    this.avatars.setCandidateState(AvatarState.INTERVIEW_COMPLETED);

    const finishRes = await apiClient.finishSession(this.currentSession.id);
    this.finalReport = finishRes.reportDetails || finishRes.report;
    this.emit('reportReady', this.finalReport);
  }

  togglePause() {
    this.isPaused = !this.isPaused;
    if (this.isPaused) {
      audioPlayback.stop();
    }
    this.emit('pauseChange', this.isPaused);
  }

  repeatCurrentQuestion() {
    if (this.currentQuestion) {
      this.setPhase(DialoguePhase.INTERVIEWER_SPEAKING);
      this.avatars.setInterviewerState(AvatarState.SPEAKING);
      audioPlayback.speak(this.currentQuestion.question_text, (viseme) => {
        this.avatars.setInterviewerViseme(viseme);
      });
      audioPlayback.onSpeechEnd = () => {
        this.beginCandidateRecording();
      };
    }
  }

  on(event, cb) {
    if (!this.listeners.has(event)) this.listeners.set(event, []);
    this.listeners.get(event).push(cb);
  }

  emit(event, data) {
    const list = this.listeners.get(event) || [];
    for (const cb of list) cb(data);
  }
}
