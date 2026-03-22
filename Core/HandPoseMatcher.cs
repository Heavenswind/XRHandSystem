using System;
using UnityEngine;

namespace XRHandSystem.Core
{
    // Pure logic — no MonoBehaviour, no engine dependencies beyond UnityEngine.Mathf.
    // Can be driven from any component or system.
    public class HandPoseMatcher
    {
        // Score must reach this to fire OnPoseMatched (0–1)
        public float MatchThreshold = 0.85f;

        // How long (seconds) the score must stay above threshold before firing
        public float HoldDuration = 0.2f;

        public HandPoseData TargetPose { get; private set; }

        public event Action<PoseMatchResult> OnPoseMatched;
        public event Action                  OnPoseLost;

        private bool  _wasMatched;
        private float _holdTimer;

        public void SetTargetPose(HandPoseData pose)
        {
            TargetPose  = pose;
            _wasMatched = false;
            _holdTimer  = 0f;
        }

        // Call this every frame with the current hand data and delta time.
        // Returns the current match result.
        public PoseMatchResult Evaluate(IHandDataProvider hand, float deltaTime)
        {
            if (TargetPose == null || !hand.IsTracked)
                return PoseMatchResult.Empty;

            float[] liveCurls   = FingerCurlCalculator.Calculate(hand);
            float[] perFinger   = new float[5];
            float   totalScore  = 0f;

            for (int i = 0; i < 5; i++)
            {
                float error    = Mathf.Abs(TargetPose.fingerCurls[i] - liveCurls[i]);
                perFinger[i]   = 1f - error;
                totalScore    += perFinger[i];
            }

            totalScore /= 5f;

            bool isMatch = totalScore >= MatchThreshold;
            var result   = new PoseMatchResult(totalScore, isMatch, perFinger);

            if (isMatch)
            {
                _holdTimer += deltaTime;
                if (_holdTimer >= HoldDuration && !_wasMatched)
                {
                    _wasMatched = true;
                    OnPoseMatched?.Invoke(result);
                }
            }
            else
            {
                if (_wasMatched)
                    OnPoseLost?.Invoke();

                _wasMatched = false;
                _holdTimer  = 0f;
            }

            return result;
        }
    }
}
