using UnityEngine;
using UnityEngine.InputSystem.XR;

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
            // Camera starts at the height it was placed in the scene (seated/standing)
            Device,
            // Camera is offset so the floor is at y=0 (room-scale)
            Floor
        }

        [SerializeField] private TrackingOrigin _trackingOrigin = TrackingOrigin.Floor;
        [SerializeField] private float          _deviceModeEyeHeight = 1.6f;

        [SerializeField] private Transform _trackingSpace;
        [SerializeField] private Camera    _camera;

        private void Awake()
        {
            ApplyTrackingOrigin();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyTrackingOrigin();
        }
#endif

        private void ApplyTrackingOrigin()
        {
            if (_trackingSpace == null) return;

            switch (_trackingOrigin)
            {
                case TrackingOrigin.Floor:
                    // OpenXR floor-level tracking — TrackingSpace sits at y=0
                    _trackingSpace.localPosition = Vector3.zero;
                    break;

                case TrackingOrigin.Device:
                    // Offset upward so the camera starts at eye height
                    _trackingSpace.localPosition = new Vector3(0f, _deviceModeEyeHeight, 0f);
                    break;
            }
        }

        public Camera Camera => _camera;
        public Transform TrackingSpace => _trackingSpace;
    }
}
