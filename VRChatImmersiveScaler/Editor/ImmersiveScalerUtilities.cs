using UnityEngine;
using System.Collections.Generic;

namespace VRChatImmersiveScaler
{
    public static class ImmersiveScalerUtilities
    {
        // Reset all bone scales to 1,1,1
        public static void ResetAvatarScales(GameObject avatar)
        {
            if (avatar == null)
                return;
                
            Transform[] allTransforms = avatar.GetComponentsInChildren<Transform>();
            int resetCount = 0;
            
            foreach (Transform t in allTransforms)
            {
                if (t.localScale != Vector3.one)
                {
                    t.localScale = Vector3.one;
                    resetCount++;
                }
            }
            
            Debug.Log($"Reset {resetCount} transform scales to (1,1,1)");
        }
        // Spread fingers for better controller tracking
        public static void SpreadFingers(GameObject avatar, bool spareThumb, float spreadFactor)
        {
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null || !animator.isHuman)
            {
                Debug.LogError("Avatar must have a humanoid animator!");
                return;
            }
            
            // Get hand bones
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            
            if (leftHand != null)
                SpreadHandFingers(leftHand, spareThumb, spreadFactor, true);
                
            if (rightHand != null)
                SpreadHandFingers(rightHand, spareThumb, spreadFactor, false);
        }
        
        private static void SpreadHandFingers(Transform hand, bool spareThumb, float spreadFactor, bool isLeftHand)
        {
            // Find finger bones
            Dictionary<string, Transform> fingers = new Dictionary<string, Transform>();
            string prefix = isLeftHand ? "Left" : "Right";
            
            // Finger names
            string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Little" };
            string[] fingerParts = { "Proximal", "Intermediate", "Distal" };
            
            // Find all finger bones
            foreach (string fingerName in fingerNames)
            {
                if (spareThumb && fingerName == "Thumb")
                    continue;
                    
                foreach (string part in fingerParts)
                {
                    string boneName = prefix + fingerName + part;
                    Transform fingerBone = VRCBoneMapper.FindFingerBone(hand.root, boneName);
                    
                    if (fingerBone != null)
                    {
                        fingers[boneName] = fingerBone;
                    }
                }
            }
            
            // Calculate spread angles
            float[] spreadAngles = isLeftHand ? 
                new float[] { -10f, -5f, 0f, 5f, 10f } :  // Left hand
                new float[] { 10f, 5f, 0f, -5f, -10f };   // Right hand (mirrored)
                
            // Apply spreading to proximal bones
            int fingerIndex = 0;
            foreach (string fingerName in fingerNames)
            {
                if (spareThumb && fingerName == "Thumb")
                {
                    fingerIndex++;
                    continue;
                }
                
                string proximalName = prefix + fingerName + "Proximal";
                if (fingers.ContainsKey(proximalName))
                {
                    Transform proximal = fingers[proximalName];
                    
                    // Calculate rotation
                    float angle = spreadAngles[fingerIndex] * spreadFactor;
                    
                    // Apply rotation around the up axis (spreading)
                    Quaternion spreadRotation = Quaternion.AngleAxis(angle, Vector3.up);
                    proximal.localRotation = proximal.localRotation * spreadRotation;
                    
                    // Also add slight forward curl for more natural pose
                    if (fingerName != "Thumb")
                    {
                        Quaternion curlRotation = Quaternion.AngleAxis(5f * spreadFactor, Vector3.right);
                        proximal.localRotation = proximal.localRotation * curlRotation;
                    }
                }
                
                fingerIndex++;
            }
        }
        
        // Apply pose as rest pose (bake transforms)
        public static void ApplyPoseAsRestPose(GameObject avatar)
        {
            // Get all skinned mesh renderers
            SkinnedMeshRenderer[] renderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null)
                    continue;
                    
                // Create a new mesh instance
                Mesh bakedMesh = new Mesh();
                bakedMesh.name = renderer.sharedMesh.name + "_scaled";
                
                // Bake the current pose into the mesh
                renderer.BakeMesh(bakedMesh);
                
                // Update blend shapes if present
                if (renderer.sharedMesh.blendShapeCount > 0)
                {
                    // This is more complex - would need to bake each blend shape
                    Debug.LogWarning($"Mesh {renderer.name} has blend shapes - manual adjustment may be needed");
                }
                
                // Apply the baked mesh
                renderer.sharedMesh = bakedMesh;
            }
            
            // Reset all bone transforms to identity
            Transform[] allTransforms = avatar.GetComponentsInChildren<Transform>();
            foreach (var t in allTransforms)
            {
                if (t != avatar.transform)
                {
                    t.localPosition = Vector3.zero;
                    t.localRotation = Quaternion.identity;
                    t.localScale = Vector3.one;
                }
            }
        }
        
        // Validate VRChat avatar
        public static bool ValidateVRChatAvatar(GameObject avatar, out string error)
        {
            error = "";
            
            if (avatar == null)
            {
                error = "No avatar selected";
                return false;
            }
            
            // Check for animator
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null)
            {
                error = "Avatar must have an Animator component";
                return false;
            }
            
            if (!animator.isHuman)
            {
                error = "Avatar must be configured as Humanoid";
                return false;
            }
            
            // Check for required bones
            HumanBodyBones[] requiredBones = 
            {
                HumanBodyBones.Hips,
                HumanBodyBones.Head,
                HumanBodyBones.LeftUpperArm,
                HumanBodyBones.RightUpperArm,
                HumanBodyBones.LeftUpperLeg,
                HumanBodyBones.RightUpperLeg
            };
            
            foreach (var bone in requiredBones)
            {
                if (animator.GetBoneTransform(bone) == null)
                {
                    error = $"Missing required bone: {bone}";
                    return false;
                }
            }
            
            // Check for VRC Avatar Descriptor (if VRChat SDK is installed)
            var vrcAvatarDescriptor = avatar.GetComponent("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            if (vrcAvatarDescriptor == null)
            {
                Debug.LogWarning("No VRCAvatarDescriptor found - avatar may not be properly configured for VRChat");
            }
            
            return true;
        }
        
        // Create a backup of the avatar
        public static GameObject CreateAvatarBackup(GameObject original)
        {
            GameObject backup = GameObject.Instantiate(original);
            backup.name = original.name + "_backup_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            backup.SetActive(false);
            
            return backup;
        }
        
        // Calculate bounds of all meshes
        public static Bounds CalculateTotalBounds(GameObject avatar)
        {
            Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>();
            
            if (renderers.Length == 0)
                return new Bounds(avatar.transform.position, Vector3.one);
                
            Bounds totalBounds = renderers[0].bounds;
            
            foreach (var renderer in renderers)
            {
                totalBounds.Encapsulate(renderer.bounds);
            }
            
            return totalBounds;
        }
        
        // Get viewpoint position (for VRChat)
        public static Vector3 GetViewPosition(GameObject avatar)
        {
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null || !animator.isHuman)
                return avatar.transform.position + Vector3.up * 1.5f;
                
            Transform leftEye = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            Transform rightEye = animator.GetBoneTransform(HumanBodyBones.RightEye);
            
            if (leftEye != null && rightEye != null)
            {
                return (leftEye.position + rightEye.position) * 0.5f;
            }
            else
            {
                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null)
                {
                    return head.position;
                }
            }
            
            return avatar.transform.position + Vector3.up * 1.5f;
        }
    }
}