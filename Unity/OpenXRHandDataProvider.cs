using UnityEngine;
using UnityEngine.XR.Hands;
using XRHandSystem.Core;

namespace XRHandSystem.Unity
{
    // Attach to a GameObject in your scene (one per hand).
    // Implements IHandDataProvider by reading from com.unity.xr.hands each frame.
    public class OpenXRHandDataProvider : MonoBehaviour, IHandDataProvider
    {
        [SerializeField] private Core.Handedness _handedness = Core.Handedness.Left;

        [Tooltip("Reference to the XRCameraRig in the scene. Used to apply tracking origin offset to hand positions.")]
        [SerializeField] private XRCameraRig _cameraRig;

        public Core.Handedness Handedness => _handedness;
        public HandTrackingState TrackingState { get; private set; }
        public bool IsTracked => TrackingState == HandTrackingState.Tracked;

        private XRHandSubsystem _subsystem;
        private XRHand _hand;

        private void OnEnable()
        {
            var subsystems = new System.Collections.Generic.List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0)
                _subsystem = subsystems[0];

            // Auto-find rig if not assigned
            if (_cameraRig == null)
                _cameraRig = FindFirstObjectByType<XRCameraRig>();
        }

        private void Update()
        {
            if (_subsystem == null)
            {
                TrackingState = HandTrackingState.Untracked;
                return;
            }

            _hand = (_handedness == Core.Handedness.Left)
                ? _subsystem.leftHand
                : _subsystem.rightHand;

            TrackingState = _hand.isTracked
                ? HandTrackingState.Tracked
                : HandTrackingState.Untracked;

            if (IsTracked)
            {
                var wristJoint = _hand.GetJoint(XRHandJointID.Wrist);
                if (wristJoint.TryGetPose(out Pose wristPose))
                    transform.position = ApplyTrackingOriginOffset(wristPose.position);
            }
        }

        public Pose GetJointPose(HandJoint joint)
        {
            if (!IsTracked)
                return Pose.identity;

            var xrJoint = (XRHandJointID)((int)joint + 1); // XRHandJointID is 1-based
            if (_hand.GetJoint(xrJoint).TryGetPose(out Pose pose))
                return new Pose(ApplyTrackingOriginOffset(pose.position), pose.rotation);

            return Pose.identity;
        }

        public Pose GetJointLocalPose(HandJoint joint)
        {
            Pose world = GetJointPose(joint);
            Pose wrist = GetJointPose(HandJoint.Wrist);

            Quaternion invWristRot = Quaternion.Inverse(wrist.rotation);
            Vector3    localPos   = invWristRot * (world.position - wrist.position);
            Quaternion localRot   = invWristRot * world.rotation;

            return new Pose(localPos, localRot);
        }

        // Applies the TrackingSpace offset from XRCameraRig so hands align with the camera
        private Vector3 ApplyTrackingOriginOffset(Vector3 worldPos)
        {
            if (_cameraRig == null || _cameraRig.TrackingSpace == null)
                return worldPos;

            return worldPos + _cameraRig.TrackingSpace.localPosition;
        }
    }
}
