import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.js';

/**
 * 3D In-VR Spatial Virtual Screen with Interactive VR Buttons (Prompt Requirements 1, 2 & 4)
 * - Displays Interviewer Question in readable 3D text inside VR headset
 * - Displays Live Candidate Speech-To-Text transcription in real-time
 * - Displays 3D VU Meter & Microphone indicator
 * - Interactive 3D VR buttons for Start, Push-to-Talk, Re-record, Submit, Mode Toggle, and End Interview
 * - Supports VR Controller laser raycasting & trigger click interaction
 */
export class SpatialVirtualScreen3D {
  constructor(scene, callbacks = {}) {
    this.scene = scene;
    this.callbacks = callbacks; // onPushToTalk, onSubmit, onRepeat, onModeToggle, onEnd, onStart

    this.root = new THREE.Group();
    this.root.name = 'SpatialVirtualScreen3D';

    // Canvas Texture for high-DPI in-VR text rendering
    this.canvasWidth = 1024;
    this.canvasHeight = 720;
    this.canvas = document.createElement('canvas');
    this.canvas.width = this.canvasWidth;
    this.canvas.height = this.canvasHeight;
    this.ctx = this.canvas.getContext('2d');

    this.texture = new THREE.CanvasTexture(this.canvas);
    this.texture.minFilter = THREE.LinearFilter;
    this.texture.magFilter = THREE.LinearFilter;

    // State
    this.questionTitle = 'QUESTION #1';
    this.questionText = 'Welcome to Wipro VR Interview. Click "Start Interview" to begin.';
    this.candidateTranscript = '';
    this.loudness = 0.0;
    this.isRecording = false;
    this.isInterviewerSpeaking = false;
    this.isCandidateListening = true;
    this.micStatusText = 'READY';
    this.listeningMode = 'PTT'; // 'PTT' or 'AUTO'
    this.hoveredButtonId = null;
    this.isTriggerDown = false;
    this.lastUpdateTime = 0;

    // Define 2D button rectangles on canvas (in canvas pixels)
    this.buttons = [
      { id: 'btn-ptt', label: '🎙️ PUSH TO TALK', x: 40, y: 580, w: 260, h: 80, color: '#00e5ff', activeColor: '#ff3366' },
      { id: 'btn-mode', label: '🎧 MODE: PTT', x: 320, y: 580, w: 180, h: 80, color: '#3a506b', activeColor: '#5bc0be' },
      { id: 'btn-repeat', label: '🔁 RE-RECORD', x: 520, y: 580, w: 180, h: 80, color: '#f39c12', activeColor: '#e67e22' },
      { id: 'btn-submit', label: '✅ SUBMIT', x: 720, y: 580, w: 160, h: 80, color: '#2ecc71', activeColor: '#27ae60' },
      { id: 'btn-end', label: '⏹️ END', x: 900, y: 580, w: 84, h: 80, color: '#e74c3c', activeColor: '#c0392b' }
    ];

    this._build3DMesh();
    this.render();
  }

  _build3DMesh() {
    // 1. Position screen in front of candidate chair at ergonomic seated eye level
    // Candidate is at (0, 1.20, -1.25), facing Dr. Vance (+Z)
    // Screen is positioned at (0, 0.96, -0.65), angled 15 degrees up
    this.root.position.set(0, 0.98, -0.62);
    this.root.rotation.x = -0.22; // Tilted slightly upwards toward eyes

    // 2. Translucent Screen Plane
    const screenGeo = new THREE.PlaneGeometry(1.28, 0.90);
    const screenMat = new THREE.MeshBasicMaterial({
      map: this.texture,
      transparent: true,
      opacity: 0.96,
      side: THREE.DoubleSide
    });
    this.screenMesh = new THREE.Mesh(screenGeo, screenMat);
    this.screenMesh.name = 'ScreenMesh_Interactive';
    this.root.add(this.screenMesh);

    // 3. Sleek Glass Bezel & Metallic Trim
    const frameGeo = new THREE.BoxGeometry(1.32, 0.94, 0.02);
    const frameMat = new THREE.MeshStandardMaterial({
      color: 0x0a101d,
      roughness: 0.4,
      metalness: 0.8
    });
    const frameMesh = new THREE.Mesh(frameGeo, frameMat);
    frameMesh.position.z = -0.012;
    this.root.add(frameMesh);

    // Glowing cyan accent edge
    const edgeGeo = new THREE.BoxGeometry(1.33, 0.01, 0.025);
    const edgeMat = new THREE.MeshBasicMaterial({ color: 0x00e5ff });
    const topEdge = new THREE.Mesh(edgeGeo, edgeMat);
    topEdge.position.set(0, 0.47, -0.008);
    this.root.add(topEdge);

    // Pedestal stand attaching to table
    const standGeo = new THREE.CylinderGeometry(0.025, 0.035, 0.28, 16);
    const standMat = new THREE.MeshStandardMaterial({ color: 0x141e2e, metalness: 0.9 });
    const stand = new THREE.Mesh(standGeo, standMat);
    stand.position.set(0, -0.48, -0.05);
    this.root.add(stand);

    this.scene.add(this.root);
  }

  updateQuestion(title, text) {
    this.questionTitle = title || 'QUESTION';
    this.questionText = text || '';
    this.render();
  }

  updateTranscript(transcript) {
    this.candidateTranscript = transcript || '';
    this.render();
  }

  setLoudness(loudness, isSpeaking) {
    this.loudness = loudness;
    if (this.isRecording) {
      this.micStatusText = isSpeaking ? '🎙️ SPEAKING NOW' : '👂 LISTENING...';
    }
    this.render();
  }

  setInterviewerSpeaking(speaking) {
    this.isInterviewerSpeaking = speaking;
    if (speaking) {
      this.micStatusText = '⏳ DR. VANCE SPEAKING (Mic Locked)';
    } else if (!this.isRecording) {
      this.micStatusText = 'READY TO ANSWER';
    }
    this.render();
  }

  setRecordingState(recording) {
    this.isRecording = recording;
    const pttBtn = this.buttons.find(b => b.id === 'btn-ptt');
    if (pttBtn) {
      pttBtn.label = recording ? '⏹️ FINISH / SUBMIT' : (this.listeningMode === 'PTT' ? '🎙️ PUSH TO TALK' : '🎙️ START SPEAKING');
    }
    this.micStatusText = recording ? '🔴 LIVE MIC RECORDING' : 'ANSWER PAUSED / READY';
    this.render();
  }

  setListeningMode(mode) {
    this.listeningMode = mode.toUpperCase();
    const modeBtn = this.buttons.find(b => b.id === 'btn-mode');
    if (modeBtn) {
      modeBtn.label = `🎧 MODE: ${this.listeningMode}`;
    }
    const pttBtn = this.buttons.find(b => b.id === 'btn-ptt');
    if (pttBtn && !this.isRecording) {
      pttBtn.label = this.listeningMode === 'PTT' ? '🎙️ PUSH TO TALK' : '🎙️ AUTO LISTEN (VAD)';
    }
    this.render();
  }

  /**
   * Main 2D Drawing onto the in-VR Canvas
   */
  render() {
    const ctx = this.ctx;
    const W = this.canvasWidth;
    const H = this.canvasHeight;

    // Background: High-tech glassmorphic dark container
    ctx.clearRect(0, 0, W, H);

    // Radial gradient glow
    const bgGrad = ctx.createLinearGradient(0, 0, W, H);
    bgGrad.addColorStop(0, 'rgba(12, 18, 30, 0.96)');
    bgGrad.addColorStop(1, 'rgba(8, 12, 20, 0.98)');
    ctx.fillStyle = bgGrad;
    ctx.beginPath();
    ctx.roundRect(10, 10, W - 20, H - 20, 24);
    ctx.fill();

    // Subtle neon border
    ctx.lineWidth = 3;
    ctx.strokeStyle = this.isRecording ? 'rgba(255, 51, 102, 0.7)' : (this.isInterviewerSpeaking ? 'rgba(0, 229, 255, 0.7)' : 'rgba(70, 90, 120, 0.5)');
    ctx.stroke();

    // ── 1. Top Brand Header ──
    ctx.fillStyle = '#00e5ff';
    ctx.font = 'bold 22px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';
    ctx.fillText('🏢 OFFICE: WIPRO PRIVATE LIMITED', 40, 52);

    ctx.fillStyle = '#8899aa';
    ctx.font = '16px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';
    ctx.fillText('EXECUTIVE BOARDROOM • VR TELEPRESENCE AUDIO LINK', 470, 52);

    // Status Pill
    const pillColor = this.isRecording ? '#ff3366' : (this.isInterviewerSpeaking ? '#00e5ff' : '#2ecc71');
    ctx.fillStyle = pillColor;
    ctx.beginPath();
    ctx.roundRect(W - 200, 32, 160, 30, 15);
    ctx.fill();
    ctx.fillStyle = '#000000';
    ctx.font = 'bold 13px sans-serif';
    ctx.fillText(this.isRecording ? '● RECORDING' : (this.isInterviewerSpeaking ? '● DR. VANCE' : '● ACTIVE 6-DoF'), W - 188, 52);

    // Divider Line
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.1)';
    ctx.beginPath();
    ctx.moveTo(40, 75);
    ctx.lineTo(W - 40, 75);
    ctx.stroke();

    // ── 2. Interviewer Question Box ──
    ctx.fillStyle = 'rgba(20, 32, 48, 0.85)';
    ctx.beginPath();
    ctx.roundRect(40, 90, W - 80, 190, 16);
    ctx.fill();
    ctx.strokeStyle = this.isInterviewerSpeaking ? 'rgba(0, 229, 255, 0.6)' : 'rgba(255, 255, 255, 0.08)';
    ctx.stroke();

    // Question Header tag
    ctx.fillStyle = '#00e5ff';
    ctx.font = 'bold 18px sans-serif';
    ctx.fillText(`👩‍💼 DR. SARAH VANCE (INTERVIEWER) — ${this.questionTitle}`, 60, 124);

    // Wave visualizer icon when Dr. Vance is speaking
    if (this.isInterviewerSpeaking) {
      ctx.fillStyle = '#00e5ff';
      const waveCount = 5;
      for (let i = 0; i < waveCount; i++) {
        const waveH = 10 + Math.sin(Date.now() * 0.01 + i) * 8;
        ctx.fillRect(W - 120 + i * 12, 120 - waveH / 2, 6, waveH);
      }
    }

    // Question Text
    ctx.fillStyle = '#ffffff';
    ctx.font = '22px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';
    this._wrapText(ctx, `"${this.questionText}"`, 60, 160, W - 120, 32, 3);

    // ── 3. Candidate Response Box ──
    ctx.fillStyle = 'rgba(16, 26, 38, 0.9)';
    ctx.beginPath();
    ctx.roundRect(40, 295, W - 80, 260, 16);
    ctx.fill();
    ctx.strokeStyle = this.isRecording ? 'rgba(255, 51, 102, 0.6)' : 'rgba(255, 255, 255, 0.08)';
    ctx.stroke();

    // Candidate Header Tag
    ctx.fillStyle = '#2ecc71';
    ctx.font = 'bold 18px sans-serif';
    ctx.fillText('🎙️ YOUR SPOKEN ANSWER (VR HEADSET MICROPHONE)', 60, 330);

    // Status Indicator
    ctx.fillStyle = this.isRecording ? '#ff3366' : '#8899aa';
    ctx.font = 'bold 15px sans-serif';
    ctx.fillText(this.micStatusText, 60, 360);

    // Candidate Transcript Content
    ctx.fillStyle = this.candidateTranscript ? '#e6f0fa' : '#6b7c96';
    ctx.font = 'italic 21px sans-serif';
    const displayTrans = this.candidateTranscript || (this.isRecording ? 'Listening to your voice... Speak your answer into the VR headset.' : 'Press "Push to Talk" or start speaking to record your answer.');
    this._wrapText(ctx, displayTrans, 60, 400, W - 120, 30, 3);

    // ── 3D VU Meter Bar ──
    const vuY = 515;
    ctx.fillStyle = '#1c2838';
    ctx.beginPath();
    ctx.roundRect(60, vuY, 260, 16, 8);
    ctx.fill();

    const fillWidth = Math.min(260, Math.round(this.loudness * 500));
    if (fillWidth > 0) {
      const vuGrad = ctx.createLinearGradient(60, 0, 320, 0);
      vuGrad.addColorStop(0, '#2ecc71');
      vuGrad.addColorStop(0.7, '#f1c40f');
      vuGrad.addColorStop(1, '#e74c3c');
      ctx.fillStyle = vuGrad;
      ctx.beginPath();
      ctx.roundRect(60, vuY, fillWidth, 16, 8);
      ctx.fill();
    }
    ctx.fillStyle = '#8899aa';
    ctx.font = '13px sans-serif';
    ctx.fillText(`MIC LEVEL: ${Math.round(this.loudness * 100)}%`, 330, vuY + 13);

    // ── 4. Interactive 3D VR Controller Buttons ──
    for (const btn of this.buttons) {
      const isHovered = (this.hoveredButtonId === btn.id);
      const isPttActive = (btn.id === 'btn-ptt' && this.isRecording);

      // Button background
      ctx.fillStyle = isPttActive ? btn.activeColor : (isHovered ? '#ffffff' : btn.color);
      ctx.beginPath();
      ctx.roundRect(btn.x, btn.y, btn.w, btn.h, 14);
      ctx.fill();

      // Button drop glow if hovered
      if (isHovered) {
        ctx.strokeStyle = '#00e5ff';
        ctx.lineWidth = 4;
        ctx.stroke();
      }

      // Button label text
      ctx.fillStyle = isHovered ? '#000000' : (isPttActive ? '#ffffff' : '#ffffff');
      ctx.font = 'bold 16px sans-serif';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(btn.label, btn.x + btn.w / 2, btn.y + btn.h / 2);
    }

    ctx.textAlign = 'left';
    ctx.textBaseline = 'alphabetic';

    this.texture.needsUpdate = true;
  }

  _wrapText(ctx, text, x, y, maxWidth, lineHeight, maxLines = 4) {
    const words = text.split(' ');
    let line = '';
    let lineCount = 0;

    for (let n = 0; n < words.length; n++) {
      const testLine = line + words[n] + ' ';
      const metrics = ctx.measureText(testLine);
      const testWidth = metrics.width;
      if (testWidth > maxWidth && n > 0) {
        ctx.fillText(line, x, y);
        line = words[n] + ' ';
        y += lineHeight;
        lineCount++;
        if (lineCount >= maxLines - 1 && n < words.length - 1) {
          ctx.fillText(line + '...', x, y);
          return;
        }
      } else {
        line = testLine;
      }
    }
    ctx.fillText(line, x, y);
  }

  /**
   * Raycast intersection test for VR Controller pointer or Mouse
   * @param {THREE.Raycaster} raycaster
   * @param {boolean} isSelectPressed (controller trigger pulled)
   */
  handleRaycast(raycaster, isSelectPressed = false) {
    if (!this.screenMesh) return null;

    const intersects = raycaster.intersectObject(this.screenMesh);
    if (intersects.length === 0) {
      if (this.hoveredButtonId !== null) {
        this.hoveredButtonId = null;
        this.render();
      }
      return null;
    }

    const hit = intersects[0];
    if (!hit.uv) return null;

    // Convert UV (0 to 1) to Canvas coordinates (0 to W, 0 to H)
    const cx = hit.uv.x * this.canvasWidth;
    const cy = (1.0 - hit.uv.y) * this.canvasHeight; // Flip Y for Three.js UV space

    let hoveredId = null;
    for (const btn of this.buttons) {
      if (cx >= btn.x && cx <= btn.x + btn.w && cy >= btn.y && cy <= btn.y + btn.h) {
        hoveredId = btn.id;
        break;
      }
    }

    if (this.hoveredButtonId !== hoveredId) {
      this.hoveredButtonId = hoveredId;
      this.render();
    }

    // Handle button click upon trigger down
    if (isSelectPressed && !this.isTriggerDown && hoveredId) {
      this.isTriggerDown = true;
      this._onButtonClick(hoveredId);
    } else if (!isSelectPressed) {
      this.isTriggerDown = false;
    }

    return { hoveredId, cx, cy, hitPoint: hit.point };
  }

  _onButtonClick(buttonId) {
    console.log(`[SpatialVirtualScreen3D] VR Button Clicked: ${buttonId}`);
    switch (buttonId) {
      case 'btn-ptt':
        if (this.callbacks.onPushToTalk) this.callbacks.onPushToTalk();
        break;
      case 'btn-mode':
        if (this.callbacks.onModeToggle) this.callbacks.onModeToggle();
        break;
      case 'btn-repeat':
        if (this.callbacks.onRepeat) this.callbacks.onRepeat();
        break;
      case 'btn-submit':
        if (this.callbacks.onSubmit) this.callbacks.onSubmit();
        break;
      case 'btn-end':
        if (this.callbacks.onEnd) this.callbacks.onEnd();
        break;
    }
  }

  dispose() {
    if (this.screenMesh) {
      this.root.remove(this.screenMesh);
      this.screenMesh.geometry.dispose();
      this.screenMesh.material.dispose();
    }
    if (this.texture) this.texture.dispose();
    this.scene.remove(this.root);
  }
}
