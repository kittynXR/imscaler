using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRChatImmersiveScaler
{
    public class ImmersiveScalerCore
    {
        private const float MaxAllowedMeshBelowFootBonesMeters = 0.5f;
        private const float MaxAllowedMeshAboveHeadBoneMeters = 2.0f;
        private const float MeasurementEpsilon = 0.0001f;

        private Animator animator;
        public GameObject avatarRoot { get; private set; }
        private Dictionary<HumanBodyBones, Transform> bones;

        private bool hasLoggedNoMeshesWarning;
        private bool hasLoggedFloorOutlierWarning;
        private bool hasLoggedHeightOutlierWarning;

        private bool useMeasurementRendererOverrides;
        private bool hasLoggedInvalidMeasurementOverridesWarning;
        private List<SkinnedMeshRenderer> measurementBodyRendererOverrides;
        private List<SkinnedMeshRenderer> measurementHeadRendererOverrides;

        public ImmersiveScalerCore(GameObject avatar)
        {
            avatarRoot = avatar;
            animator = avatar.GetComponent<Animator>();
            if (animator != null && animator.isHuman)
            {
                bones = VRCBoneMapper.GetAllBones(animator);
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsUsablePositive(float value)
        {
            return IsFinite(value) && value > MeasurementEpsilon;
        }

        private static float SafeClampScale(float value, float fallback = 1f)
        {
            return IsFinite(value) ? Mathf.Clamp(value, 0.1f, 10f) : fallback;
        }

        // A per-operation sink; the debug panel does not enable global console spam.
        public System.Action<string> DiagnosticLog { get; set; }

        private void Warn(string message)
        {
            DiagnosticLog?.Invoke("WARNING: " + message);
            Debug.LogWarning(message);
        }

        private void LogVerbose(string message)
        {
            DiagnosticLog?.Invoke(message);
#if KITTYN_IMMERSIVE_SCALER_DEBUG
            Debug.Log(message);
#endif
        }

        public void SetMeasurementRendererOverrides(
            bool enabled,
            IEnumerable<SkinnedMeshRenderer> bodyRenderers,
            IEnumerable<SkinnedMeshRenderer> headRenderers
        )
        {
            if (useMeasurementRendererOverrides != enabled)
            {
                hasLoggedInvalidMeasurementOverridesWarning = false;
            }
            useMeasurementRendererOverrides = enabled;

            if (!enabled)
            {
                measurementBodyRendererOverrides = null;
                measurementHeadRendererOverrides = null;
                return;
            }

            measurementBodyRendererOverrides = FilterAndDeduplicateSkinnedMeshRenderers(bodyRenderers);
            measurementHeadRendererOverrides = FilterAndDeduplicateSkinnedMeshRenderers(headRenderers);
        }

        private List<SkinnedMeshRenderer> FilterAndDeduplicateSkinnedMeshRenderers(IEnumerable<SkinnedMeshRenderer> renderers)
        {
            if (renderers == null) return null;

            var result = new List<SkinnedMeshRenderer>();
            var seen = new HashSet<SkinnedMeshRenderer>();
            Transform root = avatarRoot != null ? avatarRoot.transform : null;

            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.sharedMesh == null) continue;
                if (!seen.Add(renderer)) continue;
                if (root != null && !renderer.transform.IsChildOf(root)) continue;
                result.Add(renderer);
            }

            return result.Count > 0 ? result : null;
        }

        private List<SkinnedMeshRenderer> GetEnabledSkinnedMeshes()
        {
            if (avatarRoot == null) return new List<SkinnedMeshRenderer>();

            var renderers = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
            var result = new List<SkinnedMeshRenderer>(renderers.Length);

            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.sharedMesh == null) continue;
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                result.Add(renderer);
            }

            return result;
        }

        private static List<SkinnedMeshRenderer> GetEnabledSkinnedMeshesFrom(IEnumerable<SkinnedMeshRenderer> renderers)
        {
            if (renderers == null) return new List<SkinnedMeshRenderer>();

            var result = new List<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.sharedMesh == null) continue;
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                result.Add(renderer);
            }

            return result;
        }

        private static bool RendererHasBone(SkinnedMeshRenderer renderer, Transform boneTransform)
        {
            if (renderer == null || boneTransform == null) return false;

            var rendererBones = renderer.bones;
            if (rendererBones == null) return false;

            for (int i = 0; i < rendererBones.Length; i++)
            {
                if (rendererBones[i] == boneTransform) return true;
            }

            return false;
        }

        private bool RendererUsesAnyHumanoidBones(SkinnedMeshRenderer renderer, params HumanBodyBones[] boneTypes)
        {
            if (renderer == null || boneTypes == null || boneTypes.Length == 0) return false;

            foreach (var boneType in boneTypes)
            {
                var boneTransform = GetBone(boneType);
                if (RendererHasBone(renderer, boneTransform)) return true;
            }

            return false;
        }

        private List<SkinnedMeshRenderer> FilterMeshesForFloor(List<SkinnedMeshRenderer> meshes)
        {
            var filtered = meshes
                .Where(m => RendererUsesAnyHumanoidBones(m, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightToes))
                .ToList();
            if (filtered.Count > 0) return filtered;

            filtered = meshes
                .Where(m => RendererUsesAnyHumanoidBones(m,
                    HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
                    HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
                    HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
                    HumanBodyBones.LeftToes, HumanBodyBones.RightToes))
                .ToList();
            if (filtered.Count > 0) return filtered;

            filtered = meshes.Where(m => RendererUsesAnyHumanoidBones(m, HumanBodyBones.Hips)).ToList();
            return filtered.Count > 0 ? filtered : meshes;
        }

        private List<SkinnedMeshRenderer> FilterMeshesForHeight(List<SkinnedMeshRenderer> meshes)
        {
            // Prefer meshes that look like the "main body" (hips + legs), which typically excludes props like hats/weapons.
            var filtered = meshes
                .Where(m => RendererUsesAnyHumanoidBones(m, HumanBodyBones.Hips) &&
                            RendererUsesAnyHumanoidBones(m,
                                HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
                                HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
                                HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
                                HumanBodyBones.LeftToes, HumanBodyBones.RightToes))
                .ToList();
            if (filtered.Count > 0) return filtered;

            filtered = meshes.Where(m => RendererUsesAnyHumanoidBones(m, HumanBodyBones.Hips)).ToList();
            return filtered.Count > 0 ? filtered : meshes;
        }

        // Get the lowest point of all meshes (floor level)
        public float GetLowestPoint(bool useBoneBasedCalculation = false)
        {
            DiagnosticLog?.Invoke($"Floor selection: boneBased={useBoneBasedCalculation}, rendererOverrides={useMeasurementRendererOverrides}, below-foot outlier limit={MaxAllowedMeshBelowFootBonesMeters}m");
            if (useBoneBasedCalculation)
            {
                return GetLowestPointFromBones();
            }

            List<SkinnedMeshRenderer> meshes;
            List<SkinnedMeshRenderer> candidates;
            if (useMeasurementRendererOverrides)
            {
                meshes = GetEnabledSkinnedMeshesFrom(measurementBodyRendererOverrides);
                if (meshes.Count == 0)
                {
                    DiagnosticLog?.Invoke("Floor: requested Body overrides contain no enabled valid meshes; using automatic selection.");
                    if (!hasLoggedInvalidMeasurementOverridesWarning)
                    {
                        hasLoggedInvalidMeasurementOverridesWarning = true;
                        Warn("ImmersiveScaler: Measurement renderer overrides are enabled but no valid Body renderers are set; falling back to automatic mesh selection");
                    }
                    meshes = GetEnabledSkinnedMeshes();
                    candidates = FilterMeshesForFloor(meshes);
                }
                else
                {
                    candidates = meshes;
                }
            }
            else
            {
                meshes = GetEnabledSkinnedMeshes();
                candidates = FilterMeshesForFloor(meshes);
            }

            TraceCandidates("Floor", meshes, candidates);
            if (meshes.Count == 0)
            {
                DiagnosticLog?.Invoke("Floor: no valid selected renderer; using bone fallback.");
                if (!hasLoggedNoMeshesWarning)
                {
                    hasLoggedNoMeshesWarning = true;
                    Warn("ImmersiveScaler: No mesh bounds found, falling back to bone-based floor calculation");
                }
                return GetLowestPointFromBones();
            }

            // Use foot/toe bones as a sanity baseline (root/all-bone fallbacks can be far above the actual floor).
            float footBoneFloor = float.MaxValue;
            Transform leftFoot = GetBone(HumanBodyBones.LeftFoot);
            Transform rightFoot = GetBone(HumanBodyBones.RightFoot);
            Transform leftToes = GetBone(HumanBodyBones.LeftToes);
            Transform rightToes = GetBone(HumanBodyBones.RightToes);

            if (leftFoot != null) footBoneFloor = Mathf.Min(footBoneFloor, leftFoot.position.y);
            if (rightFoot != null) footBoneFloor = Mathf.Min(footBoneFloor, rightFoot.position.y);
            if (leftToes != null) footBoneFloor = Mathf.Min(footBoneFloor, leftToes.position.y);
            if (rightToes != null) footBoneFloor = Mathf.Min(footBoneFloor, rightToes.position.y);

            float boneFloor = GetLowestPointFromBones();
            float lowestPoint = float.MaxValue;
            int ignoredOutliers = 0;

            foreach (var mesh in candidates)
            {
                float meshLowest = mesh.bounds.min.y;

                // Sanity-check: if the mesh min is far below the humanoid foot bones, it's likely a prop or bad bounds.
                if (footBoneFloor != float.MaxValue && meshLowest < footBoneFloor - MaxAllowedMeshBelowFootBonesMeters)
                {
                    DiagnosticLog?.Invoke($"Floor rejected {RendererPath(mesh)}: minY={meshLowest}, footY={footBoneFloor}, allowedBelow={MaxAllowedMeshBelowFootBonesMeters}");
                    ignoredOutliers++;
                    continue;
                }

                DiagnosticLog?.Invoke($"Floor accepted {RendererPath(mesh)}: minY={meshLowest}");
                if (meshLowest < lowestPoint)
                    lowestPoint = meshLowest;
            }

            if (lowestPoint == float.MaxValue)
            {
                if (ignoredOutliers > 0 && !hasLoggedFloorOutlierWarning)
                {
                    hasLoggedFloorOutlierWarning = true;
                    Warn($"ImmersiveScaler: Ignored {ignoredOutliers} outlier mesh bounds while finding floor; using bone-based floor instead");
                }
                DiagnosticLog?.Invoke($"Floor: no usable mesh candidates; bone fallback Y={boneFloor}");
                return boneFloor;
            }

            if (ignoredOutliers > 0 && !hasLoggedFloorOutlierWarning)
            {
                hasLoggedFloorOutlierWarning = true;
                Warn($"ImmersiveScaler: Ignored {ignoredOutliers} outlier mesh bounds while finding floor (likely props or bad bounds)");
            }

            DiagnosticLog?.Invoke($"Floor result: Y={lowestPoint}");
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
                if (bones != null)
                {
                    foreach (var bone in bones.Values)
                    {
                        if (bone != null)
                            lowestPoint = Mathf.Min(lowestPoint, bone.position.y);
                    }
                }
            }

            // Last resort - use avatar root
            if (lowestPoint == float.MaxValue)
            {
                lowestPoint = avatarRoot.transform.position.y;
            }

            DiagnosticLog?.Invoke($"Bone floor result: Y={lowestPoint} (feet/toes, then all mapped bones, then root)");
            return lowestPoint;
        }

        // Get the highest point of all meshes
        public float GetHighestPoint()
        {
            DiagnosticLog?.Invoke($"Height selection: rendererOverrides={useMeasurementRendererOverrides}, above-head outlier limit={MaxAllowedMeshAboveHeadBoneMeters}m");
            List<SkinnedMeshRenderer> meshes;
            List<SkinnedMeshRenderer> candidates;
            if (useMeasurementRendererOverrides)
            {
                var overrideCandidates = new List<SkinnedMeshRenderer>();
                var seen = new HashSet<SkinnedMeshRenderer>();
                foreach (var renderer in GetEnabledSkinnedMeshesFrom(measurementBodyRendererOverrides))
                {
                    if (seen.Add(renderer)) overrideCandidates.Add(renderer);
                }
                foreach (var renderer in GetEnabledSkinnedMeshesFrom(measurementHeadRendererOverrides))
                {
                    if (seen.Add(renderer)) overrideCandidates.Add(renderer);
                }

                meshes = overrideCandidates;
                if (meshes.Count == 0)
                {
                    DiagnosticLog?.Invoke("Height: requested Body/Head overrides contain no enabled valid meshes; using automatic selection.");
                    if (!hasLoggedInvalidMeasurementOverridesWarning)
                    {
                        hasLoggedInvalidMeasurementOverridesWarning = true;
                        Warn("ImmersiveScaler: Measurement renderer overrides are enabled but no valid Body/Head renderers are set; falling back to automatic mesh selection");
                    }
                    meshes = GetEnabledSkinnedMeshes();
                    candidates = FilterMeshesForHeight(meshes);
                }
                else
                {
                    candidates = meshes;
                }
            }
            else
            {
                meshes = GetEnabledSkinnedMeshes();
                candidates = FilterMeshesForHeight(meshes);
            }

            TraceCandidates("Height", meshes, candidates);
            Transform head = GetBone(HumanBodyBones.Head);
            float headY = head != null ? head.position.y : float.NaN;

            float highestPoint = float.MinValue;
            int ignoredOutliers = 0;

            foreach (var mesh in candidates)
            {
                float meshHighest = mesh.bounds.max.y;

                // Sanity-check: if mesh max is wildly above the head bone, it's likely a prop or bad bounds.
                if (!float.IsNaN(headY) && meshHighest > headY + MaxAllowedMeshAboveHeadBoneMeters)
                {
                    DiagnosticLog?.Invoke($"Height rejected {RendererPath(mesh)}: maxY={meshHighest}, headY={headY}, allowedAbove={MaxAllowedMeshAboveHeadBoneMeters}");
                    ignoredOutliers++;
                    continue;
                }

                DiagnosticLog?.Invoke($"Height accepted {RendererPath(mesh)}: maxY={meshHighest}");
                if (meshHighest > highestPoint)
                    highestPoint = meshHighest;
            }

            if (highestPoint == float.MinValue)
            {
                // Fallback to head position
                if (head != null)
                {
                    DiagnosticLog?.Invoke($"Height: no usable mesh; head + 0.1m fallback Y={head.position.y + 0.1f}");
                    return head.position.y + 0.1f;
                }

                DiagnosticLog?.Invoke("Height: no mapped head or usable mesh; root + 1.5m fallback.");
                return avatarRoot.transform.position.y + 1.5f;
            }

            // Ensure we never under-report height below the head bone (common when the head is a separate mesh).
            if (head != null)
            {
                highestPoint = Mathf.Max(highestPoint, head.position.y + 0.1f);
            }

            if (ignoredOutliers > 0)
            {
                if (!hasLoggedHeightOutlierWarning)
                {
                    hasLoggedHeightOutlierWarning = true;
                    Warn($"ImmersiveScaler: Ignored {ignoredOutliers} outlier mesh bounds while finding height (likely props or bad bounds)");
                }
            }

            DiagnosticLog?.Invoke($"Height result: Y={highestPoint}; minimum head + 0.1m is enforced when a head is mapped.");
            return highestPoint;
        }

        private string RendererPath(SkinnedMeshRenderer renderer)
        {
            return AnimationUtility.CalculateTransformPath(renderer.transform, avatarRoot.transform);
        }

        private void TraceCandidates(string measurement, List<SkinnedMeshRenderer> meshes, List<SkinnedMeshRenderer> candidates)
        {
            if (DiagnosticLog == null) return;
            foreach (var renderer in meshes)
                DiagnosticLog(measurement + " " + RendererPath(renderer) + ": " +
                    (candidates.Contains(renderer) ? "selected candidate" : "excluded by humanoid-bone filter"));
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
                Warn("Cannot calculate head to hand distance - missing bones");
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
                Warn("Cannot calculate head to wrist distance - missing bones");
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
            DiagnosticLog?.Invoke($"Arm-derived height: {armMethod}={armValue}; arm / ratio + 0.005 = {armValue} / {customScaleRatio} + 0.005");
            return (armValue / customScaleRatio) + 0.005f;
        }

        // Get current scaling ratio
        // NOTE: This method uses hardcoded HeadToHand and EyeHeight measurements.
        // For accurate results that match the scaling algorithm, use GetArmByMethod() and GetHeightByMethod()
        // with your desired measurement types, then calculate: armValue / (heightValue - 0.005f)
        public float GetCurrentScaling(bool useBoneBasedFloorCalculation = false)
        {
            float eyeHeight = GetEyeHeight();
            float lowestPoint = GetLowestPoint(useBoneBasedFloorCalculation);
            float currentHeight = eyeHeight - lowestPoint;

            float headToHand = HeadToHand();
            float ratio = headToHand / (currentHeight - 0.005f);

            // Debug logging removed

            return ratio;
        }

        // Get upper body portion (from eyes to legs)
        public float GetUpperBodyPortion(bool useBoneBasedFloorCalculation = false)
        {
            float eyeHeight = GetEyeHeight();
            Transform leftLeg = GetBone(HumanBodyBones.LeftUpperLeg);
            Transform rightLeg = GetBone(HumanBodyBones.RightUpperLeg);

            if (leftLeg == null || rightLeg == null)
                return 0.44f; // Default value

            float legHeight = (leftLeg.position.y + rightLeg.position.y) / 2f;
            float lowestPoint = GetLowestPoint(useBoneBasedFloorCalculation);

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
                DiagnosticLog?.Invoke("ERROR: Avatar must have a humanoid animator; scaling skipped.");
                Debug.LogError("Avatar must have a humanoid animator!");
                return;
            }

            LogVerbose("=== Starting Avatar Scaling ===");
            LogVerbose($"Parameters: TargetHeight={parameters.targetHeight}, UpperBody%={parameters.upperBodyPercentage}, ArmToLegs={parameters.armToLegs}");

            // Skip main rescale if requested
            if (parameters.skipAdjust)
            {
                DiagnosticLog?.Invoke($"Main rescale skipped; skip height={parameters.skipScale}, skip floor={parameters.skipFloor}, center={parameters.centerModel}");
                // Scale to target height
                if (!parameters.skipScale)
                {
                    ScaleToHeight(
                        parameters.targetHeight,
                        parameters.targetHeightMethod == HeightMethodType.EyeHeight,
                        parameters.useBoneBasedFloorCalculation
                    );
                }

                // Move to floor after height scaling because root scaling can move the measured floor.
                if (!parameters.skipFloor)
                {
                    MoveToFloor(parameters.useBoneBasedFloorCalculation);
                }

                // Center model
                if (parameters.centerModel)
                {
                    CenterModel();
                }

                return;
            }

            float lowestPoint = GetLowestPoint(parameters.useBoneBasedFloorCalculation);

            // Use the configured height method
            float currentHeight = GetHeightByMethod(parameters.armToHeightHeightMethod, parameters.useBoneBasedFloorCalculation);

            if (!IsUsablePositive(currentHeight))
            {
                Warn($"ImmersiveScaler: Invalid avatar height measurement ({currentHeight}); skipping scale operation");
                return;
            }

            LogVerbose($"Current measurements - Lowest: {lowestPoint}, Height (using {parameters.armToHeightHeightMethod}): {currentHeight}");

            // Use the custom scale ratio from parameters with the selected arm measurement method
            float baseViewZ = GetViewZ(parameters.customScaleRatio, parameters.armToHeightRatioMethod);
            float viewZ = baseViewZ + parameters.extraLegLength;
            float eyeZ = currentHeight;

            if (!IsUsablePositive(baseViewZ) || !IsUsablePositive(viewZ))
            {
                Warn($"ImmersiveScaler: Invalid VRChat view height measurement (base={baseViewZ}, adjusted={viewZ}); skipping scale operation");
                return;
            }

            float rescaleRatio = eyeZ / viewZ;

            // Calculate what the rescale ratio would be without extra leg length
            float baseRescaleRatio = eyeZ / baseViewZ;

            if (!IsUsablePositive(rescaleRatio))
            {
                Warn($"ImmersiveScaler: Invalid rescale ratio ({rescaleRatio}); skipping scale operation");
                return;
            }

            LogVerbose($"ViewZ: {viewZ} (base: {baseViewZ}, extra: {parameters.extraLegLength}), EyeZ: {eyeZ}, RescaleRatio: {rescaleRatio}");
            LogVerbose($"Extra Leg Length effect: base rescale ratio = {baseRescaleRatio:F4}, with extra = {rescaleRatio:F4}, difference = {baseRescaleRatio - rescaleRatio:F4}");

            // Calculate leg and arm scaling ratios
            float legLength = GetLegLength(parameters.useBoneBasedFloorCalculation);
            float legHeightPortion = legLength / eyeZ;
            if (!IsUsablePositive(legHeightPortion))
            {
                Warn($"ImmersiveScaler: Invalid leg height portion ({legHeightPortion}); skipping scale operation");
                return;
            }
            LogVerbose($"LegLength: {legLength}, LegHeightPortion: {legHeightPortion}");

            float legScaleRatio, armScaleRatio;
            float armThickness = 0.8f + (parameters.armThickness / 100f * 0.4f);

            if (parameters.scaleRelative)
            {
                LogVerbose("Using relative proportions mode");
                float armToLegs = parameters.armToLegs / 100f;
                float rescaleLegRatio = Mathf.Pow(rescaleRatio, armToLegs);
                float rescaleArmRatio = Mathf.Pow(rescaleRatio, 1f - armToLegs);
                legScaleRatio = 1f - (1f - (1f / rescaleLegRatio)) / legHeightPortion;
                armScaleRatio = CalculateArmRescaling(rescaleArmRatio, parameters.armToHeightRatioMethod, armThickness, parameters.scaleHand);
            }
            else if (parameters.keepHeadSize)
            {
                LogVerbose("Using keep head size mode");
                float currentUbp = parameters.upperBodyUseLegacy ?
                    GetUpperBodyPortion(parameters.useBoneBasedFloorCalculation) :
                    GetUpperBodyRatio(parameters.upperBodyUseNeck, parameters.upperBodyTorsoUseNeck, parameters.useBoneBasedFloorCalculation);
                if (!IsUsablePositive(currentUbp) || currentUbp >= 1f)
                {
                    Warn($"ImmersiveScaler: Invalid current upper body percentage ({currentUbp}); skipping scale operation");
                    return;
                }
                float targetUbp = Mathf.Clamp(parameters.upperBodyPercentage / 100f, 0.01f, 0.99f);
                float torsoScaleRatio = targetUbp / currentUbp;
                legScaleRatio = (1f - targetUbp) / (1f - currentUbp);
                armScaleRatio = CalculateArmRescaling(rescaleRatio, parameters.armToHeightRatioMethod, armThickness, parameters.scaleHand);

                LogVerbose($"Current UBP: {currentUbp}, Target UBP: {targetUbp}, TorsoScale: {torsoScaleRatio}, LegScale: {legScaleRatio}");

                // Scale torso
                ScaleTorso(torsoScaleRatio);
            }
            else
            {
                LogVerbose("Using standard upper body percentage mode");
                float ubp = parameters.upperBodyUseLegacy ?
                    GetUpperBodyPortion(parameters.useBoneBasedFloorCalculation) :
                    GetUpperBodyRatio(parameters.upperBodyUseNeck, parameters.upperBodyTorsoUseNeck, parameters.useBoneBasedFloorCalculation);
                if (!IsUsablePositive(ubp))
                {
                    Warn($"ImmersiveScaler: Invalid upper body percentage ({ubp}); skipping scale operation");
                    return;
                }
                float targetUbp = Mathf.Clamp(parameters.upperBodyPercentage / 100f, 0.01f, 0.99f);
                float ubScaleRatio = ubp / targetUbp;

                LogVerbose($"Current UBP: {ubp}, Target UBP: {targetUbp}, UB Scale Ratio: {ubScaleRatio}");

                legScaleRatio = ubScaleRatio + ((ubScaleRatio * ubp - ubp) / legHeightPortion);
                float rescaleLegRatio = 1f / (legHeightPortion * (legScaleRatio - 1f) + 1f);
                if (!IsUsablePositive(rescaleLegRatio))
                {
                    Warn($"ImmersiveScaler: Invalid leg rescale ratio ({rescaleLegRatio}); skipping scale operation");
                    return;
                }
                float rescaleArmRatio = rescaleRatio / rescaleLegRatio;
                armScaleRatio = CalculateArmRescaling(rescaleArmRatio, parameters.armToHeightRatioMethod, armThickness, parameters.scaleHand);

                LogVerbose($"LegScaleRatio: {legScaleRatio}, RescaleLegRatio: {rescaleLegRatio}, RescaleArmRatio: {rescaleArmRatio}");
            }

            DiagnosticLog?.Invoke($"Raw scale ratios: legs={legScaleRatio}, arms={armScaleRatio}; finite ratios clamped to [0.1, 10].");
            legScaleRatio = SafeClampScale(legScaleRatio);
            armScaleRatio = SafeClampScale(armScaleRatio);

            LogVerbose($"Final scale ratios - Legs: {legScaleRatio}, Arms: {armScaleRatio}");

            // Apply leg scaling
            // Simple thickness adjustment: 0% = 0.8x, 50% = 1.0x, 100% = 1.2x
            float legThicknessNorm = parameters.legThickness / 100f;
            float legThickness = 0.8f + (legThicknessNorm * 0.4f);
            LogVerbose($"Leg thickness calculation: {parameters.legThickness}% -> {legThickness}");

            ScaleLegs(
                legScaleRatio,
                legThickness,
                parameters.scaleFoot,
                parameters.thighPercentage / 100f,
                parameters.useBoneBasedFloorCalculation
            );

            // Apply arm scaling
            // Simple thickness adjustment: 0% = 0.8x, 50% = 1.0x, 100% = 1.2x
            LogVerbose($"Arm thickness calculation: {parameters.armThickness}% -> {armThickness}");

            ScaleArms(armScaleRatio, armThickness, parameters.scaleHand);

            // Scale to target height
            if (!parameters.skipScale)
            {
                LogVerbose($"Scaling to target height: {parameters.targetHeight}");
                ScaleToHeight(parameters.targetHeight, parameters.targetHeightMethod == HeightMethodType.EyeHeight, parameters.useBoneBasedFloorCalculation);
            }

            // Move to floor after height scaling because root scaling can move the measured floor.
            if (!parameters.skipFloor)
            {
                LogVerbose($"Moving avatar to floor (bone-based: {parameters.useBoneBasedFloorCalculation})");
                MoveToFloor(parameters.useBoneBasedFloorCalculation);
            }

            // Center model
            if (parameters.centerModel)
            {
                LogVerbose("Centering model");
                CenterModel();
            }

            LogVerbose("=== Avatar Scaling Complete ===");
        }

        private void ScaleLegs(float legScaleRatio, float thickness, bool scaleFoot, float thighPercentage, bool useBoneBasedFloorCalculation)
        {
            // Get leg bones
            Transform leftUpperLeg = GetBone(HumanBodyBones.LeftUpperLeg);
            Transform rightUpperLeg = GetBone(HumanBodyBones.RightUpperLeg);
            Transform leftLowerLeg = GetBone(HumanBodyBones.LeftLowerLeg);
            Transform rightLowerLeg = GetBone(HumanBodyBones.RightLowerLeg);
            Transform leftFoot = GetBone(HumanBodyBones.LeftFoot);
            Transform rightFoot = GetBone(HumanBodyBones.RightFoot);

            LogVerbose($"ScaleLegs - legScaleRatio: {legScaleRatio}, thickness: {thickness}, thighPercentage: {thighPercentage}");
            LogVerbose($"Thickness parameter: {thickness}, should be different from legScaleRatio: {legScaleRatio}");

            // Get current leg proportions
            float[] legProportions = GetLegProportions(useBoneBasedFloorCalculation);
            float thighPortion = legProportions[0];
            float calfPortion = legProportions[1];
            float footPortion = legProportions[2];

            LogVerbose($"Current leg proportions - Thigh: {thighPortion}, Calf: {calfPortion}, Foot: {footPortion}");

            // Calculate scaling for each part based on thigh percentage
            float targetThighRatio = Mathf.Clamp(thighPercentage, 0.01f, 0.99f);
            float legSegmentTotal = thighPortion + calfPortion;
            float currentThighRatio = IsUsablePositive(legSegmentTotal) ? thighPortion / legSegmentTotal : 0.5f;
            // The ankle-to-floor part stays the same size when Scale Feet is off.
            // Allocate the remaining requested leg height to thigh and calf only.
            float segmentScaleRatio = IsUsablePositive(legSegmentTotal)
                ? SafeClampScale((legScaleRatio - footPortion * (scaleFoot ? legScaleRatio : 1f)) / legSegmentTotal)
                : legScaleRatio;

            // Adjust scales to achieve target thigh percentage while maintaining overall leg scale
            float thighScale, calfScale;

            if (!IsFinite(currentThighRatio) || currentThighRatio <= 0.01f || currentThighRatio >= 0.99f)
            {
                Warn($"ImmersiveScaler: Invalid current thigh ratio ({currentThighRatio}); using uniform leg scaling");
                thighScale = segmentScaleRatio;
                calfScale = segmentScaleRatio;
            }
            else if (Mathf.Abs(currentThighRatio - targetThighRatio) > 0.0001f)
            {
                // Calculate how much to adjust each segment to achieve target ratio
                float thighAdjustment = targetThighRatio / currentThighRatio;
                float calfAdjustment = (1f - targetThighRatio) / (1f - currentThighRatio);

                // Apply adjustments while maintaining overall leg scale
                if (!IsUsablePositive(thighAdjustment) || !IsUsablePositive(calfAdjustment))
                {
                    Warn($"ImmersiveScaler: Invalid thigh/calf adjustment values ({thighAdjustment}, {calfAdjustment}); using uniform leg scaling");
                    thighScale = segmentScaleRatio;
                    calfScale = segmentScaleRatio;
                }
                else
                {
                    thighScale = segmentScaleRatio * thighAdjustment;
                    calfScale = segmentScaleRatio * calfAdjustment;
                }

                LogVerbose($"Thigh percentage adjustment - Current: {currentThighRatio:F3}, Target: {targetThighRatio:F3}");
                LogVerbose($"Adjustments - Thigh: {thighAdjustment:F3}, Calf: {calfAdjustment:F3}");
            }
            else
            {
                // No adjustment needed, use uniform scaling
                thighScale = segmentScaleRatio;
                calfScale = segmentScaleRatio;
            }

            // Clamp values to prevent extreme scaling
            thighScale = Mathf.Clamp(thighScale, 0.1f, 10f);
            calfScale = Mathf.Clamp(calfScale, 0.1f, 10f);
            thickness = Mathf.Clamp(thickness, 0.1f, 10f);

            LogVerbose($"Final scales - Thigh: {thighScale}, Calf: {calfScale}, Thickness: {thickness}");

            // In Unity, we need to determine which axis to scale along
            // Most humanoid rigs have bones extending along the Y axis locally
            // But we should check the bone direction

            // Scale upper legs
            if (leftUpperLeg != null)
            {
                Vector3 boneDirection = GetBoneDirection(leftUpperLeg, leftLowerLeg);
                Vector3 newScale = GetDirectionalScale(boneDirection, thighScale, thickness);
                LogVerbose($"Left upper leg - Direction: {boneDirection}, New scale: {newScale}");
                LogVerbose($"   Length axis scale: {(boneDirection.y != 0 ? newScale.y : (boneDirection.x != 0 ? newScale.x : newScale.z))}, Thickness axes: X={newScale.x}, Z={newScale.z}");
                ScaleBoneWithChildCompensation(leftUpperLeg, newScale);
            }
            if (rightUpperLeg != null)
            {
                Vector3 boneDirection = GetBoneDirection(rightUpperLeg, rightLowerLeg);
                Vector3 newScale = GetDirectionalScale(boneDirection, thighScale, thickness);
                ScaleBoneWithChildCompensation(rightUpperLeg, newScale);
            }

            // Scale lower legs
            if (leftLowerLeg != null)
            {
                Vector3 boneDirection = GetBoneDirection(leftLowerLeg, leftFoot);
                Vector3 newScale = GetDirectionalScale(boneDirection, calfScale, thickness);
                LogVerbose($"Left lower leg - Direction: {boneDirection}, New scale: {newScale}");
                ScaleBoneWithChildCompensation(leftLowerLeg, newScale);
            }
            if (rightLowerLeg != null)
            {
                Vector3 boneDirection = GetBoneDirection(rightLowerLeg, rightFoot);
                Vector3 newScale = GetDirectionalScale(boneDirection, calfScale, thickness);
                ScaleBoneWithChildCompensation(rightLowerLeg, newScale);
            }

            // Scale feet if requested
            if (scaleFoot)
            {
                float footScale = Mathf.Clamp(legScaleRatio, 0.1f, 10f);
                if (leftFoot != null)
                    leftFoot.localScale *= footScale;
                if (rightFoot != null)
                    rightFoot.localScale *= footScale;
            }
        }

        private Vector3 GetBoneDirection(Transform bone, Transform nextBone = null)
        {
            // Determine which direction the bone extends in local space
            // Prefer the mapped joint: the first child may be a zero-length helper.
            if (nextBone != null || bone.childCount > 0)
            {
                Transform child = nextBone != null ? nextBone : bone.GetChild(0);
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

        private static Vector3 BasisLengths(Transform transform)
        {
            return new Vector3(transform.TransformVector(Vector3.right).magnitude,
                transform.TransformVector(Vector3.up).magnitude,
                transform.TransformVector(Vector3.forward).magnitude);
        }

        // Each joint owns one segment's length/thickness. Its children move with the
        // segment, but must not inherit the same deformation a second time. Preserve
        // authored scales and compensate in each child's axes (feet often rotate 90 degrees).
        private void ScaleBoneWithChildCompensation(Transform bone, Vector3 multiplier)
        {
            if (multiplier == Vector3.one) return;
            var children = new List<KeyValuePair<Transform, Vector3>>();
            foreach (Transform child in bone)
                children.Add(new KeyValuePair<Transform, Vector3>(child, BasisLengths(child)));
            var originalScale = bone.localScale;
            bone.localScale = Vector3.Scale(originalScale, multiplier);
            foreach (var entry in children)
            {
                var after = BasisLengths(entry.Key);
                Vector3 correction = Vector3.one;
                for (int axis = 0; axis < 3; axis++)
                    if (IsUsablePositive(after[axis])) correction[axis] = entry.Value[axis] / after[axis];
                entry.Key.localScale = Vector3.Scale(entry.Key.localScale, correction);
            }
            DiagnosticLog?.Invoke($"Bone scale {bone.name}: original={originalScale}, multiplier={multiplier}, result={bone.localScale}; compensated children={children.Count}");
        }

        private float[] GetLegProportions(bool useBoneBasedFloorCalculation)
        {
            Transform leftUpperLeg = GetBone(HumanBodyBones.LeftUpperLeg);
            Transform leftLowerLeg = GetBone(HumanBodyBones.LeftLowerLeg);
            Transform leftFoot = GetBone(HumanBodyBones.LeftFoot);

            if (leftUpperLeg == null || leftLowerLeg == null || leftFoot == null)
                return new float[] { 0.5f, 0.4f, 0.1f }; // Default proportions

            float lowestPoint = GetLowestPoint(useBoneBasedFloorCalculation);
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

            LogVerbose($"ScaleArms - armScaleRatio: {armScaleRatio}, thickness: {thickness}");

            // Clamp values
            armScaleRatio = Mathf.Clamp(armScaleRatio, 0.1f, 10f);
            thickness = Mathf.Clamp(thickness, 0.1f, 10f);

            // Scale upper arms
            if (leftUpperArm != null)
            {
                Vector3 boneDirection = GetBoneDirection(leftUpperArm, leftLowerArm);
                Vector3 newScale = GetDirectionalScale(boneDirection, armScaleRatio, thickness);
                LogVerbose($"Left upper arm - Direction: {boneDirection}, New scale: {newScale}");
                ScaleBoneWithChildCompensation(leftUpperArm, newScale);
            }
            if (rightUpperArm != null)
            {
                Vector3 boneDirection = GetBoneDirection(rightUpperArm, rightLowerArm);
                Vector3 newScale = GetDirectionalScale(boneDirection, armScaleRatio, thickness);
                ScaleBoneWithChildCompensation(rightUpperArm, newScale);
            }

            // Scale lower arms
            if (leftLowerArm != null)
            {
                Vector3 boneDirection = GetBoneDirection(leftLowerArm, leftHand);
                Vector3 newScale = GetDirectionalScale(boneDirection, armScaleRatio, thickness);
                LogVerbose($"Left lower arm - Direction: {boneDirection}, New scale: {newScale}");
                ScaleBoneWithChildCompensation(leftLowerArm, newScale);
            }
            if (rightLowerArm != null)
            {
                Vector3 boneDirection = GetBoneDirection(rightLowerArm, rightHand);
                Vector3 newScale = GetDirectionalScale(boneDirection, armScaleRatio, thickness);
                ScaleBoneWithChildCompensation(rightLowerArm, newScale);
            }

            if (scaleHand)
            {
                // Child compensation already preserves hands when this option is off.
                if (leftHand != null) leftHand.localScale *= armScaleRatio;
                if (rightHand != null) rightHand.localScale *= armScaleRatio;
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

        private float CalculateArmRescaling(float measurementChange, ArmMethodType method, float thickness, bool scaleHand)
        {
            float current = GetArmByMethod(method);
            if (!IsUsablePositive(current) || !IsUsablePositive(measurementChange)) return 1f;
            if (Mathf.Approximately(measurementChange, 1f) && Mathf.Approximately(thickness, 1f)) return 1f;

            // Solve using the selected measurement and the same bone operation that
            // will be applied. The old formula combined head-to-elbow with full arm
            // length even when the selected method measured to the wrist or fingers.
            // Only these local scales are touched; restore them after every sample.
            var saved = new Dictionary<Transform, Vector3>();
            foreach (var id in new[] { HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm,
                HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
            {
                var bone = GetBone(id);
                if (bone == null) continue;
                saved[bone] = bone.localScale;
                foreach (Transform child in bone) saved[child] = child.localScale;
            }
            var sink = DiagnosticLog;
            float target = current * measurementChange;
            float low = 0.1f, high = 10f;
            float result = 1f;
            float Measure(float scale)
            {
                foreach (var entry in saved) entry.Key.localScale = entry.Value;
                ScaleArms(scale, thickness, scaleHand);
                return GetArmByMethod(method);
            }
            try
            {
                DiagnosticLog = null;
                float minimum = Measure(low), maximum = Measure(high);
                if (!IsUsablePositive(minimum) || !IsUsablePositive(maximum) || maximum <= minimum) return 1f;
                if (target <= minimum) result = low;
                else if (target >= maximum) result = high;
                else
                {
                    for (int i = 0; i < 24; i++)
                    {
                        float middle = (low + high) * 0.5f;
                        if (Measure(middle) < target) low = middle;
                        else high = middle;
                    }
                    result = (low + high) * 0.5f;
                }
            }
            finally
            {
                foreach (var entry in saved) entry.Key.localScale = entry.Value;
                DiagnosticLog = sink;
            }
            DiagnosticLog?.Invoke($"Arm scale solve: method={method}, original={current}, target={target}, segment factor={result}");
            return result;
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
        public float GetSimpleArmRatio(bool useBoneBasedFloorCalculation = false)
        {
            float armLength = GetArmLength();
            float totalHeight = GetHighestPoint() - GetLowestPoint(useBoneBasedFloorCalculation);
            return armLength / totalHeight;
        }

        // Arm length divided by eye height
        public float GetArmToEyeRatio(bool useBoneBasedFloorCalculation = false)
        {
            float armLength = GetArmLength();
            float eyeHeight = GetEyeHeight() - GetLowestPoint(useBoneBasedFloorCalculation);
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
        public float GetHeadHeight(bool useBoneBasedFloorCalculation = false)
        {
            Transform neck = GetBone(HumanBodyBones.Neck);
            if (neck == null)
                return GetEyeHeight() - 0.1f; // Fallback

            return neck.position.y - GetLowestPoint(useBoneBasedFloorCalculation);
        }

        // Get floor to head height (floor to head bone base)
        public float GetFloorToHeadHeight(bool useBoneBasedFloorCalculation = false)
        {
            Transform head = GetBone(HumanBodyBones.Head);
            if (head == null)
                return GetHighestPoint() - GetLowestPoint(useBoneBasedFloorCalculation); // Fallback to total height

            return head.position.y - GetLowestPoint(useBoneBasedFloorCalculation);
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
        public float GetHeightByMethod(HeightMethodType method, bool useBoneBasedFloorCalculation = false)
        {
            float lowest = GetLowestPoint(useBoneBasedFloorCalculation);
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
        public float GetUpperBodyRatio(bool useNeckForHeight, bool useNeckForTorso, bool useBoneBasedFloorCalculation = false)
        {
            // Height: floor to neck or floor to head
            float height = useNeckForHeight ? GetHeadHeight(useBoneBasedFloorCalculation) : GetFloorToHeadHeight(useBoneBasedFloorCalculation);
            if (!IsUsablePositive(height)) return 0.44f;

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
            if (!IsFinite(lowestPoint)) return;

            Vector3 currentPos = avatarRoot.transform.position;
            avatarRoot.transform.position = new Vector3(
                currentPos.x,
                currentPos.y - lowestPoint,
                currentPos.z
            );

            LogVerbose($"ImmersiveScaler: Moved to floor. Lowest point was {lowestPoint}, new Y position: {avatarRoot.transform.position.y}");
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

            if (!IsUsablePositive(currentHeight) || !IsUsablePositive(targetHeight))
            {
                Warn($"ImmersiveScaler: Cannot scale to target height. Current={currentHeight}, Target={targetHeight}");
                return;
            }

            float scaleRatio = targetHeight / currentHeight;
            if (!IsUsablePositive(scaleRatio))
            {
                Warn($"ImmersiveScaler: Invalid height scale ratio ({scaleRatio}); skipping height scale");
                return;
            }

            DiagnosticLog?.Invoke($"Height scale: target={targetHeight} / measured={currentHeight} = factor={scaleRatio}; useEyes={scaleToEyes}; floorY={lowestPoint}; rootScaleBefore={avatarRoot.transform.localScale}");
            avatarRoot.transform.localScale *= scaleRatio;
            DiagnosticLog?.Invoke($"Height scale: rootScaleAfter={avatarRoot.transform.localScale}");
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
