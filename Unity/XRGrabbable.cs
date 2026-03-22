using System;
using UnityEngine;
using XRHandSystem.Core;

namespace XRHandSystem.Unity
{
    // Add this to any GameObject you want to be grabbable.
    // Requires a Rigidbody.
    [RequireComponent(typeof(Rigidbody))]
    public class XRGrabbable : MonoBehaviour, IXRGrabbable
    {
        [SerializeField] private HandPoseData _grabPose;

        public bool IsGrabbed { get; private set; }
        public HandPoseData GrabPose => _grabPose;
        public Transform Transform => transform;

        public event Action<IHandDataProvider> Grabbed;
        public event Action<IHandDataProvider> Released;

        private Rigidbody _rb;
        private Joint _joint;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public void OnGrabStart(IHandDataProvider hand)
        {
            IsGrabbed = true;
            _rb.isKinematic = false;
            Grabbed?.Invoke(hand);
        }

        public void OnGrabEnd(IHandDataProvider hand)
        {
            IsGrabbed = false;
            Released?.Invoke(hand);
        }
    }
}
