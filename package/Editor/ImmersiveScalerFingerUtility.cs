using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace VRChatImmersiveScaler
{
    public static class ImmersiveScalerFingerUtility
    {
        // Finger bone names patterns
        private static readonly string[] fingerNames = { "thumb", "index", "middle", "ring", "pinky", "little" };
        private static readonly string[] fingerSegments = { "proximal", "intermediate", "distal", "1", "2", "3" };
        
        public static void SpreadFingers(GameObject avatar, float spreadFactor, bool spareThumb)
        {
            if (avatar == null) return;
            
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null || !animator.isHuman)
            {
                Debug.LogError("Avatar must have a humanoid animator!");
                return;
            }
            
            // Process both hands
            SpreadHandFingers(animator, true, spreadFactor, spareThumb);  // Left hand
            SpreadHandFingers(animator, false, spreadFactor, spareThumb); // Right hand
        }
        
        private static void SpreadHandFingers(Animator animator, bool isLeftHand, float spreadFactor, bool spareThumb)
        {
            Transform hand = animator.GetBoneTransform(isLeftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            if (hand == null) return;
            
            // Find all finger bones
            Transform[] fingerBones = GetFingerBones(hand);
            
            // Apply spreading rotation to each finger
            foreach (var finger in fingerBones)
            {
                if (finger == null) continue;
                
                string lowerName = finger.name.ToLower();
                
                // Skip thumb if requested
                if (spareThumb && lowerName.Contains("thumb"))
                    continue;
                
                // Calculate spread multiplier: 0 = together, 1 = normal, 2 = max spread
                float spreadMultiplier = spreadFactor - 1.0f;
                
                // Apply specific rotation based on finger type
                float rotationAngle = 0f;
                
                if (lowerName.Contains("thumb"))
                {
                    // Thumb spreads outward from palm (primarily Z-axis in T-pose)
                    float thumbRotZ = spreadMultiplier * 30f * (isLeftHand ? -1 : 1);
                    float thumbRotY = spreadMultiplier * 10f * (isLeftHand ? -1 : 1);
                    finger.localRotation *= Quaternion.Euler(0, thumbRotY, thumbRotZ);
                }
                else if (lowerName.Contains("index"))
                {
                    // Index finger spreads forward (positive X)
                    rotationAngle = spreadMultiplier * 15f;
                    finger.localRotation *= Quaternion.Euler(rotationAngle, 0, 0);
                }
                else if (lowerName.Contains("middle"))
                {
                    // Middle finger has minimal spread
                    rotationAngle = spreadMultiplier * 5f;
                    finger.localRotation *= Quaternion.Euler(rotationAngle, 0, 0);
                }
                else if (lowerName.Contains("ring"))
                {
                    // Ring finger spreads backward (negative X)
                    rotationAngle = spreadMultiplier * -10f;
                    finger.localRotation *= Quaternion.Euler(rotationAngle, 0, 0);
                }
                else if (lowerName.Contains("pinky") || lowerName.Contains("little"))
                {
                    // Pinky spreads the most backward
                    rotationAngle = spreadMultiplier * -20f;
                    finger.localRotation *= Quaternion.Euler(rotationAngle, 0, 0);
                }
            }
        }
        
        private static Transform[] GetFingerBones(Transform hand)
        {
            List<Transform> fingerBones = new List<Transform>();
            
            // Search for finger root bones (proximal/1st segments)
            foreach (Transform child in hand.GetComponentsInChildren<Transform>())
            {
                string lowerName = child.name.ToLower();
                
                // Check if this is a finger bone
                bool isFinger = false;
                foreach (string fingerName in fingerNames)
                {
                    if (lowerName.Contains(fingerName))
                    {
                        isFinger = true;
                        break;
                    }
                }
                
                if (!isFinger) continue;
                
                // Check if this is the first segment (proximal or 1)
                bool isFirstSegment = lowerName.Contains("proximal") || lowerName.Contains("_1") || 
                                     lowerName.EndsWith("1") || (!lowerName.Contains("2") && !lowerName.Contains("3") && 
                                     !lowerName.Contains("intermediate") && !lowerName.Contains("distal"));
                
                if (isFirstSegment)
                {
                    fingerBones.Add(child);
                }
            }
            
            return fingerBones.ToArray();
        }
    }
}