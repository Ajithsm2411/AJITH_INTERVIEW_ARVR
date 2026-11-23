/**
 * VR Telepresence Spatial 3D Engine
 * Lightweight, high-performance WebGL 2.0 / WebXR engine
 * Designed for real-time 90/120Hz face-to-face VR telepresence.
 */

class Math3D {
  static createMat4() {
    return new Float32Array([
      1,0,0,0,
      0,1,0,0,
      0,0,1,0,
      0,0,0,1
    ]);
  }

  static identity(out) {
    out[0]=1; out[1]=0; out[2]=0; out[3]=0;
    out[4]=0; out[5]=1; out[6]=0; out[7]=0;
    out[8]=0; out[9]=0; out[10]=1; out[11]=0;
    out[12]=0; out[13]=0; out[14]=0; out[15]=1;
    return out;
  }

  static perspective(out, fovY, aspect, near, far) {
    const f = 1.0 / Math.tan(fovY / 2);
    const nf = 1 / (near - far);
    out[0] = f / aspect; out[1] = 0; out[2] = 0; out[3] = 0;
    out[4] = 0; out[5] = f; out[6] = 0; out[7] = 0;
    out[8] = 0; out[9] = 0; out[10] = (far + near) * nf; out[11] = -1;
    out[12] = 0; out[13] = 0; out[14] = (2 * far * near) * nf; out[15] = 0;
    return out;
  }

  static lookAt(out, eye, target, up) {
    let z0 = eye[0] - target[0], z1 = eye[1] - target[1], z2 = eye[2] - target[2];
    let len = 1 / Math.hypot(z0, z1, z2);
    z0 *= len; z1 *= len; z2 *= len;

    let x0 = up[1] * z2 - up[2] * z1;
    let x1 = up[2] * z0 - up[0] * z2;
    let x2 = up[0] * z1 - up[1] * z0;
    len = 1 / Math.hypot(x0, x1, x2);
    x0 *= len; x1 *= len; x2 *= len;

    let y0 = z1 * x2 - z2 * x1;
    let y1 = z2 * x0 - z0 * x2;
    let y2 = z0 * x1 - z1 * x0;

    out[0] = x0; out[1] = y0; out[2] = z0; out[3] = 0;
    out[4] = x1; out[5] = y1; out[6] = z1; out[7] = 0;
    out[8] = x2; out[9] = y2; out[10] = z2; out[11] = 0;
    out[12] = -(x0 * eye[0] + x1 * eye[1] + x2 * eye[2]);
    out[13] = -(y0 * eye[0] + y1 * eye[1] + y2 * eye[2]);
    out[14] = -(z0 * eye[0] + z1 * eye[1] + z2 * eye[2]);
    out[15] = 1;
    return out;
  }

  static multiply(out, a, b) {
    const a00 = a[0], a01 = a[1], a02 = a[2], a03 = a[3];
    const a10 = a[4], a11 = a[5], a12 = a[6], a13 = a[7];
    const a20 = a[8], a21 = a[9], a22 = a[10], a23 = a[11];
    const a30 = a[12], a31 = a[13], a32 = a[14], a33 = a[15];

    let b0 = b[0], b1 = b[1], b2 = b[2], b3 = b[3];
    out[0] = b0*a00 + b1*a10 + b2*a20 + b3*a30;
    out[1] = b0*a01 + b1*a11 + b2*a21 + b3*a31;
    out[2] = b0*a02 + b1*a12 + b2*a22 + b3*a32;
    out[3] = b0*a03 + b1*a13 + b2*a23 + b3*a33;

    b0 = b[4]; b1 = b[5]; b2 = b[6]; b3 = b[7];
    out[4] = b0*a00 + b1*a10 + b2*a20 + b3*a30;
    out[5] = b0*a01 + b1*a11 + b2*a21 + b3*a31;
    out[6] = b0*a02 + b1*a12 + b2*a22 + b3*a32;
    out[7] = b0*a03 + b1*a13 + b2*a23 + b3*a33;

    b0 = b[8]; b1 = b[9]; b2 = b[10]; b3 = b[11];
    out[8] = b0*a00 + b1*a10 + b2*a20 + b3*a30;
    out[9] = b0*a01 + b1*a11 + b2*a21 + b3*a31;
    out[10] = b0*a02 + b1*a12 + b2*a22 + b3*a32;
    out[11] = b0*a03 + b1*a13 + b2*a23 + b3*a33;

    b0 = b[12]; b1 = b[13]; b2 = b[14]; b3 = b[15];
    out[12] = b0*a00 + b1*a10 + b2*a20 + b3*a30;
    out[13] = b0*a01 + b1*a11 + b2*a21 + b3*a31;
    out[14] = b0*a02 + b1*a12 + b2*a22 + b3*a32;
    out[15] = b0*a03 + b1*a13 + b2*a23 + b3*a33;
    return out;
  }

  static fromRotationTranslationScale(out, q, v, s) {
    const x = q[0], y = q[1], z = q[2], w = q[3];
    const x2 = x + x, y2 = y + y, z2 = z + z;
    const xx = x * x2, xy = x * y2, xz = x * z2;
    const yy = y * y2, yz = y * z2, zz = z * z2;
    const wx = w * x2, wy = w * y2, wz = w * z2;
    const sx = s[0], sy = s[1], sz = s[2];

    out[0] = (1 - (yy + zz)) * sx;
    out[1] = (xy + wz) * sx;
    out[2] = (xz - wy) * sx;
    out[3] = 0;
    out[4] = (xy - wz) * sy;
    out[5] = (1 - (xx + zz)) * sy;
    out[6] = (yz + wx) * sy;
    out[7] = 0;
    out[8] = (xz + wy) * sz;
    out[9] = (yz - wx) * sz;
    out[10] = (1 - (xx + yy)) * sz;
    out[11] = 0;
    out[12] = v[0];
    out[13] = v[1];
    out[14] = v[2];
    out[15] = 1;
    return out;
  }

  static invert(out, a) {
    const a00 = a[0], a01 = a[1], a02 = a[2], a03 = a[3];
    const a10 = a[4], a11 = a[5], a12 = a[6], a13 = a[7];
    const a20 = a[8], a21 = a[9], a22 = a[10], a23 = a[11];
    const a30 = a[12], a31 = a[13], a32 = a[14], a33 = a[15];

    const b00 = a00 * a11 - a01 * a10;
    const b01 = a00 * a12 - a02 * a10;
    const b02 = a00 * a13 - a03 * a10;
    const b03 = a01 * a12 - a02 * a11;
    const b04 = a01 * a13 - a03 * a11;
    const b05 = a02 * a13 - a03 * a12;
    const b06 = a20 * a31 - a21 * a30;
    const b07 = a20 * a32 - a22 * a30;
    const b08 = a20 * a33 - a23 * a30;
    const b09 = a21 * a32 - a22 * a31;
    const b10 = a21 * a33 - a23 * a31;
    const b11 = a22 * a33 - a23 * a32;

    let det = b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06;
    if (!det) return null;
    det = 1.0 / det;

    out[0] = (a11 * b11 - a12 * b10 + a13 * b09) * det;
    out[1] = (a02 * b10 - a01 * b11 - a03 * b09) * det;
    out[2] = (a31 * b05 - a32 * b04 + a33 * b03) * det;
    out[3] = (a22 * b04 - a21 * b05 - a23 * b03) * det;
    out[4] = (a12 * b08 - a10 * b11 - a13 * b07) * det;
    out[5] = (a00 * b11 - a02 * b08 + a03 * b07) * det;
    out[6] = (a32 * b02 - a30 * b05 - a33 * b01) * det;
    out[7] = (a20 * b05 - a22 * b02 + a23 * b01) * det;
    out[8] = (a10 * b10 - a11 * b08 + a13 * b06) * det;
    out[9] = (a01 * b08 - a00 * b10 - a03 * b06) * det;
    out[10] = (a30 * b04 - a31 * b02 + a33 * b00) * det;
    out[11] = (a21 * b02 - a20 * b04 - a23 * b00) * det;
    out[12] = (a11 * b07 - a10 * b09 - a12 * b06) * det;
    out[13] = (a00 * b09 - a01 * b07 + a02 * b06) * det;
    out[14] = (a31 * b01 - a30 * b03 - a32 * b00) * det;
    out[15] = (a20 * b03 - a21 * b01 + a22 * b00) * det;
    return out;
  }

  static quatFromEuler(out, xDeg, yDeg, zDeg) {
    const x = (xDeg * Math.PI) / 360;
    const y = (yDeg * Math.PI) / 360;
    const z = (zDeg * Math.PI) / 360;
    const c1 = Math.cos(x), c2 = Math.cos(y), c3 = Math.cos(z);
    const s1 = Math.sin(x), s2 = Math.sin(y), s3 = Math.sin(z);

    out[0] = s1 * c2 * c3 + c1 * s2 * s3;
    out[1] = c1 * s2 * c3 - s1 * c2 * s3;
    out[2] = c1 * c2 * s3 + s1 * s2 * c3;
    out[3] = c1 * c2 * c3 - s1 * s2 * s3;
    return out;
  }

  static quatSlerp(out, a, b, t) {
    let ax = a[0], ay = a[1], az = a[2], aw = a[3];
    let bx = b[0], by = b[1], bz = b[2], bw = b[3];
    let cosom = ax * bx + ay * by + az * bz + aw * bw;
    if (cosom < 0) {
      cosom = -cosom;
      bx = -bx; by = -by; bz = -bz; bw = -bw;
    }
    let scale0, scale1;
    if (1.0 - cosom > 0.0001) {
      const omega = Math.acos(cosom);
      const sinom = Math.sin(omega);
      scale0 = Math.sin((1.0 - t) * omega) / sinom;
      scale1 = Math.sin(t * omega) / sinom;
    } else {
      scale0 = 1.0 - t;
      scale1 = t;
    }
    out[0] = scale0 * ax + scale1 * bx;
    out[1] = scale0 * ay + scale1 * by;
    out[2] = scale0 * az + scale1 * bz;
    out[3] = scale0 * aw + scale1 * bw;
    return out;
  }
}

/**
 * 3D Scene Graph Object
 */
class SpatialObject {
  constructor(name = "Object3D") {
    this.name = name;
    this.position = [0, 0, 0];
    this.rotation = [0, 0, 0, 1]; // Quaternion
    this.scale = [1, 1, 1];
    this.localMatrix = Math3D.createMat4();
    this.worldMatrix = Math3D.createMat4();
    this.parent = null;
    this.children = [];
    this.geometry = null;
    this.material = null;
    this.visible = true;
    this.isCulled = false;
  }

  add(child) {
    if (child.parent) child.parent.remove(child);
    child.parent = this;
    this.children.push(child);
    return child;
  }

  remove(child) {
    const idx = this.children.indexOf(child);
    if (idx !== -1) {
      this.children.splice(idx, 1);
      child.parent = null;
    }
  }

  updateMatrixWorld(force = false) {
    Math3D.fromRotationTranslationScale(this.localMatrix, this.rotation, this.position, this.scale);
    if (this.parent) {
      Math3D.multiply(this.worldMatrix, this.parent.worldMatrix, this.localMatrix);
    } else {
      for (let i = 0; i < 16; i++) this.worldMatrix[i] = this.localMatrix[i];
    }
    for (const child of this.children) {
      child.updateMatrixWorld(force);
    }
  }

  setEuler(x, y, z) {
    Math3D.quatFromEuler(this.rotation, x, y, z);
  }

  getWorldPosition() {
    return [this.worldMatrix[12], this.worldMatrix[13], this.worldMatrix[14]];
  }
}

/**
 * Procedural Geometry Helpers
 */
class GeometryFactory {
  static createBox(w = 1, h = 1, d = 1) {
    const hw = w/2, hh = h/2, hd = d/2;
    const positions = [
      // Front
      -hw,-hh, hd,  hw,-hh, hd,  hw, hh, hd, -hw, hh, hd,
      // Back
       hw,-hh,-hd, -hw,-hh,-hd, -hw, hh,-hd,  hw, hh,-hd,
      // Top
      -hw, hh, hd,  hw, hh, hd,  hw, hh,-hd, -hw, hh,-hd,
      // Bottom
      -hw,-hh,-hd,  hw,-hh,-hd,  hw,-hh, hd, -hw,-hh, hd,
      // Right
       hw,-hh, hd,  hw,-hh,-hd,  hw, hh,-hd,  hw, hh, hd,
      // Left
      -hw,-hh,-hd, -hw,-hh, hd, -hw, hh, hd, -hw, hh,-hd,
    ];
    const normals = [
      0,0,1, 0,0,1, 0,0,1, 0,0,1,
      0,0,-1, 0,0,-1, 0,0,-1, 0,0,-1,
      0,1,0, 0,1,0, 0,1,0, 0,1,0,
      0,-1,0, 0,-1,0, 0,-1,0, 0,-1,0,
      1,0,0, 1,0,0, 1,0,0, 1,0,0,
      -1,0,0, -1,0,0, -1,0,0, -1,0,0
    ];
    const uvs = [
      0,0, 1,0, 1,1, 0,1,
      0,0, 1,0, 1,1, 0,1,
      0,0, 1,0, 1,1, 0,1,
      0,0, 1,0, 1,1, 0,1,
      0,0, 1,0, 1,1, 0,1,
      0,0, 1,0, 1,1, 0,1
    ];
    const indices = [];
    for (let f = 0; f < 6; f++) {
      const b = f * 4;
      indices.push(b, b+1, b+2, b, b+2, b+3);
    }
    return { positions: new Float32Array(positions), normals: new Float32Array(normals), uvs: new Float32Array(uvs), indices: new Uint16Array(indices) };
  }

  static createCylinder(radius = 0.5, height = 1, segments = 16) {
    const positions = [], normals = [], uvs = [], indices = [];
    const hh = height / 2;

    for (let i = 0; i <= segments; i++) {
      const u = i / segments;
      const angle = u * Math.PI * 2;
      const x = Math.cos(angle);
      const z = Math.sin(angle);

      // Top vertex
      positions.push(x * radius, hh, z * radius);
      normals.push(x, 0, z);
      uvs.push(u, 1);

      // Bottom vertex
      positions.push(x * radius, -hh, z * radius);
      normals.push(x, 0, z);
      uvs.push(u, 0);
    }

    for (let i = 0; i < segments; i++) {
      const p1 = i * 2;
      const p2 = p1 + 1;
      const p3 = (i + 1) * 2;
      const p4 = p3 + 1;
      indices.push(p1, p2, p3);
      indices.push(p2, p4, p3);
    }

    return { positions: new Float32Array(positions), normals: new Float32Array(normals), uvs: new Float32Array(uvs), indices: new Uint16Array(indices) };
  }

  static createSphere(radius = 0.5, latBands = 14, lonBands = 14) {
    const positions = [], normals = [], uvs = [], indices = [];

    for (let lat = 0; lat <= latBands; lat++) {
      const theta = (lat * Math.PI) / latBands;
      const sinTheta = Math.sin(theta);
      const cosTheta = Math.cos(theta);

      for (let lon = 0; lon <= lonBands; lon++) {
        const phi = (lon * 2 * Math.PI) / lonBands;
        const sinPhi = Math.sin(phi);
        const cosPhi = Math.cos(phi);

        const x = cosPhi * sinTheta;
        const y = cosTheta;
        const z = sinPhi * sinTheta;
        const u = 1 - lon / lonBands;
        const v = 1 - lat / latBands;

        normals.push(x, y, z);
        uvs.push(u, v);
        positions.push(radius * x, radius * y, radius * z);
      }
    }

    for (let lat = 0; lat < latBands; lat++) {
      for (let lon = 0; lon < lonBands; lon++) {
        const first = lat * (lonBands + 1) + lon;
        const second = first + lonBands + 1;
        indices.push(first, second, first + 1);
        indices.push(second, second + 1, first + 1);
      }
    }

    return { positions: new Float32Array(positions), normals: new Float32Array(normals), uvs: new Float32Array(uvs), indices: new Uint16Array(indices) };
  }

  static createPlane(width = 1, height = 1) {
    const hw = width / 2, hh = height / 2;
    const positions = new Float32Array([
      -hw,  hh, 0,
       hw,  hh, 0,
      -hw, -hh, 0,
       hw, -hh, 0
    ]);
    const normals = new Float32Array([
      0, 0, 1,  0, 0, 1,  0, 0, 1,  0, 0, 1
    ]);
    const uvs = new Float32Array([
      0, 0,  1, 0,  0, 1,  1, 1
    ]);
    const indices = new Uint16Array([
      0, 2, 1,  1, 2, 3
    ]);
    return { positions, normals, uvs, indices };
  }
}

/**
 * WebGL 3D Spatial Renderer with WebXR Support
 */
class SpatialRenderer {
  constructor(canvas) {
    this.canvas = canvas;
    this.canvas.width = window.innerWidth || 1920;
    this.canvas.height = window.innerHeight || 1080;
    this.gl = canvas.getContext("webgl2", { antialias: true, alpha: false, xrCompatible: true })
           || canvas.getContext("webgl", { antialias: true, alpha: false });

    if (!this.gl) {
      alert("WebGL is not supported on this browser.");
      return;
    }

    const gl = this.gl;
    gl.enable(gl.DEPTH_TEST);
    gl.depthFunc(gl.LEQUAL);
    gl.enable(gl.CULL_FACE);
    gl.cullFace(gl.BACK);

    this.scene = new SpatialObject("RootScene");
    this.camera = new SpatialObject("MainCamera");
    this.camera.position = [0, 1.22, 0.65];
    this.projectionMatrix = Math3D.createMat4();
    this.viewMatrix = Math3D.createMat4();

    // Lighting parameters
    this.ambientLight = [0.42, 0.45, 0.52];
    this.dirLightDir = [-0.3, 0.8, -0.5]; // Normalized light vector
    this.dirLightColor = [0.95, 0.92, 0.85];
    this.spotLightPos = [0, 3.1, 0];
    this.spotLightColor = [1.0, 0.96, 0.90];

    // WebXR state
    this.xrSession = null;
    this.xrRefSpace = null;
    this.isXRActive = false;

    // Buffer cache
    this.geometryBuffers = new Map();
    this.textures = new Map();

    this.initShaders();
  }

  initShaders() {
    const gl = this.gl;
    const vsSource = `
      attribute vec3 aPosition;
      attribute vec3 aNormal;
      attribute vec2 aUV;

      uniform mat4 uWorld;
      uniform mat4 uView;
      uniform mat4 uProj;

      varying vec3 vWorldPos;
      varying vec3 vNormal;
      varying vec2 vUV;

      void main(void) {
        vec4 worldPos = uWorld * vec4(aPosition, 1.0);
        vWorldPos = worldPos.xyz;
        vNormal = mat3(uWorld) * aNormal;
        vUV = aUV;
        gl_Position = uProj * uView * worldPos;
      }
    `;

    const fsSource = `
      precision mediump float;

      varying vec3 vWorldPos;
      varying vec3 vNormal;
      varying vec2 vUV;

      uniform vec4 uBaseColor;
      uniform vec3 uEmission;
      uniform float uMetallic;
      uniform float uRoughness;
      uniform bool uHasTexture;
      uniform sampler2D uSampler;

      uniform vec3 uAmbientLight;
      uniform vec3 uDirLightDir;
      uniform vec3 uDirLightColor;
      uniform vec3 uEyePos;

      void main(void) {
        vec3 N = normalize(vNormal);
        vec3 L = normalize(uDirLightDir);
        vec3 V = normalize(uEyePos - vWorldPos);
        vec3 H = normalize(L + V);

        vec4 col = uBaseColor;
        if (uHasTexture) {
          col *= texture2D(uSampler, vUV);
        }

        // Diffuse
        float diff = max(dot(N, L), 0.0);
        vec3 diffuse = diff * uDirLightColor;

        // Blinn-phong specular
        float specPower = mix(12.0, 128.0, 1.0 - uRoughness);
        float spec = pow(max(dot(N, H), 0.0), specPower) * (uMetallic * 0.8 + 0.2);
        vec3 specular = spec * uDirLightColor;

        // Downlight spot simulation
        vec3 spotDir = normalize(vec3(0.0, 3.1, 0.0) - vWorldPos);
        float spotDiff = max(dot(N, spotDir), 0.0) * 0.45;

        vec3 finalColor = col.rgb * (uAmbientLight + diffuse + spotDiff) + specular + uEmission;
        gl_FragColor = vec4(finalColor, col.a);
      }
    `;

    const vs = this.compileShader(gl.VERTEX_SHADER, vsSource);
    const fs = this.compileShader(gl.FRAGMENT_SHADER, fsSource);
    this.program = gl.createProgram();
    gl.attachShader(this.program, vs);
    gl.attachShader(this.program, fs);
    gl.linkProgram(this.program);

    if (!gl.getProgramParameter(this.program, gl.LINK_STATUS)) {
      console.error("Shader link error:", gl.getProgramInfoLog(this.program));
    }

    this.attribs = {
      position: gl.getAttribLocation(this.program, "aPosition"),
      normal: gl.getAttribLocation(this.program, "aNormal"),
      uv: gl.getAttribLocation(this.program, "aUV"),
    };

    this.uniforms = {
      world: gl.getUniformLocation(this.program, "uWorld"),
      view: gl.getUniformLocation(this.program, "uView"),
      proj: gl.getUniformLocation(this.program, "uProj"),
      baseColor: gl.getUniformLocation(this.program, "uBaseColor"),
      emission: gl.getUniformLocation(this.program, "uEmission"),
      metallic: gl.getUniformLocation(this.program, "uMetallic"),
      roughness: gl.getUniformLocation(this.program, "uRoughness"),
      hasTexture: gl.getUniformLocation(this.program, "uHasTexture"),
      sampler: gl.getUniformLocation(this.program, "uSampler"),
      ambientLight: gl.getUniformLocation(this.program, "uAmbientLight"),
      dirLightDir: gl.getUniformLocation(this.program, "uDirLightDir"),
      dirLightColor: gl.getUniformLocation(this.program, "uDirLightColor"),
      eyePos: gl.getUniformLocation(this.program, "uEyePos"),
    };
  }

  compileShader(type, src) {
    const gl = this.gl;
    const shader = gl.createShader(type);
    gl.shaderSource(shader, src);
    gl.compileShader(shader);
    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
      console.error("Shader compilation error:", gl.getShaderInfoLog(shader));
    }
    return shader;
  }

  getBufferForGeometry(geo) {
    if (this.geometryBuffers.has(geo)) return this.geometryBuffers.get(geo);
    const gl = this.gl;
    const pBuf = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, pBuf);
    gl.bufferData(gl.ARRAY_BUFFER, geo.positions, gl.STATIC_DRAW);

    const nBuf = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, nBuf);
    gl.bufferData(gl.ARRAY_BUFFER, geo.normals, gl.STATIC_DRAW);

    const uBuf = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, uBuf);
    gl.bufferData(gl.ARRAY_BUFFER, geo.uvs, gl.STATIC_DRAW);

    const iBuf = gl.createBuffer();
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, iBuf);
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, geo.indices, gl.STATIC_DRAW);

    const record = { pBuf, nBuf, uBuf, iBuf, count: geo.indices.length };
    this.geometryBuffers.set(geo, record);
    return record;
  }

  createTextureFromCanvas(canvas) {
    const gl = this.gl;
    const tex = gl.createTexture();
    gl.bindTexture(gl.TEXTURE_2D, tex);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, canvas);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
    return tex;
  }

  updateTextureCanvas(tex, canvas) {
    const gl = this.gl;
    gl.bindTexture(gl.TEXTURE_2D, tex);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, canvas);
  }

  renderObject(obj, viewMatrix, projMatrix, eyePos) {
    if (!obj.visible || obj.isCulled) return;
    const gl = this.gl;

    if (obj.geometry && obj.material) {
      const bufs = this.getBufferForGeometry(obj.geometry);

      gl.bindBuffer(gl.ARRAY_BUFFER, bufs.pBuf);
      gl.vertexAttribPointer(this.attribs.position, 3, gl.FLOAT, false, 0, 0);
      gl.enableVertexAttribArray(this.attribs.position);

      gl.bindBuffer(gl.ARRAY_BUFFER, bufs.nBuf);
      gl.vertexAttribPointer(this.attribs.normal, 3, gl.FLOAT, false, 0, 0);
      gl.enableVertexAttribArray(this.attribs.normal);

      gl.bindBuffer(gl.ARRAY_BUFFER, bufs.uBuf);
      gl.vertexAttribPointer(this.attribs.uv, 2, gl.FLOAT, false, 0, 0);
      gl.enableVertexAttribArray(this.attribs.uv);

      gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, bufs.iBuf);

      gl.uniformMatrix4fv(this.uniforms.world, false, obj.worldMatrix);
      gl.uniformMatrix4fv(this.uniforms.view, false, viewMatrix);
      gl.uniformMatrix4fv(this.uniforms.proj, false, projMatrix);

      const mat = obj.material;
      gl.uniform4fv(this.uniforms.baseColor, mat.color || [1, 1, 1, 1]);
      gl.uniform3fv(this.uniforms.emission, mat.emission || [0, 0, 0]);
      gl.uniform1f(this.uniforms.metallic, mat.metallic ?? 0.1);
      gl.uniform1f(this.uniforms.roughness, mat.roughness ?? 0.5);

      if (mat.texture) {
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, mat.texture);
        gl.uniform1i(this.uniforms.sampler, 0);
        gl.uniform1i(this.uniforms.hasTexture, 1);
      } else {
        gl.uniform1i(this.uniforms.hasTexture, 0);
      }

      if (mat.transparent) {
        gl.enable(gl.BLEND);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
      } else {
        gl.disable(gl.BLEND);
      }

      gl.drawElements(gl.TRIANGLES, bufs.count, gl.UNSIGNED_SHORT, 0);
    }

    for (const child of obj.children) {
      this.renderObject(child, viewMatrix, projMatrix, eyePos);
    }
  }

  render(viewMatrix, projMatrix, eyePos, viewport = null) {
    const gl = this.gl;
    if (viewport) {
      gl.viewport(0, 0, this.canvas.width, this.canvas.height);
    }
    gl.clearColor(0.04, 0.06, 0.10, 1.0);
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);

    this.scene.updateMatrixWorld();

    gl.useProgram(this.program);
    gl.uniform3fv(this.uniforms.ambientLight, this.ambientLight);
    gl.uniform3fv(this.uniforms.dirLightDir, this.dirLightDir);
    gl.uniform3fv(this.uniforms.dirLightColor, this.dirLightColor);
    gl.uniform3fv(this.uniforms.eyePos, eyePos);

    this.renderObject(this.scene, viewMatrix, projMatrix, eyePos);
  }

  resize() {
    const width = window.innerWidth || this.canvas.clientWidth || 1920;
    const height = window.innerHeight || this.canvas.clientHeight || 1080;
    if (this.canvas.width !== width || this.canvas.height !== height) {
      this.canvas.width = width;
      this.canvas.height = height;
    }
  }
}

window.Math3D = Math3D;
window.SpatialObject = SpatialObject;
window.GeometryFactory = GeometryFactory;
window.SpatialRenderer = SpatialRenderer;
