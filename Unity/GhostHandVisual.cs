using UnityEngine;
using XRHandSystem.Core;

namespace XRHandSystem.Unity
{
    // Renders a semi-transparent "ghost" hand showing the target pose.
    // Attach to a hand mesh GameObject. Drive its joint transforms from a HandPoseData asset.
    //
    // Setup: assign the same bone transforms your live hand uses,
    // then assign a target pose. The ghost will hold that pose statically
    // so the user can see what shape to form.
    public class GhostHandVisual : MonoBehaviour
    {
        [SerializeField] private HandPoseData _targetPose;

        [SerializeField] private Transform _wrist;

        // Finger bone chains: proximal → intermediate → distal, per finger
        // Order: Thumb, Index, Middle, Ring, Pinky
        [SerializeField] private FingerBoneChain[] _fingers = new FingerBoneChain[5];

        [SerializeField] private Renderer[] _renderers;

        [Range(0f, 1f)]
        [SerializeField] private float _opacity = 0.3f;

        private static readonly int AlphaProperty = Shader.PropertyToID("_Alpha");

        private void OnValidate()
        {
            if (_fingers.Length != 5)
                System.Array.Resize(ref _fingers, 5);
        }

        private void Start()
        {
            SetOpacity(_opacity);
        }

        public void SetTargetPose(HandPoseData pose)
        {
            _targetPose = pose;
            ApplyPose();
        }

        // Call this to pulse opacity as match score changes (0=open, 1=fully closed)
        public void SetMatchFeedback(float matchScore)
        {
            // Lerp opacity: dim when close to matching, full when far away
            float opacity = Mathf.Lerp(_opacity, 0.05f, matchScore);
            SetOpacity(opacity);
        }

        private void ApplyPose()
        {
            if (_targetPose == null) return;

            for (int i = 0; i < 5; i++)
            {
                float curl = _targetPose.fingerCurls[i];
                ApplyFingerCurl(_fingers[i], curl);
            }
        }

        // Distributes the curl value across the 3 joints of a finger
        private void ApplyFingerCurl(FingerBoneChain chain, float curl)
        {
            if (chain.proximal     == null) return;
            if (chain.intermediate == null) return;
            if (chain.distal       == null) return;

            // Thumb has less range so scale down
            float maxAngle = (chain.isThumb) ? 30f : 90f;
            float angle    = curl * maxAngle;

            chain.proximal.localRotation     = Quaternion.Euler(angle, 0f, 0f);
            chain.intermediate.localRotation = Quaternion.Euler(angle, 0f, 0f);
            chain.distal.localRotation       = Quaternion.Euler(angle * 0.5f, 0f, 0f);
        }

        private void SetOpacity(float alpha)
        {
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetFloat(AlphaProperty, alpha);
                r.SetPropertyBlock(block);
            }
        }

        [System.Serializable]
        public class FingerBoneChain
        {
            public bool      isThumb;
            public Transform proximal;
            public Transform intermediate;
            public Transform distal;
        }
    }
}
