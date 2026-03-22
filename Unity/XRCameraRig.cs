using UnityEngine;

namespace XRHandSystem.Unity
{
    // Minimal camera rig. No XRI dependency — uses TrackedPoseDriver from
    // com.unity.inputsystem (already required by the package).
    //
    // Hierarchy this component expects:
    //   XRCameraRig (this component)
    //     └── TrackingSpace
    //           ├── MainCamera  (Camera + TrackedPoseDriver)
    //           ├── XRHand_Left
    //           └── XRHand_Right
    public class XRCameraRig : MonoBehaviour
    {
        public enum TrackingOrigin
        {
            Device,
            Floor
        }

        [SerializeField] private TrackingOrigin _trackingOrigin = TrackingOrigin.Floor;
        [SerializeField] private float          _deviceModeEyeHeight = 1.6f;
        [SerializeField] private Transform      _trackingSpace;
        [SerializeField] private Camera         _camera;

        private void Awake() => ApplyTrackingOrigin();

#if UNITY_EDITOR
        private void OnValidate() => ApplyTrackingOrigin();
#endif

        private void ApplyTrackingOrigin()
        {
            if (_trackingSpace == null) return;

            _trackingSpace.localPosition = _trackingOrigin == TrackingOrigin.Floor
                ? Vector3.zero
                : new Vector3(0f, _deviceModeEyeHeight, 0f);
        }

        public Camera    Camera         => _camera;
        public Transform TrackingSpace  => _trackingSpace;
    }
}
