using UnityEngine;
using UnityEngine.Events;
using XRHandSystem.Core;

namespace XRHandSystem.Unity
{
    // MonoBehaviour wrapper around HandPoseMatcher.
    // Drop on any GameObject, wire up events in the Inspector.
    public class HandPoseMatcherComponent : MonoBehaviour
    {
        [SerializeField] private OpenXRHandDataProvider _handProvider;
        [SerializeField] private HandPoseData           _targetPose;
        [SerializeField] private HandVisual              _ghostHand;

        [Range(0f, 1f)]
        [SerializeField] private float _matchThreshold = 0.85f;
        [SerializeField] private float _holdDuration   = 0.2f;

        [Space]
        public UnityEvent         OnPoseMatched;
        public UnityEvent         OnPoseLost;
        public UnityEvent<float>  OnScoreChanged;  // fires every frame with current score

        private HandPoseMatcher _matcher;

        private void Awake()
        {
            _matcher = new HandPoseMatcher
            {
                MatchThreshold = _matchThreshold,
                HoldDuration   = _holdDuration
            };

            _matcher.OnPoseMatched += _ =>
            {
                OnPoseMatched.Invoke();
            };

            _matcher.OnPoseLost += () =>
            {
                OnPoseLost.Invoke();
            };
        }

        private void OnEnable()
        {
            if (_targetPose != null)
                _matcher.SetTargetPose(_targetPose);
        }

        public void SetTargetPose(HandPoseData pose)
        {
            _targetPose = pose;
            _matcher.SetTargetPose(pose);

            if (_ghostHand != null)
                _ghostHand.SetTargetPose(pose);
        }

        private void Update()
        {
            if (_handProvider == null || _targetPose == null) return;

            PoseMatchResult result = _matcher.Evaluate(_handProvider, Time.deltaTime);

            OnScoreChanged.Invoke(result.Score);

            if (_ghostHand != null)
                _ghostHand.SetMatchFeedback(result.Score);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_matcher != null)
            {
                _matcher.MatchThreshold = _matchThreshold;
                _matcher.HoldDuration   = _holdDuration;
            }
        }
#endif
    }
}
