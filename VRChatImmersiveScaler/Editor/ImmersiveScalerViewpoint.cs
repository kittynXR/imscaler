using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace VRChatImmersiveScaler
{
    /// <summary>Converts between VRChat's root-relative world vector and Unity local coordinates.</summary>
    public static class ImmersiveScalerViewpoint
    {
        public static Vector3 CaptureEyeOffset(VRCAvatarDescriptor descriptor, ImmersiveScalerCore core)
        {
            // ViewPosition is a world-distance vector from the root, not an unscaled local point.
            // NDMF uses this same conversion in VRChatPlatform.ExtractCommonAvatarInfo.
            return descriptor.transform.InverseTransformVector(descriptor.ViewPosition) - core.GetEyePositionLocal();
        }

        public static void Reconcile(VRCAvatarDescriptor descriptor, ImmersiveScalerCore core, Vector3 originalEyeOffset)
        {
            // Run after every operation that can move the eyes, including the optional hip adjustment.
            // TransformVector includes root/parent scale and rotation without adding scene translation.
            Vector3 viewPosition = descriptor.transform.TransformVector(core.GetEyePositionLocal() + originalEyeOffset);
            if (IsFinite(viewPosition.x) && IsFinite(viewPosition.y) && IsFinite(viewPosition.z))
                descriptor.ViewPosition = viewPosition;
            else
                Debug.LogWarning("ImmersiveScaler: Viewpoint calculation was not finite; keeping the previous ViewPosition.");
        }

        public static Vector3 GetWorldPosition(VRCAvatarDescriptor descriptor)
        {
            return descriptor.transform.position + descriptor.ViewPosition;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
