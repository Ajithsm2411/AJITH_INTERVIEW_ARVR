/**
 * Professional VR Interview Telepresence Platform
 * True Face-to-Face Spatial Architecture (1.3m eye-level)
 */

(function () {
  const canvas = document.getElementById("gl-canvas");
  const renderer = new SpatialRenderer(canvas);

  // -------------------------------------------------------------
  // Data: Interview Questions, Candidates, Profiles
  // -------------------------------------------------------------
  const candidateProfile = {
    name: "Alex Mercer",
    role: "Senior Spatial Systems Engineer",
    experience: "8 Years",
    matchScore: 94,
    skills: ["WebXR", "Three.js", "OpenXR", "C# / Unity", "Real-Time Volumetric Video"],
    summary: "Proven background architecting low-latency 6-DoF telepresence pipelines and distributed XR runtime platforms."
  };

  const interviewerProfile = {
    name: "Dr. Sarah Vance",
    role: "VP of Spatial Engineering & Operating Systems",
    company: "MetaSphere Technologies",
    summary: "Leading spatial computing, neural telepresence, and next-generation WebXR infrastructure."
  };

  const interviewQuestions = [
    {
      id: 1,
      category: "Spatial Architecture",
      text: "How do you optimize 6-DoF tracking pipelines to maintain under 12ms motion-to-photon latency in multi-user VR?",
      rating: 0,
      interviewerVocal: "Welcome Alex. Could you start by explaining how you optimize 6-DoF tracking pipelines to maintain under 12ms motion-to-photon latency in multi-user VR?",
      candidateAnswer: "To hold 12ms round-trip, we decouple the render loop from physics updates, employ predictive pose extrapolation, and leverage asynchronous timewarp with foveated shading on the client."
    },
    {
      id: 2,
      category: "Network Telepresence",
      text: "Walk us through how you would architect real-time volumetric avatar streaming and spatial audio synchronization over WebRTC.",
      rating: 0,
      interviewerVocal: "That's a solid strategy. Now walk us through how you architect real-time volumetric avatar streaming and spatial audio synchronization over WebRTC.",
      candidateAnswer: "We stream compressed blendshape coefficients and 6-DoF joint matrices alongside Opus spatial audio packets, using deterministic NTP clocks for synchronized lip visemes."
    },
    {
      id: 3,
      category: "Graphics & Shaders",
      text: "How do you handle real-time spatial lighting, ambient occlusion, and transparent UI surfaces inside a WebXR browser context?",
      rating: 0,
      interviewerVocal: "How do you handle real-time spatial lighting, ambient occlusion, and transparent UI surfaces inside a WebXR browser context?",
      candidateAnswer: "We bake high-frequency static irradiance into lightmaps and run single-pass forward PBR shaders with depth-sorted translucent UI quads to sustain 90Hz frame rates."
    },
    {
      id: 4,
      category: "Systems & Scalability",
      text: "How would you scale this interview platform from 1-on-1 sessions to multi-panel executive boards with 8+ avatars?",
      rating: 0,
      interviewerVocal: "Excellent. How would you scale this interview platform from 1-on-1 sessions to multi-panel executive boards with 8 or more avatars?",
      candidateAnswer: "By adopting a selective forwarding unit architecture for WebRTC streams and dynamically LOD-culling non-speaking participants based on gaze direction."
    },
    {
      id: 5,
      category: "Culture & Problem Solving",
      text: "Describe a high-stakes technical disagreement on a spatial UX design and how you achieved team alignment.",
      rating: 0,
      interviewerVocal: "Lastly, describe a high-stakes technical disagreement on a spatial UX design and how you achieved team alignment.",
      candidateAnswer: "We had a dispute over raycast pointers versus direct hand pinching. I set up an A/B user study testing task completion time, which clearly validated hybrid laser-proximity interaction."
    }
  ];

  // -------------------------------------------------------------
  // Platform State
  // -------------------------------------------------------------
  const state = {
    role: "interviewer", // "interviewer" | "candidate"
    cameraPos: [0, 1.26, 0.65],
    cameraYaw: 180, // Facing -Z toward candidate
    cameraPitch: 0,
    activeQuestionIndex: 0,
    timerSeconds: 0,
    timerRunning: true,
    isMuted: false,
    cameraEnabled: true,
    notes: "Candidate demonstrates strong knowledge of predictive tracking and WebRTC audio-visual sync.\nClear explanations with precise terminology.",
    quickTags: ["Strong Knowledge", "Clear Architecture", "Fast Problem Solver", "Cultural Fit"],
    selectedTags: ["Strong Knowledge", "Clear Architecture"],
    
    // Networking
    ws: null,
    connected: false,
    peerCount: 0,
    isMultiplayer: false,
    
    // Audio / Voice
    audioCtx: null,
    analyser: null,
    micStream: null,
    localSpeaking: false,
    localAudioLevel: 0,
    oppositeSpeaking: false,
    oppositeAudioLevel: 0,
    
    // Avatar animations
    blinkTimer: 3.0,
    isBlinking: false,
    speechCycle: 0,
    lookTarget: [0, 1.22, -0.65],
    
    // In-VR Tablet
    tabletCanvas: null,
    tabletCtx: null,
    tabletTexture: null,
    tabletObject: null,
    
    // VR Controllers
    leftController: null,
    rightController: null,
  };

  // -------------------------------------------------------------
  // Build 3D Executive Boardroom Environment
  // -------------------------------------------------------------
  function buildInterviewRoom(scene) {
    const geoBox = GeometryFactory.createBox;
    const geoCyl = GeometryFactory.createCylinder;
    const geoSph = GeometryFactory.createSphere;
    const geoPlane = GeometryFactory.createPlane;

    // 1. Room Shell (7m width x 3.2m height x 6m depth)
    // Floor: Dark Walnut Parquet
    const floor = new SpatialObject("Room_Floor");
    floor.geometry = geoBox(7.2, 0.2, 6.2);
    floor.position = [0, -0.1, 0];
    floor.material = { color: [0.18, 0.14, 0.10, 1.0], roughness: 0.45, metallic: 0.15 };
    scene.add(floor);

    // Ceiling: Acoustic Tile with Recessed Lights
    const ceiling = new SpatialObject("Room_Ceiling");
    ceiling.geometry = geoBox(7.2, 0.2, 6.2);
    ceiling.position = [0, 3.2, 0];
    ceiling.material = { color: [0.82, 0.84, 0.86, 1.0], roughness: 0.9, metallic: 0.0 };
    scene.add(ceiling);

    // Ceiling LED Light Panels (4 panels)
    const lightPositions = [[-1.4, 3.12, 0.8], [1.4, 3.12, 0.8], [-1.4, 3.12, -0.8], [1.4, 3.12, -0.8]];
    lightPositions.forEach((pos, idx) => {
      const panel = new SpatialObject(`LED_Panel_${idx}`);
      panel.geometry = geoBox(1.2, 0.04, 0.5);
      panel.position = pos;
      panel.material = { color: [1.0, 0.98, 0.92, 1.0], emission: [0.95, 0.92, 0.85], roughness: 0.2 };
      scene.add(panel);
    });

    // Back Wall (Behind Candidate at Z = -3.0m): Slate Grey + Wood Slats
    const backWall = new SpatialObject("Wall_Candidate_Back");
    backWall.geometry = geoBox(7.2, 3.2, 0.15);
    backWall.position = [0, 1.5, -3.05];
    backWall.material = { color: [0.15, 0.18, 0.24, 1.0], roughness: 0.85 };
    scene.add(backWall);

    // Decorative Wood Slats
    for (let x = -2.8; x <= 2.8; x += 0.28) {
      const slat = new SpatialObject(`WoodSlat_${x}`);
      slat.geometry = geoBox(0.07, 3.1, 0.04);
      slat.position = [x, 1.5, -2.98];
      slat.material = { color: [0.38, 0.24, 0.14, 1.0], roughness: 0.5, metallic: 0.1 };
      scene.add(slat);
    }

    // Front Wall (Behind Interviewer at Z = +3.0m): Executive Skyline Window
    const frontWallLeft = new SpatialObject("Front_Wall_Left");
    frontWallLeft.geometry = geoBox(0.8, 3.2, 0.15);
    frontWallLeft.position = [-3.2, 1.5, 3.05];
    frontWallLeft.material = { color: [0.15, 0.18, 0.24, 1.0], roughness: 0.85 };
    scene.add(frontWallLeft);

    const frontWallRight = new SpatialObject("Front_Wall_Right");
    frontWallRight.geometry = geoBox(0.8, 3.2, 0.15);
    frontWallRight.position = [3.2, 1.5, 3.05];
    frontWallRight.material = { color: [0.15, 0.18, 0.24, 1.0], roughness: 0.85 };
    scene.add(frontWallRight);

    // Skyline Window Glass
    const windowGlass = new SpatialObject("Skyline_Window");
    windowGlass.geometry = geoBox(5.6, 2.9, 0.05);
    windowGlass.position = [0, 1.5, 3.0];
    windowGlass.material = { color: [0.25, 0.45, 0.65, 0.35], transparent: true, roughness: 0.05, metallic: 0.9 };
    scene.add(windowGlass);

    // City Skyline Backdrop behind window
    const skylineBackdrop = new SpatialObject("Skyline_Backdrop");
    skylineBackdrop.geometry = geoBox(7.0, 3.2, 0.05);
    skylineBackdrop.position = [0, 1.6, 4.2];
    skylineBackdrop.material = { color: [0.12, 0.18, 0.32, 1.0], emission: [0.08, 0.14, 0.24], roughness: 0.9 };
    scene.add(skylineBackdrop);

    // Distant skyscraper silhouettes
    const towers = [[-2.2, 1.4, 0.8, 2.4], [-1.0, 1.8, 0.7, 3.0], [0.4, 1.2, 0.9, 2.0], [1.8, 1.7, 0.6, 2.8], [2.7, 1.1, 0.8, 1.8]];
    towers.forEach(([tx, ty, tw, th], idx) => {
      const tower = new SpatialObject(`Skyline_Tower_${idx}`);
      tower.geometry = geoBox(tw, th, 0.2);
      tower.position = [tx, ty, 4.0];
      tower.material = { color: [0.08, 0.12, 0.22, 1.0], emission: [0.05, 0.09, 0.16] };
      scene.add(tower);
    });

    // Side Walls (Left and Right)
    const leftWall = new SpatialObject("Wall_Left");
    leftWall.geometry = geoBox(0.15, 3.2, 6.2);
    leftWall.position = [-3.6, 1.5, 0];
    leftWall.material = { color: [0.85, 0.87, 0.90, 1.0], roughness: 0.8 };
    scene.add(leftWall);

    const rightWall = new SpatialObject("Wall_Right");
    rightWall.geometry = geoBox(0.15, 3.2, 6.2);
    rightWall.position = [3.6, 1.5, 0];
    rightWall.material = { color: [0.85, 0.87, 0.90, 1.0], roughness: 0.8 };
    scene.add(rightWall);

    // Office Door & Title Plaque
    const officeDoor = new SpatialObject("Office_Door");
    officeDoor.geometry = geoBox(0.06, 2.3, 1.1);
    officeDoor.position = [3.52, 1.15, 1.8];
    officeDoor.material = { color: [0.38, 0.24, 0.14, 1.0], roughness: 0.4, metallic: 0.1 };
    scene.add(officeDoor);

    const doorPlaque = new SpatialObject("Office_Title_Plaque");
    doorPlaque.geometry = geoBox(0.04, 0.22, 0.8);
    doorPlaque.position = [3.49, 1.85, 1.8];
    doorPlaque.material = { color: [0.08, 0.12, 0.18, 1.0], emission: [0.0, 0.4, 0.8], roughness: 0.2, metallic: 0.9 };
    scene.add(doorPlaque);

    // Executive Presentation Screen on Left Wall
    const smartScreen = new SpatialObject("Presentation_SmartScreen");
    smartScreen.geometry = geoBox(0.04, 1.25, 2.3);
    smartScreen.position = [-3.52, 1.7, 0];
    smartScreen.material = { color: [0.04, 0.08, 0.16, 1.0], emission: [0.02, 0.12, 0.22], roughness: 0.1, metallic: 0.8 };
    scene.add(smartScreen);

    // 2. Executive Conference Table (1.1m width x 2.2m length, height 0.73m)
    // Centered at [0, 0, 0]
    const tableRoot = new SpatialObject("Interview_Table");
    tableRoot.position = [0, 0, 0];
    scene.add(tableRoot);

    // Walnut Tabletop
    const tabletop = new SpatialObject("Tabletop_Walnut");
    tabletop.geometry = geoBox(1.1, 0.05, 2.1);
    tabletop.position = [0, 0.73, 0];
    tabletop.material = { color: [0.26, 0.16, 0.10, 1.0], roughness: 0.35, metallic: 0.15 };
    tableRoot.add(tabletop);

    // Brushed Chrome Edge Trim
    const tableTrim = new SpatialObject("Tabletop_Trim");
    tableTrim.geometry = geoBox(1.12, 0.03, 2.12);
    tableTrim.position = [0, 0.71, 0];
    tableTrim.material = { color: [0.72, 0.74, 0.78, 1.0], roughness: 0.25, metallic: 0.9 };
    tableRoot.add(tableTrim);

    // Center Inlay / Cable Trough
    const tableInlay = new SpatialObject("Table_Inlay");
    tableInlay.geometry = geoBox(0.24, 0.005, 1.3);
    tableInlay.position = [0, 0.756, 0];
    tableInlay.material = { color: [0.12, 0.13, 0.15, 1.0], roughness: 0.4, metallic: 0.85 };
    tableRoot.add(tableInlay);

    // Chrome Table Legs & Base (4 legs)
    const legCoords = [[-0.45, -0.75], [0.45, -0.75], [-0.45, 0.75], [0.45, 0.75]];
    legCoords.forEach(([lx, lz], idx) => {
      const leg = new SpatialObject(`Table_Leg_${idx}`);
      leg.geometry = geoBox(0.06, 0.70, 0.06);
      leg.position = [lx, 0.35, lz];
      leg.material = { color: [0.80, 0.82, 0.85, 1.0], roughness: 0.2, metallic: 0.95 };
      tableRoot.add(leg);
    });

    // 3. Ergonomic Chairs
    // Interviewer Chair at [0, 0, 0.65] facing candidate (-Z, yaw 180°)
    buildExecutiveChair(scene, [0, 0, 0.65], 180, "Interviewer_Chair");

    // Candidate Chair at [0, 0, -0.65] facing interviewer (+Z, yaw 0°)
    buildExecutiveChair(scene, [0, 0, -0.65], 0, "Candidate_Chair");

    // 4. Table Props
    buildTableProps(scene);
  }

  function buildExecutiveChair(parent, position, yawDeg, name) {
    const geoBox = GeometryFactory.createBox;
    const geoCyl = GeometryFactory.createCylinder;

    const chair = new SpatialObject(name);
    chair.position = position;
    chair.setEuler(0, yawDeg, 0);
    parent.add(chair);

    const matFabric = { color: [0.12, 0.13, 0.15, 1.0], roughness: 0.6 };
    const matChrome = { color: [0.85, 0.87, 0.90, 1.0], roughness: 0.15, metallic: 0.95 };

    // Chrome Base Stem
    const stem = new SpatialObject("Stem");
    stem.geometry = geoCyl(0.04, 0.30, 12);
    stem.position = [0, 0.20, 0];
    stem.material = matChrome;
    chair.add(stem);

    // Seat Cushion (height ~ 0.46m)
    const seat = new SpatialObject("Seat");
    seat.geometry = geoBox(0.52, 0.08, 0.50);
    seat.position = [0, 0.46, 0];
    seat.material = matFabric;
    chair.add(seat);

    // Ergonomic Backrest
    const back = new SpatialObject("Backrest");
    back.geometry = geoBox(0.48, 0.65, 0.07);
    back.position = [0, 0.82, -0.23];
    back.setEuler(6, 0, 0);
    back.material = matFabric;
    chair.add(back);

    // Headrest
    const headrest = new SpatialObject("Headrest");
    headrest.geometry = geoBox(0.28, 0.14, 0.08);
    headrest.position = [0, 1.20, -0.27];
    headrest.material = matFabric;
    chair.add(headrest);

    // Armrests
    [-0.28, 0.28].forEach((ax, idx) => {
      const arm = new SpatialObject(`Armrest_${idx}`);
      arm.geometry = geoBox(0.06, 0.03, 0.32);
      arm.position = [ax, 0.65, -0.05];
      arm.material = matFabric;
      chair.add(arm);

      const pillar = new SpatialObject(`Arm_Pillar_${idx}`);
      pillar.geometry = geoCyl(0.02, 0.15, 8);
      pillar.position = [ax, 0.55, -0.05];
      pillar.material = matChrome;
      chair.add(pillar);
    });

    return chair;
  }

  function buildTableProps(parent) {
    const geoBox = GeometryFactory.createBox;
    const geoCyl = GeometryFactory.createCylinder;

    const props = new SpatialObject("Table_Props");
    props.position = [0, 0.755, 0];
    parent.add(props);

    // 1. Interviewer Laptop (at z = 0.42m)
    const laptop = new SpatialObject("Interviewer_Laptop");
    laptop.position = [-0.18, 0, 0.42];
    props.add(laptop);

    const laptopBase = new SpatialObject("Laptop_Base");
    laptopBase.geometry = geoBox(0.32, 0.012, 0.22);
    laptopBase.position = [0, 0.006, 0];
    laptopBase.material = { color: [0.22, 0.24, 0.26, 1.0], roughness: 0.3, metallic: 0.85 };
    laptop.add(laptopBase);

    const laptopScreen = new SpatialObject("Laptop_Screen");
    laptopScreen.geometry = geoBox(0.32, 0.21, 0.008);
    laptopScreen.position = [0, 0.11, -0.09];
    laptopScreen.setEuler(-22, 0, 0);
    laptopScreen.material = { color: [0.08, 0.14, 0.25, 1.0], emission: [0.15, 0.35, 0.65], roughness: 0.2 };
    laptop.add(laptopScreen);

    // 2. Coffee Mug (Interviewer) & Water Tumbler (Candidate)
    const mug = new SpatialObject("Coffee_Mug");
    mug.geometry = geoCyl(0.042, 0.09, 12);
    mug.position = [-0.42, 0.045, 0.42];
    mug.material = { color: [0.92, 0.94, 0.96, 1.0], roughness: 0.2 };
    props.add(mug);

    const waterGlass = new SpatialObject("Water_Tumbler");
    waterGlass.geometry = geoCyl(0.038, 0.11, 12);
    waterGlass.position = [0.38, 0.055, -0.42];
    waterGlass.material = { color: [0.7, 0.85, 0.95, 0.4], transparent: true, roughness: 0.05, metallic: 0.8 };
    props.add(waterGlass);

    // 3. Central Conference Speakerphone Puck
    const puck = new SpatialObject("Audio_Puck");
    puck.geometry = geoCyl(0.09, 0.02, 16);
    puck.position = [0, 0.01, 0];
    puck.material = { color: [0.15, 0.16, 0.18, 1.0], roughness: 0.4, metallic: 0.8 };
    props.add(puck);

    // Illuminated Status Halo Ring on puck
    const puckHalo = new SpatialObject("Puck_Status_Halo");
    puckHalo.geometry = geoCyl(0.04, 0.008, 16);
    puckHalo.position = [0, 0.022, 0];
    puckHalo.material = { color: [0.0, 0.9, 1.0, 1.0], emission: [0.0, 0.85, 1.0] };
    puck.add(puckHalo);
  }

  // -------------------------------------------------------------
  // Build 3D Avatars (Interviewer: Dr. Sarah Vance | Candidate: Alex Mercer)
  // -------------------------------------------------------------
  let interviewerAvatarNode = null;
  let candidateAvatarNode = null;

  function buildAvatars(scene) {
    const geoBox = GeometryFactory.createBox;
    const geoCyl = GeometryFactory.createCylinder;
    const geoSph = GeometryFactory.createSphere;

    // ── INTERVIEWER AVATAR (Dr. Sarah Vance) ──
    // Seated at z = +0.65m, facing -Z (yaw 180° toward candidate)
    interviewerAvatarNode = new SpatialObject("Avatar_Interviewer");
    interviewerAvatarNode.position = [0, 0, 0.65];
    interviewerAvatarNode.setEuler(0, 180, 0);
    scene.add(interviewerAvatarNode);

    const matSuitNavy = { color: [0.10, 0.12, 0.18, 1.0], roughness: 0.6 };
    const matShirtWhite = { color: [0.94, 0.95, 0.96, 1.0], roughness: 0.5 };
    const matTieSilk = { color: [0.65, 0.15, 0.20, 1.0], roughness: 0.35, metallic: 0.2 };
    const matSkinF = { color: [0.94, 0.78, 0.68, 1.0], roughness: 0.5 };
    const matHairAuburn = { color: [0.35, 0.22, 0.16, 1.0], roughness: 0.6 };
    const matGlasses = { color: [0.85, 0.85, 0.88, 1.0], roughness: 0.1, metallic: 0.95 };

    // Legs (seated)
    const intHips = new SpatialObject("Int_Hips");
    intHips.geometry = geoBox(0.36, 0.16, 0.28);
    intHips.position = [0, 0.50, -0.05];
    intHips.material = matSuitNavy;
    interviewerAvatarNode.add(intHips);

    [-0.11, 0.11].forEach((lx, idx) => {
      const thigh = new SpatialObject(`Int_Thigh_${idx}`);
      thigh.geometry = geoBox(0.13, 0.12, 0.38);
      thigh.position = [lx, 0.47, 0.16];
      thigh.material = matSuitNavy;
      interviewerAvatarNode.add(thigh);

      const shin = new SpatialObject(`Int_Shin_${idx}`);
      shin.geometry = geoBox(0.12, 0.36, 0.12);
      shin.position = [lx, 0.20, 0.34];
      shin.material = matSuitNavy;
      interviewerAvatarNode.add(shin);
    });

    // Torso / Chest (Breathing parent)
    const intChest = new SpatialObject("Int_Chest");
    intChest.geometry = geoBox(0.38, 0.44, 0.24);
    intChest.position = [0, 0.80, -0.02];
    intChest.material = matSuitNavy;
    interviewerAvatarNode.add(intChest);
    interviewerAvatarNode.chest = intChest;

    // Shirt & Tie
    const intCollar = new SpatialObject("Int_Collar");
    intCollar.geometry = geoBox(0.14, 0.18, 0.012);
    intCollar.position = [0, 0.12, 0.124];
    intCollar.material = matShirtWhite;
    intChest.add(intCollar);

    const intTie = new SpatialObject("Int_Tie");
    intTie.geometry = geoBox(0.055, 0.28, 0.010);
    intTie.position = [0, -0.03, 0.130];
    intTie.material = matTieSilk;
    intChest.add(intTie);

    // Head Group (articulated)
    const intHead = new SpatialObject("Int_Head");
    intHead.position = [0, 1.22, -0.01];
    interviewerAvatarNode.add(intHead);
    interviewerAvatarNode.head = intHead;

    const intFace = new SpatialObject("Int_Face");
    intFace.geometry = geoSph(0.115, 12, 12);
    intFace.position = [0, 0, 0];
    intFace.scale = [1.0, 1.15, 1.0];
    intFace.material = matSkinF;
    intHead.add(intFace);

    const intHair = new SpatialObject("Int_Hair");
    intHair.geometry = geoSph(0.125, 12, 12);
    intHair.position = [0, 0.04, -0.02];
    intHair.material = matHairAuburn;
    intHead.add(intHair);

    // Eyes
    [-0.048, 0.048].forEach((ex, idx) => {
      const eye = new SpatialObject(`Int_Eye_${idx}`);
      eye.geometry = geoSph(0.015, 8, 8);
      eye.position = [ex, 0.025, 0.105];
      eye.material = { color: [0.15, 0.12, 0.10, 1.0] };
      intHead.add(eye);
    });

    // Eyeglasses
    const glassesBridge = new SpatialObject("Int_Glasses_Bridge");
    glassesBridge.geometry = geoBox(0.13, 0.025, 0.015);
    glassesBridge.position = [0, 0.028, 0.118];
    glassesBridge.material = matGlasses;
    intHead.add(glassesBridge);

    // Mouth / Jaw (Animated Lip Sync)
    const intMouth = new SpatialObject("Int_Mouth");
    intMouth.geometry = geoBox(0.045, 0.012, 0.012);
    intMouth.position = [0, -0.045, 0.112];
    intMouth.material = { color: [0.65, 0.25, 0.28, 1.0] };
    intHead.add(intMouth);
    interviewerAvatarNode.mouth = intMouth;

    // Arms & Hands
    const intArmL = new SpatialObject("Int_Arm_L");
    intArmL.geometry = geoBox(0.10, 0.10, 0.32);
    intArmL.position = [-0.22, 0.74, 0.20];
    intArmL.setEuler(12, 10, 0);
    intArmL.material = matSuitNavy;
    interviewerAvatarNode.add(intArmL);

    const intArmR = new SpatialObject("Int_Arm_R");
    intArmR.geometry = geoBox(0.10, 0.10, 0.32);
    intArmR.position = [0.22, 0.74, 0.20];
    intArmR.setEuler(16, -12, 0);
    intArmR.material = matSuitNavy;
    interviewerAvatarNode.add(intArmR);
    interviewerAvatarNode.armR = intArmR;


    // ── CANDIDATE AVATAR (Alex Mercer) ──
    // Seated at z = -0.65m, facing +Z (yaw 0° toward interviewer)
    candidateAvatarNode = new SpatialObject("Avatar_Candidate");
    candidateAvatarNode.position = [0, 0, -0.65];
    candidateAvatarNode.setEuler(0, 0, 0);
    scene.add(candidateAvatarNode);

    const matHoodieCharcoal = { color: [0.18, 0.20, 0.24, 1.0], roughness: 0.7 };
    const matVisorCyan = { color: [0.0, 0.85, 1.0, 1.0], emission: [0.0, 0.8, 1.0], roughness: 0.1 };
    const matVRChassis = { color: [0.88, 0.90, 0.94, 1.0], roughness: 0.3, metallic: 0.6 };
    const matSkinM = { color: [0.92, 0.76, 0.65, 1.0], roughness: 0.5 };

    // Legs
    const candHips = new SpatialObject("Cand_Hips");
    candHips.geometry = geoBox(0.36, 0.16, 0.28);
    candHips.position = [0, 0.50, -0.05];
    candHips.material = matHoodieCharcoal;
    candidateAvatarNode.add(candHips);

    [-0.11, 0.11].forEach((lx, idx) => {
      const thigh = new SpatialObject(`Cand_Thigh_${idx}`);
      thigh.geometry = geoBox(0.13, 0.12, 0.38);
      thigh.position = [lx, 0.47, 0.16];
      thigh.material = matHoodieCharcoal;
      candidateAvatarNode.add(thigh);

      const shin = new SpatialObject(`Cand_Shin_${idx}`);
      shin.geometry = geoBox(0.12, 0.36, 0.12);
      shin.position = [lx, 0.20, 0.34];
      shin.material = matHoodieCharcoal;
      candidateAvatarNode.add(shin);
    });

    // Torso (Candidate)
    const candChest = new SpatialObject("Cand_Chest");
    candChest.geometry = geoBox(0.40, 0.44, 0.24);
    candChest.position = [0, 0.80, -0.02];
    candChest.material = matHoodieCharcoal;
    candidateAvatarNode.add(candChest);
    candidateAvatarNode.chest = candChest;

    // Head Group (Candidate)
    const candHead = new SpatialObject("Cand_Head");
    candHead.position = [0, 1.22, -0.01];
    candidateAvatarNode.add(candHead);
    candidateAvatarNode.head = candHead;

    const candFace = new SpatialObject("Cand_Face");
    candFace.geometry = geoSph(0.115, 12, 12);
    candFace.position = [0, 0, 0];
    candFace.scale = [1.0, 1.15, 1.0];
    candFace.material = matSkinM;
    candHead.add(candFace);

    // Spatial Computing AR/VR Headset on Candidate
    const headsetChassis = new SpatialObject("Cand_VR_Headset");
    headsetChassis.geometry = geoBox(0.24, 0.10, 0.08);
    headsetChassis.position = [0, 0.03, 0.08];
    headsetChassis.material = matVRChassis;
    candHead.add(headsetChassis);

    // Glowing Cyan Visor
    const headsetVisor = new SpatialObject("Cand_Visor");
    headsetVisor.geometry = geoBox(0.22, 0.07, 0.015);
    headsetVisor.position = [0, 0.03, 0.125];
    headsetVisor.material = matVisorCyan;
    candHead.add(headsetVisor);

    // Candidate Mouth (Lip Sync viseme)
    const candMouth = new SpatialObject("Cand_Mouth");
    candMouth.geometry = geoBox(0.045, 0.012, 0.012);
    candMouth.position = [0, -0.05, 0.11];
    candMouth.material = { color: [0.65, 0.25, 0.28, 1.0] };
    candHead.add(candMouth);
    candidateAvatarNode.mouth = candMouth;

    // Candidate Arms
    const candArmL = new SpatialObject("Cand_Arm_L");
    candArmL.geometry = geoBox(0.10, 0.10, 0.32);
    candArmL.position = [-0.22, 0.74, 0.20];
    candArmL.setEuler(10, 8, 0);
    candArmL.material = matHoodieCharcoal;
    candidateAvatarNode.add(candArmL);

    const candArmR = new SpatialObject("Cand_Arm_R");
    candArmR.geometry = geoBox(0.10, 0.10, 0.32);
    candArmR.position = [0.22, 0.74, 0.20];
    candArmR.setEuler(14, -10, 0);
    candArmR.material = matHoodieCharcoal;
    candidateAvatarNode.add(candArmR);
    candidateAvatarNode.armR = candArmR;
  }

  // -------------------------------------------------------------
  // In-VR Responsive Spatial UI (3D Interactive Floating Tablet)
  // -------------------------------------------------------------
  function buildSpatialTablet(scene) {
    const geoPlane = GeometryFactory.createPlane;
    const geoBox = GeometryFactory.createBox;

    const tabletRoot = new SpatialObject("VR_Spatial_Tablet");
    scene.add(tabletRoot);
    state.tabletObject = tabletRoot;

    // Create 2D offscreen canvas for rendering crisp in-VR UI
    const tCanvas = document.createElement("canvas");
    tCanvas.width = 1024;
    tCanvas.height = 640;
    state.tabletCanvas = tCanvas;
    state.tabletCtx = tCanvas.getContext("2d");

    // Tablet Texture
    state.tabletTexture = renderer.createTextureFromCanvas(tCanvas);

    // 3D Glass Tablet Display Quad (0.64m width x 0.40m height)
    const displayQuad = new SpatialObject("Tablet_Display_Quad");
    displayQuad.geometry = geoPlane(0.64, 0.40);
    displayQuad.position = [0, 0, 0.005];
    displayQuad.material = {
      color: [1, 1, 1, 1],
      texture: state.tabletTexture,
      roughness: 0.2,
      metallic: 0.1
    };
    tabletRoot.add(displayQuad);

    // Tablet Bezel Frame
    const frame = new SpatialObject("Tablet_Bezel");
    frame.geometry = geoBox(0.67, 0.43, 0.015);
    frame.position = [0, 0, 0];
    frame.material = { color: [0.08, 0.10, 0.14, 1.0], roughness: 0.3, metallic: 0.85 };
    tabletRoot.add(frame);

    updateTabletPlacement();
  }

  function updateTabletPlacement() {
    if (!state.tabletObject) return;
    if (state.role === "interviewer") {
      // Resting on table in front of interviewer, tilted up 32 degrees toward eyes
      state.tabletObject.position = [0.22, 0.82, 0.32];
      state.tabletObject.setEuler(32, 195, 0);
    } else {
      // Resting on table in front of candidate, tilted up 32 degrees toward eyes
      state.tabletObject.position = [-0.22, 0.82, -0.32];
      state.tabletObject.setEuler(32, 15, 0);
    }
  }

  function renderTabletUI() {
    const ctx = state.tabletCtx;
    if (!ctx) return;

    // Clear background: Sleek Glassmorphism Dark Navy
    ctx.fillStyle = "#0c1220";
    ctx.fillRect(0, 0, 1024, 640);

    // Glowing border frame
    ctx.strokeStyle = state.role === "interviewer" ? "rgba(0, 229, 255, 0.4)" : "rgba(16, 185, 129, 0.4)";
    ctx.lineWidth = 4;
    ctx.strokeRect(6, 6, 1012, 628);

    // Header Bar
    ctx.fillStyle = "rgba(18, 28, 48, 0.95)";
    ctx.fillRect(10, 10, 1004, 70);

    // Role Indicator Badge
    ctx.font = "bold 20px -apple-system, BlinkMacSystemFont, sans-serif";
    ctx.fillStyle = state.role === "interviewer" ? "#00e5ff" : "#10b981";
    const roleText = state.role === "interviewer" ? "● INTERVIEWER VIEW — EXECUTIVE PANEL" : "● CANDIDATE VIEW — SPATIAL TELEPRESENCE";
    ctx.fillText(roleText, 30, 52);

    // Live Timer in Header
    const mins = Math.floor(state.timerSeconds / 60).toString().padStart(2, "0");
    const secs = (state.timerSeconds % 60).toString().padStart(2, "0");
    ctx.font = "bold 22px SF Mono, monospace";
    ctx.fillStyle = "#ffffff";
    ctx.fillText(`SESSION TIMER: ${mins}:${secs}`, 680, 52);

    if (state.role === "interviewer") {
      // ── INTERVIEWER TABLET UI ──
      // Left Column: Candidate Profile & Rubric
      ctx.fillStyle = "rgba(24, 36, 60, 0.8)";
      ctx.fillRect(24, 100, 460, 210);

      ctx.fillStyle = "#00e5ff";
      ctx.font = "bold 16px sans-serif";
      ctx.fillText("CANDIDATE DOSSIER", 44, 130);

      ctx.font = "bold 22px sans-serif";
      ctx.fillStyle = "#ffffff";
      ctx.fillText(candidateProfile.name, 44, 165);

      ctx.font = "16px sans-serif";
      ctx.fillStyle = "#94a3b8";
      ctx.fillText(candidateProfile.role, 44, 195);
      ctx.fillText(`Experience: ${candidateProfile.experience}  |  Match Index: ${candidateProfile.matchScore}%`, 44, 225);

      // Skills chips
      let chipX = 44;
      ctx.font = "13px sans-serif";
      candidateProfile.skills.slice(0, 4).forEach((skill) => {
        ctx.fillStyle = "rgba(0, 229, 255, 0.15)";
        ctx.fillRect(chipX, 245, skill.length * 8 + 18, 26);
        ctx.fillStyle = "#00e5ff";
        ctx.fillText(skill, chipX + 9, 263);
        chipX += skill.length * 8 + 26;
      });

      // Right Column: Active Question & Rating
      const q = interviewQuestions[state.activeQuestionIndex];
      ctx.fillStyle = "rgba(24, 36, 60, 0.8)";
      ctx.fillRect(500, 100, 500, 210);

      ctx.fillStyle = "#f59e0b";
      ctx.font = "bold 15px sans-serif";
      ctx.fillText(`QUESTION ${state.activeQuestionIndex + 1} OF ${interviewQuestions.length} — ${q.category}`, 520, 130);

      ctx.font = "17px sans-serif";
      ctx.fillStyle = "#f8fafc";
      wrapText(ctx, q.text, 520, 165, 460, 24);

      // Rating Stars
      ctx.font = "24px sans-serif";
      for (let s = 1; s <= 5; s++) {
        ctx.fillStyle = s <= q.rating ? "#fbbf24" : "#475569";
        ctx.fillText("★", 520 + (s - 1) * 35, 290);
      }
      ctx.font = "14px sans-serif";
      ctx.fillStyle = "#94a3b8";
      ctx.fillText(`Rating: ${q.rating}/5`, 710, 288);

      // Bottom Section: Notes & Audio Status
      ctx.fillStyle = "rgba(24, 36, 60, 0.8)";
      ctx.fillRect(24, 325, 976, 200);

      ctx.fillStyle = "#00e5ff";
      ctx.font = "bold 15px sans-serif";
      ctx.fillText("EVALUATION NOTES & KEY SIGNALS", 44, 355);

      ctx.font = "15px SF Mono, monospace";
      ctx.fillStyle = "#e2e8f0";
      wrapText(ctx, state.notes, 44, 385, 930, 22);

      // Bottom Control Buttons Bar
      ctx.fillStyle = state.isMuted ? "rgba(244, 63, 94, 0.3)" : "rgba(16, 185, 129, 0.3)";
      ctx.fillRect(24, 545, 180, 50);
      ctx.fillStyle = "#ffffff";
      ctx.font = "bold 16px sans-serif";
      ctx.fillText(state.isMuted ? "🎤 MIC: MUTED" : "🎤 MIC: ACTIVE", 44, 576);

      ctx.fillStyle = "rgba(37, 99, 235, 0.4)";
      ctx.fillRect(220, 545, 230, 50);
      ctx.fillStyle = "#ffffff";
      ctx.fillText("▶ NEXT QUESTION", 244, 576);

      ctx.fillStyle = "rgba(220, 38, 38, 0.4)";
      ctx.fillRect(470, 545, 220, 50);
      ctx.fillStyle = "#ffffff";
      ctx.fillText("⏹ END INTERVIEW", 494, 576);

    } else {
      // ── CANDIDATE TABLET UI ──
      // Top Half: Interviewer Profile & Current Question
      ctx.fillStyle = "rgba(24, 36, 60, 0.8)";
      ctx.fillRect(24, 100, 976, 210);

      ctx.fillStyle = "#10b981";
      ctx.font = "bold 16px sans-serif";
      ctx.fillText("INTERVIEWER: DR. SARAH VANCE", 44, 130);

      ctx.font = "bold 20px sans-serif";
      ctx.fillStyle = "#ffffff";
      const curQ = interviewQuestions[state.activeQuestionIndex];
      wrapText(ctx, `Q: "${curQ.text}"`, 44, 175, 930, 28);

      // Bottom Half: Candidate Scratchpad & Confidence Telemetry
      ctx.fillStyle = "rgba(24, 36, 60, 0.8)";
      ctx.fillRect(24, 325, 976, 200);

      ctx.fillStyle = "#10b981";
      ctx.font = "bold 16px sans-serif";
      ctx.fillText("CANDIDATE RESPONSE SCRATCHPAD", 44, 355);

      ctx.font = "15px SF Mono, monospace";
      ctx.fillStyle = "#e2e8f0";
      wrapText(ctx, "Key Architectural Points:\n• Decouple tracking loops to prevent framerate jitter\n• Predictive pose extrapolation with async timewarp\n• Synchronized blendshapes for audio visemes", 44, 390, 930, 24);

      // Mic & Status bar
      ctx.fillStyle = state.isMuted ? "rgba(244, 63, 94, 0.3)" : "rgba(16, 185, 129, 0.3)";
      ctx.fillRect(24, 545, 180, 50);
      ctx.fillStyle = "#ffffff";
      ctx.font = "bold 16px sans-serif";
      ctx.fillText(state.isMuted ? "🎤 MIC: MUTED" : "🎤 MIC: ACTIVE", 44, 576);

      ctx.fillStyle = "rgba(16, 185, 129, 0.4)";
      ctx.fillRect(220, 545, 300, 50);
      ctx.fillStyle = "#ffffff";
      ctx.fillText("💬 SPEAKING ACTIVITY: OK", 244, 576);
    }

    renderer.updateTextureCanvas(state.tabletTexture, state.tabletCanvas);
  }

  function wrapText(ctx, text, x, y, maxWidth, lineHeight) {
    const lines = text.split("\n");
    for (const rawLine of lines) {
      const words = rawLine.split(" ");
      let line = "";
      for (let n = 0; n < words.length; n++) {
        const testLine = line + words[n] + " ";
        const metrics = ctx.measureText(testLine);
        if (metrics.width > maxWidth && n > 0) {
          ctx.fillText(line, x, y);
          line = words[n] + " ";
          y += lineHeight;
        } else {
          line = testLine;
        }
      }
      ctx.fillText(line, x, y);
      y += lineHeight;
    }
  }

  // -------------------------------------------------------------
  // Role Selector & True Face-to-Face Perspective Switching
  // -------------------------------------------------------------
  function setRole(newRole, smooth = true) {
    state.role = newRole;

    if (newRole === "interviewer") {
      // User is seated in Interviewer Chair
      // Eye level: 1.26m, Z = 0.65m, Yaw = 180° (facing candidate at -Z)
      state.cameraPos = [0, 1.26, 0.65];
      state.cameraYaw = 180;
      state.cameraPitch = 0;
      state.lookTarget = [0, 1.26, -0.65];

      // HEAD CULLING: Interviewer does not clip into their own head/suit
      if (interviewerAvatarNode) interviewerAvatarNode.isCulled = true;
      if (candidateAvatarNode) candidateAvatarNode.isCulled = false;

    } else {
      // User is seated in Candidate Chair
      // Eye level: 1.26m, Z = -0.65m, Yaw = 0° (facing interviewer at +Z)
      state.cameraPos = [0, 1.26, -0.65];
      state.cameraYaw = 0;
      state.cameraPitch = 0;
      state.lookTarget = [0, 1.26, 0.65];

      // HEAD CULLING: Candidate does not clip into their own head/hoodie
      if (interviewerAvatarNode) interviewerAvatarNode.isCulled = false;
      if (candidateAvatarNode) candidateAvatarNode.isCulled = true;
    }

    updateTabletPlacement();
    updateHTMLUI();
    showNotification(`Switched perspective to: ${newRole.toUpperCase()} VIEW`);

    // Broadcast role change to WebSocket peers
    if (state.ws && state.ws.readyState === WebSocket.OPEN) {
      state.ws.send(JSON.stringify({
        type: "role_switch",
        role: state.role
      }));
    }
  }

  // -------------------------------------------------------------
  // Web Audio & Positional Spatial Lip Sync
  // -------------------------------------------------------------
  function initAudioSystem() {
    try {
      const AudioCtx = window.AudioContext || window.webkitAudioContext;
      state.audioCtx = new AudioCtx();

      // Request microphone access for real-time speech viseme lip-sync
      navigator.mediaDevices.getUserMedia({ audio: true, video: false })
        .then((stream) => {
          state.micStream = stream;
          const source = state.audioCtx.createMediaStreamSource(stream);
          state.analyser = state.audioCtx.createAnalyser();
          state.analyser.fftSize = 256;
          source.connect(state.analyser);
          console.log("[Audio] Microphone stream connected for real-time speech lip sync.");
        })
        .catch((err) => {
          console.warn("[Audio] Microphone permission optional/not granted. Using simulated vocal synthesizer:", err);
        });
    } catch (e) {
      console.warn("[Audio] Web Audio initialization:", e);
    }
  }

  function triggerSyntheticDialogue() {
    const q = interviewQuestions[state.activeQuestionIndex];

    if (state.role === "interviewer") {
      // Interviewer triggers candidate answer
      state.oppositeSpeaking = true;
      showNotification("Alex Mercer (Candidate) is speaking...");
      speakTextSynthetic(q.candidateAnswer, "candidate", () => {
        state.oppositeSpeaking = false;
      });
    } else {
      // Candidate asks interviewer to speak question
      state.oppositeSpeaking = true;
      showNotification("Dr. Sarah Vance (Interviewer) is speaking...");
      speakTextSynthetic(q.interviewerVocal, "interviewer", () => {
        state.oppositeSpeaking = false;
      });
    }
  }

  function speakTextSynthetic(text, speakerRole, onComplete) {
    if (!('speechSynthesis' in window)) {
      setTimeout(onComplete, 4000);
      return;
    }

    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.rate = 1.0;
    utterance.pitch = speakerRole === "interviewer" ? 1.05 : 0.95;

    utterance.onend = () => {
      if (onComplete) onComplete();
    };
    utterance.onerror = () => {
      if (onComplete) onComplete();
    };

    window.speechSynthesis.speak(utterance);
  }

  // -------------------------------------------------------------
  // Real-Time Multi-User WebSocket Synchronization
  // -------------------------------------------------------------
  function initNetworking() {
    const params = new URLSearchParams(window.location.search);
    const roomId = params.get("room") || "executive-suite-101";
    const requestedRole = params.get("role") || state.role;

    const protocol = window.location.protocol === "https:" ? "wss:" : "ws:";
    const wsUrl = `${protocol}//${window.location.host}/ws/interview?room=${encodeURIComponent(roomId)}&role=${encodeURIComponent(requestedRole)}`;

    try {
      const ws = new WebSocket(wsUrl);
      state.ws = ws;

      ws.onopen = () => {
        state.connected = true;
        updateHTMLUI();
        console.log(`[WebSocket] Connected to room: ${roomId}`);
      };

      ws.onmessage = (event) => {
        try {
          const msg = JSON.parse(event.data);
          handleNetworkMessage(msg);
        } catch (e) { }
      };

      ws.onclose = () => {
        state.connected = false;
        updateHTMLUI();
        console.log("[WebSocket] Disconnected. Reconnecting in 3s...");
        setTimeout(initNetworking, 3000);
      };

      ws.onerror = (e) => {
        console.warn("[WebSocket] Connection error:", e);
      };
    } catch (e) {
      console.warn("[WebSocket] Failed to connect:", e);
    }
  }

  function handleNetworkMessage(msg) {
    if (msg.type === "welcome") {
      state.peerCount = msg.peerCount;
      state.isMultiplayer = msg.peerCount > 0;
      updateHTMLUI();
    } else if (msg.type === "peer_joined") {
      state.peerCount = msg.totalCount - 1;
      state.isMultiplayer = true;
      showNotification(`Remote ${msg.role.toUpperCase()} joined the VR room!`);
      updateHTMLUI();
    } else if (msg.type === "peer_left") {
      state.peerCount = Math.max(0, msg.totalCount - 1);
      state.isMultiplayer = state.peerCount > 0;
      showNotification("Remote participant disconnected.");
      updateHTMLUI();
    } else if (msg.type === "avatar_sync") {
      // Synchronize remote participant's head pose, speaking, and audio level
      if (msg.role === "candidate" && candidateAvatarNode && state.role === "interviewer") {
        if (msg.headPos) candidateAvatarNode.head.position = msg.headPos;
        if (msg.headRot) candidateAvatarNode.head.rotation = msg.headRot;
        state.oppositeSpeaking = msg.isSpeaking;
        state.oppositeAudioLevel = msg.audioLevel;
      } else if (msg.role === "interviewer" && interviewerAvatarNode && state.role === "candidate") {
        if (msg.headPos) interviewerAvatarNode.head.position = msg.headPos;
        if (msg.headRot) interviewerAvatarNode.head.rotation = msg.headRot;
        state.oppositeSpeaking = msg.isSpeaking;
        state.oppositeAudioLevel = msg.audioLevel;
      }
    } else if (msg.type === "question_change") {
      state.activeQuestionIndex = msg.index;
      updateHTMLUI();
    } else if (msg.type === "rating_update") {
      interviewQuestions[msg.index].rating = msg.rating;
      updateHTMLUI();
    }
  }

  let lastBroadcastTime = 0;
  function broadcastLocalTelemetry() {
    if (!state.ws || state.ws.readyState !== WebSocket.OPEN) return;
    const now = performance.now();
    if (now - lastBroadcastTime < 33) return; // ~30 fps sync
    lastBroadcastTime = now;

    state.ws.send(JSON.stringify({
      type: "avatar_sync",
      role: state.role,
      headPos: state.cameraPos,
      headYaw: state.cameraYaw,
      headPitch: state.cameraPitch,
      isSpeaking: state.localSpeaking,
      audioLevel: state.localAudioLevel
    }));
  }

  // -------------------------------------------------------------
  // WebXR Device API Integration (Meta Quest, Vision Pro, etc.)
  // -------------------------------------------------------------
  function checkWebXRSupport() {
    const vrBtn = document.getElementById("btn-enter-vr");
    if (!vrBtn) return;

    if ('xr' in navigator) {
      navigator.xr.isSessionSupported('immersive-vr')
        .then((supported) => {
          if (supported) {
            vrBtn.style.display = "flex";
            vrBtn.addEventListener("click", startWebXRSession);
          } else {
            vrBtn.innerHTML = "🥽 WebXR Desktop Mode";
          }
        })
        .catch(() => {
          vrBtn.innerHTML = "🥽 6-DoF Simulator Mode";
        });
    } else {
      vrBtn.innerHTML = "🥽 6-DoF Simulator Mode";
    }
  }

  function startWebXRSession() {
    if (!('xr' in navigator)) return;

    navigator.xr.requestSession('immersive-vr', {
      requiredFeatures: ['local-floor'],
      optionalFeatures: ['hand-tracking']
    }).then((session) => {
      renderer.xrSession = session;
      renderer.isXRActive = true;

      const gl = renderer.gl;
      session.updateRenderState({
        baseLayer: new XRWebGLLayer(session, gl)
      });

      session.requestReferenceSpace('local-floor').then((refSpace) => {
        renderer.xrRefSpace = refSpace;
        session.requestAnimationFrame(onXRFrame);
      });

      session.addEventListener('end', () => {
        renderer.xrSession = null;
        renderer.isXRActive = false;
        requestAnimationFrame(renderLoop);
      });
    }).catch((err) => {
      console.warn("[WebXR] Failed to create immersive VR session:", err);
    });
  }

  function onXRFrame(time, frame) {
    const session = frame.session;
    session.requestAnimationFrame(onXRFrame);

    const pose = frame.getViewerPose(renderer.xrRefSpace);
    if (pose) {
      const gl = renderer.gl;
      const glLayer = session.renderState.baseLayer;
      gl.bindFramebuffer(gl.FRAMEBUFFER, glLayer.framebuffer);

      for (const view of pose.views) {
        const viewport = glLayer.getViewport(view);
        const eyePos = [
          view.transform.position.x + state.cameraPos[0],
          view.transform.position.y,
          view.transform.position.z + state.cameraPos[2]
        ];

        renderer.render(view.transform.inverse.matrix, view.projectionMatrix, eyePos, viewport);
      }
    }

    updateSimulations(time * 0.001);
  }

  // -------------------------------------------------------------
  // Desktop VR 6-DoF Simulator Controls (Mouse Look & Interaction)
  // -------------------------------------------------------------
  let isPointerLocked = false;
  let isRightMouseDown = false;

  canvas.addEventListener("mousedown", (e) => {
    if (e.button === 2) {
      isRightMouseDown = true;
    } else if (e.button === 0) {
      // Click interaction: Test in-VR tablet or trigger dialogue
      handleSpatialClick(e);
    }
  });

  window.addEventListener("mouseup", (e) => {
    if (e.button === 2) isRightMouseDown = false;
  });

  window.addEventListener("contextmenu", (e) => e.preventDefault());

  window.addEventListener("mousemove", (e) => {
    if (isPointerLocked || isRightMouseDown) {
      const sensitivity = 0.16;
      state.cameraYaw -= e.movementX * sensitivity;
      state.cameraPitch = Math.max(-75, Math.min(75, state.cameraPitch - e.movementY * sensitivity));
    }
  });

  // Keyboard navigation & Shortcuts
  window.addEventListener("keydown", (e) => {
    if (e.key === "1") setRole("interviewer");
    if (e.key === "2") setRole("candidate");
    if (e.key === "m" || e.key === "M") toggleMute();
    if (e.key === " ") {
      e.preventDefault();
      triggerSyntheticDialogue();
    }
    if (e.key === "ArrowRight") nextQuestion();
    if (e.key === "ArrowLeft") prevQuestion();
  });

  function handleSpatialClick(e) {
    // If user clicked in center reticle area, advance dialogue or interact with tablet
    const rect = canvas.getBoundingClientRect();
    const cx = rect.width / 2;
    const cy = rect.height / 2;
    const dist = Math.hypot(e.clientX - (rect.left + cx), e.clientY - (rect.top + cy));

    if (dist < 80) {
      triggerSyntheticDialogue();
    }
  }

  // -------------------------------------------------------------
  // Avatar Procedural Animations (Idle breathing, saccades, lip-sync)
  // -------------------------------------------------------------
  function updateSimulations(dt) {
    const time = performance.now() * 0.001;

    // 1. Session Timer
    if (state.timerRunning) {
      state.timerSeconds += dt;
      const el = document.getElementById("timer-digits");
      if (el) {
        const m = Math.floor(state.timerSeconds / 60).toString().padStart(2, "0");
        const s = Math.floor(state.timerSeconds % 60).toString().padStart(2, "0");
        el.innerText = `${m}:${s}`;
      }
    }

    // 2. Microphone Audio Level Analyzer
    if (state.analyser && !state.isMuted) {
      const data = new Uint8Array(state.analyser.frequencyBinCount);
      state.analyser.getByteFrequencyData(data);
      let sum = 0;
      for (let i = 0; i < data.length; i++) sum += data[i];
      const avg = sum / data.length;
      state.localAudioLevel = Math.min(1.0, avg / 60.0);
      state.localSpeaking = state.localAudioLevel > 0.08;
    } else {
      state.localAudioLevel = 0;
      state.localSpeaking = false;
    }

    // Update HTML VU Meter bar
    const vuFill = document.getElementById("vu-meter-fill");
    if (vuFill) {
      const level = (state.localSpeaking ? state.localAudioLevel : (state.oppositeSpeaking ? 0.75 : 0)) * 100;
      vuFill.style.width = `${Math.min(100, Math.round(level))}%`;
    }

    // 3. Subtle Idle Breathing
    const breathOffset = Math.sin(time * 1.8) * 0.006;
    if (interviewerAvatarNode && interviewerAvatarNode.chest) {
      interviewerAvatarNode.chest.position[1] = 0.80 + breathOffset;
    }
    if (candidateAvatarNode && candidateAvatarNode.chest) {
      candidateAvatarNode.chest.position[1] = 0.80 + breathOffset;
    }

    // 4. Natural Eye Blinks
    state.blinkTimer -= dt;
    if (state.blinkTimer <= 0) {
      state.isBlinking = true;
      state.blinkTimer = 2.5 + Math.random() * 2.5;
    }

    // 5. Opposite Avatar Procedural Animations & Lip-Sync Visemes
    const oppositeAvatar = state.role === "interviewer" ? candidateAvatarNode : interviewerAvatarNode;
    if (oppositeAvatar) {
      // Natural eye contact tracking with micro head tilt
      const headTilt = Math.sin(time * 0.8) * 2.0;
      const headNod = state.oppositeSpeaking ? Math.sin(time * 6.0) * 4.0 : Math.sin(time * 1.2) * 1.2;
      oppositeAvatar.head.setEuler(headNod, headTilt, 0);

      // Lip-Sync Viseme mouth scaling
      if (oppositeAvatar.mouth) {
        if (state.oppositeSpeaking) {
          const syllableScale = 1.0 + Math.abs(Math.sin(time * 14.0)) * 2.2;
          oppositeAvatar.mouth.scale = [1.2, syllableScale, 1.0];
        } else {
          oppositeAvatar.mouth.scale = [1.0, 1.0, 1.0];
        }
      }

      // Conversational hand gestures when speaking
      if (oppositeAvatar.armR && state.oppositeSpeaking) {
        const gestureAngle = 14 + Math.sin(time * 4.0) * 12.0;
        oppositeAvatar.armR.setEuler(gestureAngle, -12, 0);
      }
    }

    // Local Avatar Lip-Sync when user speaks into microphone
    const localAvatar = state.role === "interviewer" ? interviewerAvatarNode : candidateAvatarNode;
    if (localAvatar && localAvatar.mouth) {
      if (state.localSpeaking) {
        const mouthOpen = 1.0 + state.localAudioLevel * 3.5;
        localAvatar.mouth.scale = [1.2, mouthOpen, 1.0];
      } else {
        localAvatar.mouth.scale = [1.0, 1.0, 1.0];
      }
    }

    // Update In-VR Tablet display texture (~15 fps refresh)
    if (Math.floor(time * 15) % 2 === 0) {
      renderTabletUI();
    }

    // Broadcast telemetry to peers
    broadcastLocalTelemetry();
  }

  // -------------------------------------------------------------
  // Main Desktop Render Loop
  // -------------------------------------------------------------
  let lastFrameTime = performance.now();

  function renderLoop(now) {
    if (renderer.isXRActive) return; // Handled by onXRFrame
    requestAnimationFrame(renderLoop);

    const dt = Math.min(0.1, (now - lastFrameTime) * 0.001);
    lastFrameTime = now;

    renderer.resize();

    // Compute View Matrix from Camera Position and Pitch/Yaw
    const pitchRad = (state.cameraPitch * Math.PI) / 180;
    const yawRad = (state.cameraYaw * Math.PI) / 180;
    const fwdX = Math.sin(yawRad) * Math.cos(pitchRad);
    const fwdY = Math.sin(pitchRad);
    const fwdZ = Math.cos(yawRad) * Math.cos(pitchRad);

    const eye = state.cameraPos;
    const target = [eye[0] + fwdX, eye[1] + fwdY, eye[2] + fwdZ];
    const up = [0, 1, 0];

    Math3D.lookAt(renderer.viewMatrix, eye, target, up);

    const aspect = canvas.width / canvas.height;
    Math3D.perspective(renderer.projectionMatrix, (62 * Math.PI) / 180, aspect, 0.05, 50.0);

    renderer.render(renderer.viewMatrix, renderer.projectionMatrix, eye);

    updateSimulations(dt);
  }

  // -------------------------------------------------------------
  // UI Interactions & Dom Controllers
  // -------------------------------------------------------------
  function setupDOMControls() {
    // Role switch buttons
    document.getElementById("btn-role-interviewer").addEventListener("click", () => setRole("interviewer"));
    document.getElementById("btn-role-candidate").addEventListener("click", () => setRole("candidate"));

    // Audio & Mute
    document.getElementById("btn-toggle-mic").addEventListener("click", toggleMute);
    document.getElementById("btn-dock-mic").addEventListener("click", toggleMute);

    // Dialogue trigger
    document.getElementById("btn-speak").addEventListener("click", triggerSyntheticDialogue);

    // Question navigation
    document.getElementById("btn-next-q").addEventListener("click", nextQuestion);

    // End Interview Modal
    document.getElementById("btn-end-interview").addEventListener("click", () => {
      document.getElementById("modal-end-interview").classList.add("active");
    });
    document.getElementById("btn-close-modal").addEventListener("click", () => {
      document.getElementById("modal-end-interview").classList.remove("active");
    });
    document.getElementById("btn-confirm-end").addEventListener("click", () => {
      document.getElementById("modal-end-interview").classList.remove("active");
      showNotification("Interview Session Concluded. Scorecard Saved.");
    });

    // Notes auto-save
    const notesEl = document.getElementById("interview-notes");
    if (notesEl) {
      notesEl.value = state.notes;
      notesEl.addEventListener("input", (e) => {
        state.notes = e.target.value;
      });
    }

    // Quick tag pills
    document.querySelectorAll(".tag-btn").forEach((btn) => {
      btn.addEventListener("click", () => {
        const tag = btn.innerText.replace("+ ", "");
        if (!state.selectedTags.includes(tag)) {
          state.selectedTags.push(tag);
          state.notes += `\n• ${tag}`;
          if (notesEl) notesEl.value = state.notes;
        }
      });
    });

    renderQuestionsHTML();
    updateHTMLUI();
  }

  function toggleMute() {
    state.isMuted = !state.isMuted;
    if (state.micStream) {
      state.micStream.getAudioTracks().forEach(t => t.enabled = !state.isMuted);
    }
    updateHTMLUI();
    showNotification(state.isMuted ? "Microphone Muted" : "Microphone Active");
  }

  function nextQuestion() {
    state.activeQuestionIndex = (state.activeQuestionIndex + 1) % interviewQuestions.length;
    renderQuestionsHTML();
    updateHTMLUI();
    showNotification(`Advanced to Question ${state.activeQuestionIndex + 1}`);

    if (state.ws && state.ws.readyState === WebSocket.OPEN) {
      state.ws.send(JSON.stringify({
        type: "question_change",
        index: state.activeQuestionIndex
      }));
    }
  }

  function prevQuestion() {
    state.activeQuestionIndex = (state.activeQuestionIndex - 1 + interviewQuestions.length) % interviewQuestions.length;
    renderQuestionsHTML();
    updateHTMLUI();
  }

  function renderQuestionsHTML() {
    const list = document.getElementById("questions-list");
    if (!list) return;
    list.innerHTML = "";

    interviewQuestions.forEach((q, idx) => {
      const item = document.createElement("div");
      item.className = `question-item ${idx === state.activeQuestionIndex ? "active" : ""}`;
      item.innerHTML = `
        <div class="q-header">
          <span class="q-num">Q${idx + 1}</span>
          <span class="q-category">${q.category}</span>
        </div>
        <div class="q-text">${q.text}</div>
        <div class="rating-stars" data-q="${idx}">
          ${[1, 2, 3, 4, 5].map(s => `<span class="star ${s <= q.rating ? 'filled' : ''}" data-star="${s}">★</span>`).join('')}
        </div>
      `;

      item.addEventListener("click", (e) => {
        if (!e.target.classList.contains("star")) {
          state.activeQuestionIndex = idx;
          renderQuestionsHTML();
          updateHTMLUI();
        }
      });

      item.querySelectorAll(".star").forEach(star => {
        star.addEventListener("click", (e) => {
          e.stopPropagation();
          const sVal = parseInt(star.getAttribute("data-star"), 10);
          q.rating = sVal;
          renderQuestionsHTML();
          updateHTMLUI();

          if (state.ws && state.ws.readyState === WebSocket.OPEN) {
            state.ws.send(JSON.stringify({
              type: "rating_update",
              index: idx,
              rating: sVal
            }));
          }
        });
      });

      list.appendChild(item);
    });
  }

  function updateHTMLUI() {
    // Role switcher active classes
    const btnInt = document.getElementById("btn-role-interviewer");
    const btnCand = document.getElementById("btn-role-candidate");
    if (btnInt && btnCand) {
      if (state.role === "interviewer") {
        btnInt.classList.add("active");
        btnCand.classList.remove("active", "candidate");
      } else {
        btnInt.classList.remove("active");
        btnCand.classList.add("active", "candidate");
      }
    }

    // Role-dependent UI panels
    const leftPanelTitle = document.getElementById("panel-profile-title");
    const profileName = document.getElementById("profile-name");
    const profileRole = document.getElementById("profile-role");
    const profileMeta = document.getElementById("profile-meta");

    if (state.role === "interviewer") {
      if (leftPanelTitle) leftPanelTitle.innerText = "Candidate Profile";
      if (profileName) profileName.innerText = candidateProfile.name;
      if (profileRole) profileRole.innerText = candidateProfile.role;
      if (profileMeta) profileMeta.innerText = `${candidateProfile.experience} Experience • 94% Match Score`;
    } else {
      if (leftPanelTitle) leftPanelTitle.innerText = "Interviewer Profile";
      if (profileName) profileName.innerText = interviewerProfile.name;
      if (profileRole) profileRole.innerText = interviewerProfile.role;
      if (profileMeta) profileMeta.innerText = `${interviewerProfile.company}`;
    }

    // Mic badge
    const micBtn = document.getElementById("btn-toggle-mic");
    const dockMic = document.getElementById("btn-dock-mic");
    const micText = state.isMuted ? "Mic: Off" : "Mic: On";
    if (micBtn) micBtn.innerHTML = `<span>${state.isMuted ? '🔇' : '🎤'}</span> ${micText}`;
    if (dockMic) {
      dockMic.className = `dock-btn ${state.isMuted ? 'muted' : 'active'}`;
      dockMic.innerHTML = `<span>${state.isMuted ? '🔇' : '🎤'}</span> ${state.isMuted ? 'Unmute Mic' : 'Mute Mic'}`;
    }

    // Connection badge
    const connBadge = document.getElementById("connection-badge");
    if (connBadge) {
      if (state.connected) {
        connBadge.innerText = state.isMultiplayer ? `Online (${state.peerCount + 1} in Room)` : "Standalone VR Active";
      } else {
        connBadge.innerText = "Connecting...";
      }
    }
  }

  function showNotification(text) {
    const el = document.getElementById("vr-notification");
    if (!el) return;
    el.innerText = text;
    el.classList.add("show");
    setTimeout(() => {
      el.classList.remove("show");
    }, 2800);
  }

  // -------------------------------------------------------------
  // Initialization
  // -------------------------------------------------------------
  function init() {
    buildInterviewRoom(renderer.scene);
    buildAvatars(renderer.scene);
    buildSpatialTablet(renderer.scene);

    const urlParams = new URLSearchParams(window.location.search);
    const initialRole = urlParams.get("role") || "interviewer";
    const autoModal = urlParams.get("modal") === "end";
    const autoDialogue = urlParams.get("dialogue") === "1";

    setRole(initialRole, false);
    setupDOMControls();
    initAudioSystem();
    initNetworking();
    checkWebXRSupport();

    if (autoModal) {
      document.getElementById("modal-end-interview").classList.add("active");
    }
    if (autoDialogue) {
      state.oppositeSpeaking = true;
      state.timerSeconds = 412;
      const vu = document.getElementById("vu-meter-fill");
      if (vu) vu.style.width = "78%";
      const timer = document.getElementById("timer-digits");
      if (timer) timer.innerText = "06:52";
      showNotification("Alex Mercer (Candidate) speaking: Real-Time Audio Visemes Active");
    }

    requestAnimationFrame(renderLoop);
  }

  window.addEventListener("DOMContentLoaded", init);
})();
