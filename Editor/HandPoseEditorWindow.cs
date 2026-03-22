using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Hands;
using XRHandSystem.Core;
using XRHandSystem.Unity;

namespace XRHandSystem.Editor
{
    public class HandPoseEditorWindow : EditorWindow
    {
        private HandPoseData _targetPose;
        private OpenXRHandDataProvider _leftProvider;
        private OpenXRHandDataProvider _rightProvider;

        // Preview hand
        private GameObject _previewHand;
        private Transform[] _fingerRoots = new Transform[5];
        private bool _previewVisible;
        private bool _isLeftPreview = true;

        private static readonly string[] FingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };

        // Per-finger max bend angles matching the hand mesh rig
        private static readonly float[] MaxAngles = { 30f, 90f, 90f, 90f, 90f };

        // Joint indices per finger in XRHandJointID order (proximal, intermediate, distal)
        private static readonly XRHandJointID[][] FingerJoints =
        {
            new[] { XRHandJointID.ThumbProximal,  XRHandJointID.ThumbDistal,         XRHandJointID.ThumbTip         },
            new[] { XRHandJointID.IndexProximal,   XRHandJointID.IndexIntermediate,   XRHandJointID.IndexDistal      },
            new[] { XRHandJointID.MiddleProximal,  XRHandJointID.MiddleIntermediate,  XRHandJointID.MiddleDistal     },
            new[] { XRHandJointID.RingProximal,    XRHandJointID.RingIntermediate,    XRHandJointID.RingDistal       },
            new[] { XRHandJointID.LittleProximal,  XRHandJointID.LittleIntermediate,  XRHandJointID.LittleDistal    },
        };

        [MenuItem("XRHandSystem/Pose Editor")]
        public static void Open()
        {
            GetWindow<HandPoseEditorWindow>("Hand Pose Editor");
        }

        private void OnDestroy() => DestroyPreview();
        private void OnDisable() => DestroyPreview();

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Hand Pose Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _targetPose = (HandPoseData)EditorGUILayout.ObjectField(
                "Pose Asset", _targetPose, typeof(HandPoseData), false);

            if (_targetPose == null)
            {
                EditorGUILayout.HelpBox("Assign or create a Hand Pose asset above.", MessageType.Info);
                if (GUILayout.Button("Create New Pose Asset"))
                    CreateNewPoseAsset();
                return;
            }

            // ── Preview Hand ──────────────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview Hand", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _isLeftPreview = EditorGUILayout.Toggle("Left Hand", _isLeftPreview);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (!_previewVisible)
            {
                if (GUILayout.Button("Spawn Preview Hand"))
                    SpawnPreview();
            }
            else
            {
                if (GUILayout.Button("Remove Preview Hand"))
                    DestroyPreview();
            }
            EditorGUILayout.EndHorizontal();

            // ── Finger Curls ──────────────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Finger Curls", EditorStyles.boldLabel);

            bool changed = false;
            for (int i = 0; i < 5; i++)
            {
                float newVal = EditorGUILayout.Slider(
                    FingerNames[i], _targetPose.fingerCurls[i], 0f, 1f);
                if (!Mathf.Approximately(newVal, _targetPose.fingerCurls[i]))
                {
                    _targetPose.fingerCurls[i] = newVal;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(_targetPose);
                if (_previewVisible)
                    ApplyCurlsToPreview();
            }

            // ── Record from Live Hand ─────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Record from Live Hand", EditorStyles.boldLabel);

            _leftProvider  = (OpenXRHandDataProvider)EditorGUILayout.ObjectField(
                "Left Hand Provider",  _leftProvider,  typeof(OpenXRHandDataProvider), true);
            _rightProvider = (OpenXRHandDataProvider)EditorGUILayout.ObjectField(
                "Right Hand Provider", _rightProvider, typeof(OpenXRHandDataProvider), true);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Record Left") && _leftProvider != null && _leftProvider.IsTracked)
                RecordFromProvider(_leftProvider);
            if (GUILayout.Button("Record Right") && _rightProvider != null && _rightProvider.IsTracked)
                RecordFromProvider(_rightProvider);
            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to record from a live hand.", MessageType.Warning);

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            _targetPose.usePreciseMatching = EditorGUILayout.Toggle(
                "Use Precise Matching", _targetPose.usePreciseMatching);
        }

        // ── Preview Hand ──────────────────────────────────────────────────

        private void SpawnPreview()
        {
            DestroyPreview();

            string prefabName = _isLeftPreview ? "Left Hand Tracking" : "Right Hand Tracking";
            string[] guids = AssetDatabase.FindAssets($"t:Prefab {prefabName}",
                new[] { "Assets/Samples/XR Hands" });

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("XR Hand System",
                    "Hand Visualizer sample not found.\nRun XRHandSystem > Setup Scene first.", "OK");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                AssetDatabase.GUIDToAssetPath(guids[0]));

            _previewHand = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            _previewHand.name = "[XRHandSystem Preview]";
            _previewHand.hideFlags = HideFlags.DontSave; // auto-cleanup on domain reload

            // Disable runtime components so they don't error in editor
            foreach (var mb in _previewHand.GetComponentsInChildren<MonoBehaviour>())
                mb.enabled = false;

            // Map joint transforms from the skeleton driver
            MapFingerRoots();

            ApplyCurlsToPreview();
            _previewVisible = true;

            SceneView.RepaintAll();
        }

        private void MapFingerRoots()
        {
            // The skeleton driver stores a jointTransformReferences list we can read
            var driver = _previewHand.GetComponentInChildren<XRHandSkeletonDriver>();
            if (driver == null) return;

            for (int f = 0; f < 5; f++)
            {
                XRHandJointID proximalId = FingerJoints[f][0];
                foreach (var entry in driver.jointTransformReferences)
                {
                    if (entry.xrHandJointID == proximalId)
                    {
                        _fingerRoots[f] = entry.jointTransform;
                        break;
                    }
                }
            }
        }

        private void ApplyCurlsToPreview()
        {
            if (_previewHand == null) return;

            var driver = _previewHand.GetComponentInChildren<XRHandSkeletonDriver>();
            if (driver == null) return;

            for (int f = 0; f < 5; f++)
            {
                float curl     = _targetPose.fingerCurls[f];
                float maxAngle = MaxAngles[f];
                float angle    = curl * maxAngle;

                foreach (var jointId in FingerJoints[f])
                {
                    foreach (var entry in driver.jointTransformReferences)
                    {
                        if (entry.xrHandJointID == jointId && entry.jointTransform != null)
                        {
                            entry.jointTransform.localRotation = Quaternion.Euler(angle, 0f, 0f);
                            break;
                        }
                    }
                }
            }

            SceneView.RepaintAll();
        }

        private void DestroyPreview()
        {
            if (_previewHand != null)
                DestroyImmediate(_previewHand);

            _previewHand    = null;
            _previewVisible = false;
            System.Array.Clear(_fingerRoots, 0, _fingerRoots.Length);
        }

        // ── Record / Save ─────────────────────────────────────────────────

        private void RecordFromProvider(OpenXRHandDataProvider provider)
        {
            float[] curls = FingerCurlCalculator.Calculate(provider);

            Undo.RecordObject(_targetPose, "Record Hand Pose");
            for (int i = 0; i < 5; i++)
                _targetPose.fingerCurls[i] = curls[i];

            EditorUtility.SetDirty(_targetPose);

            if (_previewVisible)
                ApplyCurlsToPreview();

            Debug.Log($"[XRHandSystem] Recorded pose from {provider.Handedness} hand.");
        }

        private void CreateNewPoseAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Hand Pose", "NewHandPose", "asset", "Choose save location");

            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<HandPoseData>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _targetPose = asset;
        }
    }
}
