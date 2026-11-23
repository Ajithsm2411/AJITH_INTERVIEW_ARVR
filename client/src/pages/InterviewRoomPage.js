import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.js';
import { SpatialEnvironment } from '../vr/SpatialEnvironment.js';
import { WebXRManager } from '../vr/WebXRManager.js';
import { SpatialVirtualScreen3D } from '../vr/SpatialVirtualScreen3D.js';
import { AvatarManager } from '../avatars/AvatarManager.js';
import { SpeechRecognitionController } from '../hooks/useSpeechRecognition.js';
import { InterviewStateMachine } from '../hooks/useInterviewState.js';
import { InterviewSetupModal } from '../components/InterviewSetupModal.js';
import { VRControlsBar } from '../components/VRControlsBar.js';
import { LiveTranscriptionOverlay } from '../components/LiveTranscriptionOverlay.js';
import { EvaluationReportModal } from '../components/EvaluationReportModal.js';
import { NonVRFallbackBanner } from '../components/NonVRFallbackBanner.js';
import { ConsentModal } from '../components/ConsentModal.js';
import { InterviewerControlDock } from '../components/InterviewerControlDock.js';
import { webRTCManager } from '../services/webRTCManager.js';
import { webSocketClient } from '../services/webSocketClient.js';

export class InterviewRoomPage {
  constructor(container) {
    this.container = container;

    // 1. Three.js Core
    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x0c1017);

    this.camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 100);
    this.camera.position.set(0, 1.20, 0); // Natural seated eye level (1.20m)

    this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false });
    this.renderer.setSize(window.innerWidth, window.innerHeight);
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.xr.enabled = true;
    this.renderer.shadowMap.enabled = true;
    this.container.appendChild(this.renderer.domElement);

    // 2. VR Environment & Rig
    this.environment = new SpatialEnvironment(this.scene);
    this.xrManager = new WebXRManager(this.renderer, this.camera, this.scene);

    // 3. 3D In-VR Spatial Virtual Screen with Interactive Buttons (Prompt Req 1, 2 & 4)
    this.virtualScreen = new SpatialVirtualScreen3D(this.scene, {
      onPushToTalk: () => this._handleVirtualScreenPTT(),
      onSubmit: () => this.stateMachine.submitSpokenAnswer(),
      onRepeat: () => this.stateMachine.reRecordAnswer(),
      onModeToggle: () => this._handleVirtualScreenModeToggle(),
      onEnd: () => this.stateMachine.finishInterview()
    });

    // 4. Avatars
    this.avatarManager = new AvatarManager(this.scene);

    // 5. Speech & Interview State
    this.speechController = new SpeechRecognitionController();
    this.stateMachine = new InterviewStateMachine(this.avatarManager, this.speechController, this.virtualScreen);

    // 6. UI Overlays
    this.setupModal = null;
    this.controlsBar = null;
    this.transcriptionOverlay = null;
    this.reportModal = null;
    this.nonVRBanner = null;
    this.consentModal = null;
    this.interviewerDock = null;

    // Desktop mouse look & raycasting variables
    this.isMouseDown = false;
    this.mouseX = 0;
    this.mouseY = 0;
    this.yaw = 0;
    this.pitch = 0;
    this.desktopRaycaster = new THREE.Raycaster();
    this.mouseCoords = new THREE.Vector2();

    this.clock = new THREE.Clock();
    this.init();
  }

  init() {
    this._initUI();
    this._setupDesktopControls();
    this._setupStateMachineBridges();
    this._setupResize();
    this._initNetworkServices();

    // Start render loop
    this.renderer.setAnimationLoop(() => this._renderLoop());
  }

  _initNetworkServices() {
    try {
      webSocketClient.connect();
      webRTCManager.init('candidate');
    } catch (e) {
      console.warn('[InterviewRoomPage] Network services init warning:', e);
    }
  }

  _handleVirtualScreenPTT() {
    if (this.stateMachine.phase === 'candidate_recording') {
      this.stateMachine.submitSpokenAnswer();
    } else {
      this.stateMachine.beginCandidateRecording();
    }
  }

  _handleVirtualScreenModeToggle() {
    const nextMode = (this.stateMachine.listeningMode === 'PTT') ? 'AUTO' : 'PTT';
    this.stateMachine.setListeningMode(nextMode);
    if (this.transcriptionOverlay) {
      this.transcriptionOverlay.listeningMode = nextMode;
      const modeBtn = document.querySelector('#btn-mode-toggle');
      if (modeBtn) modeBtn.textContent = `🎧 Mode: ${nextMode}`;
    }
  }

  _initUI() {
    this.uiContainer = document.createElement('div');
    this.uiContainer.className = 'interview-ui-container';
    this.container.appendChild(this.uiContainer);

    // ── TOP OFFICE NAME & TITLE HEADER ──
    this.topHeader = document.createElement('header');
    this.topHeader.className = 'office-top-header';
    this.topHeader.innerHTML = `
      <div class="office-brand">
        <span class="office-icon">🏢</span>
        <span class="office-name">OFFICE: WIPRO PRIVATE LIMITED</span>
        <span class="office-divider">—</span>
        <span class="office-title">EXECUTIVE CORPORATE BOARDROOM</span>
        <span class="office-sub">• MetaSphere VR Telepresence</span>
      </div>
      <div class="office-status-badge">
        <span class="status-dot">●</span> ACTIVE 6-DoF SPATIAL SESSION
      </div>
    `;
    this.uiContainer.appendChild(this.topHeader);

    this.nonVRBanner = new NonVRFallbackBanner(this.uiContainer);
    this.consentModal = new ConsentModal(this.uiContainer);

    this.setupModal = new InterviewSetupModal(this.uiContainer, (config) => {
      this.stateMachine.startInterview(config);
    });

    this.controlsBar = new VRControlsBar(this.uiContainer, {
      onPause: (paused) => this.stateMachine.togglePause(),
      onRepeat: () => this.stateMachine.repeatCurrentQuestion(),
      onEnd: () => this.stateMachine.finishInterview(),
      onEnterVR: async () => {
        const entered = await this.xrManager.enterVR();
        if (entered) {
          this.avatarManager.setCandidateFirstPersonVR(true);
          this.nonVRBanner.hide();
        }
      }
    });

    this.transcriptionOverlay = new LiveTranscriptionOverlay(this.uiContainer, {
      onSubmitAnswer: (textFallback) => this.stateMachine.submitSpokenAnswer(textFallback),
      onRepeatAnswer: () => this.stateMachine.reRecordAnswer(),
      onModeToggle: (mode) => this.stateMachine.setListeningMode(mode),
      onDeviceSelect: (deviceId) => this.stateMachine.setAudioDevice(deviceId)
    });

    // Interviewer Question Dock (Prompt Requirement 1)
    this.interviewerDock = new InterviewerControlDock(this.uiContainer, {
      onAskQuestion: (qText) => this.stateMachine.askCustomQuestion(qText),
      onGenerateFollowup: () => this.stateMachine.generateAIFollowup(),
      onRepeatQuestion: () => this.stateMachine.repeatCurrentQuestion()
    });

    this.reportModal = new EvaluationReportModal(this.uiContainer, {
      onRestart: () => this.setupModal.show()
    });
  }

  _setupStateMachineBridges() {
    this.stateMachine.on('phaseChange', (phase) => {
      if (this.stateMachine.currentQuestion) {
        const isSpeaking = (phase === 'interviewer_speaking' || phase === 'interviewer_acknowledging');
        this.transcriptionOverlay.setInterviewerSpeech(this.stateMachine.currentQuestion.question_text, isSpeaking);
        this.transcriptionOverlay.setPhase(`QUESTION #${this.stateMachine.currentQuestion.question_index + 1}`, phase === 'candidate_recording');
        if (this.interviewerDock) {
          this.interviewerDock.setInterviewerSpeaking(isSpeaking);
        }
      }
    });

    this.stateMachine.on('transcript', (data) => {
      this.transcriptionOverlay.setCandidateTranscript(data.full);
      if (this.interviewerDock) {
        this.interviewerDock.setCandidateAnswer(data.full);
      }
    });

    this.stateMachine.on('loudness', ({ loudness, isSpeaking }) => {
      this.transcriptionOverlay.setLoudness(loudness, isSpeaking);
    });

    this.stateMachine.on('reportReady', (report) => {
      this.reportModal.show(report);
    });
  }

  _setupDesktopControls() {
    window.addEventListener('mousedown', (e) => {
      if (e.target.tagName !== 'CANVAS') return;
      this.isMouseDown = true;
      this.mouseX = e.clientX;
      this.mouseY = e.clientY;

      // Mouse raycast on 3D virtual screen buttons in desktop mode
      this.mouseCoords.x = (e.clientX / window.innerWidth) * 2 - 1;
      this.mouseCoords.y = -(e.clientY / window.innerHeight) * 2 + 1;
      this.desktopRaycaster.setFromCamera(this.mouseCoords, this.camera);
      this.virtualScreen.handleRaycast(this.desktopRaycaster, true);
    });

    window.addEventListener('mouseup', () => {
      this.isMouseDown = false;
      if (this.virtualScreen) {
        this.virtualScreen.handleRaycast(this.desktopRaycaster, false);
      }
    });

    window.addEventListener('mousemove', (e) => {
      // Raycast hover on 3D virtual screen
      this.mouseCoords.x = (e.clientX / window.innerWidth) * 2 - 1;
      this.mouseCoords.y = -(e.clientY / window.innerHeight) * 2 + 1;
      this.desktopRaycaster.setFromCamera(this.mouseCoords, this.camera);
      this.virtualScreen.handleRaycast(this.desktopRaycaster, false);

      if (!this.isMouseDown || this.renderer.xr.isPresenting) return;
      const dx = e.clientX - this.mouseX;
      const dy = e.clientY - this.mouseY;
      this.mouseX = e.clientX;
      this.mouseY = e.clientY;

      this.yaw -= dx * 0.003;
      this.pitch -= dy * 0.003;
      this.pitch = Math.max(-Math.PI / 4, Math.min(Math.PI / 4, this.pitch));

      this.camera.rotation.set(this.pitch, this.yaw, 0, 'YXZ');
    });
  }

  _setupResize() {
    window.addEventListener('resize', () => {
      this.camera.aspect = window.innerWidth / window.innerHeight;
      this.camera.updateProjectionMatrix();
      this.renderer.setSize(window.innerWidth, window.innerHeight);
    });
  }

  _renderLoop() {
    const dt = this.clock.getDelta();

    // 1. Update avatars (idle breathing, lip-sync, gestures)
    this.avatarManager.update(dt);

    // 2. Update WebXR VR controller raycasting on 3D virtual screen
    this.xrManager.update(this.virtualScreen);

    // 3. Update elapsed timer
    if (this.stateMachine.currentSession && !this.stateMachine.isPaused) {
      this.stateMachine.elapsedSeconds += dt;
      this.controlsBar.updateTimer(this.stateMachine.elapsedSeconds, this.stateMachine.durationSeconds);
    }

    this.renderer.render(this.scene, this.camera);
  }
}

