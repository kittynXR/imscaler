using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRChatImmersiveScaler
{
    public class ImmersiveScalerCore
    {
        private Animator animator;
        public GameObject avatarRoot { get; private set; }
        private Dictionary<HumanBodyBones, Transform> bones;
        
        public ImmersiveScalerCore(GameObject avatar)
        {
            avatarRoot = avatar;
            animator = avatar.GetComponent<Animator>();
            if (animator != null && animator.isHuman)
            {
                bones = VRCBoneMapper.GetAllBones(animator);
            }
        }
        
        // Get the lowest point of all meshes (floor level)
        public float GetLowestPoint(bool useBoneBasedCalculation = false)
        {
            if (useBoneBasedCalculation)
            {
                return GetLowestPointFromBones();
            }
            
            float lowestPoint = float.MaxValue;
            
            // Get all skinned mesh renderers
            SkinnedMeshRenderer[] meshes = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            foreach (var mesh in meshes)
            {
                if (mesh.sharedMesh == null) continue;
                if (!mesh.enabled || !mesh.gameObject.activeInHierarchy) continue;
                
                // SkinnedMeshRenderer bounds are automatically updated by Unity
                Bounds bounds = mesh.bounds;
                float meshLowest = bounds.min.y;
                
                if (meshLowest < lowestPoint)
                    lowestPoint = meshLowest;
            }
            
            // If no meshes found, use bone-based calculation
            if (lowestPoint == float.MaxValue)
            {
                Debug.LogWarning("ImmersiveScaler: No mesh bounds found, falling back to bone-based calculation");
                return GetLowestPointFromBones();
            }
            
            return lowestPoint;
        }
        
        // Alternative bone-based calculation for more reliability
        private float GetLowestPointFromBones()
        {
            float lowestPoint = float.MaxValue;
            
            // Check foot bones first
            Transform leftFoot = GetBone(HumanBodyBones.LeftFoot);
            Transform rightFoot = GetBone(HumanBodyBones.RightFoot);
            Transform leftToes = GetBone(HumanBodyBones.LeftToes);
            Transform rightToes = GetBone(HumanBodyBones.RightToes);
            
            if (leftFoot != null)
                lowestPoint = Mathf.Min(lowestPoint, leftFoot.position.y);
            if (rightFoot != null)
                lowestPoint = Mathf.Min(lowestPoint, rightFoot.position.y);
            if (leftToes != null)
                lowestPoint = Mathf.Min(lowestPoint, leftToes.position.y);
            if (rightToes != null)
                lowestPoint = Mathf.Min(lowestPoint, rightToes.position.y);
                
            // If still no valid point, check all bones
            if (lowestPoint == float.MaxValue)
            {
                foreach (var bone in bones.Values)
                {
                    if (bone != null)
                        lowestPoint = Mathf.Min(lowestPoint, bone.position.y);
                }
            }
            
            // Last resort - use avatar root
            if (lowestPoint == float.MaxValue)
            {
                lowestPoint = avatarRoot.transform.position.y;
            }
            
            // Debug.Log($"ImmersiveScaler: Lowest point from bones: {lowestPoint}");
            return lowestPoint;
        }
        
        // Get the highest point of all meshes
        public float GetHighestPoint()
        {
            float highestPoint = float.MinValue;
            
            SkinnedMeshRenderer[] meshes = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            foreach (var mesh in meshes)
            {
                if (mesh.sharedMesh == null) continue;
                
                Bounds bounds = mesh.bounds;
                float meshHighest = bounds.max.y;
                
                if (meshHighest > highestPoint)
                    highestPoint = meshHighest;
            }
            
            // If no meshes found, use head position
            if (highestPoint == float.MinValue)
            {
                Transform head = GetBone(HumanBodyBones.Head);
                if (head != null)
                {
                    highestPoint = head.position.y + 0.1f; // Add small offset
                }
                else
                {
                    highestPoint = avatarRoot.transform.position.y + 1.5f;
                }
            }
            
            return highestPoint;
        }
        
        // Get eye height (average of both eyes)
        public float GetEyeHeight()
        {
            Transform leftEye = GetBone(HumanBodyBones.LeftEye);
            Transform rightEye = GetBone(HumanBodyBones.RightEye);
            
            if (leftEye != null && rightEye != null)
            {
                return (leftEye.position.y + rightEye.position.y) / 2f;
            }
            else if (leftEye != null)
            {
                return leftEye.position.y;
            }
            else if (rightEye != null)
            {
                return rightEye.position.y;
            }
            else
            {
                // Fallback to head position
                Transform head = GetBone(HumanBodyBones.Head);
                if (head != null)
                {
                    return head.position.y;
                }
            }
            
            return avatarRoot.transform.position.y + 1.5f; // Default height
        }
        
        // Get eye position in local space relative to avatar root
        public Vector3 GetEyePositionLocal()
        {
            Transform leftEye = GetBone(HumanBodyBones.LeftEye);
            Transform rightEye = GetBone(HumanBodyBones.RightEye);
            Vector3 eyeWorldPos;
            
            if (leftEye != null && rightEye != null)
            {
                eyeWorldPos = (leftEye.position + rightEye.position) / 2f;
            }
            else if (leftEye != null)
            {
                eyeWorldPos = leftEye.position;
            }
            else if (rightEye != null)
            {
                eyeWorldPos = rightEye.position;
            }
            else
            {
                // Fallback to head position
                Transform head = GetBone(HumanBodyBones.Head);
                if (head != null)
                {
                    eyeWorldPos = head.position;
                }
                else
                {
                    // Last resort - estimate based on avatar height
                    eyeWorldPos = avatarRoot.transform.position + Vector3.up * 1.5f;
                }
            }
            
            // Convert world position to local position relative to avatar root
            return avatarRoot.transform.InverseTransformPoint(eyeWorldPos);
        }
        
        // Calculate head to hand distance (VRChat's scaling metric - measures to elbow)
        public float HeadToHand()
        {
            Transform head = GetBone(HumanBodyBones.Head);
            Transform rightUpperArm = GetBone(HumanBodyBones.RightUpperArm);
            Transform rightLowerArm = GetBone(HumanBodyBones.RightLowerArm);
            
            if (head == null || rightUpperArm == null || rightLowerArm == null)
            {
                Debug.LogWarning("Cannot calculate head to hand distance - missing bones");
                return 0.4537f; // Return VRChat default
            }
            
            // VRChat measures from head to elbow (lower arm root) in T-pose
            // Calculate theoretical T-pose elbow position
            float upperArmLength = Vector3.Distance(rightUpperArm.position, rightLowerArm.position);
            
            // Detect which direction the arm extends
            float armDirection = Mathf.Sign(rightLowerArm.position.x - rightUpperArm.position.x);
            if (armDirection == 0) armDirection = -1; // Default to -X if no difference
            
            Vector3 theoreticalElbowPos = rightUpperArm.position;
            theoreticalElbowPos.x += armDirection * upperArmLength;
            
            // Calculate distance from head to theoretical T-pose elbow position
            float distance = Vector3.Distance(head.position, theoreticalElbowPos);
            
            // Debug logging removed
            
            return distance;
        }
        
        // Calculate head to wrist distance (actual hand position in T-pose)
        public float HeadToWrist()
        {
            Transform head = GetBone(HumanBodyBones.Head);
            Transform rightUpperArm = GetBone(HumanBodyBones.RightUpperArm);
            Transform rightLowerArm = GetBone(HumanBodyBones.RightLowerArm);
            Transform rightHand = GetBone(HumanBodyBones.RightHand);
            
            if (head == null || rightUpperArm == null || rightLowerArm == null || rightHand == null)
            {
                Debug.LogWarning("Cannot calculate head to wrist distance - missing bones");
                return GetArmLength(); // Fallback
            }
            
            // Detect which direction the arm extends
            float armDirection = Mathf.Sign(rightLowerArm.position.x - rightUpperArm.position.x);
            if (armDirection == 0) armDirection = -1; // Default to -X if no difference
            
            // Calculate theoretical T-pose hand position
            float armLength = GetArmLength();
            Vector3 theoreticalHandPos = rightUpperArm.position;
            theoreticalHandPos.x += armDirection * armLength;
            
            // Calculate distance from head to theoretical T-pose hand position
            return Vector3.Distance(head.position, theoreticalHandPos);
        }
        
        // Get the VRChat view height based on arm ratio
        public float GetViewZ(float customScaleRatio = 0.4537f, ArmMethodType armMethod = ArmMethodType.HeadToElbowVRC)
        {
            float armValue = GetArmByMethod(armMethod);
            // VRChat formula: viewHeight = (armLength / ratio) + 0.005
            return (armValue / customScaleRatio) + 0.005f;
        }
        
        // Get current scaling ratio
        // NOTE: This method uses hardcoded HeadToHand and EyeHeight measurements.
        // For accurate results that match the scaling algorithm, use GetArmByMethod() and GetHeightByMethod()
        // with your desired measurement types, then calculate: armValue / (heightValue - 0.005f)
        public float GetCurrentScaling()
        {
            float eyeHeight = GetEyeHeight();
            float lowestPoint = GetLowestPoint();
            float currentHeight = eyeHeight - lowestPoint;
            
            float headToHand = HeadToHand();
            float ratio = headToHand / (currentHeight - 0.005f);
            
            // Debug logging removed
            
            return ratio;
        }
        
        // Get upper body portion (from eyes to legs)
        public float GetUpperBodyPortion()
        {
            float eyeHeight = GetEyeHeight();
            Transform leftLeg = GetBone(HumanBodyBones.LeftUpperLeg);
            Transform rightLeg = GetBone(HumanBodyBones.RightUpperLeg);
            
            if (leftLeg == null || rightLeg == null)
                return 0.44f; // Default value
                
            float legHeight = (leftLeg.position.y + rightLeg.position.y) / 2f;
            float lowestPoint = GetLowestPoint();
            
            return 1f - (legHeight - lowestPoint) / (eyeHeight - lowestPoint);
        }
        
        // Get leg proportions (thigh vs calf)
        public float GetThighPercentage()
        {
            Transform leftUpperLeg = GetBone(HumanBodyBones.LeftUpperLeg);
            Transform leftLowerLeg = GetBone(HumanBodyBones.LeftLowerLeg);
            Transform leftFoot = GetBone(HumanBodyBones.LeftFoot);
            
            if (leftUpperLeg == null || leftLowerLeg == null || leftFoot == null)
                return 0.53f; // Default value
                
            float thighLength = leftUpperLeg.position.y - leftLowerLeg.position.y;
            float calfLength = leftLowerLeg.position.y - leftFoot.position.y;
            
            return thighLength / (thighLength + calfLength);
        }
        
        // Get current arm thickness ratio (comparing arm width to arm length)
        public float GetCurrentArmThickness()
        {
            // For now, return 0.5 (50%) as a sensible default
            // Most avatars should use around 50% thickness
            // This could be improved in the future with better heuristics
            return 0.5f;
        }
        
        // Get current leg thickness ratio (comparing leg width to leg length)
        public float GetCurrentLegThickness()
        {
            // For now, return 0.5 (50%) as a sensible default
            // Most avatars should use around 50% thickness
            // This could be improved in the future with better heuristics
            return 0.5f;
        }
        
        // Main scaling function
        public void ScaleAvatar(ScalingParameters parameters)
        {
            if (animator == null || !animator.isHuman)
            {
                Debug.LogError("Avatar must have a humanoid animator!");
                return;
            }
            
            Debug.Log("=== Starting Avatar Scaling ===");
            Debug.Log($"Parameters: TargetHeight={parameters.targetHeight}, UpperBody%={parameters.upperBodyPercentage}, ArmToLegs={parameters.armToLegs}");
            
            // Store original values
            Vector3 originalPosition = avatarRoot.transform.position;
            Vector3 originalScale = avatarRoot.transform.localScale;
            
            // Skip main rescale if requested
            if (parameters.skipAdjust)
            {
                Debug.Log("Skipping main rescale adjustments");
                
                // Move to floor
                if (!parameters.skipFloor)
                {
                    MoveToFloor();
                }
                
                // Scale to target height
                if (!parameters.skipScale)
                {
                    ScaleToHeight(parameters.targetHeight, parameters.targetHeightMethod == HeightMethodType.EyeHeight);
                }
                
                // Center model
                if (parameters.centerModel)
                {
                    CenterModel();
                }
                
                return;
            }
            
            float lowestPoint = GetLowestPoint();
            
            // Use the configured height method
            float currentHeight = GetHeightByMethod(parameters.armToHeightHeightMethod);
            
            Debug.Log($"Current measurements - Lowest: {lowestPoint}, Height (using {parameters.armToHeightHeightMethod}): {currentHeight}");
            
            // Use the custom scale ratio from parameters with the selected arm measurement method
            float baseViewZ = GetViewZ(parameters.customScaleRatio, parameters.armToHeightRatioMethod);
            float viewZ = baseViewZ + parameters.extraLegLength;
            float eyeZ = currentHeight;
            
            float rescaleRatio = eyeZ / viewZ;
            
            // Calculate what the rescale ratio would be without extra leg length
            float baseRescaleRatio = eyeZ / baseViewZ;
            
            Debug.Log($"ViewZ: {viewZ} (base: {baseViewZ}, extra: {parameters.extraLegLength}), EyeZ: {eyeZ}, RescaleRatio: {rescaleRatio}");
            Debug.Log($"Extra Leg Length effect: base rescale ratio = {baseRescaleRatio:F4}, with extra = {rescaleRatio:F4}, difference = {baseRescaleRatio - rescaleRatio:F4}");
            
            // Calculate leg and arm scaling ratios
            float legLength = GetLegLength(parameters.useBoneBasedFloorCalculation);
            float legHeightPortion = legLength / eyeZ;
            Debug.Log($"LegLength: {legLength}, LegHeightPortion: {legHeightPortion}");
            
            float legScaleRatio, armScaleRatio;
            
            if (parameters.scaleRelative)
            {
                Debug.Log("Using relative proportions mode");
                float armToLegs = parameters.armToLegs / 100f;
                float rescaleLegRatio = Mathf.Pow(rescaleRatio, armToLegs);
                float rescaleArmRatio = Mathf.Pow(rescaleRatio, 1f - armToLegs);
                legScaleRatio = 1f - (1f - (1f / rescaleLegRatio)) / legHeightPortion;
                armScaleRatio = CalculateArmRescaling(rescaleArmRatio);
            }
            else if (parameters.keepHeadSize)
            {
                Debug.Log("Using keep head size mode");
                float currentUbp = parameters.upperBodyUseLegacy ? 
                    GetUpperBodyPortion() : 
                    GetUpperBodyRatio(parameters.upperBodyUseNeck, parameters.upperBodyTorsoUseNeck);
                float targetUbp = parameters.upperBodyPercentage / 100f;
                float torsoScaleRatio = targetUbp / currentUbp;
                legScaleRatio = (1f - targetUbp) / (1f - currentUbp);
                armScaleRatio = rescaleRatio;
                
                Debug.Log($"Current UBP: {currentUbp}, Target UBP: {targetUbp}, TorsoScale: {torsoScaleRatio}, LegScale: {legScaleRatio}");
                
                // Scale torso
                ScaleTorso(torsoScaleRatio);
            }
            else
            {
                Debug.Log("Using standard upper body percentage mode");
                float ubp = parameters.upperBodyUseLegacy ? 
                    GetUpperBodyPortion() : 
                    GetUpperBodyRatio(parameters.upperBodyUseNeck, parameters.upperBodyTorsoUseNeck);
                float targetUbp = parameters.upperBodyPercentage / 100f;
                float ubScaleRatio = ubp / targetUbp;
                
                Debug.Log($"Current UBP: {ubp}, Target UBP: {targetUbp}, UB Scale Ratio: {ubScaleRatio}");
                
                legScaleRatio = ubScaleRatio + ((ubScaleRatio * ubp - ubp) / legHeightPortion);
                float rescaleLegRatio = 1f / (legHeightPortion * (legScaleRatio - 1f) + 1f);
                float rescaleArmRatio = rescaleRatio / rescaleLegRatio;
                armScaleRatio = CalculateArmRescaling(rescaleArmRatio);
                
                Debug.Log($"LegScaleRatio: {legScaleRatio}, RescaleLegRatio: {rescaleLegRatio}, RescaleArmRatio: {rescaleArmRatio}");
            }
            
            Debug.Log($"Final scale ratios - Legs: {legScaleRatio}, Arms: {armScaleRatio}");
            
            // Apply leg scaling
            // Simple thickness adjustment: 0% = 0.8x, 50% = 1.0x, 100% = 1.2x
            float legThicknessNorm = parameters.legThickness / 100f;
            float legThickness = 0.8f + (legThicknessNorm * 0.4f);
            Debug.Log($"Leg thickness calculation: {parameters.legThickness}% -> {legThickness}");
            
            ScaleLegs(legScaleRatio, legThickness, parameters.scaleFoot, parameters.thighPercentage / 100f);
            
            // Apply arm scaling
            // Simple thickness adjustment: 0% = 0.8x, 50% = 1.0x, 100% = 1.2x
            float armThicknessNorm = parameters.armThickness / 100f;
            float armThickness = 0.8f + (armThicknessNorm * 0.4f);
            Debug.Log($"Arm thickness calculation: {parameters.armThickness}% -> {armThickness}");
            
            ScaleArms(armScaleRatio, armThickness, parameters.scaleHand);
            
            // Move to floor
            if (!parameters.skipFloor)
            {
                Debug.Log($"Moving avatar to floor (bone-based: {parameters.useBoneBasedFloorCalculation})");
                MoveToFloor(parameters.useBoneBasedFloorCalculation);
            }
            
            // Scale to target height
            if (!parameters.skipScale)
            {
                Debug.Log($"Scaling to target height: {parameters.targetHeight}");
                ScaleToHeight(parameters.targetHeight, parameters.targetHeightMethod == HeightMethodType.EyeHeight, parameters.useBoneBasedFloorCalculation);
            }
            
            // Center model
            if (parameters.centerModel)
            {
                Debug.Log("Centering model");
                CenterModel();
            }
            
            Debug.Log("=== Avatar Scaling Complete ===");
        }
        
        private void ScaleLegs(float legScaleRatio, float thickness, bool scaleFoot, float thighPercentage)
        {
            // Get leg bones
            Transform leftUpperLeg = GetBone(HumanBodyBones.LeftUpperLeg);
            Transform rightUpperLeg = GetBone(HumanBodyBones.RightUpperLeg);
            Transform leftLowerLeg = GetBone(HumanBodyBones.LeftLowerLeg);
            Transform rightLowerLeg = GetBone(HumanBodyBones.RightLowerLeg);
            Transform leftFoot = GetBone(HumanBodyBones.LeftFoot);
            Transform rightFoot = GetBone(HumanBodyBones.RightFoot);
            
            Debug.Log($"ScaleLegs - legScaleRatio: {legScaleRatio}, thickness: {thickness}, thighPercentage: {thighPercentage}");
            Debug.Log($"Thickness parameter: {thickness}, should be different from legScaleRatio: {legScaleRatio}");
            
            // Get current leg proportions
            float[] legProportions = GetLegProportions();
            float thighPortion = legProportions[0];
            float calfPortion = legProportions[1];
            float footPortion = legProportions[2];
            
            Debug.Log($"Current leg proportions - Thigh: {thighPortion}, Calf: {calfPortion}, Foot: {footPortion}");
            
            // Calculate scaling for each part based on thigh percentage
            float targetThighRatio = thighPercentage;
            float currentThighRatio = thighPortion / (thighPortion + calfPortion);
            
            // Adjust scales to achieve target thigh percentage while maintaining overall leg scale
            float thighScale, calfScale;
            
            if (Mathf.Abs(currentThighRatio - targetThighRatio) > 0.01f) // Only adjust if there's a significant difference
            {
                // Calculate how much to adjust each segment to achieve target ratio
                float thighAdjustment = targetThighRatio / currentThighRatio;
                float calfAdjustment = (1f - targetThighRatio) / (1f - currentThighRatio);
                
                // Apply adjustments while maintaining overall leg scale
                thighScale = legScaleRatio * Mathf.Sqrt(thighAdjustment);
                calfScale = legScaleRatio * Mathf.Sqrt(calfAdjustment);
                
                Debug.Log($"Thigh percentage adjustment - Current: {currentThighRatio:F3}, Target: {targetThighRatio:F3}");
                Debug.Log($"Adjustments - Thigh: {thighAdjustment:F3}, Calf: {calfAdjustment:F3}");
            }
            else
            {
                // No adjustment needed, use uniform scaling
                thighScale = legScaleRatio;
                calfScale = legScaleRatio;
            }
            
            // Clamp values to prevent extreme scaling
            thighScale = Mathf.Clamp(thighScale, 0.1f, 10f);
            calfScale = Mathf.Clamp(calfScale, 0.1f, 10f);
            thickness = Mathf.Clamp(thickness, 0.1f, 10f);
            
            Debug.Log($"Final scales - Thigh: {thighScale}, Calf: {calfScale}, Thickness: {thickness}");
            
            // In Unity, we need to determine which axis to scale along
            // Most humanoid rigs have bones extending along the Y axis locally
            // But we should check the bone direction
            
            // Scale upper legs
            if (leftUpperLeg != null)
            {
                Vector3 boneDirection = GetBoneDirection(leftUpperLeg);
                Vector3 newScale = GetDirectionalScale(boneDirection, thighScale, thickness);
                Debug.Log($"Left upper leg - Direction: {boneDirection}, New scale: {newScale}");
                Debug.Log($"   Length axis scale: {(boneDirection.y != 0 ? newScale.y : (boneDirection.x != 0 ? newScale.x : newScale.z))}, Thickness axes: X={newScale.x}, Z={newScale.z}");
                leftUpperLeg.localScale = newScale;
            }
            if (rightUpperLeg != null)
            {
                Vector3 boneDirection = GetBoneDirection(rightUpperLeg);
                Vector3 newScale = GetDirectionalScale(boneDirection, thighScale, thickness);
                rightUpperLeg.localScale = newScale;
            }
                
            // Scale lower legs
            if (leftLowerLeg != null)
            {
                Vector3 boneDirection = GetBoneDirection(leftLowerLeg);
                Vector3 newScale = GetDirectionalScale(boneDirection, calfScale, thickness);
                Debug.Log($"Left lower leg - Direction: {boneDirection}, New scale: {newScale}");
                leftLowerLeg.localScale = newScale;
            }
            if (rightLowerLeg != null)
            {
                Vector3 boneDirection = GetBoneDirection(rightLowerLeg);
                Vector3 newScale = GetDirectionalScale(boneDirection, calfScale, thickness);
                rightLowerLeg.localScale = newScale;
            }
                
            // Scale feet if requested
            if (scaleFoot)
            {
                float footScale = Mathf.Clamp(legScaleRatio, 0.1f, 10f);
                if (leftFoot != null)
                    leftFoot.localScale = Vector3.one * footScale;
                if (rightFoot != null)
                    rightFoot.localScale = Vector3.one * footScale;
            }
        }
        
        private Vector3 GetBoneDirection(Transform bone)
        {
            // Determine which direction the bone extends in local space
            // This is typically towards the first child
            if (bone.childCount > 0)
            {
                Transform child = bone.GetChild(0);
                Vector3 localChildPos = bone.InverseTransformPoint(child.position);
                
                // Find the dominant axis
                float absX = Mathf.Abs(localChildPos.x);
                float absY = Mathf.Abs(localChildPos.y);
                float absZ = Mathf.Abs(localChildPos.z);
                
                if (absX > absY && absX > absZ)
                    return new Vector3(Mathf.Sign(localChildPos.x), 0, 0);
                else if (absY > absX && absY > absZ)
                    return new Vector3(0, Mathf.Sign(localChildPos.y), 0);
                else
                    return new Vector3(0, 0, Mathf.Sign(localChildPos.z));
            }
            
            // Default to Y axis if no child
            return Vector3.up;
        }
        
        private Vector3 GetDirectionalScale(Vector3 direction, float lengthScale, float thickness)
        {
            // Apply length scale along the bone direction, thickness on other axes
            Vector3 scale = Vector3.one;
            
            if (direction.x != 0)
            {
                scale.x = lengthScale;
                scale.y = thickness;
                scale.z = thickness;
            }
            else if (direction.y != 0)
            {
                scale.x = thickness;
                scale.y = lengthScale;
                scale.z = thickness;
            }
            else if (direction.z != 0)
            {
                scale.x = thickness;
                scale.y = thickness;
                scale.z = lengthScale;
            }
            
            return scale;
        }
        
        private float[] GetLegProportions()
        {
            Transform leftUpperLeg = GetBone(HumanBodyBones.LeftUpperLeg);
            Transform leftLowerLeg = GetBone(HumanBodyBones.LeftLowerLeg);
            Transform leftFoot = GetBone(HumanBodyBones.LeftFoot);
            
            if (leftUpperLeg == null || leftLowerLeg == null || leftFoot == null)
                return new float[] { 0.5f, 0.4f, 0.1f }; // Default proportions
                
            float lowestPoint = GetLowestPoint();
            float legTop = leftUpperLeg.position.y;
            float knee = leftLowerLeg.position.y;
            float ankle = leftFoot.position.y;
            
            float totalLength = legTop - lowestPoint;
            if (totalLength <= 0)
                return new float[] { 0.5f, 0.4f, 0.1f };
                
            float thighPortion = (legTop - knee) / totalLength;
            float calfPortion = (knee - ankle) / totalLength;
            float footPortion = (ankle - lowestPoint) / totalLength;
            
            return new float[] { thighPortion, calfPortion, footPortion };
        }
        
        private void ScaleArms(float armScaleRatio, float thickness, bool scaleHand)
        {
            Transform leftUpperArm = GetBone(HumanBodyBones.LeftUpperArm);
            Transform rightUpperArm = GetBone(HumanBodyBones.RightUpperArm);
            Transform leftLowerArm = GetBone(HumanBodyBones.LeftLowerArm);
            Transform rightLowerArm = GetBone(HumanBodyBones.RightLowerArm);
            Transform leftHand = GetBone(HumanBodyBones.LeftHand);
            Transform rightHand = GetBone(HumanBodyBones.RightHand);
            
            Debug.Log($"ScaleArms - armScaleRatio: {armScaleRatio}, thickness: {thickness}");
            
            // Clamp values
            armScaleRatio = Mathf.Clamp(armScaleRatio, 0.1f, 10f);
            thickness = Mathf.Clamp(thickness, 0.1f, 10f);
            
            // Scale upper arms
            if (leftUpperArm != null)
            {
                Vector3 boneDirection = GetBoneDirection(leftUpperArm);
                Vector3 newScale = GetDirectionalScale(boneDirection, armScaleRatio, thickness);
                Debug.Log($"Left upper arm - Direction: {boneDirection}, New scale: {newScale}");
                leftUpperArm.localScale = newScale;
            }
            if (rightUpperArm != null)
            {
                Vector3 boneDirection = GetBoneDirection(rightUpperArm);
                Vector3 newScale = GetDirectionalScale(boneDirection, armScaleRatio, thickness);
                rightUpperArm.localScale = newScale;
            }
            
            // Scale lower arms
            if (leftLowerArm != null)
            {
                Vector3 boneDirection = GetBoneDirection(leftLowerArm);
                Vector3 newScale = GetDirectionalScale(boneDirection, armScaleRatio, thickness);
                Debug.Log($"Left lower arm - Direction: {boneDirection}, New scale: {newScale}");
                leftLowerArm.localScale = newScale;
            }
            if (rightLowerArm != null)
            {
                Vector3 boneDirection = GetBoneDirection(rightLowerArm);
                Vector3 newScale = GetDirectionalScale(boneDirection, armScaleRatio, thickness);
                rightLowerArm.localScale = newScale;
            }
                
            if (!scaleHand)
            {
                // Counter-scale hands to keep them original size
                if (leftHand != null)
                {
                    Vector3 boneDirection = GetBoneDirection(leftHand.parent); // Use parent's direction
                    Vector3 counterScale = GetDirectionalScale(boneDirection, 1f / armScaleRatio, 1f / thickness);
                    leftHand.localScale = counterScale;
                }
                if (rightHand != null)
                {
                    Vector3 boneDirection = GetBoneDirection(rightHand.parent); // Use parent's direction
                    Vector3 counterScale = GetDirectionalScale(boneDirection, 1f / armScaleRatio, 1f / thickness);
                    rightHand.localScale = counterScale;
                }
            }
        }
        
        private void ScaleTorso(float torsoScaleRatio)
        {
            Transform hips = GetBone(HumanBodyBones.Hips);
            if (hips != null)
            {
                hips.localScale = new Vector3(1f, torsoScaleRatio, 1f);
            }
        }
        
        private float CalculateArmRescaling(float headArmChange)
        {
            Transform rightUpperArm = GetBone(HumanBodyBones.RightUpperArm);
            Transform head = GetBone(HumanBodyBones.Head);
            
            if (rightUpperArm == null || head == null)
                return 1f;
                
            float totalLength = HeadToHand();
            float armLength = GetArmLength();
            float neckLength = Mathf.Abs(head.position.y - rightUpperArm.position.y);
            
            float shoulderLength = Mathf.Sqrt((totalLength - neckLength) * (totalLength + neckLength)) - armLength;
            
            float armChange = (Mathf.Sqrt(
                (headArmChange * totalLength - neckLength) *
                (headArmChange * totalLength + neckLength)
            ) / armLength) - (shoulderLength / armLength);
            
            return armChange;
        }
        
        public float GetArmLength()
        {
            Transform upperArm = GetBone(HumanBodyBones.RightUpperArm);
            Transform lowerArm = GetBone(HumanBodyBones.RightLowerArm);
            Transform hand = GetBone(HumanBodyBones.RightHand);
            
            if (upperArm == null || lowerArm == null || hand == null)
                return 0.5f; // Default
                
            float upperLength = Vector3.Distance(upperArm.position, lowerArm.position);
            float lowerLength = Vector3.Distance(lowerArm.position, hand.position);
            
            return upperLength + lowerLength;
        }
        
        // Get fingertip to fingertip distance (full wingspan)
        public float GetFingertipToFingertip()
        {
            Transform leftShoulder = GetBone(HumanBodyBones.LeftUpperArm);
            Transform rightShoulder = GetBone(HumanBodyBones.RightUpperArm);
            
            if (leftShoulder == null || rightShoulder == null)
                return 1.5f; // Default
            
            // Try to find middle finger tips
            Transform leftMiddleTip = null;
            Transform rightMiddleTip = null;
            
            // Search for finger bones
            if (animator != null)
            {
                Transform leftHand = GetBone(HumanBodyBones.LeftHand);
                Transform rightHand = GetBone(HumanBodyBones.RightHand);
                
                if (leftHand != null)
                {
                    // Look for middle finger distal bone
                    leftMiddleTip = VRCBoneMapper.FindFingerBone(leftHand, "LeftMiddleDistal");
                }
                
                if (rightHand != null)
                {
                    rightMiddleTip = VRCBoneMapper.FindFingerBone(rightHand, "RightMiddleDistal");
                }
            }
            
            // If we found finger tips, use them
            if (leftMiddleTip != null && rightMiddleTip != null)
            {
                return Vector3.Distance(leftMiddleTip.position, rightMiddleTip.position);
            }
            
            // Otherwise fall back to hand positions
            Transform leftHandFallback = GetBone(HumanBodyBones.LeftHand);
            Transform rightHandFallback = GetBone(HumanBodyBones.RightHand);
            
            if (leftHandFallback != null && rightHandFallback != null)
            {
                return Vector3.Distance(leftHandFallback.position, rightHandFallback.position);
            }
            
            // Last resort: shoulder to shoulder plus arm lengths
            float shoulderDist = Vector3.Distance(leftShoulder.position, rightShoulder.position);
            return shoulderDist + (GetArmLength() * 2);
        }
        
        // Get shoulder to hand distance
        public float GetShoulderToHand()
        {
            return GetArmLength();
        }
        
        // Get shoulder to fingertip distance
        public float GetShoulderToFingertip()
        {
            Transform shoulder = GetBone(HumanBodyBones.RightUpperArm);
            Transform hand = GetBone(HumanBodyBones.RightHand);
            
            if (shoulder == null || hand == null)
                return GetArmLength(); // Fallback
            
            // Try to find middle finger tip
            Transform middleTip = null;
            if (animator != null && hand != null)
            {
                middleTip = VRCBoneMapper.FindFingerBone(hand, "RightMiddleDistal");
            }
            
            if (middleTip != null)
            {
                return Vector3.Distance(shoulder.position, middleTip.position);
            }
            
            // Fallback to hand position + estimated finger length
            return GetArmLength() + 0.1f; // Add ~10cm for hand/fingers
        }
        
        // Simple arm length divided by total height
        public float GetSimpleArmRatio()
        {
            float armLength = GetArmLength();
            float totalHeight = GetHighestPoint() - GetLowestPoint();
            return armLength / totalHeight;
        }
        
        // Arm length divided by eye height
        public float GetArmToEyeRatio()
        {
            float armLength = GetArmLength();
            float eyeHeight = GetEyeHeight() - GetLowestPoint();
            return armLength / eyeHeight;
        }
        
        // Get distance from body center (chest/spine) to hand
        public float GetCenterToHand()
        {
            Transform chest = GetBone(HumanBodyBones.Chest);
            Transform rightHand = GetBone(HumanBodyBones.RightHand);
            
            if (chest == null || rightHand == null)
            {
                // Fallback to spine if chest not found
                chest = GetBone(HumanBodyBones.Spine);
                if (chest == null) return 0.5f;
            }
            
            // Get the horizontal distance from chest center to hand
            Vector3 chestPos = chest.position;
            Vector3 handPos = rightHand.position;
            
            // Calculate horizontal distance (ignore Y)
            Vector3 chestToHand = handPos - chestPos;
            chestToHand.y = 0;
            
            return chestToHand.magnitude;
        }
        
        // Get distance from body center to fingertip
        public float GetCenterToFingertip()
        {
            Transform chest = GetBone(HumanBodyBones.Chest);
            Transform rightHand = GetBone(HumanBodyBones.RightHand);
            
            if (chest == null || rightHand == null)
            {
                chest = GetBone(HumanBodyBones.Spine);
                if (chest == null) return GetCenterToHand() + 0.1f;
            }
            
            // Try to find middle finger tip
            Transform middleTip = null;
            if (rightHand != null)
            {
                middleTip = VRCBoneMapper.FindFingerBone(rightHand, "RightMiddleDistal");
            }
            
            Vector3 endPoint = middleTip != null ? middleTip.position : rightHand.position;
            Vector3 chestPos = chest.position;
            
            // Calculate horizontal distance
            Vector3 chestToEnd = endPoint - chestPos;
            chestToEnd.y = 0;
            
            return chestToEnd.magnitude;
        }
        
        // Get upper body length (upper leg height to neck)
        public float GetUpperBodyLength()
        {
            Transform leftLeg = GetBone(HumanBodyBones.LeftUpperLeg);
            Transform rightLeg = GetBone(HumanBodyBones.RightUpperLeg);
            Transform neck = GetBone(HumanBodyBones.Neck);
            
            if (leftLeg == null || rightLeg == null || neck == null)
                return 0.6f; // Default
                
            float legY = (leftLeg.position.y + rightLeg.position.y) / 2f;
            return Mathf.Abs(neck.position.y - legY);
        }
        
        // Get head height (floor to neck)
        public float GetHeadHeight()
        {
            Transform neck = GetBone(HumanBodyBones.Neck);
            if (neck == null)
                return GetEyeHeight() - 0.1f; // Fallback
                
            return neck.position.y - GetLowestPoint();
        }
        
        // Get floor to head height (floor to head bone base)
        public float GetFloorToHeadHeight()
        {
            Transform head = GetBone(HumanBodyBones.Head);
            if (head == null)
                return GetHighestPoint() - GetLowestPoint(); // Fallback to total height
                
            return head.position.y - GetLowestPoint();
        }
        
        // Get alternate upper body ratio
        public float GetAlternateUpperBodyRatio()
        {
            float upperBodyLength = GetUpperBodyLength();
            float headHeight = GetHeadHeight();
            
            if (headHeight <= 0) return 0.5f;
            
            return upperBodyLength / headHeight;
        }
        
        // Get center-to-hand divided by total height
        public float GetCenterHandToHeightRatio()
        {
            float centerToHand = GetCenterToHand();
            float totalHeight = GetHighestPoint() - GetLowestPoint();
            if (totalHeight <= 0) return 0.3f;
            return centerToHand / totalHeight;
        }
        
        // Get center-to-hand divided by eye height
        public float GetCenterHandToEyeRatio()
        {
            float centerToHand = GetCenterToHand();
            float eyeHeight = GetEyeHeight() - GetLowestPoint();
            if (eyeHeight <= 0) return 0.3f;
            return centerToHand / eyeHeight;
        }
        
        // Get center-to-fingertip divided by total height
        public float GetCenterFingertipToHeightRatio()
        {
            float centerToFingertip = GetCenterToFingertip();
            float totalHeight = GetHighestPoint() - GetLowestPoint();
            if (totalHeight <= 0) return 0.35f;
            return centerToFingertip / totalHeight;
        }
        
        // Get center-to-fingertip divided by eye height
        public float GetCenterFingertipToEyeRatio()
        {
            float centerToFingertip = GetCenterToFingertip();
            float eyeHeight = GetEyeHeight() - GetLowestPoint();
            if (eyeHeight <= 0) return 0.35f;
            return centerToFingertip / eyeHeight;
        }
        
        // Get head-to-wrist divided by eye height
        public float GetHeadWristToEyeRatio()
        {
            float headToWrist = HeadToWrist();
            float eyeHeight = GetEyeHeight() - GetLowestPoint();
            if (eyeHeight <= 0) return 0.5f;
            return headToWrist / eyeHeight;
        }
        
        // Get head-to-wrist divided by total height
        public float GetHeadWristToHeightRatio()
        {
            float headToWrist = HeadToWrist();
            float totalHeight = GetHighestPoint() - GetLowestPoint();
            if (totalHeight <= 0) return 0.45f;
            return headToWrist / totalHeight;
        }
        
        // Get height measurement based on selected method
        public float GetHeightByMethod(HeightMethodType method)
        {
            float lowest = GetLowestPoint();
            switch (method)
            {
                case HeightMethodType.TotalHeight:
                    return GetHighestPoint() - lowest;
                case HeightMethodType.EyeHeight:
                default:
                    return GetEyeHeight() - lowest;
            }
        }
        
        // Get arm measurement based on selected method
        public float GetArmByMethod(ArmMethodType method)
        {
            switch (method)
            {
                case ArmMethodType.HeadToElbowVRC:
                    return HeadToHand(); // This is actually head to elbow
                case ArmMethodType.HeadToHand:
                    return HeadToWrist();
                case ArmMethodType.ArmLength:
                    return GetArmLength();
                case ArmMethodType.ShoulderToFingertip:
                    return GetShoulderToFingertip();
                case ArmMethodType.CenterToHand:
                    return GetCenterToHand();
                case ArmMethodType.CenterToFingertip:
                    return GetCenterToFingertip();
                default:
                    return GetArmLength();
            }
        }
        
        // Get upper body ratio based on selected methods
        public float GetUpperBodyRatio(bool useNeckForHeight, bool useNeckForTorso)
        {
            // Height: floor to neck or floor to head
            float height = useNeckForHeight ? GetHeadHeight() : GetFloorToHeadHeight();
            
            // Torso: upper leg to neck or upper leg to head
            Transform leftLeg = GetBone(HumanBodyBones.LeftUpperLeg);
            Transform rightLeg = GetBone(HumanBodyBones.RightUpperLeg);
            Transform neck = GetBone(HumanBodyBones.Neck);
            Transform head = GetBone(HumanBodyBones.Head);
            
            if (leftLeg == null || rightLeg == null || neck == null || head == null)
                return 0.44f; // Default
                
            float legY = (leftLeg.position.y + rightLeg.position.y) / 2f;
            float targetY = useNeckForTorso ? neck.position.y : head.position.y;
            float torsoLength = targetY - legY;
            
            return torsoLength / height;
        }
        
        private float GetLegLength(bool useBoneBasedCalculation = false)
        {
            Transform upperLeg = GetBone(HumanBodyBones.LeftUpperLeg);
            if (upperLeg == null)
                return 0.8f; // Default
                
            return upperLeg.position.y - GetLowestPoint(useBoneBasedCalculation);
        }
        
        private void MoveToFloor(bool useBoneBasedCalculation = false)
        {
            float lowestPoint = GetLowestPoint(useBoneBasedCalculation);
            
            // Move avatar so that its lowest point is at Y=0
            // If lowestPoint is negative (below root), we need to move up
            // If lowestPoint is positive (above root), we need to move down
            Vector3 currentPos = avatarRoot.transform.position;
            avatarRoot.transform.position = new Vector3(
                currentPos.x,
                -lowestPoint,  // This ensures the lowest point ends up at Y=0
                currentPos.z
            );
            
            Debug.Log($"ImmersiveScaler: Moved to floor. Lowest point was {lowestPoint}, new Y position: {avatarRoot.transform.position.y}");
        }
        
        private void ScaleToHeight(float targetHeight, bool scaleToEyes, bool useBoneBasedCalculation = false)
        {
            float lowestPoint = GetLowestPoint(useBoneBasedCalculation);
            float currentHeight;
            
            if (scaleToEyes)
            {
                currentHeight = GetEyeHeight() - lowestPoint;
            }
            else
            {
                currentHeight = GetHighestPoint() - lowestPoint;
            }
            
            float scaleRatio = targetHeight / currentHeight;
            avatarRoot.transform.localScale *= scaleRatio;
            
            // Note: We don't adjust position after scaling because:
            // 1. MoveToFloor has already positioned the avatar correctly
            // 2. Uniform scaling shouldn't change the floor position if the pivot is set correctly
            // 3. Recalculating lowest point after scaling can introduce small errors due to mesh bounds recalculation
        }
        
        private void CenterModel()
        {
            Vector3 pos = avatarRoot.transform.position;
            avatarRoot.transform.position = new Vector3(0, pos.y, 0);
        }
        
        public Transform GetBone(HumanBodyBones bone)
        {
            if (bones != null && bones.ContainsKey(bone))
                return bones[bone];
            return null;
        }
    }
    
    [System.Serializable]
    public class ScalingParameters
    {
        public float targetHeight = 1.61f;
        public float armToLegs = 55f;
        public float upperBodyPercentage = 44f;
        public float armThickness = 50f;
        public float legThickness = 50f;
        public float extraLegLength = 0f;
        public float thighPercentage = 53f;
        public float customScaleRatio = 0.4537f;
        
        public bool scaleHand = false;
        public bool scaleFoot = false;
        public bool centerModel = false;
        public bool scaleEyes = true;
        public bool scaleRelative = false;
        public bool keepHeadSize = false;
        
        // Measurement method selections
        public HeightMethodType targetHeightMethod = HeightMethodType.EyeHeight;
        public ArmMethodType armToHeightRatioMethod = ArmMethodType.HeadToHand;
        public HeightMethodType armToHeightHeightMethod = HeightMethodType.EyeHeight;
        public bool upperBodyUseNeck = true;
        public bool upperBodyTorsoUseNeck = true;
        public bool upperBodyUseLegacy = false;
        
        public bool skipScale = false;
        public bool skipFloor = true;
        public bool skipAdjust = false;
        
        public bool useBoneBasedFloorCalculation = false;
        
        // Finger spreading parameters
        public bool spareThumb = true;
        public float fingerSpreadFactor = 1.0f;
    }
}