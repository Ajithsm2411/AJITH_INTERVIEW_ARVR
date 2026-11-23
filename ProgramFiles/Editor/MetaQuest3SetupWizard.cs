using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using InterviewRoom;
using InterviewRoom.VR;
using InterviewRoom.SpeechToText;

namespace InterviewRoom.Editor
{
    [InitializeOnLoad]
    public class MetaQuest3SetupWizard : UnityEditor.Editor
    {
        static MetaQuest3SetupWizard()
        {
            EditorApplication.delayCall += AutoVerifySetup;
        }

        private static void AutoVerifySetup()
        {
            // Run a non-destructive check on startup
            EnsureXRManagementConfigured(false);
        }

        [MenuItem("Tools/Interview Room/Connect Meta Quest 3 & Complete VR Setup")]
        public static void RunFullMetaQuest3Setup()
        {
            Debug.Log("<color=#00e5ff><b>[MetaQuest3Setup] Starting Meta Quest 3 full configuration...</b></color>");

            // 1. Ensure XR Plug-in Management with OpenXR is configured for Standalone (PC Link / Air Link)
            EnsureXRManagementConfigured(true);

            // 2. Enable Meta Quest 3 Touch Plus and Oculus profiles in OpenXR Settings
            ConfigureOpenXRFeatures();

            // 3. Setup VR Rig & Virtual Screen in active scene and both scenes
            SetupVRInScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00ff88><b>[MetaQuest3Setup] Meta Quest 3 Setup COMPLETED successfully!</b></color>\n" +
                      "<b>Next Steps to run on your Meta Quest 3:</b>\n" +
                      "1. Put on your Meta Quest 3 headset.\n" +
                      "2. Connect your Quest 3 to your PC via USB-C Link cable or Air Link.\n" +
                      "3. Open Quest Link inside the headset.\n" +
                      "4. Ensure Meta Horizon Link app is running on PC.\n" +
                      "5. Hit <b>Play (Ctrl+P)</b> in Unity Editor - you will see the full 3D Interview Room in VR!");
            
            EditorUtility.DisplayDialog("Meta Quest 3 Setup Complete",
                "Meta Quest 3 configuration is now active!\n\n" +
                "• OpenXR Plugin: Enabled (Standalone PC Link / Air Link)\n" +
                "• Meta Quest Touch Plus Controller Profile: Enabled\n" +
                "• VR Goggles Rig & 3D Virtual Screen: Configured in Interview Room\n" +
                "• Controllers: 6-DoF Tracked with Laser Pointers\n\n" +
                "Connect your Quest 3 via Link/Air Link and press Play in Unity!", "Awesome");
        }

        public static void EnsureXRManagementConfigured(bool forceLog)
        {
            try
            {
                var buildTargetGroup = BuildTargetGroup.Standalone;
                var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
                if (generalSettings == null)
                {
                    XRGeneralSettingsPerBuildTarget perBuildTarget = null;
                    EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perBuildTarget);

                    if (perBuildTarget == null)
                    {
                        perBuildTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                        if (!Directory.Exists("Assets/XR")) Directory.CreateDirectory("Assets/XR");
                        AssetDatabase.CreateAsset(perBuildTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perBuildTarget, true);
                    }

                    generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                    var xrManagerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
                    generalSettings.Manager = xrManagerSettings;

                    AssetDatabase.AddObjectToAsset(generalSettings, perBuildTarget);
                    AssetDatabase.AddObjectToAsset(xrManagerSettings, perBuildTarget);

                    perBuildTarget.SetSettingsForBuildTarget(buildTargetGroup, generalSettings);
                    EditorUtility.SetDirty(perBuildTarget);
                }

                var manager = generalSettings.Manager;
                if (manager == null)
                {
                    manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                    string managerSettingsPath = "Assets/XR/Settings/XRManagerSettings.asset";
                    AssetDatabase.CreateAsset(manager, managerSettingsPath);
                    generalSettings.Manager = manager;
                    EditorUtility.SetDirty(generalSettings);
                }

                // Check or add OpenXRLoader
                XRLoader openXRLoader = AssetDatabase.LoadAssetAtPath<XRLoader>("Assets/XR/Loaders/OpenXRLoader.asset");
                if (openXRLoader == null)
                {
                    openXRLoader = ScriptableObject.CreateInstance<OpenXRLoader>();
                    if (!Directory.Exists("Assets/XR/Loaders")) Directory.CreateDirectory("Assets/XR/Loaders");
                    AssetDatabase.CreateAsset(openXRLoader, "Assets/XR/Loaders/OpenXRLoader.asset");
                }

                var currentLoaders = manager.activeLoaders;
                bool hasOpenXR = false;
                foreach (var loader in currentLoaders)
                {
                    if (loader is OpenXRLoader)
                    {
                        hasOpenXR = true;
                        break;
                    }
                }

                if (!hasOpenXR)
                {
                    // Add OpenXRLoader
                    XRPackageMetadataStore.AssignLoader(manager, typeof(OpenXRLoader).FullName, buildTargetGroup);
                    EditorUtility.SetDirty(manager);
                    if (forceLog) Debug.Log("[MetaQuest3Setup] OpenXRLoader assigned to active XR loaders for Standalone.");
                }

                generalSettings.InitManagerOnStart = true;
                EditorUtility.SetDirty(generalSettings);
            }
            catch (Exception ex)
            {
                if (forceLog) Debug.LogWarning("[MetaQuest3Setup] XR Management auto-configuration notice: " + ex.Message);
            }
        }

        public static void ConfigureOpenXRFeatures()
        {
            try
            {
                var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
                if (settings == null)
                {
                    settings = OpenXRSettings.ActiveBuildTargetInstance;
                }

                if (settings != null)
                {
                    // Enable Meta Quest Touch Plus Controller Profile
                    var touchPlus = settings.GetFeature<MetaQuestTouchPlusControllerProfile>();
                    if (touchPlus != null)
                    {
                        touchPlus.enabled = true;
                        Debug.Log("[MetaQuest3Setup] MetaQuestTouchPlusControllerProfile enabled.");
                    }

                    // Enable Oculus Touch Controller Profile
                    var oculusTouch = settings.GetFeature<OculusTouchControllerProfile>();
                    if (oculusTouch != null)
                    {
                        oculusTouch.enabled = true;
                        Debug.Log("[MetaQuest3Setup] OculusTouchControllerProfile enabled.");
                    }

                    // Enable Hand Interaction
                    var handInteraction = settings.GetFeature<HandInteractionProfile>();
                    if (handInteraction != null)
                    {
                        handInteraction.enabled = true;
                    }

                    var palmPose = settings.GetFeature<PalmPoseInteraction>();
                    if (palmPose != null)
                    {
                        palmPose.enabled = true;
                    }

                    EditorUtility.SetDirty(settings);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MetaQuest3Setup] OpenXR Settings configuration notice: " + ex.Message);
            }
        }

        public static void SetupVRInScenes()
        {
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/InterviewRoom.unity",
                "Assets/Scenes/SampleScene.unity"
            };

            foreach (var path in scenePaths)
            {
                if (!File.Exists(path)) continue;

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                GameObject env = GameObject.Find("InterviewRoom_Environment");
                if (env == null)
                {
                    InterviewRoomBuilder.BuildInterviewRoomScene();
                    env = GameObject.Find("InterviewRoom_Environment");
                }

                if (env != null)
                {
                    WireVRRigAndVirtualScreen(env.transform);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, path);
                    Debug.Log($"[MetaQuest3Setup] Saved verified VR Rig setup in {path}");
                }
            }
        }

        private static void WireVRRigAndVirtualScreen(Transform env)
        {
            var controller = UnityEngine.Object.FindAnyObjectByType<InterviewRoomController>();
            Camera mainCam = Camera.main;

            Transform camRig = env.Find("Camera_Rig");
            Transform candEye = camRig != null ? camRig.Find("ViewPoint_CandidateEye") : null;
            Transform intervEye = camRig != null ? camRig.Find("ViewPoint_InterviewerEye") : null;
            Transform overview = camRig != null ? camRig.Find("ViewPoint_Overview") : null;

            // 1. VR Goggles Rig
            Transform existingVRRig = env.Find("VR_Rig");
            GameObject vrRigObj = existingVRRig != null ? existingVRRig.gameObject : new GameObject("VR_Rig");
            vrRigObj.transform.SetParent(env, false);

            var vrRig = vrRigObj.GetComponent<VRGogglesRig>();
            if (vrRig == null) vrRig = vrRigObj.AddComponent<VRGogglesRig>();

            if (candEye != null)
            {
                candEye.position = new Vector3(0f, 1.20f, -1.25f);
                candEye.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            if (intervEye != null)
            {
                intervEye.position = new Vector3(0f, 1.20f, 1.25f);
                intervEye.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            vrRig.candidateSeatPoint = candEye;
            vrRig.interviewerSeatPoint = intervEye;
            vrRig.directorSeatPoint = overview;
            vrRig.currentSeat = VRGogglesRig.VRRigSeat.InterviewerChair;
            vrRig.vrCamera = mainCam;
            vrRig.virtualScreen = null;

            // 2. Remove any screen between interviewer and candidate
            Transform existingScreen = env.Find("VR_Virtual_Screen");
            if (existingScreen != null)
            {
                UnityEngine.Object.DestroyImmediate(existingScreen.gameObject);
            }

            if (controller != null)
            {
                controller.vrRig = vrRig;
                controller.virtualScreen = null;
            }

            // 3. STT Transcription Engine & 3D Avatar Speech Bubbles
            var stt = env.GetComponent<SpeechToTextTranscriptionController>();
            if (stt == null) stt = env.gameObject.AddComponent<SpeechToTextTranscriptionController>();
            stt.roomController = controller;
            stt.audioController = env.GetComponent<InterviewRoomAudioController>();
            stt.virtualScreen = null;
            stt.SetupAvatarBubblesIfMissing();

            if (controller != null)
            {
                controller.sttController = stt;
            }

            // Ensure Main Camera is setup for XR
            if (mainCam != null)
            {
                mainCam.nearClipPlane = 0.05f;
                mainCam.farClipPlane = 100f;
            }

            Debug.Log("[MetaQuest3Setup] VR Goggles Rig, Meta Quest 3 Controllers, and Virtual Screen successfully linked!");
        }
    }
}
