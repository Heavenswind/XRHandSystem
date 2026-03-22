using UnityEngine;

namespace XRHandSystem.Core
{
    public enum HandTrackingState
    {
        Untracked,
        Inferred,
        Tracked
    }

    public enum Handedness
    {
        Left,
        Right
    }

    // Joint indices match XR_HAND_JOINT_EXT ordering from OpenXR spec
    public enum HandJoint
    {
        Palm           = 0,
        Wrist          = 1,
        ThumbMetacarpal    = 2,
        ThumbProximal      = 3,
        ThumbDistal        = 4,
        ThumbTip           = 5,
        IndexMetacarpal    = 6,
        IndexProximal      = 7,
        IndexIntermediate  = 8,
        IndexDistal        = 9,
        IndexTip           = 10,
        MiddleMetacarpal   = 11,
        MiddleProximal     = 12,
        MiddleIntermediate = 13,
        MiddleDistal       = 14,
        MiddleTip          = 15,
        RingMetacarpal     = 16,
        RingProximal       = 17,
        RingIntermediate   = 18,
        RingDistal         = 19,
        RingTip            = 20,
        LittleMetacarpal   = 21,
        LittleProximal     = 22,
        LittleIntermediate = 23,
        LittleDistal       = 24,
        LittleTip          = 25
    }

    public interface IHandDataProvider
    {
        Handedness Handedness { get; }
        HandTrackingState TrackingState { get; }
        bool IsTracked { get; }

        // Returns joint pose in world space
        Pose GetJointPose(HandJoint joint);

        // Returns joint pose relative to wrist — useful for pose comparison
        Pose GetJointLocalPose(HandJoint joint);
    }
}
