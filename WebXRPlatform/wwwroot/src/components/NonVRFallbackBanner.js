/**
 * Non-VR Fallback Notification Banner (Spec Section 14)
 */
export class NonVRFallbackBanner {
  constructor(container) {
    this.container = container;
    this.element = null;
    this.render();
  }

  render() {
    this.element = document.createElement('div');
    this.element.className = 'non-vr-banner';
    this.element.innerHTML = `
      <div class="banner-content">
        <span class="banner-icon">🖥️</span>
        <div class="banner-text">
          <b>Desktop 3D Preview Active:</b> WebXR headset not detected or browser running non-immersive mode.
          Using seated desktop camera at eye level (1.20m). Click and drag to look around, or connect a Quest 3 / OpenXR headset for full VR immersion.
        </div>
        <button type="button" class="banner-close" id="btn-close-banner">✕</button>
      </div>
    `;

    this.element.querySelector('#btn-close-banner').addEventListener('click', () => {
      this.element.style.display = 'none';
    });

    this.container.appendChild(this.element);
  }

  hide() {
    if (this.element) this.element.style.display = 'none';
  }
}
