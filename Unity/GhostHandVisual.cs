using UnityEngine;
using XRHandSystem.Core;

namespace XRHandSystem.Unity
{
    // Renders a semi-transparent "ghost" hand showing the target pose.
    // Also drives the live hand mesh via pose projection — blending between
    // the tracked hand and a grab pose as the hand approaches a grabbable.
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

        // Current blended curls — updated each frame by HandGrabber via SetBlendedCurls
        private float[] _currentCurls = new float[5];

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
            ApplyCurls(_targetPose.fingerCurls);
        }

        // Called by HandGrabber every frame with the blended curl values
        public void SetBlendedCurls(float[] blendedCurls)
        {
            _currentCurls = blendedCurls;
            ApplyCurls(_currentCurls);
        }

        // Called by HandPoseMatcherComponent — fades ghost out as pose is matched
        public void SetMatchFeedback(float matchScore)
        {
            float opacity = Mathf.Lerp(_opacity, 0.05f, matchScore);
            SetOpacity(opacity);
        }

        public void Show() => SetOpacity(_opacity);
        public void Hide() => SetOpacity(0f);

        private void ApplyCurls(float[] curls)
        {
            if (curls == null) return;
            for (int i = 0; i < 5; i++)
                ApplyFingerCurl(_fingers[i], curls[i]);
        }

        private void ApplyFingerCurl(FingerBoneChain chain, float curl)
        {
            if (chain.proximal     == null) return;
            if (chain.intermediate == null) return;
            if (chain.distal       == null) return;

            float maxAngle = chain.isThumb ? 30f : 90f;
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
