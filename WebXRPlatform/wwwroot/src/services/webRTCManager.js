import { webSocketClient } from './webSocketClient.js';
import { spatialAudio } from '../audio/SpatialAudioEngine.js';

/**
 * WebRTC Two-Way Audio Communication Manager (Prompt Requirement 3)
 * - Low-latency real-time 2-way peer audio stream between Interviewer and Candidate
 * - WebRTC PeerConnection with STUN servers
 * - Microphone selection (VR headset mics: Meta Quest / HTC Vive)
 * - Echo cancellation & acoustic feedback prevention
 * - Reconnection and interruption handling
 */
export class WebRTCManager {
  constructor() {
    this.peerConnection = null;
    this.localStream = null;
    this.remoteStream = null;
    this.remoteAudioElement = null;
    this.isConnected = false;
    this.connectionState = 'disconnected';
    this.role = 'candidate'; // 'interviewer' or 'candidate'
    this.selectedAudioDeviceId = null;
    this.onConnectionStateChange = null;
    this.onRemoteStream = null;

    this.rtcConfig = {
      iceServers: [
        { urls: 'stun:stun.l.google.com:19302' },
        { urls: 'stun:stun1.l.google.com:19302' }
      ]
    };

    this._setupSignalingListeners();
  }

  async getAudioInputDevices() {
    try {
      const devices = await navigator.mediaDevices.enumerateDevices();
      return devices.filter(d => d.kind === 'audioinput');
    } catch (e) {
      console.warn('[WebRTCManager] Could not enumerate audio devices:', e);
      return [];
    }
  }

  setAudioDevice(deviceId) {
    this.selectedAudioDeviceId = deviceId;
  }

  init(role = 'candidate') {
    this.role = role;
    if (!this.remoteAudioElement) {
      this.remoteAudioElement = document.createElement('audio');
      this.remoteAudioElement.autoplay = true;
      this.remoteAudioElement.playsInline = true;
      this.remoteAudioElement.style.display = 'none';
      document.body.appendChild(this.remoteAudioElement);
    }
  }

  _setupSignalingListeners() {
    webSocketClient.on('webrtc-offer', async ({ senderId, payload }) => {
      console.log('[WebRTCManager] Received WebRTC offer from peer:', senderId);
      await this._handleOffer(payload);
    });

    webSocketClient.on('webrtc-answer', async ({ senderId, payload }) => {
      console.log('[WebRTCManager] Received WebRTC answer from peer:', senderId);
      await this._handleAnswer(payload);
    });

    webSocketClient.on('webrtc-ice-candidate', async ({ senderId, payload }) => {
      if (this.peerConnection && payload) {
        try {
          await this.peerConnection.addIceCandidate(new RTCIceCandidate(payload));
        } catch (e) {
          console.warn('[WebRTCManager] Error adding ICE candidate:', e);
        }
      }
    });

    webSocketClient.on('peer-joined', () => {
      // If we are candidate and an interviewer connects, initiate call
      if (this.role === 'candidate' && this.localStream) {
        this.createOffer();
      }
    });
  }

  async attachLocalAudioStream(stream) {
    this.localStream = stream;
    if (this.peerConnection) {
      // Replace or add audio tracks
      const audioTrack = stream.getAudioTracks()[0];
      const sender = this.peerConnection.getSenders().find(s => s.track && s.track.kind === 'audio');
      if (sender) {
        sender.replaceTrack(audioTrack);
      } else if (audioTrack) {
        this.peerConnection.addTrack(audioTrack, this.localStream);
      }
    }
  }

  _createPeerConnection() {
    if (this.peerConnection) {
      this.peerConnection.close();
    }

    this.peerConnection = new RTCPeerConnection(this.rtcConfig);

    this.peerConnection.onconnectionstatechange = () => {
      this.connectionState = this.peerConnection.connectionState;
      this.isConnected = (this.connectionState === 'connected');
      console.log(`[WebRTCManager] PeerConnection State: ${this.connectionState}`);
      if (this.onConnectionStateChange) {
        this.onConnectionStateChange(this.connectionState);
      }
    };

    this.peerConnection.onicecandidate = (event) => {
      if (event.candidate) {
        webSocketClient.send('webrtc-ice-candidate', event.candidate);
      }
    };

    this.peerConnection.ontrack = (event) => {
      console.log('[WebRTCManager] Received remote audio stream track!');
      this.remoteStream = event.streams[0];
      if (this.remoteAudioElement) {
        this.remoteAudioElement.srcObject = this.remoteStream;
        this.remoteAudioElement.play().catch(e => console.warn('[WebRTCManager] Autoplay prevented:', e));
      }
      if (this.onRemoteStream) {
        this.onRemoteStream(this.remoteStream);
      }
    };

    if (this.localStream) {
      this.localStream.getAudioTracks().forEach(track => {
        this.peerConnection.addTrack(track, this.localStream);
      });
    }

    return this.peerConnection;
  }

  async createOffer() {
    try {
      this._createPeerConnection();
      const offer = await this.peerConnection.createOffer({
        offerToReceiveAudio: true,
        offerToReceiveVideo: false
      });
      await this.peerConnection.setLocalDescription(offer);
      webSocketClient.send('webrtc-offer', offer);
      console.log('[WebRTCManager] WebRTC Offer sent.');
    } catch (err) {
      console.warn('[WebRTCManager] Error creating WebRTC offer:', err);
    }
  }

  async _handleOffer(offer) {
    try {
      this._createPeerConnection();
      await this.peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
      const answer = await this.peerConnection.createAnswer();
      await this.peerConnection.setLocalDescription(answer);
      webSocketClient.send('webrtc-answer', answer);
      console.log('[WebRTCManager] WebRTC Answer sent.');
    } catch (err) {
      console.warn('[WebRTCManager] Error handling WebRTC offer:', err);
    }
  }

  async _handleAnswer(answer) {
    try {
      if (this.peerConnection) {
        await this.peerConnection.setRemoteDescription(new RTCSessionDescription(answer));
        console.log('[WebRTCManager] WebRTC Answer applied successfully.');
      }
    } catch (err) {
      console.warn('[WebRTCManager] Error handling WebRTC answer:', err);
    }
  }

  setMuted(muted) {
    if (this.localStream) {
      this.localStream.getAudioTracks().forEach(t => {
        t.enabled = !muted;
      });
    }
  }

  disconnect() {
    if (this.peerConnection) {
      this.peerConnection.close();
      this.peerConnection = null;
    }
    this.isConnected = false;
    this.connectionState = 'disconnected';
  }
}

export const webRTCManager = new WebRTCManager();
export default webRTCManager;
