using UnityEditor;
using UnityEngine;
using XRHandSystem.Core;
using XRHandSystem.Unity;

namespace XRHandSystem.Editor
{
    public class HandPoseEditorWindow : EditorWindow
    {
        private HandPoseData _targetPose;
        private OpenXRHandDataProvider _leftProvider;
        private OpenXRHandDataProvider _rightProvider;

        private static readonly string[] FingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };

        [MenuItem("XRHandSystem/Pose Editor")]
        public static void Open()
        {
            GetWindow<HandPoseEditorWindow>("Hand Pose Editor");
        }

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
                EditorUtility.SetDirty(_targetPose);

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

        private void RecordFromProvider(OpenXRHandDataProvider provider)
        {
            float[] curls = FingerCurlCalculator.Calculate(provider);

            Undo.RecordObject(_targetPose, "Record Hand Pose");
            for (int i = 0; i < 5; i++)
                _targetPose.fingerCurls[i] = curls[i];

            EditorUtility.SetDirty(_targetPose);
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
