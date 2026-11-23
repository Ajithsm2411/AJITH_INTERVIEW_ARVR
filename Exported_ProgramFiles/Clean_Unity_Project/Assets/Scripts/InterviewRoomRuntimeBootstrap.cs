using System.IO;
using UnityEngine;
using InterviewRoom.VR;
using InterviewRoom.SpeechToText;

namespace InterviewRoom
{
    /// <summary>
    /// Runtime bootstrap that automatically spawns the full 3D Interview Room,
    /// both avatars (Interviewer with photo assets & AR/VR Candidate), furniture,
    /// AR holograms, lighting, and camera system whenever the scene runs.
    /// </summary>
    public class InterviewRoomRuntimeBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureInterviewRoomExists()
        {
            RemoveScreensBetweenInterviewerAndCandidate();

            var envObj = GameObject.Find("InterviewRoom_Environment");
            if (envObj != null)
            {
                Debug.Log("[InterviewRoomBootstrap] Room environment already present in scene. Verifying VR Rig...");
                if (envObj.transform.Find("VR_Rig") == null)
                {
                    var builder = envObj.GetComponent<InterviewRoomRuntimeBootstrap>();
                    if (builder == null) builder = envObj.AddComponent<InterviewRoomRuntimeBootstrap>();
                    builder.SetupVRGogglesRigAndVirtualScreen(envObj.transform);
                }
                return;
            }

            Debug.Log("[InterviewRoomBootstrap] Spawning Interview Room runtime environment with Meta Quest 3 VR Rig...");
            GameObject root = new GameObject("InterviewRoom_Environment");
            var bootstrap = root.AddComponent<InterviewRoomRuntimeBootstrap>();
            bootstrap.BuildRoom(root.transform);
        }

        private void Awake()
        {
            RemoveScreensBetweenInterviewerAndCandidate();
        }

        public static void RemoveScreensBetweenInterviewerAndCandidate()
        {
            // Destroy any 3D Virtual Screens and white desk screens between interviewer and candidate
            var virtualScreens = Object.FindObjectsByType<VRSpatialVirtualScreen>(FindObjectsInactive.Include);
            foreach (var vs in virtualScreens)
            {
                if (vs != null && vs.gameObject != null)
                {
                    vs.gameObject.SetActive(false);
                    if (Application.isPlaying) Object.Destroy(vs.gameObject);
                    else Object.DestroyImmediate(vs.gameObject);
                }
            }

            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allObjects)
            {
                if (go == null) continue;
                string n = go.name;
                if (n == "VR_Virtual_Screen" || n == "Interviewer_Desk_PhotoDisplay" || n == "Screen_Glass_Display" || n == "Screen_Bezel_Frame")
                {
                    go.SetActive(false);
                    if (Application.isPlaying) Object.Destroy(go);
                    else Object.DestroyImmediate(go);
                }
            }
        }

        private Material matFloor;
        private Material matWall;
        private Material matWoodAccent;
        private Material matCeiling;
        private Material matTableWalnut;
        private Material matMetalTrim;
        private Material matChairFabric;
        private Material matChairChrome;
        private Material matGlass;
        private Material matWhiteboard;
        private Material matLaptop;
        private Material matLaptopScreen;
        private Material matSkin;
        private Material matHairDark;
        private Material matHairPhoto;
        private Material matInterviewerSuit;
        private Material matInterviewerTie;
        private Material matCandidateHoodie;
        private Material matVRHeadset;
        private Material matVRVisor;
        private Material matARHologram;
        private Material matPlant;
        private Material matPot;
        private Material matLightEmissive;
        private Material matInterviewerPhoto;
        private Material matInterviewerCutout;

        public void BuildRoom(Transform root)
        {
            InitMaterials();

            // 1. Architecture
            BuildRoomShell(root);

            // 2. Table & Chairs
            Transform tableTransform = BuildInterviewTable(root);
            BuildChairs(root);

            // 3. Table Props & Decor
            BuildTableProps(tableTransform);
            BuildOfficeDecor(root);

            // 4. Avatars (Interviewer & Candidate)
            GameObject interviewer = BuildInterviewerAvatar(root);
            GameObject candidate = BuildCandidateVRAvatar(root);

            // 5. AR Hologram System
            GameObject[] holograms = BuildARHologramSystem(root);

            // 6. Camera Rig & Lighting
            SetupCameraAndLighting(root, interviewer, candidate, holograms);

            // 7. VR Goggles Rig & 3D Virtual Screen
            SetupVRGogglesRigAndVirtualScreen(root);

            Debug.Log("[InterviewRoomBootstrap] Interview Room successfully created with VR Goggles and Virtual Screen!");
        }

        private void InitMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");

            matFloor = CreateMat(lit, new Color(0.22f, 0.16f, 0.11f), smoothness: 0.65f);
            matWall = CreateMat(lit, new Color(0.88f, 0.89f, 0.91f), smoothness: 0.2f);
            matWoodAccent = CreateMat(lit, new Color(0.38f, 0.24f, 0.16f), smoothness: 0.45f);
            matCeiling = CreateMat(lit, new Color(0.84f, 0.85f, 0.86f), smoothness: 0.1f);
            matTableWalnut = CreateMat(lit, new Color(0.28f, 0.18f, 0.12f), smoothness: 0.72f);
            matMetalTrim = CreateMat(lit, new Color(0.75f, 0.76f, 0.78f), metallic: 0.85f, smoothness: 0.8f);
            matChairFabric = CreateMat(lit, new Color(0.14f, 0.15f, 0.17f), smoothness: 0.25f);
            matChairChrome = CreateMat(lit, new Color(0.9f, 0.92f, 0.95f), metallic: 0.95f, smoothness: 0.9f);
            matWhiteboard = CreateMat(lit, new Color(0.96f, 0.97f, 0.98f), smoothness: 0.85f);
            matLaptop = CreateMat(lit, new Color(0.22f, 0.23f, 0.25f), metallic: 0.8f, smoothness: 0.65f);

            matLaptopScreen = CreateMat(lit, new Color(0.1f, 0.2f, 0.35f), emission: new Color(0.2f, 0.6f, 1.0f) * 1.5f);
            matLightEmissive = CreateMat(lit, Color.white, emission: new Color(1.0f, 0.96f, 0.88f) * 3.0f);
            matGlass = CreateMat(lit, new Color(0.7f, 0.85f, 0.95f, 0.25f), smoothness: 0.95f, isTransparent: true);

            matSkin = CreateMat(lit, new Color(0.92f, 0.76f, 0.66f), smoothness: 0.3f);
            matHairDark = CreateMat(lit, new Color(0.10f, 0.08f, 0.07f), smoothness: 0.35f);
            matHairPhoto = CreateMat(lit, new Color(0.52f, 0.38f, 0.25f), smoothness: 0.35f);
            matInterviewerSuit = CreateMat(lit, new Color(0.12f, 0.12f, 0.14f), smoothness: 0.3f);
            matInterviewerTie = CreateMat(lit, new Color(0.42f, 0.44f, 0.46f), smoothness: 0.4f);
            matCandidateHoodie = CreateMat(lit, new Color(0.20f, 0.22f, 0.25f), smoothness: 0.3f);

            matVRHeadset = CreateMat(lit, new Color(0.92f, 0.93f, 0.95f), smoothness: 0.7f);
            matVRVisor = CreateMat(lit, new Color(0.04f, 0.06f, 0.08f), smoothness: 0.95f, emission: new Color(0.0f, 0.85f, 1.0f) * 2.5f);
            matARHologram = CreateMat(lit, new Color(0.0f, 0.75f, 1.0f, 0.45f), smoothness: 0.9f, emission: new Color(0.0f, 0.8f, 1.0f) * 2.0f, isTransparent: true);

            matPlant = CreateMat(lit, new Color(0.12f, 0.35f, 0.15f), smoothness: 0.4f);
            matPot = CreateMat(lit, new Color(0.94f, 0.94f, 0.94f), smoothness: 0.6f);

            // Load Interviewer Textures
            Texture2D photoTex = LoadTextureFromFile("Assets/Textures/InterviewerPhoto.jpg");
            Texture2D cutoutTex = LoadTextureFromFile("Assets/Textures/InterviewerPhoto_Cutout.png");

            matInterviewerPhoto = CreateMatWithTexture(lit, photoTex, Color.white, smoothness: 0.6f);
            matInterviewerCutout = CreateMatWithTexture(lit, cutoutTex != null ? cutoutTex : photoTex, Color.white, smoothness: 0.35f, isTransparent: true);
        }

        private Texture2D LoadTextureFromFile(string relativePath)
        {
            string fullPath = Path.Combine(Application.dataPath, relativePath.Replace("Assets/", ""));
            if (File.Exists(fullPath))
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    return tex;
                }
            }
            return null;
        }

        private Material CreateMat(Shader shader, Color baseColor, float metallic = 0f, float smoothness = 0.5f, Color? emission = null, bool isTransparent = false)
        {
            Material mat = new Material(shader);
            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission.Value);
            }

            if (isTransparent)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.renderQueue = 3000;
            }

            return mat;
        }

        private Material CreateMatWithTexture(Shader shader, Texture2D texture, Color baseColor, float metallic = 0f, float smoothness = 0.5f, bool isTransparent = false)
        {
            Material mat = new Material(shader);
            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (texture != null)
            {
                mat.mainTexture = texture;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            }
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (isTransparent)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.renderQueue = 3000;
            }

            return mat;
        }

        private void BuildRoomShell(Transform parent)
        {
            GameObject room = new GameObject("Room_Architecture");
            room.transform.SetParent(parent, false);

            CreatePrimitive(PrimitiveType.Cube, "Floor", room.transform, new Vector3(0, -0.1f, 0), new Vector3(8.0f, 0.2f, 6.0f), matFloor);
            CreatePrimitive(PrimitiveType.Cube, "Ceiling", room.transform, new Vector3(0, 3.2f, 0), new Vector3(8.0f, 0.2f, 6.0f), matCeiling);

            CreateCeilingLightPanel(room.transform, new Vector3(-1.5f, 3.18f, 0.8f));
            CreateCeilingLightPanel(room.transform, new Vector3(1.5f, 3.18f, 0.8f));
            CreateCeilingLightPanel(room.transform, new Vector3(-1.5f, 3.18f, -0.8f));
            CreateCeilingLightPanel(room.transform, new Vector3(1.5f, 3.18f, -0.8f));

            CreatePrimitive(PrimitiveType.Cube, "Wall_Back", room.transform, new Vector3(0, 1.55f, 3.05f), new Vector3(8.0f, 3.1f, 0.1f), matWall);

            GameObject slatGroup = new GameObject("Decorative_WoodSlats");
            slatGroup.transform.SetParent(room.transform, false);
            for (float x = -3.5f; x <= 3.5f; x += 0.35f)
            {
                CreatePrimitive(PrimitiveType.Cube, $"Slat_{x:F1}", slatGroup.transform, new Vector3(x, 1.55f, 2.98f), new Vector3(0.08f, 3.1f, 0.04f), matWoodAccent);
            }

            CreatePrimitive(PrimitiveType.Cube, "Wall_Left", room.transform, new Vector3(-4.05f, 1.55f, 0), new Vector3(0.1f, 3.1f, 6.0f), matWall);
            BuildSmartboard(room.transform, new Vector3(-3.98f, 1.7f, 0));

            CreatePrimitive(PrimitiveType.Cube, "Wall_Right", room.transform, new Vector3(4.05f, 1.55f, 0), new Vector3(0.1f, 3.1f, 6.0f), matWall);
            CreatePrimitive(PrimitiveType.Cube, "Office_Door", room.transform, new Vector3(3.98f, 1.15f, 1.8f), new Vector3(0.05f, 2.3f, 1.1f), matWoodAccent);
            CreatePrimitive(PrimitiveType.Cylinder, "Door_Handle", room.transform, new Vector3(3.92f, 1.15f, 1.4f), new Vector3(0.03f, 0.08f, 0.03f), matMetalTrim);

            // ── OFFICE NAME & TITLE PLAQUE ──
            GameObject officeSign = CreatePrimitive(PrimitiveType.Cube, "Office_Title_Plaque", room.transform, new Vector3(3.95f, 1.85f, 1.8f), new Vector3(0.02f, 0.22f, 0.85f), matMetalTrim);
            GameObject officeSignText = new GameObject("Office_Sign_TextMesh");
            officeSignText.transform.SetParent(officeSign.transform, false);
            officeSignText.transform.localPosition = new Vector3(-0.55f, 0f, 0f);
            officeSignText.transform.localRotation = Quaternion.Euler(0, 90f, 0);
            TextMesh signTm = officeSignText.AddComponent<TextMesh>();
            signTm.text = "WIPRO PRIVATE LIMITED\nEXECUTIVE INTERVIEW ROOM 1";
            signTm.fontSize = 24;
            signTm.characterSize = 0.007f;
            signTm.anchor = TextAnchor.MiddleCenter;
            signTm.alignment = TextAlignment.Center;
            signTm.color = new Color(0.95f, 0.95f, 1.0f);
            signTm.fontStyle = FontStyle.Bold;

            CreatePrimitive(PrimitiveType.Cube, "Wall_Front_LeftPillar", room.transform, new Vector3(-3.6f, 1.55f, -3.05f), new Vector3(0.8f, 3.1f, 0.1f), matWall);
            CreatePrimitive(PrimitiveType.Cube, "Wall_Front_RightPillar", room.transform, new Vector3(3.6f, 1.55f, -3.05f), new Vector3(0.8f, 3.1f, 0.1f), matWall);
            CreatePrimitive(PrimitiveType.Cube, "Window_Glass", room.transform, new Vector3(0, 1.55f, -3.0f), new Vector3(6.4f, 2.9f, 0.05f), matGlass);

            CreatePrimitive(PrimitiveType.Cube, "Mullion_H_Mid", room.transform, new Vector3(0, 1.55f, -2.98f), new Vector3(6.4f, 0.06f, 0.06f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Mullion_V_1", room.transform, new Vector3(-2.1f, 1.55f, -2.98f), new Vector3(0.06f, 2.9f, 0.06f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Mullion_V_2", room.transform, new Vector3(0f, 1.55f, -2.98f), new Vector3(0.06f, 2.9f, 0.06f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Mullion_V_3", room.transform, new Vector3(2.1f, 1.55f, -2.98f), new Vector3(0.06f, 2.9f, 0.06f), matMetalTrim);
        }

        private void CreateCeilingLightPanel(Transform parent, Vector3 position)
        {
            GameObject fixture = CreatePrimitive(PrimitiveType.Cube, "Ceiling_LED_Panel", parent, position, new Vector3(1.2f, 0.03f, 0.4f), matLightEmissive);
            GameObject lightObj = new GameObject("Downlight_Spot");
            lightObj.transform.SetParent(fixture.transform, false);
            lightObj.transform.localPosition = new Vector3(0, -0.05f, 0);
            lightObj.transform.localRotation = Quaternion.Euler(90, 0, 0);

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 75f;
            light.range = 5.0f;
            light.color = new Color(1.0f, 0.96f, 0.90f);
            light.intensity = 25f;
            light.shadows = LightShadows.Soft;
        }

        private void BuildSmartboard(Transform parent, Vector3 position)
        {
            GameObject board = new GameObject("Smartboard_Presentation");
            board.transform.SetParent(parent, false);
            board.transform.position = position;

            CreatePrimitive(PrimitiveType.Cube, "Frame", board.transform, Vector3.zero, new Vector3(0.04f, 1.3f, 2.4f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Screen", board.transform, new Vector3(0.025f, 0, 0), new Vector3(0.01f, 1.22f, 2.32f), matWhiteboard);
            CreatePrimitive(PrimitiveType.Cube, "Status_Banner", board.transform, new Vector3(0.032f, 0.48f, 0), new Vector3(0.01f, 0.16f, 2.2f), matLaptopScreen);

            if (matInterviewerPhoto != null)
            {
                GameObject photoOnBoard = CreatePrimitive(PrimitiveType.Quad, "Interviewer_Photo_OnSmartboard", board.transform, new Vector3(0.034f, 0.02f, 0.72f), new Vector3(0.45f, 0.62f, 1f), matInterviewerPhoto);
                photoOnBoard.transform.localRotation = Quaternion.Euler(0, 90f, 0);
            }
        }

        private Transform BuildInterviewTable(Transform parent)
        {
            GameObject table = new GameObject("Interview_Table");
            table.transform.SetParent(parent, false);
            table.transform.position = Vector3.zero;

            CreatePrimitive(PrimitiveType.Cube, "TableTop_Wood", table.transform, new Vector3(0, 0.73f, 0), new Vector3(1.1f, 0.05f, 1.70f), matTableWalnut);
            CreatePrimitive(PrimitiveType.Cube, "TableTop_EdgeTrim", table.transform, new Vector3(0, 0.71f, 0), new Vector3(1.12f, 0.03f, 1.72f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Center_Inlay", table.transform, new Vector3(0, 0.756f, 0), new Vector3(0.25f, 0.005f, 1.20f), matMetalTrim);

            CreatePrimitive(PrimitiveType.Cube, "Leg_Front_Left", table.transform, new Vector3(-0.45f, 0.35f, -0.70f), new Vector3(0.06f, 0.70f, 0.06f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Leg_Front_Right", table.transform, new Vector3(0.45f, 0.35f, -0.70f), new Vector3(0.06f, 0.70f, 0.06f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Leg_Front_Crossbar", table.transform, new Vector3(0, 0.1f, -0.70f), new Vector3(0.9f, 0.05f, 0.05f), matMetalTrim);

            CreatePrimitive(PrimitiveType.Cube, "Leg_Back_Left", table.transform, new Vector3(-0.45f, 0.35f, 0.70f), new Vector3(0.06f, 0.70f, 0.06f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Leg_Back_Right", table.transform, new Vector3(0.45f, 0.35f, 0.70f), new Vector3(0.06f, 0.70f, 0.06f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Leg_Back_Crossbar", table.transform, new Vector3(0, 0.1f, 0.70f), new Vector3(0.9f, 0.05f, 0.05f), matMetalTrim);

            CreatePrimitive(PrimitiveType.Cube, "Leg_Center_Spine", table.transform, new Vector3(0, 0.1f, 0), new Vector3(0.05f, 0.05f, 1.35f), matMetalTrim);

            return table.transform;
        }

        private void BuildChairs(Transform parent)
        {
            BuildChair(parent, new Vector3(0, 0, 1.25f), 180f, "Interviewer_Chair");
            BuildChair(parent, new Vector3(0, 0, -1.25f), 0f, "Candidate_Chair");
        }

        private GameObject BuildChair(Transform parent, Vector3 position, float yRot, string name)
        {
            GameObject chair = new GameObject(name);
            chair.transform.SetParent(parent, false);
            chair.transform.position = position;
            chair.transform.rotation = Quaternion.Euler(0, yRot, 0);

            CreatePrimitive(PrimitiveType.Cylinder, "Central_Cylinder", chair.transform, new Vector3(0, 0.22f, 0), new Vector3(0.08f, 0.18f, 0.08f), matChairChrome);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f * Mathf.Deg2Rad;
                Vector3 legEnd = new Vector3(Mathf.Sin(angle) * 0.28f, 0.05f, Mathf.Cos(angle) * 0.28f);
                CreatePrimitive(PrimitiveType.Cube, $"Base_Spoke_{i}", chair.transform, legEnd * 0.5f + new Vector3(0, 0.05f, 0), new Vector3(0.04f, 0.03f, 0.28f), matChairChrome);
                CreatePrimitive(PrimitiveType.Sphere, $"Wheel_{i}", chair.transform, legEnd, new Vector3(0.05f, 0.05f, 0.05f), matChairFabric);
            }

            CreatePrimitive(PrimitiveType.Cube, "Seat_Cushion", chair.transform, new Vector3(0, 0.46f, 0), new Vector3(0.52f, 0.08f, 0.50f), matChairFabric);

            GameObject backrest = CreatePrimitive(PrimitiveType.Cube, "Backrest", chair.transform, new Vector3(0, 0.82f, -0.23f), new Vector3(0.48f, 0.65f, 0.07f), matChairFabric);
            backrest.transform.localRotation = Quaternion.Euler(6f, 0, 0);
            CreatePrimitive(PrimitiveType.Cube, "Headrest", chair.transform, new Vector3(0, 1.20f, -0.27f), new Vector3(0.28f, 0.14f, 0.08f), matChairFabric);

            CreatePrimitive(PrimitiveType.Cube, "Armrest_Left", chair.transform, new Vector3(-0.28f, 0.65f, -0.05f), new Vector3(0.06f, 0.03f, 0.32f), matChairFabric);
            CreatePrimitive(PrimitiveType.Cylinder, "Arm_Pillar_L", chair.transform, new Vector3(-0.28f, 0.55f, -0.05f), new Vector3(0.03f, 0.1f, 0.03f), matChairChrome);

            CreatePrimitive(PrimitiveType.Cube, "Armrest_Right", chair.transform, new Vector3(0.28f, 0.65f, -0.05f), new Vector3(0.06f, 0.03f, 0.32f), matChairFabric);
            CreatePrimitive(PrimitiveType.Cylinder, "Arm_Pillar_R", chair.transform, new Vector3(0.28f, 0.55f, -0.05f), new Vector3(0.03f, 0.1f, 0.03f), matChairChrome);

            return chair;
        }

        private void BuildTableProps(Transform table)
        {
            BuildLaptop(table, new Vector3(-0.15f, 0.755f, 0.50f), 0f, "Interviewer_Laptop");

            CreatePrimitive(PrimitiveType.Cylinder, "Coffee_Mug", table, new Vector3(-0.38f, 0.80f, 0.50f), new Vector3(0.08f, 0.05f, 0.08f), matPot);
            CreatePrimitive(PrimitiveType.Cylinder, "Water_Glass", table, new Vector3(0.35f, 0.80f, -0.50f), new Vector3(0.07f, 0.06f, 0.07f), matGlass);

            // Gooseneck Microphones
            BuildGooseneckMicrophone(table, new Vector3(0.20f, 0.755f, 0.55f), 180f, "Interviewer_Gooseneck_Mic");
            BuildGooseneckMicrophone(table, new Vector3(-0.20f, 0.755f, -0.55f), 0f, "Candidate_Gooseneck_Mic");

            // Central Conference Speakerphone Puck
            BuildConferenceSpeakerphonePuck(table, new Vector3(0, 0.755f, 0), "Conference_Speakerphone_Puck");

            BuildNameplate(table, new Vector3(-0.25f, 0.77f, 0.25f), 0f, "INTERVIEWER: DR. SARAH VANCE");
            BuildNameplate(table, new Vector3(0.25f, 0.77f, -0.25f), 180f, "CANDIDATE: ALEX MERCER (XR)");
        }

        private GameObject BuildGooseneckMicrophone(Transform parent, Vector3 localPos, float yRot, string name)
        {
            GameObject mic = new GameObject(name);
            mic.transform.SetParent(parent, false);
            mic.transform.localPosition = localPos;
            mic.transform.localRotation = Quaternion.Euler(0, yRot, 0);

            // 1. Weighted Cast Metal Base (beveled cylinder with push-to-talk button)
            CreatePrimitive(PrimitiveType.Cylinder, "Mic_Base", mic.transform, new Vector3(0, 0.012f, 0), new Vector3(0.11f, 0.014f, 0.11f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cylinder, "Mute_Button", mic.transform, new Vector3(0, 0.022f, 0.028f), new Vector3(0.022f, 0.005f, 0.022f), matChairFabric);

            // 2. Rigid lower stem
            GameObject lowerStem = CreatePrimitive(PrimitiveType.Cylinder, "Lower_Stem", mic.transform, new Vector3(0, 0.09f, 0.015f), new Vector3(0.010f, 0.07f, 0.010f), matMetalTrim);
            lowerStem.transform.localRotation = Quaternion.Euler(16f, 0, 0);

            // 3. Flexible gooseneck neck
            GameObject flexNeck = CreatePrimitive(PrimitiveType.Cylinder, "Flexible_Gooseneck", mic.transform, new Vector3(0, 0.20f, 0.05f), new Vector3(0.008f, 0.06f, 0.008f), matMetalTrim);
            flexNeck.transform.localRotation = Quaternion.Euler(32f, 0, 0);

            // 4. Microphone capsule head (cardioid condenser grille)
            GameObject capsule = CreatePrimitive(PrimitiveType.Cylinder, "Condenser_Capsule", mic.transform, new Vector3(0, 0.265f, 0.095f), new Vector3(0.022f, 0.025f, 0.022f), matChairFabric);
            capsule.transform.localRotation = Quaternion.Euler(32f, 0, 0);

            // 5. LED Status Ring around capsule collar
            GameObject ledRing = CreatePrimitive(PrimitiveType.Cylinder, "Capsule_LED_Ring", mic.transform, new Vector3(0, 0.245f, 0.082f), new Vector3(0.024f, 0.006f, 0.024f), matVRVisor);
            ledRing.transform.localRotation = Quaternion.Euler(32f, 0, 0);

            return mic;
        }

        private GameObject BuildConferenceSpeakerphonePuck(Transform parent, Vector3 localPos, string name)
        {
            GameObject puck = new GameObject(name);
            puck.transform.SetParent(parent, false);
            puck.transform.localPosition = localPos;

            // 1. Sleek circular puck base
            CreatePrimitive(PrimitiveType.Cylinder, "Puck_Base", puck.transform, new Vector3(0, 0.012f, 0), new Vector3(0.20f, 0.014f, 0.20f), matMetalTrim);

            // 2. Center acoustic mesh
            CreatePrimitive(PrimitiveType.Cylinder, "Acoustic_Mesh", puck.transform, new Vector3(0, 0.021f, 0), new Vector3(0.16f, 0.005f, 0.16f), matChairFabric);

            // 3. Illuminated 360-degree LED Status Halo
            CreatePrimitive(PrimitiveType.Cylinder, "Puck_LED_Halo", puck.transform, new Vector3(0, 0.025f, 0), new Vector3(0.06f, 0.005f, 0.06f), matVRVisor);

            return puck;
        }

        private void BuildInterviewerPhotoStand(Transform table, Vector3 position)
        {
            GameObject stand = new GameObject("Interviewer_Desk_PhotoDisplay");
            stand.transform.SetParent(table, false);
            stand.transform.position = position;
            stand.transform.rotation = Quaternion.Euler(0, 195f, 0);

            CreatePrimitive(PrimitiveType.Cube, "Stand_Base", stand.transform, new Vector3(0, 0.01f, 0), new Vector3(0.24f, 0.02f, 0.08f), matMetalTrim);
            GameObject frame = CreatePrimitive(PrimitiveType.Cube, "Photo_Frame", stand.transform, new Vector3(0, 0.12f, -0.01f), new Vector3(0.22f, 0.24f, 0.012f), matMetalTrim);
            frame.transform.localRotation = Quaternion.Euler(12f, 0, 0);

            if (matInterviewerPhoto != null)
            {
                GameObject photoQuad = CreatePrimitive(PrimitiveType.Quad, "Interviewer_Photo_Image", frame.transform, new Vector3(0, 0, -0.52f), new Vector3(0.92f, 0.92f, 1f), matInterviewerPhoto);
                photoQuad.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            }
        }

        private void BuildLaptop(Transform parent, Vector3 position, float yRot, string name)
        {
            GameObject laptop = new GameObject(name);
            laptop.transform.SetParent(parent, false);
            laptop.transform.position = position;
            laptop.transform.rotation = Quaternion.Euler(0, yRot, 0);

            CreatePrimitive(PrimitiveType.Cube, "Base", laptop.transform, new Vector3(0, 0.006f, 0), new Vector3(0.32f, 0.012f, 0.22f), matLaptop);
            CreatePrimitive(PrimitiveType.Cube, "Trackpad", laptop.transform, new Vector3(0, 0.013f, 0.06f), new Vector3(0.11f, 0.002f, 0.07f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Keyboard_Area", laptop.transform, new Vector3(0, 0.013f, -0.03f), new Vector3(0.28f, 0.002f, 0.10f), matChairFabric);

            GameObject lid = new GameObject("Lid_Hinge");
            lid.transform.SetParent(laptop.transform, false);
            lid.transform.localPosition = new Vector3(0, 0.012f, -0.11f);
            lid.transform.localRotation = Quaternion.Euler(-112f, 0, 0);

            CreatePrimitive(PrimitiveType.Cube, "Screen_Shell", lid.transform, new Vector3(0, 0.105f, 0), new Vector3(0.32f, 0.21f, 0.008f), matLaptop);
            CreatePrimitive(PrimitiveType.Cube, "Screen_Display", lid.transform, new Vector3(0, 0.105f, 0.005f), new Vector3(0.30f, 0.19f, 0.003f), matLaptopScreen);
        }

        private void BuildNameplate(Transform parent, Vector3 position, float yRot, string text)
        {
            GameObject plate = new GameObject("Nameplate");
            plate.transform.SetParent(parent, false);
            plate.transform.position = position;
            plate.transform.rotation = Quaternion.Euler(0, yRot, 0);

            GameObject wedge = CreatePrimitive(PrimitiveType.Cube, "Prism", plate.transform, Vector3.zero, new Vector3(0.34f, 0.045f, 0.065f), matMetalTrim);
            wedge.transform.localRotation = Quaternion.Euler(25f, 0, 0);

            GameObject textObj = new GameObject("Nameplate_Text");
            textObj.transform.SetParent(plate.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0.018f, -0.015f);
            textObj.transform.localRotation = Quaternion.Euler(25f, 0, 0);
            TextMesh tm = textObj.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 20;
            tm.characterSize = 0.0038f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.95f, 0.85f, 0.25f);
            tm.fontStyle = FontStyle.Bold;
        }

        private void BuildOfficeDecor(Transform parent)
        {
            GameObject plantGroup = new GameObject("Potted_Plant");
            plantGroup.transform.SetParent(parent, false);
            plantGroup.transform.position = new Vector3(3.2f, 0, 2.3f);

            CreatePrimitive(PrimitiveType.Cylinder, "Pot", plantGroup.transform, new Vector3(0, 0.35f, 0), new Vector3(0.55f, 0.35f, 0.55f), matPot);
            CreatePrimitive(PrimitiveType.Cylinder, "Soil", plantGroup.transform, new Vector3(0, 0.65f, 0), new Vector3(0.50f, 0.05f, 0.50f), matFloor);
            CreatePrimitive(PrimitiveType.Cylinder, "Stem", plantGroup.transform, new Vector3(0, 1.1f, 0), new Vector3(0.06f, 0.5f, 0.06f), matWoodAccent);

            CreatePrimitive(PrimitiveType.Sphere, "Foliage_1", plantGroup.transform, new Vector3(0, 1.6f, 0), new Vector3(0.7f, 0.5f, 0.7f), matPlant);
            CreatePrimitive(PrimitiveType.Sphere, "Foliage_2", plantGroup.transform, new Vector3(0.15f, 1.85f, 0.1f), new Vector3(0.6f, 0.45f, 0.6f), matPlant);
            CreatePrimitive(PrimitiveType.Sphere, "Foliage_3", plantGroup.transform, new Vector3(-0.1f, 1.4f, -0.1f), new Vector3(0.5f, 0.4f, 0.5f), matPlant);

            GameObject clock = CreatePrimitive(PrimitiveType.Cylinder, "Wall_Clock", parent, new Vector3(3.98f, 2.4f, 0), new Vector3(0.45f, 0.02f, 0.45f), matWhiteboard);
            clock.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            CreatePrimitive(PrimitiveType.Cylinder, "Clock_Rim", parent, new Vector3(3.97f, 2.4f, 0), new Vector3(0.48f, 0.015f, 0.48f), matMetalTrim).transform.localRotation = Quaternion.Euler(0, 0, 90f);
        }

        private GameObject BuildInterviewerAvatar(Transform parent)
        {
            // Always build a fresh procedural interviewer avatar — no FBX dependency
            // Destroy any pre-existing interviewer avatars first
            Transform existing = parent.Find("Avatar_Interviewer_DrSarah");
            if (existing != null) DestroyImmediate(existing.gameObject);

            // Chair is at z = +1.25 facing -Z (rotation 180). Avatar sits there.
            GameObject avatar = new GameObject("Avatar_Interviewer_DrSarah");
            avatar.transform.SetParent(parent, false);
            // Seated position: chair seat height ~0.46, avatar origin at floor
            avatar.transform.position = new Vector3(0f, 0f, 1.25f);
            avatar.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // faces -Z (toward candidate)

            // ── LOWER BODY (seated, legs bent forward) ──────────────────────────
            // Hips block
            CreatePrimitive(PrimitiveType.Cube, "Hips", avatar.transform,
                new Vector3(0f, 0.50f, -0.05f), new Vector3(0.36f, 0.18f, 0.28f), matInterviewerSuit);

            // Thighs (horizontal, pointing forward)
            CreatePrimitive(PrimitiveType.Cube, "Thigh_L", avatar.transform,
                new Vector3(-0.11f, 0.47f, 0.16f), new Vector3(0.14f, 0.13f, 0.38f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "Thigh_R", avatar.transform,
                new Vector3(0.11f, 0.47f, 0.16f), new Vector3(0.14f, 0.13f, 0.38f), matInterviewerSuit);

            // Shins (vertical, dropping down)
            CreatePrimitive(PrimitiveType.Cube, "Shin_L", avatar.transform,
                new Vector3(-0.11f, 0.20f, 0.34f), new Vector3(0.12f, 0.36f, 0.12f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "Shin_R", avatar.transform,
                new Vector3(0.11f, 0.20f, 0.34f), new Vector3(0.12f, 0.36f, 0.12f), matInterviewerSuit);

            // Shoes
            CreatePrimitive(PrimitiveType.Cube, "Shoe_L", avatar.transform,
                new Vector3(-0.11f, 0.04f, 0.38f), new Vector3(0.12f, 0.07f, 0.22f), matHairDark);
            CreatePrimitive(PrimitiveType.Cube, "Shoe_R", avatar.transform,
                new Vector3(0.11f, 0.04f, 0.38f), new Vector3(0.12f, 0.07f, 0.22f), matHairDark);

            // ── UPPER BODY ───────────────────────────────────────────────────────
            GameObject chest = CreatePrimitive(PrimitiveType.Cube, "Chest", avatar.transform,
                new Vector3(0f, 0.80f, -0.02f), new Vector3(0.40f, 0.46f, 0.24f), matInterviewerSuit);
            // White shirt collar
            CreatePrimitive(PrimitiveType.Cube, "Shirt_Collar", chest.transform,
                new Vector3(0f, 0.13f, 0.124f), new Vector3(0.15f, 0.20f, 0.012f), matWhiteboard);
            // Blue tie
            CreatePrimitive(PrimitiveType.Cube, "Tie", chest.transform,
                new Vector3(0f, -0.02f, 0.130f), new Vector3(0.06f, 0.30f, 0.010f), matInterviewerTie);
            // Lapels
            CreatePrimitive(PrimitiveType.Cube, "Lapel_L", chest.transform,
                new Vector3(-0.09f, 0.10f, 0.122f), new Vector3(0.08f, 0.18f, 0.010f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "Lapel_R", chest.transform,
                new Vector3(0.09f, 0.10f, 0.122f), new Vector3(0.08f, 0.18f, 0.010f), matInterviewerSuit);

            // Lanyard / ID badge
            CreatePrimitive(PrimitiveType.Cube, "ID_Badge", chest.transform,
                new Vector3(-0.09f, -0.10f, 0.132f), new Vector3(0.07f, 0.10f, 0.008f), matLaptopScreen);

            // ── NECK ─────────────────────────────────────────────────────────────
            CreatePrimitive(PrimitiveType.Cylinder, "Neck", avatar.transform,
                new Vector3(0f, 1.06f, -0.01f), new Vector3(0.10f, 0.08f, 0.10f), matSkin);

            // ── HEAD ─────────────────────────────────────────────────────────────
            GameObject head = new GameObject("Head");
            head.transform.SetParent(avatar.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.24f, -0.01f);

            CreatePrimitive(PrimitiveType.Sphere, "Face", head.transform,
                Vector3.zero, new Vector3(0.23f, 0.26f, 0.22f), matSkin);
            // Hair — dark auburn
            CreatePrimitive(PrimitiveType.Sphere, "Hair_Top", head.transform,
                new Vector3(0f, 0.09f, -0.02f), new Vector3(0.25f, 0.17f, 0.25f), matHairPhoto);
            CreatePrimitive(PrimitiveType.Cube, "Hair_Back", head.transform,
                new Vector3(0f, 0.02f, -0.11f), new Vector3(0.23f, 0.22f, 0.06f), matHairPhoto);
            // Ears
            CreatePrimitive(PrimitiveType.Sphere, "Ear_L", head.transform,
                new Vector3(-0.12f, 0f, 0f), new Vector3(0.05f, 0.07f, 0.04f), matSkin);
            CreatePrimitive(PrimitiveType.Sphere, "Ear_R", head.transform,
                new Vector3(0.12f, 0f, 0f), new Vector3(0.05f, 0.07f, 0.04f), matSkin);
            // Eyes
            CreatePrimitive(PrimitiveType.Sphere, "Eye_L", head.transform,
                new Vector3(-0.06f, 0.04f, 0.10f), new Vector3(0.04f, 0.035f, 0.03f), matHairDark);
            CreatePrimitive(PrimitiveType.Sphere, "Eye_R", head.transform,
                new Vector3(0.06f, 0.04f, 0.10f), new Vector3(0.04f, 0.035f, 0.03f), matHairDark);
            // Glasses frames
            CreatePrimitive(PrimitiveType.Cube, "Glasses_Bridge", head.transform,
                new Vector3(0f, 0.04f, 0.113f), new Vector3(0.08f, 0.012f, 0.008f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Glasses_L", head.transform,
                new Vector3(-0.07f, 0.04f, 0.110f), new Vector3(0.06f, 0.048f, 0.006f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Glasses_R", head.transform,
                new Vector3(0.07f, 0.04f, 0.110f), new Vector3(0.06f, 0.048f, 0.006f), matMetalTrim);
            // Nose
            CreatePrimitive(PrimitiveType.Sphere, "Nose", head.transform,
                new Vector3(0f, -0.02f, 0.112f), new Vector3(0.04f, 0.04f, 0.04f), matSkin);
            // Mouth
            CreatePrimitive(PrimitiveType.Cube, "Mouth", head.transform,
                new Vector3(0f, -0.07f, 0.110f), new Vector3(0.08f, 0.018f, 0.012f), matHairDark);

            // ── LEFT ARM (resting on table, holding folder) ───────────────────────
            GameObject leftArm = new GameObject("Left_Arm");
            leftArm.transform.SetParent(avatar.transform, false);
            leftArm.transform.localPosition = new Vector3(-0.24f, 0.90f, 0.02f);

            CreatePrimitive(PrimitiveType.Cube, "UpperArm_L", leftArm.transform,
                new Vector3(0f, -0.12f, 0.10f), new Vector3(0.13f, 0.28f, 0.13f), matInterviewerSuit)
                .transform.localRotation = Quaternion.Euler(30f, 0f, 10f);
            CreatePrimitive(PrimitiveType.Cube, "Forearm_L", leftArm.transform,
                new Vector3(0.06f, -0.15f, 0.30f), new Vector3(0.11f, 0.11f, 0.28f), matInterviewerSuit)
                .transform.localRotation = Quaternion.Euler(-8f, 12f, 0f);
            CreatePrimitive(PrimitiveType.Sphere, "Hand_L", leftArm.transform,
                new Vector3(0.12f, -0.14f, 0.44f), new Vector3(0.09f, 0.06f, 0.11f), matSkin);

            // ── RIGHT ARM (gesturing / presenting) ────────────────────────────────
            GameObject rightArm = new GameObject("Right_Arm");
            rightArm.transform.SetParent(avatar.transform, false);
            rightArm.transform.localPosition = new Vector3(0.24f, 0.90f, 0.02f);

            CreatePrimitive(PrimitiveType.Cube, "UpperArm_R", rightArm.transform,
                new Vector3(0f, -0.10f, 0.12f), new Vector3(0.13f, 0.28f, 0.13f), matInterviewerSuit)
                .transform.localRotation = Quaternion.Euler(40f, 0f, -10f);
            CreatePrimitive(PrimitiveType.Cube, "Forearm_R", rightArm.transform,
                new Vector3(-0.05f, -0.08f, 0.34f), new Vector3(0.11f, 0.11f, 0.28f), matInterviewerSuit)
                .transform.localRotation = Quaternion.Euler(-14f, -18f, 0f);
            CreatePrimitive(PrimitiveType.Sphere, "Hand_R", rightArm.transform,
                new Vector3(-0.10f, -0.05f, 0.46f), new Vector3(0.09f, 0.06f, 0.11f), matSkin);

            // ── PORTFOLIO FOLDER ─────────────────────────────────────────────────
            GameObject portfolio = new GameObject("Portfolio_Folder");
            portfolio.transform.SetParent(avatar.transform, false);
            portfolio.transform.localPosition = new Vector3(-0.06f, 0.77f, 0.36f);
            portfolio.transform.localRotation = Quaternion.Euler(16f, -10f, 4f);
            CreatePrimitive(PrimitiveType.Cube, "Folder_Cover", portfolio.transform,
                Vector3.zero, new Vector3(0.28f, 0.01f, 0.36f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "Folder_Pages", portfolio.transform,
                new Vector3(0f, 0.012f, 0f), new Vector3(0.26f, 0.008f, 0.34f), matWhiteboard);

            // ── WIRE UP ANIMATION CONTROLLER ─────────────────────────────────────
            var anim = avatar.AddComponent<AvatarAnimationController>();
            anim.isInterviewer = true;
            anim.isCandidateVR = false;
            anim.head = head.transform;
            anim.chest = chest.transform;
            anim.leftArm = leftArm.transform;
            anim.rightArm = rightArm.transform;

            return avatar;
        }

        private GameObject BuildCandidateVRAvatar(Transform parent)
        {
            // Always build a fresh procedural candidate avatar — no FBX dependency
            // Destroy any pre-existing candidate avatars first
            Transform existing = parent.Find("Avatar_Candidate_Alex_ARVR");
            if (existing != null) DestroyImmediate(existing.gameObject);
            Transform existing2 = parent.Find("Avatar_Candidate");
            if (existing2 != null) DestroyImmediate(existing2.gameObject);

            // Chair is at z = -1.25 facing +Z (rotation 0). Avatar sits there.
            GameObject avatar = new GameObject("Avatar_Candidate_Alex_ARVR");
            avatar.transform.SetParent(parent, false);
            avatar.transform.position = new Vector3(0f, 0f, -1.25f);
            avatar.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // faces +Z (toward interviewer)

            // ── LOWER BODY (seated) ──────────────────────────────────────────────
            CreatePrimitive(PrimitiveType.Cube, "Hips", avatar.transform,
                new Vector3(0f, 0.50f, -0.05f), new Vector3(0.36f, 0.18f, 0.28f), matCandidateHoodie);

            CreatePrimitive(PrimitiveType.Cube, "Thigh_L", avatar.transform,
                new Vector3(-0.11f, 0.47f, 0.16f), new Vector3(0.14f, 0.13f, 0.38f), matCandidateHoodie);
            CreatePrimitive(PrimitiveType.Cube, "Thigh_R", avatar.transform,
                new Vector3(0.11f, 0.47f, 0.16f), new Vector3(0.14f, 0.13f, 0.38f), matCandidateHoodie);

            CreatePrimitive(PrimitiveType.Cube, "Shin_L", avatar.transform,
                new Vector3(-0.11f, 0.20f, 0.34f), new Vector3(0.12f, 0.36f, 0.12f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "Shin_R", avatar.transform,
                new Vector3(0.11f, 0.20f, 0.34f), new Vector3(0.12f, 0.36f, 0.12f), matInterviewerSuit);

            // Sneakers (white)
            CreatePrimitive(PrimitiveType.Cube, "Sneaker_L", avatar.transform,
                new Vector3(-0.11f, 0.04f, 0.40f), new Vector3(0.12f, 0.08f, 0.24f), matVRHeadset);
            CreatePrimitive(PrimitiveType.Cube, "Sneaker_R", avatar.transform,
                new Vector3(0.11f, 0.04f, 0.40f), new Vector3(0.12f, 0.08f, 0.24f), matVRHeadset);

            // ── UPPER BODY — Hoodie ──────────────────────────────────────────────
            GameObject chest = CreatePrimitive(PrimitiveType.Cube, "Chest", avatar.transform,
                new Vector3(0f, 0.80f, -0.02f), new Vector3(0.40f, 0.46f, 0.24f), matCandidateHoodie);
            // Hoodie front pocket
            CreatePrimitive(PrimitiveType.Cube, "Hoodie_Pocket", chest.transform,
                new Vector3(0f, -0.11f, 0.125f), new Vector3(0.26f, 0.16f, 0.018f), matCandidateHoodie);
            // Hoodie centre zipper line
            CreatePrimitive(PrimitiveType.Cube, "Zipper", chest.transform,
                new Vector3(0f, 0.04f, 0.127f), new Vector3(0.012f, 0.28f, 0.010f), matHairDark);
            // Small logo patch on chest
            CreatePrimitive(PrimitiveType.Cube, "Logo_Patch", chest.transform,
                new Vector3(-0.10f, 0.12f, 0.128f), new Vector3(0.06f, 0.06f, 0.010f), matLaptopScreen);

            // ── NECK ────────────────────────────────────────────────────────────
            CreatePrimitive(PrimitiveType.Cylinder, "Neck", avatar.transform,
                new Vector3(0f, 1.06f, -0.01f), new Vector3(0.10f, 0.08f, 0.10f), matSkin);

            // ── HEAD ─────────────────────────────────────────────────────────────
            GameObject head = new GameObject("Head");
            head.transform.SetParent(avatar.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.24f, -0.01f);

            CreatePrimitive(PrimitiveType.Sphere, "Face", head.transform,
                Vector3.zero, new Vector3(0.23f, 0.26f, 0.22f), matSkin);
            // Dark hair — short
            CreatePrimitive(PrimitiveType.Sphere, "Hair_Top", head.transform,
                new Vector3(0f, 0.10f, -0.02f), new Vector3(0.25f, 0.16f, 0.25f), matHairDark);
            // Ears
            CreatePrimitive(PrimitiveType.Sphere, "Ear_L", head.transform,
                new Vector3(-0.12f, 0f, 0f), new Vector3(0.05f, 0.07f, 0.04f), matSkin);
            CreatePrimitive(PrimitiveType.Sphere, "Ear_R", head.transform,
                new Vector3(0.12f, 0f, 0f), new Vector3(0.05f, 0.07f, 0.04f), matSkin);
            // Eyes (under the headset — partially visible)
            CreatePrimitive(PrimitiveType.Sphere, "Eye_L", head.transform,
                new Vector3(-0.06f, 0.03f, 0.10f), new Vector3(0.04f, 0.035f, 0.03f), matHairDark);
            CreatePrimitive(PrimitiveType.Sphere, "Eye_R", head.transform,
                new Vector3(0.06f, 0.03f, 0.10f), new Vector3(0.04f, 0.035f, 0.03f), matHairDark);
            // Nose and mouth
            CreatePrimitive(PrimitiveType.Sphere, "Nose", head.transform,
                new Vector3(0f, -0.02f, 0.112f), new Vector3(0.04f, 0.04f, 0.04f), matSkin);
            CreatePrimitive(PrimitiveType.Cube, "Mouth", head.transform,
                new Vector3(0f, -0.07f, 0.110f), new Vector3(0.08f, 0.018f, 0.012f), matHairDark);

            // ── SPATIAL AR/VR HEADSET ────────────────────────────────────────────
            GameObject headset = new GameObject("Spatial_ARVR_Headset");
            headset.transform.SetParent(head.transform, false);
            headset.transform.localPosition = new Vector3(0f, 0.03f, 0.06f);

            // Main headset body
            CreatePrimitive(PrimitiveType.Cube, "Headset_Body", headset.transform,
                new Vector3(0f, 0f, 0.06f), new Vector3(0.26f, 0.12f, 0.10f), matVRHeadset);
            // Glowing visor
            GameObject visor = CreatePrimitive(PrimitiveType.Cube, "Visor", headset.transform,
                new Vector3(0f, 0f, 0.115f), new Vector3(0.24f, 0.09f, 0.015f), matVRVisor);
            // Visor glow light
            GameObject visorLight = new GameObject("Visor_GlowLight");
            visorLight.transform.SetParent(visor.transform, false);
            visorLight.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            Light vLight = visorLight.AddComponent<Light>();
            vLight.type = LightType.Point;
            vLight.color = new Color(0.0f, 0.85f, 1.0f);
            vLight.range = 1.4f;
            vLight.intensity = 10f;
            // Side camera bumps
            CreatePrimitive(PrimitiveType.Sphere, "Cam_L", headset.transform,
                new Vector3(-0.13f, 0f, 0.12f), new Vector3(0.04f, 0.04f, 0.05f), matHairDark);
            CreatePrimitive(PrimitiveType.Sphere, "Cam_R", headset.transform,
                new Vector3(0.13f, 0f, 0.12f), new Vector3(0.04f, 0.04f, 0.05f), matHairDark);
            // Audio pods
            CreatePrimitive(PrimitiveType.Cylinder, "AudioPod_L", headset.transform,
                new Vector3(-0.14f, 0f, -0.02f), new Vector3(0.055f, 0.030f, 0.055f), matVRHeadset)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            CreatePrimitive(PrimitiveType.Cylinder, "AudioPod_R", headset.transform,
                new Vector3(0.14f, 0f, -0.02f), new Vector3(0.055f, 0.030f, 0.055f), matVRHeadset)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            // Rear strap / halo band
            CreatePrimitive(PrimitiveType.Cube, "Rear_Band", headset.transform,
                new Vector3(0f, 0f, -0.12f), new Vector3(0.24f, 0.06f, 0.18f), matChairFabric);
            // IPD adjustment knob
            CreatePrimitive(PrimitiveType.Cylinder, "IPD_Knob", headset.transform,
                new Vector3(0f, -0.065f, 0.02f), new Vector3(0.02f, 0.02f, 0.02f), matMetalTrim);
            // Forehead pad
            CreatePrimitive(PrimitiveType.Cube, "Forehead_Pad", headset.transform,
                new Vector3(0f, 0.07f, -0.04f), new Vector3(0.20f, 0.03f, 0.05f), matChairFabric);

            // ── LEFT ARM (resting on table) ──────────────────────────────────────
            GameObject leftArm = new GameObject("Left_Arm");
            leftArm.transform.SetParent(avatar.transform, false);
            leftArm.transform.localPosition = new Vector3(-0.24f, 0.90f, 0.02f);

            CreatePrimitive(PrimitiveType.Cube, "UpperArm_L", leftArm.transform,
                new Vector3(0f, -0.12f, 0.10f), new Vector3(0.13f, 0.28f, 0.13f), matCandidateHoodie)
                .transform.localRotation = Quaternion.Euler(28f, 0f, 8f);
            CreatePrimitive(PrimitiveType.Cube, "Forearm_L", leftArm.transform,
                new Vector3(0.05f, -0.14f, 0.30f), new Vector3(0.11f, 0.11f, 0.28f), matCandidateHoodie);
            CreatePrimitive(PrimitiveType.Sphere, "Hand_L", leftArm.transform,
                new Vector3(0.07f, -0.14f, 0.46f), new Vector3(0.09f, 0.06f, 0.11f), matSkin);

            // ── RIGHT ARM (raised in spatial gesture) ─────────────────────────────
            GameObject rightArm = new GameObject("Right_Arm");
            rightArm.transform.SetParent(avatar.transform, false);
            rightArm.transform.localPosition = new Vector3(0.24f, 0.90f, 0.02f);

            CreatePrimitive(PrimitiveType.Cube, "UpperArm_R", rightArm.transform,
                new Vector3(0f, -0.08f, 0.14f), new Vector3(0.13f, 0.28f, 0.13f), matCandidateHoodie)
                .transform.localRotation = Quaternion.Euler(48f, 0f, -10f);
            CreatePrimitive(PrimitiveType.Cube, "Forearm_R", rightArm.transform,
                new Vector3(-0.05f, -0.04f, 0.36f), new Vector3(0.11f, 0.11f, 0.28f), matCandidateHoodie)
                .transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Sphere, "Hand_R", rightArm.transform,
                new Vector3(-0.07f, 0.04f, 0.50f), new Vector3(0.09f, 0.06f, 0.11f), matSkin);

            // ── ANIMATION CONTROLLER ─────────────────────────────────────────────
            var anim = avatar.AddComponent<AvatarAnimationController>();
            anim.isInterviewer = false;
            anim.isCandidateVR = true;
            anim.head = head.transform;
            anim.chest = chest.transform;
            anim.leftArm = leftArm.transform;
            anim.rightArm = rightArm.transform;

            return avatar;
        }

        private void ConfigureFbxAvatar(GameObject fbxAvatar, bool isInterviewer)
        {
            // 1. Strip embedded Blender cameras and lights
            var toDestroy = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in fbxAvatar.transform)
            {
                string cName = child.name.ToLower();
                if (cName.Contains("camera") || cName.Contains("light") || child.GetComponent<Light>() != null || child.GetComponent<Camera>() != null)
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            foreach (var go in toDestroy) Destroy(go);

            // 2. Reparent facial features under Head
            Transform headTransform = fbxAvatar.transform.Find("Head");
            if (headTransform != null)
            {
                string[] faceParts = { "Hair", "Hair Left", "Hair Right", "Left Eye", "Right Eye", "Left Ear", "Right Ear", "Nose", "Smile" };
                foreach (var partName in faceParts)
                {
                    Transform part = fbxAvatar.transform.Find(partName);
                    if (part != null && part != headTransform) part.SetParent(headTransform, true);
                }
            }

            Transform leftArm = fbxAvatar.transform.Find("Left Arm");
            Transform leftHand = fbxAvatar.transform.Find("Left Hand");
            if (leftArm != null && leftHand != null) leftHand.SetParent(leftArm, true);

            Transform rightArm = fbxAvatar.transform.Find("Right Arm");
            Transform rightHand = fbxAvatar.transform.Find("Right Hand");
            if (rightArm != null && rightHand != null) rightHand.SetParent(rightArm, true);

            Transform chestTransform = fbxAvatar.transform.Find("Suit Torso");

            // 3. Apply identical character materials
            ApplyRuntimeCharacterMaterials(fbxAvatar);

            // 4. Attach animation controller
            var fbxAnim = fbxAvatar.GetComponent<AvatarAnimationController>();
            if (fbxAnim == null) fbxAnim = fbxAvatar.AddComponent<AvatarAnimationController>();
            fbxAnim.isInterviewer = isInterviewer;
            fbxAnim.isCandidateVR = !isInterviewer;
            fbxAnim.head = headTransform;
            fbxAnim.chest = chestTransform;
            fbxAnim.leftArm = leftArm;
            fbxAnim.rightArm = rightArm;
        }

        private void ApplyRuntimeCharacterMaterials(GameObject avatar)
        {
            foreach (var mr in avatar.GetComponentsInChildren<MeshRenderer>(true))
            {
                string n = mr.name;
                if (n.Contains("Suit") || n.Contains("Thigh") || n.Contains("Lower Leg") || n.Contains("Arm"))
                {
                    mr.material = matInterviewerSuit;
                }
                else if (n.Contains("Shirt") || n.Contains("Collar"))
                {
                    mr.material = matWhiteboard;
                }
                else if (n.Contains("Tie"))
                {
                    mr.material = matInterviewerTie;
                }
                else if (n.Contains("Shoe"))
                {
                    mr.material = matHairDark;
                }
                else if (n.Contains("Hair"))
                {
                    mr.material = matHairDark;
                }
                else if (n.Contains("Eye"))
                {
                    mr.material = matHairDark;
                }
                else if (n.Contains("Smile"))
                {
                    mr.material = matInterviewerTie;
                }
                else
                {
                    mr.material = matSkin;
                }
            }
        }

        private GameObject[] BuildARHologramSystem(Transform parent)
        {
            GameObject holoRoot = new GameObject("AR_Hologram_SpatialSystem");
            holoRoot.transform.SetParent(parent, false);

            GameObject panel1 = CreatePrimitive(PrimitiveType.Cube, "Holo_Panel_Telemetry", holoRoot.transform, new Vector3(-0.35f, 1.12f, -0.50f), new Vector3(0.38f, 0.26f, 0.008f), matARHologram);
            panel1.transform.localRotation = Quaternion.Euler(8f, -20f, 0);
            var effect1 = panel1.AddComponent<HologramEffect>();
            effect1.bobSpeed = 1.6f;
            effect1.bobHeight = 0.02f;
            effect1.rotateY = false;

            CreatePrimitive(PrimitiveType.Cube, "Holo_Header", panel1.transform, new Vector3(0, 0.40f, 0.002f), new Vector3(0.92f, 0.12f, 0.002f), matVRVisor);

            GameObject panel2 = CreatePrimitive(PrimitiveType.Cube, "Holo_Panel_CodeGraph", holoRoot.transform, new Vector3(0.38f, 1.16f, -0.45f), new Vector3(0.32f, 0.22f, 0.008f), matARHologram);
            panel2.transform.localRotation = Quaternion.Euler(6f, 22f, 0);
            var effect2 = panel2.AddComponent<HologramEffect>();
            effect2.bobSpeed = 1.4f;
            effect2.bobHeight = 0.025f;
            effect2.rotateY = false;

            GameObject holo3D = new GameObject("Holo_Spatial_3D_Artifact");
            holo3D.transform.SetParent(holoRoot.transform, false);
            holo3D.transform.position = new Vector3(0, 1.05f, -0.15f);

            GameObject sphereCore = CreatePrimitive(PrimitiveType.Sphere, "Core_Sphere", holo3D.transform, Vector3.zero, new Vector3(0.12f, 0.12f, 0.12f), matARHologram);
            CreatePrimitive(PrimitiveType.Cylinder, "Orbit_Ring_1", holo3D.transform, Vector3.zero, new Vector3(0.24f, 0.004f, 0.24f), matVRVisor).transform.localRotation = Quaternion.Euler(45f, 0, 0);
            CreatePrimitive(PrimitiveType.Cylinder, "Orbit_Ring_2", holo3D.transform, Vector3.zero, new Vector3(0.28f, 0.004f, 0.28f), matARHologram).transform.localRotation = Quaternion.Euler(-45f, 45f, 0);

            var effect3D = holo3D.AddComponent<HologramEffect>();
            effect3D.targetRenderer = sphereCore.GetComponent<MeshRenderer>();
            effect3D.bobSpeed = 2.0f;
            effect3D.bobHeight = 0.035f;
            effect3D.rotateSpeed = 35f;
            effect3D.rotateY = true;

            GameObject holoLight = new GameObject("Holo_Ambient_PointLight");
            holoLight.transform.SetParent(holo3D.transform, false);
            holoLight.transform.localPosition = Vector3.zero;
            Light hl = holoLight.AddComponent<Light>();
            hl.type = LightType.Point;
            hl.color = new Color(0.0f, 0.8f, 1.0f);
            hl.range = 2.2f;
            hl.intensity = 6.0f;

            return new GameObject[] { panel1, panel2, holo3D };
        }

        private void SetupCameraAndLighting(Transform parent, GameObject interviewerObj, GameObject candidateObj, GameObject[] holograms)
        {
            var interviewerAnim = interviewerObj != null ? interviewerObj.GetComponent<AvatarAnimationController>() : null;
            var candidateAnim = candidateObj != null ? candidateObj.GetComponent<AvatarAnimationController>() : null;

            if (interviewerAnim != null && candidateAnim != null)
            {
                interviewerAnim.lookTarget = candidateAnim.head;
                candidateAnim.lookTarget = interviewerAnim.head;
            }

            GameObject camRig = new GameObject("Camera_Rig");
            camRig.transform.SetParent(parent, false);

            GameObject overviewPoint = new GameObject("ViewPoint_Overview");
            overviewPoint.transform.SetParent(camRig.transform, false);
            overviewPoint.transform.position = new Vector3(2.6f, 2.1f, -2.2f);
            overviewPoint.transform.rotation = Quaternion.Euler(22f, -48f, 0);

            GameObject interviewerEye = new GameObject("ViewPoint_InterviewerEye");
            interviewerEye.transform.SetParent(camRig.transform, false);
            interviewerEye.transform.position = new Vector3(0, 1.20f, 1.25f);
            interviewerEye.transform.rotation = Quaternion.Euler(0f, 180f, 0);

            GameObject candidateEye = new GameObject("ViewPoint_CandidateEye");
            candidateEye.transform.SetParent(camRig.transform, false);
            candidateEye.transform.position = new Vector3(0, 1.20f, -1.25f);
            candidateEye.transform.rotation = Quaternion.Euler(0f, 0, 0);

            GameObject sidePoint = new GameObject("ViewPoint_SideBroadcast");
            sidePoint.transform.SetParent(camRig.transform, false);
            sidePoint.transform.position = new Vector3(-2.4f, 1.26f, 0);
            sidePoint.transform.rotation = Quaternion.Euler(0f, 90f, 0);

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            mainCam.transform.position = candidateEye.transform.position;
            mainCam.transform.rotation = candidateEye.transform.rotation;
            mainCam.fieldOfView = 58f;
            mainCam.nearClipPlane = 0.1f;

            var freeFly = mainCam.GetComponent<FreeFlyCamera>();
            if (freeFly == null) freeFly = mainCam.gameObject.AddComponent<FreeFlyCamera>();
            freeFly.enabled = false;

            var controller = mainCam.GetComponent<InterviewRoomController>();
            if (controller == null) controller = mainCam.gameObject.AddComponent<InterviewRoomController>();

            controller.mainCamera = mainCam;
            controller.overviewCamPoint = overviewPoint.transform;
            controller.interviewerEyePoint = interviewerEye.transform;
            controller.candidateEyePoint = candidateEye.transform;
            controller.sideViewPoint = sidePoint.transform;
            controller.freeFlyController = freeFly;
            controller.interviewerAvatar = interviewerAnim;
            controller.candidateAvatar = candidateAnim;
            controller.arHologramObjects = holograms;

            var aiGen = mainCam.GetComponent<AIInterviewQuestionGenerator>();
            if (aiGen == null) aiGen = mainCam.gameObject.AddComponent<AIInterviewQuestionGenerator>();
            controller.aiQuestionGenerator = aiGen;

            GameObject sunObj = GameObject.Find("Directional Light");
            if (sunObj == null) sunObj = new GameObject("Directional Light");
            Light sunLight = sunObj.GetComponent<Light>();
            if (sunLight == null) sunLight = sunObj.AddComponent<Light>();

            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1.0f, 0.96f, 0.90f);
            sunLight.intensity = 1.6f;
            sunLight.shadows = LightShadows.Soft;
            sunObj.transform.rotation = Quaternion.Euler(38f, 25f, 0);

            controller.mainDirectionalLight = sunLight;

            SetupAudioSystem(parent, interviewerObj, candidateObj, controller);
        }

        private void SetupAudioSystem(Transform root, GameObject interviewerObj, GameObject candidateObj, InterviewRoomController controller)
        {
            var audioCtrl = root.GetComponent<InterviewRoomAudioController>();
            if (audioCtrl == null) audioCtrl = root.gameObject.AddComponent<InterviewRoomAudioController>();

            // 1. Interviewer Audio Source
            Transform interviewerHead = interviewerObj != null ? interviewerObj.transform.Find("Head") : null;
            Transform interviewerChair = root.Find("Interviewer_Chair");
            Transform interviewerAudioObj = interviewerHead != null ? interviewerHead : (interviewerChair != null ? interviewerChair : root);
            var interviewerSrc = interviewerAudioObj.GetComponent<AudioSource>();
            if (interviewerSrc == null) interviewerSrc = interviewerAudioObj.gameObject.AddComponent<AudioSource>();
            interviewerSrc.spatialBlend = 0.85f;
            interviewerSrc.minDistance = 0.5f;
            interviewerSrc.maxDistance = 14f;
            interviewerSrc.rolloffMode = AudioRolloffMode.Linear;
            interviewerSrc.playOnAwake = false;
            audioCtrl.interviewerAudioSource = interviewerSrc;

            // 2. Candidate Audio Source
            Transform candidateHead = candidateObj != null ? candidateObj.transform.Find("Head") : null;
            Transform candidateChair = root.Find("Candidate_Chair");
            Transform candidateAudioObj = candidateHead != null ? candidateHead : (candidateChair != null ? candidateChair : root);
            var candidateSrc = candidateAudioObj.GetComponent<AudioSource>();
            if (candidateSrc == null) candidateSrc = candidateAudioObj.gameObject.AddComponent<AudioSource>();
            candidateSrc.spatialBlend = 0.85f;
            candidateSrc.minDistance = 0.5f;
            candidateSrc.maxDistance = 14f;
            candidateSrc.rolloffMode = AudioRolloffMode.Linear;
            candidateSrc.playOnAwake = false;
            audioCtrl.candidateAudioSource = candidateSrc;

            // 3. Conference Table Mic Audio Source
            Transform puck = root.Find("Table/Conference_Speakerphone_Puck");
            Transform puckTarget = puck != null ? puck : root;
            var conferenceSrc = puckTarget.GetComponent<AudioSource>();
            if (conferenceSrc == null) conferenceSrc = puckTarget.gameObject.AddComponent<AudioSource>();
            conferenceSrc.spatialBlend = 0.7f;
            conferenceSrc.playOnAwake = false;
            audioCtrl.conferenceMicAudioSource = conferenceSrc;

            // 4. Ambient Audio Source
            var ambientSrc = root.GetComponent<AudioSource>();
            if (ambientSrc == null) ambientSrc = root.gameObject.AddComponent<AudioSource>();
            ambientSrc.spatialBlend = 0.15f;
            ambientSrc.playOnAwake = true;
            ambientSrc.loop = true;
            audioCtrl.ambientAudioSource = ambientSrc;

            // 5. LED Indicator Renderers
            Transform intervRing = root.Find("Table/Interviewer_Gooseneck_Mic/Capsule_LED_Ring");
            if (intervRing != null) audioCtrl.interviewerMicLed = intervRing.GetComponent<Renderer>();

            Transform candRing = root.Find("Table/Candidate_Gooseneck_Mic/Capsule_LED_Ring");
            if (candRing != null) audioCtrl.candidateMicLed = candRing.GetComponent<Renderer>();

            Transform puckRing = root.Find("Table/Conference_Speakerphone_Puck/Puck_LED_Halo");
            if (puckRing != null) audioCtrl.conferencePuckLed = puckRing.GetComponent<Renderer>();

            if (controller != null)
            {
                controller.audioController = audioCtrl;
            }

            audioCtrl.InitializeVoiceAudioClips();
        }

        private void SetupVRGogglesRigAndVirtualScreen(Transform parent)
        {
            var controller = Object.FindAnyObjectByType<InterviewRoomController>();
            Camera mainCam = Camera.main;

            Transform camRig = parent.Find("Camera_Rig");
            Transform candEye = camRig != null ? camRig.Find("ViewPoint_CandidateEye") : null;
            Transform intervEye = camRig != null ? camRig.Find("ViewPoint_InterviewerEye") : null;
            Transform overview = camRig != null ? camRig.Find("ViewPoint_Overview") : null;

            // 1. VR Goggles Rig
            Transform existingVRRig = parent.Find("VR_Rig");
            GameObject vrRigObj;
            if (existingVRRig != null) vrRigObj = existingVRRig.gameObject;
            else
            {
                vrRigObj = new GameObject("VR_Rig");
                vrRigObj.transform.SetParent(parent, false);
            }

            var vrRig = vrRigObj.GetComponent<VRGogglesRig>();
            if (vrRig == null) vrRig = vrRigObj.AddComponent<VRGogglesRig>();

            vrRig.candidateSeatPoint = candEye;
            vrRig.interviewerSeatPoint = intervEye;
            vrRig.directorSeatPoint = overview;
            vrRig.vrCamera = mainCam;
            vrRig.virtualScreen = null;
            vrRig.EnsureRigHierarchy();
            vrRig.SetVRViewMode(VRGogglesRig.VRRigSeat.CandidateChair);

            // 2. Remove any screen between interviewer and candidate
            Transform existingScreen = parent.Find("VR_Virtual_Screen");
            if (existingScreen != null)
            {
                if (Application.isPlaying) Destroy(existingScreen.gameObject);
                else DestroyImmediate(existingScreen.gameObject);
            }

            if (controller != null)
            {
                controller.vrRig = vrRig;
                controller.virtualScreen = null;
            }

            // 3. Speech-to-Text Transcription Engine & 3D Avatar Bubbles
            var stt = parent.GetComponent<SpeechToTextTranscriptionController>();
            if (stt == null) stt = parent.gameObject.AddComponent<SpeechToTextTranscriptionController>();
            stt.roomController = controller;
            stt.audioController = parent.GetComponent<InterviewRoomAudioController>();
            stt.virtualScreen = null;
            stt.SetupAvatarBubblesIfMissing();

            if (controller != null)
            {
                controller.sttController = stt;
            }

            Debug.Log("[InterviewRoomBootstrap] VR Goggles Rig and Speech-to-Text Engine successfully deployed with unobstructed line-of-sight!");
        }

        private GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;

            if (material != null)
            {
                var renderer = obj.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.material = material;
            }

            return obj;
        }
    }
}
