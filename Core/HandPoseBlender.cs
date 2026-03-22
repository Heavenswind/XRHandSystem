using UnityEngine;

namespace XRHandSystem.Core
{
    // Blends between a live hand pose and a target grab pose.
    // blend = 0 → fully live tracked hand
    // blend = 1 → fully snapped to grab pose
    public static class HandPoseBlender
    {
        // Returns a new curl array blended between live and target
        public static float[] Blend(float[] liveCurls, float[] targetCurls, float blend)
        {
            blend = Mathf.Clamp01(blend);
            var result = new float[5];
            for (int i = 0; i < 5; i++)
                result[i] = Mathf.Lerp(liveCurls[i], targetCurls[i], blend);
            return result;
        }

        // Computes blend value (0-1) from palm distance to a grabbable surface.
        // blendStartDistance: how far away blending begins
        // blendEndDistance:   distance at which blend reaches 1 (fully snapped)
        public static float BlendFromDistance(float distance, float blendStartDistance, float blendEndDistance)
        {
            if (distance >= blendStartDistance) return 0f;
            if (distance <= blendEndDistance)   return 1f;
            return 1f - Mathf.InverseLerp(blendEndDistance, blendStartDistance, distance);
        }

        // Optionally combine distance-based blend with pinch input so the
        // pose only projects when the user is actively closing their hand
        public static float BlendFromDistanceAndPinch(float distance, float pinch,
            float blendStartDistance, float blendEndDistance)
        {
            float distanceBlend = BlendFromDistance(distance, blendStartDistance, blendEndDistance);
            return distanceBlend * pinch;
        }
    }
}
