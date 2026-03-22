using UnityEngine;
using UnityEngine.XR.Hands;
using XRHandSystem.Core;

namespace XRHandSystem.Unity
{
    // Attach to a GameObject in your scene (one per hand).
    // Implements IHandDataProvider by reading from com.unity.xr.hands each frame.
    // Also moves this GameObject to follow the wrist joint in world space.
    public class OpenXRHandDataProvider : MonoBehaviour, IHandDataProvider
    {
        [SerializeField] private Core.Handedness _handedness = Core.Handedness.Left;

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

            // Move this GameObject to the wrist joint each frame so grabber and
            // other components that read from transform are at the right position.
            // The hand mesh is driven by XRHandMeshController directly from joints,
            // not from this transform, so no double-transform occurs.
            if (IsTracked)
            {
                var wristJoint = _hand.GetJoint(XRHandJointID.Wrist);
                if (wristJoint.TryGetPose(out Pose wristPose))
                    transform.position = wristPose.position;
                    // Do NOT set rotation here — mesh controller owns the rotation per-bone
            }
        }

        public Pose GetJointPose(HandJoint joint)
        {
            if (!IsTracked)
                return Pose.identity;

            var xrJoint = (XRHandJointID)((int)joint + 1); // XRHandJointID is 1-based
            if (_hand.GetJoint(xrJoint).TryGetPose(out Pose pose))
                return pose;

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
    }
}
