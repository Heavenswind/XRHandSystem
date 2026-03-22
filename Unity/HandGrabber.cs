using UnityEngine;
using XRHandSystem.Core;

namespace XRHandSystem.Unity
{
    // Attach alongside OpenXRHandDataProvider on each hand GameObject.
    // Handles grab detection, joint attachment, throw velocity, and pose projection.
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

        [Header("Pose Projection")]
        [Tooltip("Distance at which pose blending starts.")]
        [SerializeField] private float _blendStartDistance = 0.15f;

        [Tooltip("Distance at which pose is fully snapped to grab pose.")]
        [SerializeField] private float _blendEndDistance = 0.01f;

        [Tooltip("Optional — drives blend using pinch value as well as distance.")]
        [SerializeField] private bool _usePinchBlend = true;

        // Set this from HandInputBinding so the blender can factor in pinch value
        public float CurrentPinchValue { get; set; }

        private const int VelocityFrames = 5;

        private OpenXRHandDataProvider _handProvider;
        private HandVisual             _ghostHand;
        private IXRGrabbable           _heldObject;
        private IXRGrabbable           _nearestCandidate;
        private ConfigurableJoint      _joint;

        private Vector3[] _velocityBuffer;
        private int       _velocityIndex;

        public bool IsHolding => _heldObject != null;

        private void Awake()
        {
            _handProvider   = GetComponent<OpenXRHandDataProvider>();
            _ghostHand      = GetComponent<HandVisual>();
            _velocityBuffer = new Vector3[VelocityFrames];
        }

        private void Update()
        {
            if (!_handProvider.IsTracked) return;

            TrackVelocity();
            UpdatePoseProjection();
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

        // ── Pose Projection ───────────────────────────────────────────────────

        private void UpdatePoseProjection()
        {
            if (_ghostHand == null) return;

            // While holding, stay fully in grab pose
            if (_heldObject != null)
            {
                if (_heldObject.GrabPose != null)
                    _ghostHand.SetBlendedCurls(_heldObject.GrabPose.fingerCurls);
                return;
            }

            // Find nearest grabbable within blend range
            Pose palmPose = _handProvider.GetJointPose(HandJoint.Palm);
            _nearestCandidate = FindNearestInRange(palmPose.position, _blendStartDistance);

            if (_nearestCandidate == null || _nearestCandidate.GrabPose == null)
            {
                // No nearby grabbable — show live hand pose
                float[] liveCurls = FingerCurlCalculator.Calculate(_handProvider);
                _ghostHand.SetBlendedCurls(liveCurls);
                return;
            }

            // Compute blend
            float distance = Vector3.Distance(palmPose.position, _nearestCandidate.Transform.position);
            float blend    = _usePinchBlend
                ? HandPoseBlender.BlendFromDistanceAndPinch(distance, CurrentPinchValue, _blendStartDistance, _blendEndDistance)
                : HandPoseBlender.BlendFromDistance(distance, _blendStartDistance, _blendEndDistance);

            float[] live    = FingerCurlCalculator.Calculate(_handProvider);
            float[] blended = HandPoseBlender.Blend(live, _nearestCandidate.GrabPose.fingerCurls, blend);

            _ghostHand.SetBlendedCurls(blended);
        }

        // ── Attach / Detach ───────────────────────────────────────────────────

        private void Attach(IXRGrabbable grabbable)
        {
            _heldObject = grabbable;

            _joint = grabbable.Transform.gameObject.AddComponent<ConfigurableJoint>();

            _joint.xMotion        = ConfigurableJointMotion.Free;
            _joint.yMotion        = ConfigurableJointMotion.Free;
            _joint.zMotion        = ConfigurableJointMotion.Free;
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
            _joint.slerpDrive        = drive;
            _joint.rotationDriveMode = RotationDriveMode.Slerp;
            _joint.connectedAnchor   = _handProvider.GetJointPose(HandJoint.Palm).position;

            _heldObject.OnGrabStart(_handProvider);
        }

        private void Detach()
        {
            if (_joint != null)
                Destroy(_joint);

            var rb = _heldObject.Transform.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = AverageVelocity();

            _heldObject.OnGrabEnd(_handProvider);
            _heldObject = null;
        }

        private void LateUpdate()
        {
            if (_joint == null) return;

            Pose palm = _handProvider.GetJointPose(HandJoint.Palm);
            _joint.connectedAnchor = palm.position;
            _joint.targetRotation  = Quaternion.Inverse(palm.rotation);
        }

        // ── Velocity Tracking ─────────────────────────────────────────────────

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

        // ── Nearest Grabbable ─────────────────────────────────────────────────

        private IXRGrabbable FindNearest(Collider[] hits, Vector3 origin)
        {
            IXRGrabbable best     = null;
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

        private IXRGrabbable FindNearestInRange(Vector3 origin, float range)
        {
            Collider[] hits = Physics.OverlapSphere(origin, range, _grabMask);
            return FindNearest(hits, origin);
        }
    }
}
