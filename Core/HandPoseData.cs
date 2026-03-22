using UnityEngine;

namespace XRHandSystem.Core
{
    [CreateAssetMenu(fileName = "NewHandPose", menuName = "XRHandSystem/Hand Pose")]
    public class HandPoseData : ScriptableObject
    {
        // One curl value per finger: 0 = fully open, 1 = fully closed
        // Order: Thumb, Index, Middle, Ring, Pinky
        public float[] fingerCurls = new float[5];

        // Full joint rotations for precise matching (26 joints, XR_HAND_JOINT_COUNT_EXT)
        // Optional — fingerCurls alone is enough for most use cases
        public Quaternion[] jointRotations = new Quaternion[26];

        public bool usePreciseMatching = false;
    }

    public static class FingerIndex
    {
        public const int Thumb  = 0;
        public const int Index  = 1;
        public const int Middle = 2;
        public const int Ring   = 3;
        public const int Pinky  = 4;
    }
}
