using UnityEngine;

namespace XRHandSystem.Core
{
    public interface IXRGrabbable
    {
        // Whether this object is currently held
        bool IsGrabbed { get; }

        // Optional pose to snap the hand to when grabbed (null = no snap)
        HandPoseData GrabPose { get; }

        Transform Transform { get; }

        void OnGrabStart(IHandDataProvider hand);
        void OnGrabEnd(IHandDataProvider hand);
    }
}
