namespace XRHandSystem.Core
{
    public readonly struct PoseMatchResult
    {
        // Overall match score: 0 = no match, 1 = perfect match
        public readonly float Score;

        // True when Score >= the threshold set on the matcher
        public readonly bool IsMatch;

        // Per-finger scores — useful for UI feedback (e.g. highlight which fingers are wrong)
        // Order: Thumb, Index, Middle, Ring, Pinky
        public readonly float[] PerFingerScore;

        public PoseMatchResult(float score, bool isMatch, float[] perFingerScore)
        {
            Score         = score;
            IsMatch        = isMatch;
            PerFingerScore = perFingerScore;
        }

        public static readonly PoseMatchResult Empty = new PoseMatchResult(0f, false, new float[5]);
    }
}
