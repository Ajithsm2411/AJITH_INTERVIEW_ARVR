using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using InterviewRoom.VR;
using InterviewRoom.SpeechToText;

namespace InterviewRoom.Editor
{
    [InitializeOnLoad]
    public class InterviewRoomBuilder : UnityEditor.Editor
    {
        static InterviewRoomBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    UpdateAvatarsInOpenScene();
                }
            };
        }

        private static Material matFloor;
        private static Material matWall;
        private static Material matWoodAccent;
        private static Material matCeiling;
        private static Material matTableWalnut;
        private static Material matMetalTrim;
        private static Material matChairFabric;
        private static Material matChairChrome;
        private static Material matGlass;
        private static Material matWhiteboard;
        private static Material matLaptop;
        private static Material matLaptopScreen;
        private static Material matSkin;
        private static Material matHairDark;
        private static Material matHairPhoto;
        private static Material matInterviewerSuit;
        private static Material matInterviewerTie;
        private static Material matCandidateSuit;
        private static Material matCandidateTie;
        private static Material matShirtWhite;
        private static Material matShoesBrown;
        private static Material matShoesSole;
        private static Material matEyesDark;
        private static Material matSmile;
        private static Material matCandidateHoodie;
        private static Material matVRHeadset;
        private static Material matVRVisor;
        private static Material matARHologram;
        private static Material matPlant;
        private static Material matPot;
        private static Material matLightEmissive;
        private static Material matInterviewerPhoto;
        private static Material matInterviewerCutout;

        [MenuItem("Tools/Interview Room/Build Complete Interview Room Scene")]
        public static void BuildSceneMenuItem()
        {
            BuildInterviewRoomScene();
        }

        [MenuItem("Tools/Interview Room/Remove Screen Between Interviewer & Candidate")]
        public static void RemoveScreenMenuItem()
        {
            RemoveScreensBetweenInterviewerAndCandidate();
            var env = GameObject.Find("InterviewRoom_Environment");
            if (env != null) EditorSceneManager.MarkSceneDirty(env.scene);
            Debug.Log("[InterviewRoomBuilder] All screen objects between interviewer and candidate removed!");
        }

        [MenuItem("Tools/Interview Room/Setup VR Goggles Rig (Clean Unobstructed View)")]
        public static void SetupVRGogglesMenuItem()
        {
            GameObject env = GameObject.Find("InterviewRoom_Environment");
            if (env != null)
            {
                SetupVRGogglesRigAndVirtualScreen(env.transform);
                EditorSceneManager.MarkSceneDirty(env.scene);
                Debug.Log("[InterviewRoomBuilder] VR Goggles Rig updated with unobstructed view!");
            }
            else
            {
                BuildInterviewRoomScene();
            }
        }

        [MenuItem("Tools/Interview Room/Update Avatars (Scale & Position for Chairs)")]
        public static void UpdateAvatarsInSceneMenuItem()
        {
            UpdateAvatarsInOpenScene();
        }

        public static void BuildInterviewRoomScene()
        {
            Debug.Log("[InterviewRoomBuilder] Starting interview room generation with Interviewer and Candidate avatars...");

            EnsureFolders();
            CreateOrLoadMaterials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("InterviewRoom_Environment");

            // 1. Build Room Shell
            BuildRoomShell(root.transform);

            // 2. Build Furniture
            Transform tableTransform = BuildInterviewTable(root.transform);
            BuildChairs(root.transform);

            // 3. Build Table Props & Decor (including Interviewer Desk Photo Display)
            BuildTableProps(tableTransform);
            BuildOfficeDecor(root.transform);

            // 4. Build Interviewer Avatar (styled with business suit, tie, glasses, folder)
            GameObject interviewerObj = BuildInterviewerAvatar(root.transform);

            // 5. Build Candidate Avatar (styled with modern hoodie, AR/VR spatial headset, gesture arms)
            GameObject candidateObj = BuildCandidateVRAvatar(root.transform);

            // 6. Build AR Holographic System
            GameObject[] holograms = BuildARHologramSystem(root.transform, candidateObj != null ? candidateObj.transform : null);

            // 7. Setup Camera Rig and Controller
            SetupCameraAndLighting(root.transform, interviewerObj, candidateObj, holograms);

            // 8. Setup VR Goggles Rig and 3D Virtual Screen
            SetupVRGogglesRigAndVirtualScreen(root.transform);

            // Save scenes
            string scenePath = "Assets/Scenes/InterviewRoom.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[InterviewRoomBuilder] Scene saved to {scenePath}!");

            string sampleScenePath = "Assets/Scenes/SampleScene.unity";
            EditorSceneManager.SaveScene(scene, sampleScenePath);
            Debug.Log($"[InterviewRoomBuilder] Scene saved to {sampleScenePath}!");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Textures")) AssetDatabase.CreateFolder("Assets", "Textures");
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");

            string resPrefabPath = "Assets/Resources/Avatar_Character.prefab";
            if (!System.IO.File.Exists(resPrefabPath))
            {
                GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Untitled.fbx");
                if (fbxPrefab != null)
                {
                    PrefabUtility.SaveAsPrefabAsset(fbxPrefab, resPrefabPath);
                }
            }
        }

        private static void CreateOrLoadMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpLit == null) urpLit = Shader.Find("Standard");
            if (urpUnlit == null) urpUnlit = urpLit;

            matFloor = GetOrCreateMaterial("Assets/Materials/Floor_Wood.mat", urpLit, new Color(0.22f, 0.16f, 0.11f), smoothness: 0.65f);
            matWall = GetOrCreateMaterial("Assets/Materials/Wall_Modern.mat", urpLit, new Color(0.88f, 0.89f, 0.91f), smoothness: 0.2f);
            matWoodAccent = GetOrCreateMaterial("Assets/Materials/Wall_WoodSlat.mat", urpLit, new Color(0.38f, 0.24f, 0.16f), smoothness: 0.45f);
            matCeiling = GetOrCreateMaterial("Assets/Materials/Ceiling_Tile.mat", urpLit, new Color(0.84f, 0.85f, 0.86f), smoothness: 0.1f);
            matTableWalnut = GetOrCreateMaterial("Assets/Materials/Table_Walnut.mat", urpLit, new Color(0.28f, 0.18f, 0.12f), smoothness: 0.72f);
            matMetalTrim = GetOrCreateMaterial("Assets/Materials/Metal_Trim.mat", urpLit, new Color(0.75f, 0.76f, 0.78f), metallic: 0.85f, smoothness: 0.8f);
            matChairFabric = GetOrCreateMaterial("Assets/Materials/Chair_Fabric.mat", urpLit, new Color(0.14f, 0.15f, 0.17f), smoothness: 0.25f);
            matChairChrome = GetOrCreateMaterial("Assets/Materials/Chair_Chrome.mat", urpLit, new Color(0.9f, 0.92f, 0.95f), metallic: 0.95f, smoothness: 0.9f);
            matWhiteboard = GetOrCreateMaterial("Assets/Materials/Whiteboard.mat", urpLit, new Color(0.96f, 0.97f, 0.98f), smoothness: 0.85f);
            matLaptop = GetOrCreateMaterial("Assets/Materials/Laptop_Body.mat", urpLit, new Color(0.22f, 0.23f, 0.25f), metallic: 0.8f, smoothness: 0.65f);

            matLaptopScreen = GetOrCreateMaterial("Assets/Materials/Laptop_Screen.mat", urpLit, new Color(0.1f, 0.2f, 0.35f), emission: new Color(0.2f, 0.6f, 1.0f) * 1.5f);
            matLightEmissive = GetOrCreateMaterial("Assets/Materials/Light_Emissive.mat", urpLit, Color.white, emission: new Color(1.0f, 0.96f, 0.88f) * 3.0f);
            matGlass = GetOrCreateMaterial("Assets/Materials/Glass_Window.mat", urpLit, new Color(0.7f, 0.85f, 0.95f, 0.25f), smoothness: 0.95f, isTransparent: true);

            matSkin = GetOrCreateMaterial("Assets/Materials/Avatar_Skin.mat", urpLit, new Color(0.92f, 0.76f, 0.66f), smoothness: 0.3f);
            matHairDark = GetOrCreateMaterial("Assets/Materials/Hair_Dark.mat", urpLit, new Color(0.10f, 0.08f, 0.07f), smoothness: 0.35f);
            matHairPhoto = GetOrCreateMaterial("Assets/Materials/Hair_Photo_Blonde.mat", urpLit, new Color(0.52f, 0.38f, 0.25f), smoothness: 0.35f);
            matInterviewerSuit = GetOrCreateMaterial("Assets/Materials/Interviewer_Suit.mat", urpLit, new Color(0.14f, 0.15f, 0.17f), smoothness: 0.3f);
            matInterviewerTie = GetOrCreateMaterial("Assets/Materials/Interviewer_Tie.mat", urpLit, new Color(0.15f, 0.35f, 0.65f), smoothness: 0.45f);
            matCandidateSuit = GetOrCreateMaterial("Assets/Materials/Candidate_Suit.mat", urpLit, new Color(0.12f, 0.16f, 0.23f), smoothness: 0.3f);
            matCandidateTie = GetOrCreateMaterial("Assets/Materials/Candidate_Tie.mat", urpLit, new Color(0.52f, 0.12f, 0.16f), smoothness: 0.45f);
            matShirtWhite = GetOrCreateMaterial("Assets/Materials/Avatar_Shirt_White.mat", urpLit, new Color(0.96f, 0.97f, 0.98f), smoothness: 0.35f);
            matShoesBrown = GetOrCreateMaterial("Assets/Materials/Avatar_Shoes_Brown.mat", urpLit, new Color(0.24f, 0.14f, 0.09f), smoothness: 0.65f);
            matShoesSole = GetOrCreateMaterial("Assets/Materials/Avatar_Shoes_Sole.mat", urpLit, new Color(0.10f, 0.10f, 0.11f), smoothness: 0.20f);
            matEyesDark = GetOrCreateMaterial("Assets/Materials/Avatar_Eyes_Dark.mat", urpLit, new Color(0.08f, 0.08f, 0.09f), smoothness: 0.85f);
            matSmile = GetOrCreateMaterial("Assets/Materials/Avatar_Smile.mat", urpLit, new Color(0.78f, 0.48f, 0.46f), smoothness: 0.35f);
            matCandidateHoodie = GetOrCreateMaterial("Assets/Materials/Candidate_Hoodie.mat", urpLit, new Color(0.20f, 0.22f, 0.25f), smoothness: 0.3f);

            matVRHeadset = GetOrCreateMaterial("Assets/Materials/VR_Headset_Chassis.mat", urpLit, new Color(0.92f, 0.93f, 0.95f), smoothness: 0.7f);
            matVRVisor = GetOrCreateMaterial("Assets/Materials/VR_Visor_Glow.mat", urpLit, new Color(0.04f, 0.06f, 0.08f), smoothness: 0.95f, emission: new Color(0.0f, 0.85f, 1.0f) * 2.2f);
            matARHologram = GetOrCreateMaterial("Assets/Materials/AR_Hologram_Panel.mat", urpLit, new Color(0.0f, 0.75f, 1.0f, 0.45f), smoothness: 0.9f, emission: new Color(0.0f, 0.8f, 1.0f) * 2.0f, isTransparent: true);

            matPlant = GetOrCreateMaterial("Assets/Materials/Plant_Leaves.mat", urpLit, new Color(0.12f, 0.35f, 0.15f), smoothness: 0.4f);
            matPot = GetOrCreateMaterial("Assets/Materials/Pot_Ceramic.mat", urpLit, new Color(0.94f, 0.94f, 0.94f), smoothness: 0.6f);

            // ==========================================
            // Load Interviewer Photo Textures & Materials
            // ==========================================
            string photoPath = "Assets/Textures/InterviewerPhoto.jpg";
            Texture2D photoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(photoPath);
            matInterviewerPhoto = GetOrCreateMaterialWithTexture("Assets/Materials/Interviewer_Photo.mat", urpLit, photoTex, Color.white, smoothness: 0.6f);

            string cutoutPath = "Assets/Textures/InterviewerPhoto_Cutout.png";
            TextureImporter importer = AssetImporter.GetAtPath(cutoutPath) as TextureImporter;
            if (importer != null && !importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            Texture2D cutoutTex = AssetDatabase.LoadAssetAtPath<Texture2D>(cutoutPath);
            matInterviewerCutout = GetOrCreateMaterialWithTexture("Assets/Materials/Interviewer_Cutout.mat", urpLit, cutoutTex != null ? cutoutTex : photoTex, Color.white, smoothness: 0.35f, isTransparent: true);
        }

        private static Material GetOrCreateMaterial(string path, Shader shader, Color baseColor, float metallic = 0f, float smoothness = 0.5f, Color? emission = null, bool isTransparent = false)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

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
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateMaterialWithTexture(string path, Shader shader, Texture2D texture, Color baseColor, float metallic = 0f, float smoothness = 0.5f, bool isTransparent = false)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

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
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void BuildRoomShell(Transform parent)
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

            CreatePrimitive(PrimitiveType.Cube, "Wall_Front_LeftPillar", room.transform, new Vector3(-3.6f, 1.55f, -3.05f), new Vector3(0.8f, 3.1f, 0.1f), matWall);
            CreatePrimitive(PrimitiveType.Cube, "Wall_Front_RightPillar", room.transform, new Vector3(3.6f, 1.55f, -3.05f), new Vector3(0.8f, 3.1f, 0.1f), matWall);
            CreatePrimitive(PrimitiveType.Cube, "Window_Glass", room.transform, new Vector3(0, 1.55f, -3.0f), new Vector3(6.4f, 2.9f, 0.05f), matGlass);

            CreatePrimitive(PrimitiveType.Cube, "Mullion_H_Mid", room.transform, new Vector3(0, 1.55f, -2.98f), new Vector3(6.4f, 0.06f, 0.06f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Mullion_V_1", room.transform, new Vector3(-2.1f, 1.55f, -2.98f), new Vector3(0.06f, 2.9f, 0.06f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Mullion_V_2", room.transform, new Vector3(0f, 1.55f, -2.98f), new Vector3(0.06f, 2.9f, 0.06f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Mullion_V_3", room.transform, new Vector3(2.1f, 1.55f, -2.98f), new Vector3(0.06f, 2.9f, 0.06f), matMetalTrim);
        }

        private static void CreateCeilingLightPanel(Transform parent, Vector3 position)
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

        private static void BuildSmartboard(Transform parent, Vector3 position)
        {
            GameObject board = new GameObject("Smartboard_Presentation");
            board.transform.SetParent(parent, false);
            board.transform.position = position;

            CreatePrimitive(PrimitiveType.Cube, "Frame", board.transform, Vector3.zero, new Vector3(0.04f, 1.3f, 2.4f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Screen", board.transform, new Vector3(0.025f, 0, 0), new Vector3(0.01f, 1.22f, 2.32f), matWhiteboard);
            CreatePrimitive(PrimitiveType.Cube, "Status_Banner", board.transform, new Vector3(0.032f, 0.48f, 0), new Vector3(0.01f, 0.16f, 2.2f), matLaptopScreen);

            // Display Interviewer's photo card on the Smartboard
            if (matInterviewerPhoto != null)
            {
                GameObject photoOnBoard = CreatePrimitive(PrimitiveType.Quad, "Interviewer_Photo_OnSmartboard", board.transform, new Vector3(0.034f, 0.02f, 0.72f), new Vector3(0.45f, 0.62f, 1f), matInterviewerPhoto);
                photoOnBoard.transform.localRotation = Quaternion.Euler(0, 90f, 0);
            }
        }

        private static Transform BuildInterviewTable(Transform parent)
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

        private static void BuildChairs(Transform parent)
        {
            BuildExecutiveOfficeChair(parent, new Vector3(0, 0, 1.25f), 180f, "Interviewer_Chair");
            BuildExecutiveOfficeChair(parent, new Vector3(0, 0, -1.25f), 0f, "Candidate_Chair");
        }

        private static GameObject BuildExecutiveOfficeChair(Transform parent, Vector3 position, float yRotation, string name)
        {
            GameObject chair = new GameObject(name);
            chair.transform.SetParent(parent, false);
            chair.transform.position = position;
            chair.transform.rotation = Quaternion.Euler(0, yRotation, 0);

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

        private static void BuildTableProps(Transform table)
        {
            // 1. Interviewer Laptop (at Z = 0.50, facing interviewer)
            BuildLaptop(table, new Vector3(-0.15f, 0.755f, 0.50f), 0f, "Interviewer_Laptop");

            // 2. Interviewer Ceramic Coffee Mug
            CreatePrimitive(PrimitiveType.Cylinder, "Coffee_Mug", table, new Vector3(-0.38f, 0.80f, 0.50f), new Vector3(0.08f, 0.05f, 0.08f), matPot);

            // 4. Candidate Water Glass
            CreatePrimitive(PrimitiveType.Cylinder, "Water_Glass", table, new Vector3(0.35f, 0.80f, -0.50f), new Vector3(0.07f, 0.06f, 0.07f), matGlass);

            // 5. Professional Gooseneck Microphones
            BuildGooseneckMicrophone(table, new Vector3(0.20f, 0.755f, 0.55f), 180f, "Interviewer_Gooseneck_Mic");
            BuildGooseneckMicrophone(table, new Vector3(-0.20f, 0.755f, -0.55f), 0f, "Candidate_Gooseneck_Mic");

            // 6. Central 360-degree Conference Speakerphone Puck
            BuildConferenceSpeakerphonePuck(table, new Vector3(0, 0.755f, 0), "Conference_Speakerphone_Puck");

            // 7. Nameplates
            BuildNameplate(table, new Vector3(-0.25f, 0.77f, 0.25f), 0f, "INTERVIEWER: DAVID RICHARDSON");
            BuildNameplate(table, new Vector3(0.25f, 0.77f, -0.25f), 180f, "CANDIDATE: ALEX MERCER (AR/VR)");
        }

        private static GameObject BuildGooseneckMicrophone(Transform parent, Vector3 localPos, float yRot, string name)
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

        private static GameObject BuildConferenceSpeakerphonePuck(Transform parent, Vector3 localPos, string name)
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

        private static void BuildInterviewerPhotoStand(Transform table, Vector3 position)
        {
            GameObject stand = new GameObject("Interviewer_Desk_PhotoDisplay");
            stand.transform.SetParent(table, false);
            stand.transform.position = position;
            stand.transform.rotation = Quaternion.Euler(0, 195f, 0); // Angled slightly towards candidate

            // Acrylic / Metal Base
            CreatePrimitive(PrimitiveType.Cube, "Stand_Base", stand.transform, new Vector3(0, 0.01f, 0), new Vector3(0.24f, 0.02f, 0.08f), matMetalTrim);

            // Framed Photo Picture
            GameObject frame = CreatePrimitive(PrimitiveType.Cube, "Photo_Frame", stand.transform, new Vector3(0, 0.12f, -0.01f), new Vector3(0.22f, 0.24f, 0.012f), matMetalTrim);
            frame.transform.localRotation = Quaternion.Euler(12f, 0, 0);

            if (matInterviewerPhoto != null)
            {
                // Portrait photo quad
                GameObject photoQuad = CreatePrimitive(PrimitiveType.Quad, "Interviewer_Photo_Image", frame.transform, new Vector3(0, 0, -0.52f), new Vector3(0.92f, 0.92f, 1f), matInterviewerPhoto);
                photoQuad.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            }
        }

        private static void BuildLaptop(Transform parent, Vector3 position, float yRotation, string name)
        {
            GameObject laptop = new GameObject(name);
            laptop.transform.SetParent(parent, false);
            laptop.transform.position = position;
            laptop.transform.rotation = Quaternion.Euler(0, yRotation, 0);

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

        private static void BuildNameplate(Transform parent, Vector3 position, float yRotation, string text)
        {
            GameObject plate = new GameObject("Nameplate");
            plate.transform.SetParent(parent, false);
            plate.transform.position = position;
            plate.transform.rotation = Quaternion.Euler(0, yRotation, 0);

            GameObject wedge = CreatePrimitive(PrimitiveType.Cube, "Prism", plate.transform, Vector3.zero, new Vector3(0.26f, 0.04f, 0.06f), matMetalTrim);
            wedge.transform.localRotation = Quaternion.Euler(25f, 0, 0);
        }

        private static void BuildOfficeDecor(Transform parent)
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

        public static void UpdateAvatarsInOpenScene()
        {
            RemoveScreensBetweenInterviewerAndCandidate();

            GameObject root = GameObject.Find("InterviewRoom_Environment");
            if (root == null)
            {
                Debug.Log("[InterviewRoomBuilder] Rebuilding entire scene...");
                BuildInterviewRoomScene();
                return;
            }

            // Remove any misplaced Untitled FBX objects in the scene
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allObjects)
            {
                if (go != null && (go.name.StartsWith("Untitled") || (go.name.Contains("(Clone)") && go.name.Contains("Untitled"))))
                {
                    Object.DestroyImmediate(go);
                }
            }

            // Remove any old avatar objects
            string[] oldAvatarNames = { "Avatar_Interviewer_DrSarah", "Avatar_Interviewer", "Avatar_Candidate_Alex_ARVR", "Avatar_Candidate_Alex", "Avatar_Candidate" };
            foreach (var aName in oldAvatarNames)
            {
                Transform oldA = root.transform.Find(aName);
                if (oldA != null) Object.DestroyImmediate(oldA.gameObject);
            }

            CreateOrLoadMaterials();

            GameObject interviewerObj = BuildInterviewerAvatar(root.transform);
            GameObject candidateObj = BuildCandidateVRAvatar(root.transform);

            var interviewerAnim = interviewerObj != null ? interviewerObj.GetComponent<AvatarAnimationController>() : null;
            var candidateAnim = candidateObj != null ? candidateObj.GetComponent<AvatarAnimationController>() : null;
            if (interviewerAnim != null && candidateAnim != null)
            {
                interviewerAnim.lookTarget = candidateAnim.head;
                candidateAnim.lookTarget = interviewerAnim.head;
            }

            Transform table = root.transform.Find("Table");
            if (table != null)
            {
                // Clean up old legacy mic pucks if present
                Transform legacyPuck = table.Find("Conference_Mic_Puck");
                if (legacyPuck != null) Object.DestroyImmediate(legacyPuck.gameObject);
                Transform legacyLed = table.Find("Mic_LED_Indicator");
                if (legacyLed != null) Object.DestroyImmediate(legacyLed.gameObject);

                // Ensure gooseneck microphones and conference puck exist
                if (table.Find("Interviewer_Gooseneck_Mic") == null)
                    BuildGooseneckMicrophone(table, new Vector3(0.18f, 0.755f, 0.60f), 180f, "Interviewer_Gooseneck_Mic");
                if (table.Find("Candidate_Gooseneck_Mic") == null)
                    BuildGooseneckMicrophone(table, new Vector3(-0.18f, 0.755f, -0.60f), 0f, "Candidate_Gooseneck_Mic");
                if (table.Find("Conference_Speakerphone_Puck") == null)
                    BuildConferenceSpeakerphonePuck(table, new Vector3(0, 0.755f, 0), "Conference_Speakerphone_Puck");
            }

            var controller = Object.FindAnyObjectByType<InterviewRoomController>();
            if (controller != null)
            {
                controller.interviewerAvatar = interviewerAnim;
                controller.candidateAvatar = candidateAnim;
            }

            SetupAudioSystem(root.transform, interviewerObj, candidateObj, controller);

            // Update camera viewpoints to true face-to-face line of sight (eye level: 1.20m, 0 deg pitch)
            Transform camRig = root.transform.Find("Camera_Rig");
            if (camRig != null)
            {
                Transform intervEye = camRig.Find("ViewPoint_InterviewerEye");
                if (intervEye != null)
                {
                    intervEye.position = new Vector3(0f, 1.20f, 1.25f);
                    intervEye.rotation = Quaternion.Euler(0f, 180f, 0f);
                }
                Transform candEye = camRig.Find("ViewPoint_CandidateEye");
                if (candEye != null)
                {
                    candEye.position = new Vector3(0f, 1.20f, -1.25f);
                    candEye.rotation = Quaternion.Euler(0f, 0f, 0f);
                }
            }

            var vrRig = Object.FindAnyObjectByType<VRGogglesRig>();
            if (vrRig != null)
            {
                if (vrRig.candidateSeatPoint != null)
                {
                    vrRig.candidateSeatPoint.position = new Vector3(0f, 1.20f, -1.25f);
                    vrRig.candidateSeatPoint.rotation = Quaternion.Euler(0f, 0f, 0f);
                }
                if (vrRig.interviewerSeatPoint != null)
                {
                    vrRig.interviewerSeatPoint.position = new Vector3(0f, 1.20f, 1.25f);
                    vrRig.interviewerSeatPoint.rotation = Quaternion.Euler(0f, 180f, 0f);
                }
                vrRig.currentSeat = VRGogglesRig.VRRigSeat.InterviewerChair;
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 1.20f, 1.25f);
                mainCam.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/InterviewRoom.unity");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[InterviewRoomBuilder] Both avatars (Interviewer and Candidate) placed and seated in chairs!");
        }

        private static GameObject BuildInterviewerAvatar(Transform parent)
        {
            return BuildCharacterFromFbx(parent, new Vector3(0, 0, 1.25f), 180f, "Avatar_Interviewer_DrSarah", isInterviewer: true);
        }

        private static GameObject BuildCandidateVRAvatar(Transform parent)
        {
            return BuildCharacterFromFbx(parent, new Vector3(0, 0, -1.25f), 0f, "Avatar_Candidate_Alex_ARVR", isInterviewer: false);
        }

        private static GameObject BuildCharacterFromFbx(Transform parent, Vector3 chairPosition, float yRotation, string avatarName, bool isInterviewer)
        {
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Untitled.fbx");
            GameObject avatar = null;
            if (fbxPrefab != null)
            {
                avatar = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
                if (avatar != null)
                {
                    PrefabUtility.UnpackPrefabInstance(avatar, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }

            if (avatar == null)
            {
                Debug.LogWarning("[InterviewRoomBuilder] Untitled.fbx could not be instantiated. Falling back to procedural avatar.");
                return isInterviewer ? BuildProceduralInterviewer(parent) : BuildProceduralCandidate(parent);
            }

            avatar.name = avatarName;
            avatar.transform.SetParent(parent, false);

            // Strip any embedded Blender cameras, lights, or helper objects
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in avatar.transform)
            {
                string cName = child.name.ToLower();
                if (cName.Contains("camera") || cName.Contains("light") || child.GetComponent<Light>() != null || child.GetComponent<Camera>() != null)
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            foreach (var go in toDestroy)
            {
                Object.DestroyImmediate(go);
            }

            // Scale down from Blender FBX (~380 units height) to realistic seated human size (~1.28m)
            float scale = 0.0033f;
            avatar.transform.localScale = new Vector3(scale, scale, scale);
            avatar.transform.rotation = Quaternion.Euler(0, yRotation, 0);

            // Position thighs on seat cushion
            Vector3 worldPos = chairPosition;
            worldPos.y = chairPosition.y - 0.08f;
            worldPos -= avatar.transform.forward * 0.04f;
            avatar.transform.position = worldPos;

            // Apply high-quality URP materials
            ApplyCharacterMaterials(avatar, isInterviewer);

            // Reparent facial features under Head so animation moves head and face together
            Transform headTransform = avatar.transform.Find("Head");
            if (headTransform != null)
            {
                string[] faceParts = { "Hair", "Hair Left", "Hair Right", "Left Eye", "Right Eye", "Left Ear", "Right Ear", "Nose", "Smile" };
                foreach (var partName in faceParts)
                {
                    Transform part = avatar.transform.Find(partName);
                    if (part != null && part != headTransform)
                    {
                        part.SetParent(headTransform, true);
                    }
                }
            }

            // Reparent hands under arms
            Transform leftArm = avatar.transform.Find("Left Arm");
            Transform leftHand = avatar.transform.Find("Left Hand");
            if (leftArm != null && leftHand != null) leftHand.SetParent(leftArm, true);

            Transform rightArm = avatar.transform.Find("Right Arm");
            Transform rightHand = avatar.transform.Find("Right Hand");
            if (rightArm != null && rightHand != null) rightHand.SetParent(rightArm, true);

            Transform chestTransform = avatar.transform.Find("Suit Torso");

            // Attach Animation Controller
            var anim = avatar.AddComponent<AvatarAnimationController>();
            anim.isInterviewer = isInterviewer;
            anim.isCandidateVR = !isInterviewer;
            anim.head = headTransform;
            anim.chest = chestTransform;
            anim.leftArm = leftArm;
            anim.rightArm = rightArm;

            return avatar;
        }

        private static void ApplyCharacterMaterials(GameObject avatar, bool isInterviewer)
        {
            Material suitMat = matInterviewerSuit;
            Material tieMat = matInterviewerTie;
            Material shirtMat = matShirtWhite;
            Material skinMat = matSkin;
            Material hairMat = matHairDark;
            Material shoesMat = matShoesBrown;
            Material soleMat = matShoesSole;
            Material eyesMat = matEyesDark;
            Material smileMat = matSmile;

            foreach (var mr in avatar.GetComponentsInChildren<MeshRenderer>(true))
            {
                string n = mr.name;
                if (n.Contains("Suit") || n.Contains("Thigh") || n.Contains("Lower Leg") || n.Contains("Arm"))
                {
                    mr.sharedMaterial = suitMat;
                }
                else if (n.Contains("Shirt") || n.Contains("Collar"))
                {
                    mr.sharedMaterial = shirtMat;
                }
                else if (n.Contains("Tie"))
                {
                    mr.sharedMaterial = tieMat;
                }
                else if (n.Contains("Shoe"))
                {
                    mr.sharedMaterial = shoesMat;
                }
                else if (n.Contains("Sole"))
                {
                    mr.sharedMaterial = soleMat;
                }
                else if (n.Contains("Hair"))
                {
                    mr.sharedMaterial = hairMat;
                }
                else if (n.Contains("Eye"))
                {
                    mr.sharedMaterial = eyesMat;
                }
                else if (n.Contains("Smile"))
                {
                    mr.sharedMaterial = smileMat;
                }
                else if (n.Contains("Head") || n.Contains("Neck") || n.Contains("Nose") || n.Contains("Ear") || n.Contains("Hand"))
                {
                    mr.sharedMaterial = skinMat;
                }
                else
                {
                    mr.sharedMaterial = skinMat;
                }
            }
        }

        private static GameObject BuildProceduralInterviewer(Transform parent)
        {
            GameObject avatar = new GameObject("Avatar_Interviewer_DrSarah");
            avatar.transform.SetParent(parent, false);
            avatar.transform.position = new Vector3(0f, 0f, 1.25f);
            avatar.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // Seated Lower Body
            CreatePrimitive(PrimitiveType.Cube, "Hips", avatar.transform,
                new Vector3(0f, 0.50f, -0.05f), new Vector3(0.36f, 0.18f, 0.28f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "Thigh_L", avatar.transform,
                new Vector3(-0.11f, 0.47f, 0.16f), new Vector3(0.14f, 0.13f, 0.38f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "Thigh_R", avatar.transform,
                new Vector3(0.11f, 0.47f, 0.16f), new Vector3(0.14f, 0.13f, 0.38f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "Shin_L", avatar.transform,
                new Vector3(-0.11f, 0.20f, 0.34f), new Vector3(0.12f, 0.36f, 0.12f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "Shin_R", avatar.transform,
                new Vector3(0.11f, 0.20f, 0.34f), new Vector3(0.12f, 0.36f, 0.12f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "Shoe_L", avatar.transform,
                new Vector3(-0.11f, 0.04f, 0.38f), new Vector3(0.12f, 0.07f, 0.22f), matHairDark);
            CreatePrimitive(PrimitiveType.Cube, "Shoe_R", avatar.transform,
                new Vector3(0.11f, 0.04f, 0.38f), new Vector3(0.12f, 0.07f, 0.22f), matHairDark);

            // Upper Body
            GameObject chest = CreatePrimitive(PrimitiveType.Cube, "Chest", avatar.transform,
                new Vector3(0f, 0.80f, -0.02f), new Vector3(0.40f, 0.46f, 0.24f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "Shirt_Collar", chest.transform,
                new Vector3(0f, 0.13f, 0.124f), new Vector3(0.15f, 0.20f, 0.012f), matShirtWhite);
            CreatePrimitive(PrimitiveType.Cube, "Tie", chest.transform,
                new Vector3(0f, -0.02f, 0.130f), new Vector3(0.06f, 0.30f, 0.010f), matInterviewerTie);
            CreatePrimitive(PrimitiveType.Cube, "Lapel_L", chest.transform,
                new Vector3(-0.09f, 0.10f, 0.122f), new Vector3(0.08f, 0.18f, 0.010f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "Lapel_R", chest.transform,
                new Vector3(0.09f, 0.10f, 0.122f), new Vector3(0.08f, 0.18f, 0.010f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "ID_Badge", chest.transform,
                new Vector3(-0.09f, -0.10f, 0.132f), new Vector3(0.07f, 0.10f, 0.008f), matLaptopScreen);

            // Neck & Head
            CreatePrimitive(PrimitiveType.Cylinder, "Neck", avatar.transform,
                new Vector3(0f, 1.06f, -0.01f), new Vector3(0.10f, 0.08f, 0.10f), matSkin);

            GameObject head = new GameObject("Head");
            head.transform.SetParent(avatar.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.24f, -0.01f);

            CreatePrimitive(PrimitiveType.Sphere, "Face", head.transform,
                Vector3.zero, new Vector3(0.23f, 0.26f, 0.22f), matSkin);
            CreatePrimitive(PrimitiveType.Sphere, "Hair_Top", head.transform,
                new Vector3(0f, 0.09f, -0.02f), new Vector3(0.25f, 0.17f, 0.25f), matHairPhoto);
            CreatePrimitive(PrimitiveType.Cube, "Hair_Back", head.transform,
                new Vector3(0f, 0.02f, -0.11f), new Vector3(0.23f, 0.22f, 0.06f), matHairPhoto);
            CreatePrimitive(PrimitiveType.Sphere, "Ear_L", head.transform,
                new Vector3(-0.12f, 0f, 0f), new Vector3(0.05f, 0.07f, 0.04f), matSkin);
            CreatePrimitive(PrimitiveType.Sphere, "Ear_R", head.transform,
                new Vector3(0.12f, 0f, 0f), new Vector3(0.05f, 0.07f, 0.04f), matSkin);
            CreatePrimitive(PrimitiveType.Sphere, "Eye_L", head.transform,
                new Vector3(-0.06f, 0.04f, 0.10f), new Vector3(0.04f, 0.035f, 0.03f), matEyesDark);
            CreatePrimitive(PrimitiveType.Sphere, "Eye_R", head.transform,
                new Vector3(0.06f, 0.04f, 0.10f), new Vector3(0.04f, 0.035f, 0.03f), matEyesDark);
            CreatePrimitive(PrimitiveType.Cube, "Glasses_Bridge", head.transform,
                new Vector3(0f, 0.04f, 0.113f), new Vector3(0.08f, 0.012f, 0.008f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Glasses_L", head.transform,
                new Vector3(-0.07f, 0.04f, 0.110f), new Vector3(0.06f, 0.048f, 0.006f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Cube, "Glasses_R", head.transform,
                new Vector3(0.07f, 0.04f, 0.110f), new Vector3(0.06f, 0.048f, 0.006f), matMetalTrim);
            CreatePrimitive(PrimitiveType.Sphere, "Nose", head.transform,
                new Vector3(0f, -0.02f, 0.112f), new Vector3(0.04f, 0.04f, 0.04f), matSkin);
            CreatePrimitive(PrimitiveType.Cube, "Mouth", head.transform,
                new Vector3(0f, -0.07f, 0.110f), new Vector3(0.08f, 0.018f, 0.012f), matSmile);

            // Left Arm with Portfolio
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

            // Right Arm (Gesturing)
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

            // Portfolio Folder
            GameObject portfolio = new GameObject("Portfolio_Folder");
            portfolio.transform.SetParent(avatar.transform, false);
            portfolio.transform.localPosition = new Vector3(-0.06f, 0.77f, 0.36f);
            portfolio.transform.localRotation = Quaternion.Euler(16f, -10f, 4f);
            CreatePrimitive(PrimitiveType.Cube, "Folder_Cover", portfolio.transform,
                Vector3.zero, new Vector3(0.28f, 0.01f, 0.36f), matInterviewerSuit);
            CreatePrimitive(PrimitiveType.Cube, "Folder_Pages", portfolio.transform,
                new Vector3(0f, 0.012f, 0f), new Vector3(0.26f, 0.008f, 0.34f), matWhiteboard);

            // Wire up Animation Controller
            var anim = avatar.AddComponent<AvatarAnimationController>();
            anim.isInterviewer = true;
            anim.isCandidateVR = false;
            anim.head = head.transform;
            anim.chest = chest.transform;
            anim.leftArm = leftArm.transform;
            anim.rightArm = rightArm.transform;

            return avatar;
        }

        private static GameObject BuildProceduralCandidate(Transform parent)
        {
            GameObject avatar = new GameObject("Avatar_Candidate_Alex_ARVR");
            avatar.transform.SetParent(parent, false);
            avatar.transform.position = new Vector3(0f, 0f, -1.25f);
            avatar.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            // Seated Lower Body
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
            CreatePrimitive(PrimitiveType.Cube, "Sneaker_L", avatar.transform,
                new Vector3(-0.11f, 0.04f, 0.40f), new Vector3(0.12f, 0.08f, 0.24f), matVRHeadset);
            CreatePrimitive(PrimitiveType.Cube, "Sneaker_R", avatar.transform,
                new Vector3(0.11f, 0.04f, 0.40f), new Vector3(0.12f, 0.08f, 0.24f), matVRHeadset);

            // Upper Body Hoodie
            GameObject chest = CreatePrimitive(PrimitiveType.Cube, "Chest", avatar.transform,
                new Vector3(0f, 0.80f, -0.02f), new Vector3(0.40f, 0.46f, 0.24f), matCandidateHoodie);
            CreatePrimitive(PrimitiveType.Cube, "Hoodie_Pocket", chest.transform,
                new Vector3(0f, -0.11f, 0.125f), new Vector3(0.26f, 0.16f, 0.018f), matCandidateHoodie);
            CreatePrimitive(PrimitiveType.Cube, "Zipper", chest.transform,
                new Vector3(0f, 0.04f, 0.127f), new Vector3(0.012f, 0.28f, 0.010f), matHairDark);
            CreatePrimitive(PrimitiveType.Cube, "Logo_Patch", chest.transform,
                new Vector3(-0.10f, 0.12f, 0.128f), new Vector3(0.06f, 0.06f, 0.010f), matLaptopScreen);

            // Neck & Head
            CreatePrimitive(PrimitiveType.Cylinder, "Neck", avatar.transform,
                new Vector3(0f, 1.06f, -0.01f), new Vector3(0.10f, 0.08f, 0.10f), matSkin);

            GameObject head = new GameObject("Head");
            head.transform.SetParent(avatar.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.24f, -0.01f);

            CreatePrimitive(PrimitiveType.Sphere, "Face", head.transform,
                Vector3.zero, new Vector3(0.23f, 0.26f, 0.22f), matSkin);
            CreatePrimitive(PrimitiveType.Sphere, "Hair_Top", head.transform,
                new Vector3(0f, 0.10f, -0.02f), new Vector3(0.25f, 0.16f, 0.25f), matHairDark);
            CreatePrimitive(PrimitiveType.Sphere, "Ear_L", head.transform,
                new Vector3(-0.12f, 0f, 0f), new Vector3(0.05f, 0.07f, 0.04f), matSkin);
            CreatePrimitive(PrimitiveType.Sphere, "Ear_R", head.transform,
                new Vector3(0.12f, 0f, 0f), new Vector3(0.05f, 0.07f, 0.04f), matSkin);
            CreatePrimitive(PrimitiveType.Sphere, "Eye_L", head.transform,
                new Vector3(-0.06f, 0.03f, 0.10f), new Vector3(0.04f, 0.035f, 0.03f), matEyesDark);
            CreatePrimitive(PrimitiveType.Sphere, "Eye_R", head.transform,
                new Vector3(0.06f, 0.03f, 0.10f), new Vector3(0.04f, 0.035f, 0.03f), matEyesDark);
            CreatePrimitive(PrimitiveType.Sphere, "Nose", head.transform,
                new Vector3(0f, -0.02f, 0.112f), new Vector3(0.04f, 0.04f, 0.04f), matSkin);
            CreatePrimitive(PrimitiveType.Cube, "Mouth", head.transform,
                new Vector3(0f, -0.07f, 0.110f), new Vector3(0.08f, 0.018f, 0.012f), matSmile);

            // Spatial AR/VR Headset
            GameObject headset = new GameObject("Spatial_ARVR_Headset");
            headset.transform.SetParent(head.transform, false);
            headset.transform.localPosition = new Vector3(0f, 0.03f, 0.06f);

            CreatePrimitive(PrimitiveType.Cube, "Headset_Body", headset.transform,
                new Vector3(0f, 0f, 0.06f), new Vector3(0.26f, 0.12f, 0.10f), matVRHeadset);
            GameObject visor = CreatePrimitive(PrimitiveType.Cube, "Visor", headset.transform,
                new Vector3(0f, 0f, 0.115f), new Vector3(0.24f, 0.09f, 0.015f), matVRVisor);
            GameObject visorLight = new GameObject("Visor_GlowLight");
            visorLight.transform.SetParent(visor.transform, false);
            visorLight.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            Light vLight = visorLight.AddComponent<Light>();
            vLight.type = LightType.Point;
            vLight.color = new Color(0.0f, 0.85f, 1.0f);
            vLight.range = 1.4f;
            vLight.intensity = 10f;

            CreatePrimitive(PrimitiveType.Sphere, "Cam_L", headset.transform,
                new Vector3(-0.13f, 0f, 0.12f), new Vector3(0.04f, 0.04f, 0.05f), matHairDark);
            CreatePrimitive(PrimitiveType.Sphere, "Cam_R", headset.transform,
                new Vector3(0.13f, 0f, 0.12f), new Vector3(0.04f, 0.04f, 0.05f), matHairDark);
            CreatePrimitive(PrimitiveType.Cylinder, "AudioPod_L", headset.transform,
                new Vector3(-0.14f, 0f, -0.02f), new Vector3(0.055f, 0.030f, 0.055f), matVRHeadset)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            CreatePrimitive(PrimitiveType.Cylinder, "AudioPod_R", headset.transform,
                new Vector3(0.14f, 0f, -0.02f), new Vector3(0.055f, 0.030f, 0.055f), matVRHeadset)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            CreatePrimitive(PrimitiveType.Cube, "Rear_Band", headset.transform,
                new Vector3(0f, 0f, -0.12f), new Vector3(0.24f, 0.06f, 0.18f), matChairFabric);

            // Left Arm on Desk
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

            // Right Arm in Spatial Gesture Pose
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

            // Animation Controller
            var anim = avatar.AddComponent<AvatarAnimationController>();
            anim.isInterviewer = false;
            anim.isCandidateVR = true;
            anim.head = head.transform;
            anim.chest = chest.transform;
            anim.leftArm = leftArm.transform;
            anim.rightArm = rightArm.transform;

            return avatar;
        }

        private static GameObject[] BuildARHologramSystem(Transform parent, Transform candidateTransform)
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

        private static void SetupCameraAndLighting(Transform parent, GameObject interviewerObj, GameObject candidateObj, GameObject[] holograms)
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

            mainCam.transform.position = interviewerEye.transform.position;
            mainCam.transform.rotation = interviewerEye.transform.rotation;
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

        private static void SetupAudioSystem(Transform root, GameObject interviewerObj, GameObject candidateObj, InterviewRoomController controller)
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
        }

        private static void SetupVRGogglesRigAndVirtualScreen(Transform parent)
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

            // 2. Remove any screen between interviewer and candidate
            Transform existingScreen = parent.Find("VR_Virtual_Screen");
            if (existingScreen != null)
            {
                Object.DestroyImmediate(existingScreen.gameObject);
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

            Debug.Log("[InterviewRoomBuilder] VR Goggles Rig and Speech-to-Text Engine successfully generated with clean line of sight!");
        }

        public static void RemoveScreensBetweenInterviewerAndCandidate()
        {
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            int count = 0;
            foreach (var go in allObjects)
            {
                if (go == null) continue;
                string n = go.name;
                if (n == "VR_Virtual_Screen" || n == "Interviewer_Desk_PhotoDisplay" || n == "Screen_Glass_Display" || n == "Screen_Bezel_Frame" || go.GetComponent<VRSpatialVirtualScreen>() != null)
                {
                    Object.DestroyImmediate(go);
                    count++;
                }
            }
            if (count > 0)
            {
                Debug.Log($"[InterviewRoomBuilder] Successfully removed {count} screen objects between interviewer and candidate!");
            }
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;

            if (material != null)
            {
                var renderer = obj.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = material;
            }

            return obj;
        }
    }
}
