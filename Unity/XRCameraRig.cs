using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace XRHandSystem.Unity
{
    public class XRCameraRig : MonoBehaviour
    {
        public enum TrackingOrigin
        {
            Device,
            Floor
        }

        [SerializeField] private TrackingOrigin _trackingOrigin = TrackingOrigin.Floor;
        [SerializeField] private Transform      _trackingSpace;
        [SerializeField] private Camera         _camera;

        public Camera    Camera        => _camera;
        public Transform TrackingSpace => _trackingSpace;

        private void Start()
        {
            ApplyTrackingOrigin();
        }

        private void ApplyTrackingOrigin()
        {
            // Tell the XR subsystem which origin mode to use — this is the correct
            // way to set floor vs device tracking. No manual transform offset needed.
            var subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);

            var mode = _trackingOrigin == TrackingOrigin.Floor
                ? TrackingOriginModeFlags.Floor
                : TrackingOriginModeFlags.Device;

            foreach (var subsystem in subsystems)
                subsystem.TrySetTrackingOriginMode(mode);

            // Keep TrackingSpace at zero — no manual offset
            if (_trackingSpace != null)
                _trackingSpace.localPosition = Vector3.zero;
        }
    }
}
