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
        public override string DisplayName => "🐟 Immersive Scaler 📐📏🎨";

        private static void LogDebug(string message)
        {
#if KITTYN_IMMERSIVE_SCALER_DEBUG
            Debug.Log(message);
#endif
        }
        
        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                // If this runs before armature merge tools (e.g. Modular Avatar Merge Armature),
                // those tools may rewrite bindposes to preserve the outfit's *current* appearance and
                // effectively cancel out the scaling we apply to humanoid bones.
                //
                // Running after MA ensures merged/retargeted outfits are already bound to the avatar armature,
                // so they receive the same bone scaling as the base body.
                .AfterPlugin("nadena.dev.modular-avatar")
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
                    
                    
                    LogDebug($"ImmersiveScaler: Starting scaling process. Target height: {component.targetHeight}m");
                    
                    // Create scaling core
                    var scalerCore = new ImmersiveScalerCore(ctx.AvatarRootTransform.gameObject);
                    scalerCore.SetMeasurementRendererOverrides(
                        component.useMeasurementRendererOverrides,
                        component.measurementBodyRenderers,
                        component.measurementHeadRenderers
                    );
                    
                    var parameters = ImmersiveScalerDiagnostics.ParametersFrom(component);
                    using (var report = new ImmersiveScalerDiagnostics(descriptor, scalerCore, parameters,
                        "NDMF build: Immersive Scaler pass (later plugins may still change the avatar)",
                        ImmersiveScalerDiagnostics.DescribeTools(component)))
                    {
                        Vector3 eyeOffset = ImmersiveScalerViewpoint.CaptureEyeOffset(descriptor, scalerCore);
                        report.Record("Original eye offset (avatar-local): " + eyeOffset.ToString("F6"));
                        scalerCore.ScaleAvatar(parameters);

                        // Apply additional tools if enabled
                        if (component.applyFingerSpreading)
                        {
                            LogDebug($"ImmersiveScaler: Applying finger spreading with factor {component.fingerSpreadFactor}");
                            ImmersiveScalerFingerUtility.SpreadFingers(ctx.AvatarRootTransform.gameObject,
                                component.fingerSpreadFactor, component.spareThumb);
                        }
                        
                        if (component.applyShrinkHipBone)
                        {
                            LogDebug("ImmersiveScaler: Applying hip bone fix");
                            ApplyHipBoneFix(ctx.AvatarRootTransform.gameObject);
                        }
                        
                        ImmersiveScalerViewpoint.Reconcile(descriptor, scalerCore, eyeOffset);
                        EditorUtility.SetDirty(descriptor);
                        report.Record("Viewpoint reconciled after scaling and additional tools.");
                        ImmersiveScalerDiagnostics.LastBuildReport = report.Complete();
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
