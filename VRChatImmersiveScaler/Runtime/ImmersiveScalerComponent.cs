using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
using IImmersiveScalerEditorOnly = VRC.SDKBase.IEditorOnly;
#else
using IImmersiveScalerEditorOnly = VRChatImmersiveScaler.IEditorOnly;
#endif

namespace VRChatImmersiveScaler
{
    // Measurement method enums
    public enum HeightMethodType
    {
        TotalHeight,
        EyeHeight
    }
    
    public enum ArmMethodType
    {
        HeadToElbowVRC,
        HeadToHand,
        ArmLength,
        ShoulderToFingertip,
        CenterToHand,
        CenterToFingertip
    }
    
    public enum UpperBodyMethodType
    {
        UpperBodyPercent,
        UpperBodyLength,
        AlternateUpperBody
    }
    
    [AddComponentMenu("VRChat/Immersive Scaler")]
    [DisallowMultipleComponent]
    public class ImmersiveScalerComponent : MonoBehaviour, IImmersiveScalerEditorOnly
    {
        [Header("Basic Settings")]
        [Tooltip("Target height of the avatar in meters")]
        public float targetHeight = 1.61f;
        
        [Tooltip("Percentage of height from eyes to feet that should be upper body")]
        [Range(30f, 75f)]
        public float upperBodyPercentage = 44f;
        
        [Tooltip("VRChat's arm ratio - controls IK arm length (default: 0.4537, lower = longer arms)")]
        public float customScaleRatio = 0.4537f;
        
        [Header("Body Proportions")]
        [Tooltip("How much arm thickness to maintain when scaling")]
        [Range(0f, 100f)]
        public float armThickness = 50f;
        
        [Tooltip("How much leg thickness to maintain when scaling")]
        [Range(0f, 100f)]
        public float legThickness = 50f;
        
        [Tooltip("Percentage of leg that should be thigh vs calf")]
        [Range(10f, 90f)]
        public float thighPercentage = 53f;
        
        [Header("Scaling Options")]
        [Tooltip("Scale hands proportionally with arms")]
        public bool scaleHand = false;
        
        [Tooltip("Scale feet proportionally with legs")]
        public bool scaleFoot = false;
        
        [Tooltip("Measure height to eyes instead of top of head")]
        public bool scaleEyes = true;
        
        [Tooltip("Center avatar at world origin (X=0, Z=0)")]
        public bool centerModel = false;
        
        [Header("Advanced Options")]
        [Tooltip("Additional leg length below the floor")]
        public float extraLegLength = 0f;
        
        [Tooltip("Use relative proportions mode instead of upper body percentage")]
        public bool scaleRelative = false;
        
        [Tooltip("Percentage of scaling to apply to legs (only in relative mode)")]
        [Range(0f, 100f)]
        public float armToLegs = 55f;
        
        [Tooltip("Keep head size constant by scaling torso")]
        public bool keepHeadSize = false;
        
        [Header("Additional Tools")]
        [Tooltip("Apply finger spreading during avatar build")]
        public bool applyFingerSpreading = false;
        
        [Tooltip("How much to spread fingers apart")]
        [Range(0f, 2f)]
        public float fingerSpreadFactor = 1.0f;
        
        [Tooltip("Don't spread the thumb")]
        public bool spareThumb = true;
        
        [Tooltip("Apply hip bone fix during avatar build")]
        public bool applyShrinkHipBone = false;
        
        [Header("Debug Options")]
        public bool skipMainRescale = false;
        public bool skipMoveToFloor = true;
        public bool skipHeightScaling = false;
        
        [Tooltip("Use bone positions instead of mesh bounds for floor calculation (more reliable but less accurate)")]
        public bool useBoneBasedFloorCalculation = false;
        
        // Store original ViewPosition for restoration if needed
        [HideInInspector]
        public Vector3 originalViewPosition;
        [HideInInspector]
        public bool hasStoredOriginalViewPosition = false;
        
        // Debug visualization
        [HideInInspector]
        public string debugMeasurement = "";
        
        // Measurement method selections
        [HideInInspector]
        public HeightMethodType targetHeightMethod = HeightMethodType.EyeHeight;
        
        [HideInInspector]
        public ArmMethodType armToHeightRatioMethod = ArmMethodType.HeadToHand;
        
        [HideInInspector]
        public HeightMethodType armToHeightHeightMethod = HeightMethodType.EyeHeight;
        
        // Upper body calculation methods
        [HideInInspector]
        public bool upperBodyUseNeck = false; // true = floor to neck, false = floor to head
        
        [HideInInspector]
        public bool upperBodyTorsoUseNeck = false; // true = leg to neck, false = leg to head
        
        [HideInInspector]
        public bool upperBodyUseLegacy = true; // true = use legacy (leg to eye)/(floor to eye) calculation
    }
    
    // Fallback interface for non-VRChat environments
#if !VRC_SDK_VRCSDK3
    public interface IEditorOnly
    {
        // Marker interface
    }
#endif
}