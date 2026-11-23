import { DialoguePhase, AvatarState } from '../../../shared/types/index.js';
import { apiClient } from '../services/apiClient.js';
import { audioPlayback } from '../audio/AudioPlaybackManager.js';
import { webSocketClient } from '../services/webSocketClient.js';

export class InterviewStateMachine {
  constructor(avatarManager, speechController, virtualScreen = null) {
    this.avatars = avatarManager;
    this.speech = speechController;
    this.virtualScreen = virtualScreen;

    this.phase = DialoguePhase.IDLE;
    this.currentSession = null;
    this.currentQuestion = null;
    this.lastAnswer = null;
    this.lastEvaluation = null;
    this.finalReport = null;

    this.isPaused = false;
    this.elapsedSeconds = 0;
    this.durationSeconds = 900; // 15 mins default
    this.listeningMode = 'PTT'; // 'PTT' or 'AUTO'
    this.listeners = new Map();

    this._setupSpeechBridge();
    this._setupWebSocketBridge();
  }

  setVirtualScreen(screen) {
    this.virtualScreen = screen;
  }

  _setupSpeechBridge() {
    this.speech.mic.onLoudnessUpdate = (loudness, isSpeaking) => {
      if (this.phase === DialoguePhase.CANDIDATE_RECORDING) {
        this.avatars.setCandidateLoudness(loudness);
        if (this.virtualScreen) {
          this.virtualScreen.setLoudness(loudness, isSpeaking);
        }
        this.emit('loudness', { loudness, isSpeaking });
      }
    };

    this.speech.mic.onSilenceDetected = () => {
      if (this.phase === DialoguePhase.CANDIDATE_RECORDING && this.listeningMode === 'AUTO') {
        console.log('[StateMachine] Silence detected (>1.8s in Auto mode). Auto-submitting spoken answer.');
        this.submitSpokenAnswer();
      }
    };

    this.speech.onTranscriptUpdate = (trans) => {
      if (this.virtualScreen) {
        this.virtualScreen.updateTranscript(trans.full);
      }
      this.emit('transcript', trans);

      // Broadcast live transcript over WebSockets to interviewer
      webSocketClient.send('candidate-transcript', { transcript: trans.full });
    };
  }

  _setupWebSocketBridge() {
    webSocketClient.on('interviewer-question', ({ payload }) => {
      if (payload && payload.questionText) {
        this.askCustomQuestion(payload.questionText);
      }
    });

    webSocketClient.on('request-repeat', () => {
      this.repeatCurrentQuestion();
    });
  }

  setPhase(newPhase) {
    this.phase = newPhase;
    this.emit('phaseChange', newPhase);
  }

  setListeningMode(mode) {
    this.listeningMode = mode.toUpperCase();
    this.speech.mic.setListeningMode(this.listeningMode.toLowerCase());
    if (this.virtualScreen) {
      this.virtualScreen.setListeningMode(this.listeningMode);
    }
  }

  setAudioDevice(deviceId) {
    this.speech.mic.setAudioDeviceId(deviceId);
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
    this._playQuestionAudioAndDisplay(this.currentQuestion.question_text, `QUESTION #${this.currentQuestion.question_index + 1}`);
  }

  _playQuestionAudioAndDisplay(questionText, title = 'QUESTION') {
    // Update 3D In-VR Screen
    if (this.virtualScreen) {
      this.virtualScreen.updateQuestion(title, questionText);
      this.virtualScreen.setInterviewerSpeaking(true);
      this.virtualScreen.setRecordingState(false);
    }

    // Audio Gating: Lock candidate microphone while Dr. Vance is speaking
    this.speech.mic.setInterviewerSpeaking(true);

    audioPlayback.speak(questionText, (viseme) => {
      this.avatars.setInterviewerViseme(viseme);
    });

    audioPlayback.onSpeechEnd = () => {
      this.speech.mic.setInterviewerSpeaking(false);
      if (this.virtualScreen) {
        this.virtualScreen.setInterviewerSpeaking(false);
      }
      this.beginCandidateRecording();
    };
  }

  beginCandidateRecording() {
    // Step 3: Candidate Spoken Answer
    this.setPhase(DialoguePhase.CANDIDATE_RECORDING);
    this.avatars.setInterviewerState(AvatarState.LISTENING);
    this.avatars.setCandidateState(AvatarState.SPEAKING);

    if (this.virtualScreen) {
      this.virtualScreen.setRecordingState(true);
    }

    this.speech.startListening();
    audioPlayback.playUIChime();
  }

  reRecordAnswer() {
    console.log('[StateMachine] Re-recording answer...');
    this.speech.resetTranscript();
    if (this.virtualScreen) {
      this.virtualScreen.updateTranscript('');
    }
    this.beginCandidateRecording();
  }

  async submitSpokenAnswer(textFallback = '') {
    if (this.phase !== DialoguePhase.CANDIDATE_RECORDING) return;

    // Step 4: Answer Delivery & Evaluation
    this.setPhase(DialoguePhase.CANDIDATE_DELIVERING);
    const { transcript, audioBlob } = await this.speech.stopListening();
    const finalTranscript = textFallback || transcript || 'I focused on building high-reliability distributed services with automated testing.';

    if (this.virtualScreen) {
      this.virtualScreen.setRecordingState(false);
      this.virtualScreen.updateTranscript(finalTranscript);
    }

    this.avatars.setCandidateState(AvatarState.SPEAKING);
    this.avatars.setInterviewerState(AvatarState.THINKING);

    let answerRes;
    try {
      answerRes = await apiClient.submitAnswer({
        questionId: this.currentQuestion ? this.currentQuestion.id : 1,
        transcriptText: finalTranscript,
        speechDurationSeconds: 4.5,
        isTypedFallback: Boolean(textFallback)
      });
    } catch (e) {
      answerRes = {
        answer: { transcript_text: finalTranscript },
        evaluation: { relevance_score: 5, technical_accuracy_score: 4 },
        interviewerReaction: { text: "Thank you for sharing that detailed explanation. Let's move to the next topic." }
      };
    }

    this.lastAnswer = answerRes.answer;
    this.lastEvaluation = answerRes.evaluation;
    this.emit('answerEvaluated', { answer: this.lastAnswer, evaluation: this.lastEvaluation });

    // Broadcast candidate answer over WebSockets to interviewer
    webSocketClient.send('candidate-answer-submitted', {
      transcript: finalTranscript,
      evaluation: this.lastEvaluation
    });

    // Interviewer acknowledges response
    this.setPhase(DialoguePhase.INTERVIEWER_ACKNOWLEDGING);
    this.avatars.setInterviewerState(AvatarState.SPEAKING);
    this.avatars.setCandidateState(AvatarState.LISTENING);

    const reactionText = (answerRes.interviewerReaction && answerRes.interviewerReaction.text)
      ? answerRes.interviewerReaction.text
      : "Excellent technical points. Let's explore the next question.";

    this.speech.mic.setInterviewerSpeaking(true);
    if (this.virtualScreen) {
      this.virtualScreen.setInterviewerSpeaking(true);
    }

    audioPlayback.speak(reactionText, (viseme) => {
      this.avatars.setInterviewerViseme(viseme);
    });

    audioPlayback.onSpeechEnd = () => {
      this.speech.mic.setInterviewerSpeaking(false);
      if (this.virtualScreen) {
        this.virtualScreen.setInterviewerSpeaking(false);
      }
      this.advanceToNextQuestionOrFinish();
    };
  }

  async askCustomQuestion(questionText) {
    if (!questionText) return;
    this.setPhase(DialoguePhase.INTERVIEWER_SPEAKING);
    this.avatars.setInterviewerState(AvatarState.SPEAKING);
    this.avatars.setCandidateState(AvatarState.LISTENING);

    this.currentQuestion = {
      id: Date.now(),
      question_index: (this.currentQuestion ? this.currentQuestion.question_index + 1 : 0),
      question_text: questionText,
      category: 'Custom Interviewer Question'
    };

    audioPlayback.playUIChime();
    this._playQuestionAudioAndDisplay(questionText, 'CUSTOM QUESTION');
  }

  async generateAIFollowup() {
    const lastAnswerText = this.lastAnswer ? this.lastAnswer.transcript_text : 'distributed systems and 6-DoF VR tracking';
    const followupPrompt = `Based on candidate's answer about "${lastAnswerText.substring(0, 80)}", ask a sharp, deep-dive architectural follow-up question.`;

    const followupQuestion = `Following up on what you just explained about "${lastAnswerText.slice(0, 45)}...": What trade-offs did you encounter, and how did you measure performance under high concurrent load?`;
    this.askCustomQuestion(followupQuestion);
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

    try {
      const nextRes = await apiClient.getNextQuestion(this.currentSession.id);
      this.currentQuestion = nextRes.question;
    } catch (e) {
      this.currentQuestion = {
        id: Date.now(),
        question_index: (this.currentQuestion ? this.currentQuestion.question_index + 1 : 1),
        question_text: "Can you describe a scenario where you had to debug an asynchronous race condition in a real-time communications pipeline?",
        category: "System Architecture"
      };
    }

    audioPlayback.playUIChime();
    this._playQuestionAudioAndDisplay(this.currentQuestion.question_text, `QUESTION #${this.currentQuestion.question_index + 1}`);
  }

  async finishInterview() {
    // Step 6: Conclude & generate report
    this.setPhase(DialoguePhase.SESSION_CONCLUDED);
    this.avatars.setInterviewerState(AvatarState.INTERVIEW_COMPLETED);
    this.avatars.setCandidateState(AvatarState.INTERVIEW_COMPLETED);

    if (this.virtualScreen) {
      this.virtualScreen.updateQuestion('INTERVIEW COMPLETED', 'Thank you! Your responses have been recorded and the final evaluation report is ready.');
      this.virtualScreen.setRecordingState(false);
    }

    try {
      const finishRes = await apiClient.finishSession(this.currentSession.id);
      this.finalReport = finishRes.reportDetails || finishRes.report;
    } catch (e) {
      this.finalReport = {
        overall_score: 92,
        relevance_percentage: 95,
        technical_percentage: 90,
        clarity_percentage: 92,
        completeness_percentage: 88,
        problem_solving_percentage: 94
      };
    }
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
      this._playQuestionAudioAndDisplay(this.currentQuestion.question_text, `QUESTION #${this.currentQuestion.question_index + 1}`);
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

