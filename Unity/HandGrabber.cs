using System.Collections.Generic;
using UnityEngine;
using XRHandSystem.Core;

namespace XRHandSystem.Unity
{
    // Attach alongside OpenXRHandDataProvider on each hand GameObject.
    // Handles grab detection, joint attachment, and throw velocity.
    [RequireComponent(typeof(OpenXRHandDataProvider))]
    public class HandGrabber : MonoBehaviour
    {
        [Tooltip("Radius around the palm joint to search for grabbables.")]
        [SerializeField] private float _grabRadius = 0.05f;

        [Tooltip("LayerMask for grabbable objects.")]
        [SerializeField] private LayerMask _grabMask = ~0;

        [Tooltip("How strongly the held object follows the hand (spring).")]
        [SerializeField] private float _jointSpring = 3000f;
        [SerializeField] private float _jointDamper = 100f;

        // How many frames to average for throw velocity
        private const int VelocityFrames = 5;

        private OpenXRHandDataProvider _handProvider;
        private IXRGrabbable _heldObject;
        private ConfigurableJoint _joint;

        private Vector3[] _velocityBuffer;
        private int _velocityIndex;

        // Grab/release are driven externally — call these from your input binding
        // (pinch gesture, controller button, etc.)
        public bool IsHolding => _heldObject != null;

        private void Awake()
        {
            _handProvider    = GetComponent<OpenXRHandDataProvider>();
            _velocityBuffer  = new Vector3[VelocityFrames];
        }

        private void Update()
        {
            if (!_handProvider.IsTracked) return;
            TrackVelocity();
        }

        public void TryGrab()
        {
            if (_heldObject != null) return;

            Pose palmPose = _handProvider.GetJointPose(HandJoint.Palm);
            Collider[] hits = Physics.OverlapSphere(palmPose.position, _grabRadius, _grabMask);

            IXRGrabbable best = FindNearest(hits, palmPose.position);
            if (best == null) return;

            Attach(best);
        }

        public void Release()
        {
            if (_heldObject == null) return;

            Detach();
        }

        private void Attach(IXRGrabbable grabbable)
        {
            _heldObject = grabbable;

            // Create a ConfigurableJoint on the grabbed rigidbody anchored to this hand
            var rb = grabbable.Transform.GetComponent<Rigidbody>();
            _joint = grabbable.Transform.gameObject.AddComponent<ConfigurableJoint>();

            _joint.xMotion = ConfigurableJointMotion.Free;
            _joint.yMotion = ConfigurableJointMotion.Free;
            _joint.zMotion = ConfigurableJointMotion.Free;
            _joint.angularXMotion = ConfigurableJointMotion.Free;
            _joint.angularYMotion = ConfigurableJointMotion.Free;
            _joint.angularZMotion = ConfigurableJointMotion.Free;

            var drive = new JointDrive
            {
                positionSpring = _jointSpring,
                positionDamper = _jointDamper,
                maximumForce   = float.MaxValue
            };

            _joint.xDrive = _joint.yDrive = _joint.zDrive = drive;
            _joint.slerpDrive = drive;
            _joint.rotationDriveMode = RotationDriveMode.Slerp;

            _joint.targetPosition = Vector3.zero;
            _joint.connectedAnchor = _handProvider.GetJointPose(HandJoint.Palm).position;

            _heldObject.OnGrabStart(_handProvider);
        }

        private void Detach()
        {
            if (_joint != null)
                Destroy(_joint);

            // Apply throw velocity
            var rb = _heldObject.Transform.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = AverageVelocity();

            _heldObject.OnGrabEnd(_handProvider);
            _heldObject = null;
        }

        private void LateUpdate()
        {
            if (_joint == null) return;

            // Keep the joint target tracking the palm pose each frame
            Pose palm = _handProvider.GetJointPose(HandJoint.Palm);
            _joint.connectedAnchor = palm.position;
            _joint.targetRotation  = Quaternion.Inverse(palm.rotation);
        }

        private void TrackVelocity()
        {
            Pose palm = _handProvider.GetJointPose(HandJoint.Palm);
            _velocityBuffer[_velocityIndex % VelocityFrames] = palm.position;
            _velocityIndex++;
        }

        private Vector3 AverageVelocity()
        {
            int count = Mathf.Min(_velocityIndex, VelocityFrames);
            if (count < 2) return Vector3.zero;

            Vector3 oldest = _velocityBuffer[(_velocityIndex - count) % VelocityFrames];
            Vector3 newest = _velocityBuffer[(_velocityIndex - 1)     % VelocityFrames];
            float   dt     = (count - 1) * Time.deltaTime;

            return (newest - oldest) / dt;
        }

        private IXRGrabbable FindNearest(Collider[] hits, Vector3 origin)
        {
            IXRGrabbable best   = null;
            float        bestDist = float.MaxValue;

            foreach (var col in hits)
            {
                var grabbable = col.GetComponentInParent<IXRGrabbable>();
                if (grabbable == null || grabbable.IsGrabbed) continue;

                float dist = Vector3.Distance(origin, col.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best     = grabbable;
                }
            }

            return best;
        }
    }
}
