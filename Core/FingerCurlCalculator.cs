using UnityEngine;

namespace XRHandSystem.Core
{
    // Computes a 0-1 curl value per finger from raw joint poses.
    // 0 = fully open/extended, 1 = fully closed/curled.
    //
    // Strategy: for each finger, sum the bend angles across its proximal,
    // intermediate, and distal joints, then normalize against the expected
    // max bend range.
    public static class FingerCurlCalculator
    {
        // Max total bend angle (degrees) across all joints of a fully closed finger.
        // Thumb has less range so uses a separate constant.
        private const float MaxFingerBendDegrees = 270f; // 3 joints × ~90° each
        private const float MaxThumbBendDegrees  = 150f; // thumb has less range

        // Joint triplets (proximal, intermediate, distal) per finger
        // We measure bend at each joint relative to its parent
        private static readonly (HandJoint proximal, HandJoint intermediate, HandJoint distal)[] FingerJoints =
        {
            (HandJoint.ThumbProximal,  HandJoint.ThumbDistal,        HandJoint.ThumbTip),         // Thumb
            (HandJoint.IndexProximal,  HandJoint.IndexIntermediate,  HandJoint.IndexDistal),       // Index
            (HandJoint.MiddleProximal, HandJoint.MiddleIntermediate, HandJoint.MiddleDistal),      // Middle
            (HandJoint.RingProximal,   HandJoint.RingIntermediate,   HandJoint.RingDistal),        // Ring
            (HandJoint.LittleProximal, HandJoint.LittleIntermediate, HandJoint.LittleDistal),      // Pinky
        };

        public static float[] Calculate(IHandDataProvider hand)
        {
            var curls = new float[5];

            for (int i = 0; i < 5; i++)
            {
                var (proximal, intermediate, distal) = FingerJoints[i];

                float bend = 0f;
                bend += BendAngle(hand, proximal,     intermediate);
                bend += BendAngle(hand, intermediate, distal);

                float maxBend = (i == FingerIndex.Thumb) ? MaxThumbBendDegrees : MaxFingerBendDegrees;
                curls[i] = Mathf.Clamp01(bend / maxBend);
            }

            return curls;
        }

        // Measures how much 'child' bends relative to 'parent' along the flex axis.
        // Uses the angle between the forward vectors of consecutive joints in local space.
        private static float BendAngle(IHandDataProvider hand, HandJoint parent, HandJoint child)
        {
            Pose parentPose = hand.GetJointPose(parent);
            Pose childPose  = hand.GetJointPose(child);

            // Project child's forward onto parent's plane to get bend in the flex axis only
            Vector3 parentForward = parentPose.rotation * Vector3.forward;
            Vector3 childForward  = childPose.rotation  * Vector3.forward;

            return Vector3.Angle(parentForward, childForward);
        }
    }
}
