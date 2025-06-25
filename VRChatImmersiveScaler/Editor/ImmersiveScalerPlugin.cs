using System.Reflection;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

[assembly: ExportsPlugin(typeof(VRChatImmersiveScaler.Editor.ImmersiveScalerPlugin))]

namespace VRChatImmersiveScaler.Editor
{
    public class ImmersiveScalerPlugin : Plugin<ImmersiveScalerPlugin>
    {
        public override string QualifiedName => "com.vrchat.immersivescaler";
        public override string DisplayName => "VRChat Immersive Scaler";
        
        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .Run("Apply Immersive Scaling", ctx =>
                {
                    var component = ctx.AvatarRootTransform.GetComponentInChildren<ImmersiveScalerComponent>(true);
                    if (component == null) return;
                    
                    // Get VRCAvatarDescriptor
                    var descriptor = ctx.AvatarDescriptor as VRCAvatarDescriptor;
                    if (descriptor == null)
                    {
                        Debug.LogError("ImmersiveScaler: No VRCAvatarDescriptor found!");
                        return;
                    }
                    
                    // Store original ViewPosition if not already stored
                    if (!component.hasStoredOriginalViewPosition)
                    {
                        component.originalViewPosition = descriptor.ViewPosition;
                        component.hasStoredOriginalViewPosition = true;
                    }
                    
                    Debug.Log($"ImmersiveScaler: Starting scaling process. Target height: {component.targetHeight}m");
                    
                    // Create scaling core
                    var scalerCore = new ImmersiveScalerCore(ctx.AvatarRootTransform.gameObject);
                    
                    // Measure original eye height and position
                    float originalEyeHeight = scalerCore.GetEyeHeight();
                    float originalLowestPoint = scalerCore.GetLowestPoint();
                    float originalAvatarHeight = originalEyeHeight - originalLowestPoint;
                    Vector3 originalEyeLocalPos = scalerCore.GetEyePositionLocal();
                    
                    // Store original avatar scale
                    Vector3 originalAvatarScale = ctx.AvatarRootTransform.localScale;
                    
                    // Create parameters from component
                    var parameters = new ScalingParameters
                    {
                        targetHeight = component.targetHeight,
                        upperBodyPercentage = component.upperBodyPercentage,
                        customScaleRatio = component.customScaleRatio,
                        armThickness = component.armThickness,
                        legThickness = component.legThickness,
                        thighPercentage = component.thighPercentage,
                        scaleHand = component.scaleHand,
                        scaleFoot = component.scaleFoot,
                        scaleEyes = component.scaleEyes,
                        centerModel = component.centerModel,
                        extraLegLength = component.extraLegLength,
                        scaleRelative = component.scaleRelative,
                        armToLegs = component.armToLegs,
                        keepHeadSize = component.keepHeadSize,
                        skipAdjust = component.skipMainRescale,
                        skipFloor = component.skipMoveToFloor,
                        skipScale = component.skipHeightScaling,
                        useBoneBasedFloorCalculation = component.useBoneBasedFloorCalculation,
                        // Pass measurement method configuration
                        targetHeightMethod = component.targetHeightMethod,
                        armToHeightRatioMethod = component.armToHeightRatioMethod,
                        armToHeightHeightMethod = component.armToHeightHeightMethod,
                        upperBodyUseNeck = component.upperBodyUseNeck,
                        upperBodyTorsoUseNeck = component.upperBodyTorsoUseNeck,
                        upperBodyUseLegacy = component.upperBodyUseLegacy
                    };
                    
                    // Apply scaling
                    scalerCore.ScaleAvatar(parameters);
                    
                    // Measure new eye position after scaling
                    float newEyeHeight = scalerCore.GetEyeHeight();
                    float newLowestPoint = scalerCore.GetLowestPoint();
                    Vector3 newEyeLocalPos = scalerCore.GetEyePositionLocal();
                    
                    // Calculate the actual scale ratio applied to the avatar
                    Vector3 newAvatarScale = ctx.AvatarRootTransform.localScale;
                    float scaleRatio = newAvatarScale.y / originalAvatarScale.y;
                    
                    // Update ViewPosition to match the new eye position
                    Vector3 newViewPosition = descriptor.ViewPosition;
                    
                    // The ViewPosition is in local space relative to the avatar root
                    // Scale the ViewPosition proportionally with the avatar scale
                    if (!component.skipMainRescale || !component.skipHeightScaling)
                    {
                        // Scale the ViewPosition by the same ratio as the avatar
                        newViewPosition = component.originalViewPosition * scaleRatio;
                        
                        descriptor.ViewPosition = newViewPosition;
                        EditorUtility.SetDirty(descriptor);
                        
                        // Enhanced debug logging
                        Debug.Log($"ImmersiveScaler: ViewPosition Update Details:");
                        Debug.Log($"  Original ViewPosition: {component.originalViewPosition}");
                        Debug.Log($"  New ViewPosition: {newViewPosition}");
                        Debug.Log($"  Eye Position (Local Space) - Before: {originalEyeLocalPos}, After: {newEyeLocalPos}");
                        Debug.Log($"  Eye Movement Delta (Local): {newEyeLocalPos - originalEyeLocalPos}");
                        Debug.Log($"  Eye Height (World) - Before: {originalEyeHeight:F3}, After: {newEyeHeight:F3}");
                    }
                    
                    Debug.Log($"ImmersiveScaler: Scaling complete. Final height: {scalerCore.GetHighestPoint() - scalerCore.GetLowestPoint():F3}m");
                    
                    // Apply additional tools if enabled
                    if (component.applyFingerSpreading)
                    {
                        Debug.Log($"ImmersiveScaler: Applying finger spreading with factor {component.fingerSpreadFactor}");
                        ImmersiveScalerFingerUtility.SpreadFingers(ctx.AvatarRootTransform.gameObject, 
                            component.fingerSpreadFactor, component.spareThumb);
                    }
                    
                    if (component.applyShrinkHipBone)
                    {
                        Debug.Log("ImmersiveScaler: Applying hip bone fix");
                        ApplyHipBoneFix(ctx.AvatarRootTransform.gameObject);
                    }
                    
                    // Remove component after processing
                    Object.DestroyImmediate(component);
                });
        }
        
        private static void ApplyHipBoneFix(GameObject avatar)
        {
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null || !animator.isHuman) return;
            
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform leftLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform rightLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            
            if (hips == null || spine == null || leftLeg == null || rightLeg == null)
            {
                Debug.LogError("ImmersiveScaler: Cannot find required bones for hip shrinking");
                return;
            }
            
            float legStartY = (leftLeg.position.y + rightLeg.position.y) / 2f;
            float spineStartY = spine.position.y;
            
            // Move hip 90% of the way between legs and spine
            Vector3 newPosition = hips.position;
            newPosition.y = legStartY + (spineStartY - legStartY) * 0.9f;
            newPosition.x = spine.position.x;
            newPosition.z = spine.position.z;
            
            hips.position = newPosition;
        }
    }
}