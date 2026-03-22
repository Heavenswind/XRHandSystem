using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using XRHandSystem.Unity;
using XRHandSystem.Core;

namespace XRHandSystem.Editor
{
    public static class XRHandSystemSetup
    {
        private const string AssetPath          = "Assets/XRHandSystem/XRHandInputActions.inputactions";
        private const string XRHandsPackageId   = "com.unity.xr.hands";
        private const string HandVisualizerSample = "HandVisualizer";
        private const string SampleImportPath   = "Assets/Samples/XR Hands";

        [MenuItem("XRHandSystem/Setup Scene")]
        public static void SetupScene()
        {
            // Import Hand Visualizer sample first — scene setup runs in the callback
            ImportHandVisualizerSample();
        }

        // ── Sample Import ─────────────────────────────────────────────────────

        private static void ImportHandVisualizerSample()
        {
            // Check if already imported
            if (AssetDatabase.IsValidFolder(SampleImportPath))
            {
                BuildScene();
                return;
            }

            var listRequest = Client.List(offlineMode: false, includeIndirectDependencies: false);
            EditorApplication.update += WaitForList;

            void WaitForList()
            {
                if (!listRequest.IsCompleted) return;
                EditorApplication.update -= WaitForList;

                if (listRequest.Status != StatusCode.Success)
                {
                    Debug.LogError("[XRHandSystem] Could not list packages. Import Hand Visualizer manually via Package Manager.");
                    BuildScene();
                    return;
                }

                var xrHandsPackage = listRequest.Result.FirstOrDefault(p => p.name == XRHandsPackageId);
                if (xrHandsPackage == null)
                {
                    Debug.LogWarning($"[XRHandSystem] {XRHandsPackageId} not found. Add it via Package Manager first.");
                    BuildScene();
                    return;
                }

                var sample = Sample.FindByPackage(XRHandsPackageId, xrHandsPackage.version)
                    .FirstOrDefault(s => s.displayName == HandVisualizerSample);

                if (sample.displayName == null)
                {
                    Debug.LogWarning("[XRHandSystem] Hand Visualizer sample not found in XR Hands package.");
                    BuildScene();
                    return;
                }

                sample.Import(Sample.ImportOptions.OverridePreviousImports);
                AssetDatabase.Refresh();
                Debug.Log("[XRHandSystem] Hand Visualizer sample imported.");

                BuildScene();
            }
        }

        // ── Scene Build ───────────────────────────────────────────────────────

        private static void BuildScene()
        {
            Undo.SetCurrentGroupName("XRHandSystem Setup");
            int undoGroup = Undo.GetCurrentGroup();

            var inputAsset = CreateOrLoadInputActions();

            // Camera rig
            var rig           = GetOrCreateGameObject("XRCameraRig");
            var trackingSpace = GetOrCreateGameObject("TrackingSpace", rig.transform);
            var cameraGo      = GetOrCreateGameObject("MainCamera",    trackingSpace.transform);

            var rigComponent = GetOrAdd<XRCameraRig>(rig);
            var rigSo        = new SerializedObject(rigComponent);
            rigSo.FindProperty("_trackingSpace").objectReferenceValue = trackingSpace.transform;
            rigSo.FindProperty("_camera").objectReferenceValue        = GetOrAdd<Camera>(cameraGo);
            rigSo.ApplyModifiedProperties();

            var cam = GetOrAdd<Camera>(cameraGo);
            cam.tag      = "MainCamera";
            cameraGo.tag = "MainCamera";

            var tpd = GetOrAdd<TrackedPoseDriver>(cameraGo);
            tpd.positionInput = new InputActionProperty(
                new InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
            tpd.rotationInput = new InputActionProperty(
                new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
            tpd.trackingStateInput = new InputActionProperty(
                new InputAction("Tracking State", binding: "<XRHMD>/trackingState", expectedControlType: "Integer"));
            EditorUtility.SetDirty(cameraGo);

            // Hands
            var leftHandVisualizer  = FindHandVisualizerPrefab("Left Hand Tracking");
            var rightHandVisualizer = FindHandVisualizerPrefab("Right Hand Tracking");

            // Hands must be at scene root — OpenXR joint poses are already in world space
            // Parenting under TrackingSpace would double-transform them
            SetupHand("XRHand_Left",  Handedness.Left,  inputAsset, rig.transform, leftHandVisualizer, rigComponent);
            SetupHand("XRHand_Right", Handedness.Right, inputAsset, rig.transform, rightHandVisualizer, rigComponent);

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = rig;

            bool hasMesh = leftHandVisualizer != null && rightHandVisualizer != null;

            EditorUtility.DisplayDialog(
                "XR Hand System — Setup Complete",
                "Created:\n" +
                "  • XRCameraRig\n" +
                "      └── TrackingSpace\n" +
                "            ├── MainCamera  (TrackedPoseDriver)\n" +
                "            ├── XRHand_Left\n" +
                "            └── XRHand_Right\n" +
                "  • XRHandInputActions.inputactions\n\n" +
                (hasMesh
                    ? "Hand Visualizer meshes wired up automatically.\n\n"
                    : "Hand Visualizer prefabs not found — assign mesh renderers and bone transforms to HandVisual manually.\n\n") +
                "Remaining steps:\n" +
                "  1. Create pose assets via XRHandSystem > Pose Editor\n" +
                "  2. Assign poses to HandPoseMatcherComponent",
                "OK");
        }

        // ── Input Actions ─────────────────────────────────────────────────────

        private static InputActionAsset CreateOrLoadInputActions()
        {
            var existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "XRHandInputActions";

            AddHandActions(asset.AddActionMap("LeftHand"),  "Left");
            AddHandActions(asset.AddActionMap("RightHand"), "Right");

            // .inputactions files must be written as JSON, not via CreateAsset
            System.IO.Directory.CreateDirectory("Assets/XRHandSystem");
            System.IO.File.WriteAllText(AssetPath, asset.ToJson());
            AssetDatabase.ImportAsset(AssetPath);
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
        }

        private static void AddHandActions(InputActionMap map, string side)
        {
            var pinch = map.AddAction("Pinch", InputActionType.Value);
            pinch.expectedControlType = "Axis";
            pinch.AddBinding($"<XRHandInteraction>{{XR{side}Hand}}/selectValue");
            pinch.AddBinding($"<HandInteraction>{{XR{side}Hand}}/selectValue");
            pinch.AddBinding($"<XRController>{{XR{side}Hand}}/grip");

            var grip = map.AddAction("Grip", InputActionType.Value);
            grip.expectedControlType = "Axis";
            grip.AddBinding($"<XRHandInteraction>{{XR{side}Hand}}/squeezeValue");
            grip.AddBinding($"<XRController>{{XR{side}Hand}}/grip");

            var aimPose = map.AddAction("AimPose", InputActionType.Value);
            aimPose.expectedControlType = "Pose";
            aimPose.AddBinding($"<XRHandInteraction>{{XR{side}Hand}}/aimPose");
            aimPose.AddBinding($"<XRController>{{XR{side}Hand}}/devicePose");

            var gripPose = map.AddAction("GripPose", InputActionType.Value);
            gripPose.expectedControlType = "Pose";
            gripPose.AddBinding($"<XRHandInteraction>{{XR{side}Hand}}/gripPose");
            gripPose.AddBinding($"<XRController>{{XR{side}Hand}}/devicePose");
        }

        // ── Hand Setup ────────────────────────────────────────────────────────

        private static void SetupHand(string name, Handedness handedness, InputActionAsset inputAsset, Transform parent, GameObject visualizerPrefab, XRCameraRig rig = null)
        {
            var go = GetOrCreateGameObject(name, parent);

            var provider   = GetOrAdd<OpenXRHandDataProvider>(go);
            var providerSo = new SerializedObject(provider);
            providerSo.FindProperty("_handedness").enumValueIndex = (int)handedness;
            providerSo.FindProperty("_cameraRig").objectReferenceValue = rig;
            providerSo.ApplyModifiedProperties();

            GetOrAdd<HandGrabber>(go);

            var ghost   = GetOrAdd<HandVisual>(go);

            // If we have the Hand Visualizer prefab, instantiate it as a child
            // and auto-wire the renderers into HandVisual
            if (visualizerPrefab != null)
            {
                var meshChild = parent.Find(name + "_Mesh")?.gameObject
                    ?? (GameObject)PrefabUtility.InstantiatePrefab(visualizerPrefab, go.transform);
                meshChild.name = name + "_Mesh";

                var renderers = meshChild.GetComponentsInChildren<Renderer>();
                var ghostSo   = new SerializedObject(ghost);
                var renderersArray = ghostSo.FindProperty("_renderers");
                renderersArray.arraySize = renderers.Length;
                for (int i = 0; i < renderers.Length; i++)
                    renderersArray.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                ghostSo.ApplyModifiedProperties();
            }

            var matcher   = GetOrAdd<HandPoseMatcherComponent>(go);
            var matcherSo = new SerializedObject(matcher);
            matcherSo.FindProperty("_handProvider").objectReferenceValue = provider;
            matcherSo.ApplyModifiedProperties();

            var binding   = GetOrAdd<HandInputBinding>(go);
            var bindingSo = new SerializedObject(binding);
            string mapName = handedness == Handedness.Left ? "LeftHand" : "RightHand";
            var pinchRef   = InputActionReference.Create(inputAsset.FindActionMap(mapName).FindAction("Pinch"));
            bindingSo.FindProperty("_pinchAction").objectReferenceValue = pinchRef;
            bindingSo.ApplyModifiedProperties();

            EditorUtility.SetDirty(go);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        // Searches the imported Hand Visualizer sample for a prefab matching the hand side
        private static GameObject FindHandVisualizerPrefab(string prefabName)
        {
            var guids = AssetDatabase.FindAssets($"t:Prefab {prefabName}", new[] { SampleImportPath });
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static GameObject GetOrCreateGameObject(string name, Transform parent = null)
        {
            var existing = parent != null
                ? parent.Find(name)?.gameObject
                : GameObject.Find(name);

            if (existing != null) return existing;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

            if (parent != null)
                go.transform.SetParent(parent, false);

            return go;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            return go.TryGetComponent<T>(out var c) ? c : Undo.AddComponent<T>(go);
        }
    }
}
