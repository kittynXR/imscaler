using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;

namespace VRChatImmersiveScaler.Editor
{
    // Component parameter provider wrapper
    internal class ComponentParameterProvider : ImmersiveScalerUIShared.IParameterProvider
    {
        private ImmersiveScalerComponent component;
        
        public ComponentParameterProvider(ImmersiveScalerComponent comp)
        {
            component = comp;
        }
        
        // Basic Settings
        public float targetHeight
        {
            get => component.targetHeight;
            set => component.targetHeight = value;
        }
        
        public float upperBodyPercentage
        {
            get => component.upperBodyPercentage;
            set => component.upperBodyPercentage = value;
        }
        
        public float customScaleRatio
        {
            get => component.customScaleRatio;
            set => component.customScaleRatio = value;
        }
        
        // Body Proportions
        public float armThickness
        {
            get => component.armThickness;
            set => component.armThickness = value;
        }
        
        public float legThickness
        {
            get => component.legThickness;
            set => component.legThickness = value;
        }
        
        public float thighPercentage
        {
            get => component.thighPercentage;
            set => component.thighPercentage = value;
        }
        
        // Scaling Options
        public bool scaleHand
        {
            get => component.scaleHand;
            set => component.scaleHand = value;
        }
        
        public bool scaleFoot
        {
            get => component.scaleFoot;
            set => component.scaleFoot = value;
        }
        
        public bool scaleEyes
        {
            get => component.scaleEyes;
            set => component.scaleEyes = value;
        }
        
        public bool centerModel
        {
            get => component.centerModel;
            set => component.centerModel = value;
        }
        
        // Advanced Options
        public float extraLegLength
        {
            get => component.extraLegLength;
            set => component.extraLegLength = value;
        }
        
        public bool scaleRelative
        {
            get => component.scaleRelative;
            set => component.scaleRelative = value;
        }
        
        public float armToLegs
        {
            get => component.armToLegs;
            set => component.armToLegs = value;
        }
        
        public bool keepHeadSize
        {
            get => component.keepHeadSize;
            set => component.keepHeadSize = value;
        }
        
        // Debug Options
        public bool skipMainRescale
        {
            get => component.skipMainRescale;
            set => component.skipMainRescale = value;
        }
        
        public bool skipMoveToFloor
        {
            get => component.skipMoveToFloor;
            set => component.skipMoveToFloor = value;
        }
        
        public bool skipHeightScaling
        {
            get => component.skipHeightScaling;
            set => component.skipHeightScaling = value;
        }
        
        public bool useBoneBasedFloorCalculation
        {
            get => component.useBoneBasedFloorCalculation;
            set => component.useBoneBasedFloorCalculation = value;
        }
        
        // Additional Tools
        public bool applyFingerSpreading
        {
            get => component.applyFingerSpreading;
            set => component.applyFingerSpreading = value;
        }
        
        public float fingerSpreadFactor
        {
            get => component.fingerSpreadFactor;
            set => component.fingerSpreadFactor = value;
        }
        
        public bool spareThumb
        {
            get => component.spareThumb;
            set => component.spareThumb = value;
        }
        
        public bool applyShrinkHipBone
        {
            get => component.applyShrinkHipBone;
            set => component.applyShrinkHipBone = value;
        }
        
        // Measurement methods
        public HeightMethodType targetHeightMethod
        {
            get => component.targetHeightMethod;
            set => component.targetHeightMethod = value;
        }
        
        public ArmMethodType armToHeightRatioMethod
        {
            get => component.armToHeightRatioMethod;
            set => component.armToHeightRatioMethod = value;
        }
        
        public HeightMethodType armToHeightHeightMethod
        {
            get => component.armToHeightHeightMethod;
            set => component.armToHeightHeightMethod = value;
        }
        
        public bool upperBodyUseNeck
        {
            get => component.upperBodyUseNeck;
            set => component.upperBodyUseNeck = value;
        }
        
        public bool upperBodyTorsoUseNeck
        {
            get => component.upperBodyTorsoUseNeck;
            set => component.upperBodyTorsoUseNeck = value;
        }
        
        public bool upperBodyUseLegacy
        {
            get => component.upperBodyUseLegacy;
            set => component.upperBodyUseLegacy = value;
        }
        
        // Debug visualization
        public string debugMeasurement
        {
            get => component.debugMeasurement;
            set => component.debugMeasurement = value;
        }
        
        public void SetDirty()
        {
            EditorUtility.SetDirty(component);
        }
    }
    
    [CustomEditor(typeof(ImmersiveScalerComponent))]
    public class ImmersiveScalerComponentEditor : UnityEditor.Editor
    {
        private ImmersiveScalerCore scalerCore;
        private ComponentParameterProvider paramProvider;
        private bool showAdvanced = false;
        private bool showDebug = false;
        private bool showCurrentStats = true;
        private bool showAdditionalTools = false;
        private bool showDebugMeasurements = false;
        private bool showDebugRatios = false;
        
        // Preview state tracking
        private bool isPreviewActive = false;
        private Dictionary<Transform, TransformState> originalTransformStates = new Dictionary<Transform, TransformState>();
        private VRCAvatarDescriptor previewAvatar = null;
        private Vector3 storedOriginalViewPosition;
        
        // Helper class to store transform state
        private class TransformState
        {
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            
            public TransformState(Transform t)
            {
                localPosition = t.localPosition;
                localRotation = t.localRotation;
                localScale = t.localScale;
            }
            
            public void RestoreTo(Transform t)
            {
                t.localPosition = localPosition;
                t.localRotation = localRotation;
                t.localScale = localScale;
            }
        }
        
        private void OnEnable()
        {
            var component = (ImmersiveScalerComponent)target;
            paramProvider = new ComponentParameterProvider(component);
            
            var avatar = component.GetComponentInParent<VRCAvatarDescriptor>();
            if (avatar != null)
            {
                scalerCore = new ImmersiveScalerCore(avatar.gameObject);
                
                // Auto-populate values if they're at defaults
                if (Mathf.Approximately(component.targetHeight, 1.61f) && 
                    Mathf.Approximately(component.upperBodyPercentage, 44f) &&
                    Mathf.Approximately(component.customScaleRatio, 0.4537f))
                {
                    AutoPopulateValues(component);
                }
            }
            
            // Subscribe to scene GUI
            SceneView.duringSceneGui += OnSceneGUI;
            
            // Subscribe to selection changes to auto-cancel preview
            Selection.selectionChanged += OnSelectionChanged;
        }
        
        private void OnDisable()
        {
            // Unsubscribe from scene GUI
            SceneView.duringSceneGui -= OnSceneGUI;
            
            // Unsubscribe from selection changes
            Selection.selectionChanged -= OnSelectionChanged;
            
            // Cancel preview if active
            if (isPreviewActive)
            {
                // First try using the component reference
                var component = (ImmersiveScalerComponent)target;
                if (component != null)
                {
                    var avatar = component.GetComponentInParent<VRCAvatarDescriptor>();
                    if (avatar != null)
                    {
                        ResetPreview(component, avatar);
                        return;
                    }
                }
                
                // If component is null (being deleted), use stored references
                if (previewAvatar != null)
                {
                    ResetPreviewWithStoredReferences();
                }
            }
        }
        
        private void OnDestroy()
        {
            // This is called when the inspector is being destroyed
            // Make sure to clean up preview if it's still active
            if (isPreviewActive && previewAvatar != null)
            {
                ResetPreviewWithStoredReferences();
            }
        }
        
        private void OnSelectionChanged()
        {
            // Check if selection changed away from our component
            if (isPreviewActive && target != null)
            {
                var component = (ImmersiveScalerComponent)target;
                bool isStillSelected = false;
                
                // Check if our component is still in the selection
                foreach (var obj in Selection.objects)
                {
                    if (obj == component || obj == component.gameObject)
                    {
                        isStillSelected = true;
                        break;
                    }
                }
                
                // If not selected anymore, cancel preview
                if (!isStillSelected)
                {
                    var avatar = component.GetComponentInParent<VRCAvatarDescriptor>();
                    if (avatar != null)
                    {
                        ResetPreview(component, avatar);
                    }
                }
            }
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            var component = (ImmersiveScalerComponent)target;
            if (string.IsNullOrEmpty(component.debugMeasurement)) return;
            if (scalerCore == null) return;
            
            var avatar = component.GetComponentInParent<VRCAvatarDescriptor>();
            if (avatar == null) return;
            
            // Use shared visualization method
            ImmersiveScalerUIShared.DrawMeasurementWithHandles(component.debugMeasurement, scalerCore, paramProvider, avatar);
        }
        
        
        // Remove the old DrawMeasurementWithHandles method - now using shared version
        
        public override void OnInspectorGUI()
        {
            var component = (ImmersiveScalerComponent)target;
            var avatar = component.GetComponentInParent<VRCAvatarDescriptor>();
            
            if (avatar == null)
            {
                EditorGUILayout.HelpBox("This component must be on a VRChat avatar with a VRCAvatarDescriptor!", MessageType.Error);
                return;
            }
            
            // Update scaler core if needed
            if (scalerCore == null || scalerCore.avatarRoot != avatar.gameObject)
            {
                scalerCore = new ImmersiveScalerCore(avatar.gameObject);
            }
            
            serializedObject.Update();
            
            // Current Stats Section
            Vector3 origViewPos = component.hasStoredOriginalViewPosition ? component.originalViewPosition : avatar.ViewPosition;
            ImmersiveScalerUIShared.DrawCurrentStatsSection(paramProvider, scalerCore, avatar, ref showCurrentStats, isPreviewActive, origViewPos);
            
            // Measurement Config Section
            ImmersiveScalerUIShared.DrawMeasurementConfigSection(paramProvider, scalerCore, ref showDebugMeasurements, ref showDebugRatios);
            
            EditorGUILayout.Space();
            
            // Basic Settings
            ImmersiveScalerUIShared.DrawBasicSettingsSection(paramProvider, scalerCore, serializedObject);
            
            EditorGUILayout.Space();
            
            // Body Proportions
            ImmersiveScalerUIShared.DrawBodyProportionsSection(paramProvider, scalerCore, serializedObject);
            
            EditorGUILayout.Space();
            
            // Scaling Options
            ImmersiveScalerUIShared.DrawScalingOptionsSection(paramProvider, serializedObject);
            
            EditorGUILayout.Space();
            
            // Advanced Options
            ImmersiveScalerUIShared.DrawAdvancedOptionsSection(paramProvider, ref showAdvanced, serializedObject);
            
            EditorGUILayout.Space();
            
            // Additional Tools
            ImmersiveScalerUIShared.DrawAdditionalToolsSection(paramProvider, ref showAdditionalTools, serializedObject);
            
            EditorGUILayout.Space();
            
            // Action Buttons
            if (!isPreviewActive)
            {
                // Not in preview mode - show preview button
                GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                if (GUILayout.Button("Preview Scaling", GUILayout.Height(30)))
                {
                    StartPreview(component, avatar);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                // In preview mode - show cancel button
                EditorGUILayout.HelpBox("Preview Mode Active - Showing how avatar will look after build", MessageType.Info);
                
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("Cancel Preview", GUILayout.Height(30)))
                {
                    ResetPreview(component, avatar);
                }
                GUI.backgroundColor = Color.white;
            }
            
            EditorGUILayout.HelpBox("Scaling will be applied automatically when building/uploading the avatar to VRChat.", MessageType.Info);
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void StartPreview(ImmersiveScalerComponent component, VRCAvatarDescriptor avatar)
        {
            // Store references for cleanup
            previewAvatar = avatar;
            storedOriginalViewPosition = avatar.ViewPosition;
            
            PreviewScaling(component, avatar);
        }
        
        private void PreviewScaling(ImmersiveScalerComponent component, VRCAvatarDescriptor avatar)
        {
            // Store original state for manual restoration
            originalTransformStates.Clear();
            Transform[] allTransforms = avatar.GetComponentsInChildren<Transform>();
            foreach (var t in allTransforms)
            {
                originalTransformStates[t] = new TransformState(t);
            }
            
            // Also record for undo as a backup
            Undo.RecordObject(avatar.transform, "Preview Immersive Scaling");
            foreach (var t in allTransforms)
            {
                Undo.RecordObject(t, "Preview Immersive Scaling");
            }
            Undo.RecordObject(avatar, "Preview Immersive Scaling");
            
            // Store original ViewPosition
            if (!component.hasStoredOriginalViewPosition)
            {
                component.originalViewPosition = avatar.ViewPosition;
                component.hasStoredOriginalViewPosition = true;
            }
            
            // Apply scaling
            var scalerCore = new ImmersiveScalerCore(avatar.gameObject);
            
            // Measure original eye height and position
            float originalEyeHeight = scalerCore.GetEyeHeight();
            Vector3 originalEyeLocalPos = scalerCore.GetEyePositionLocal();
            
            // Store original avatar scale
            Vector3 originalAvatarScale = avatar.transform.localScale;
            
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
            
            scalerCore.ScaleAvatar(parameters);
            
            // Update ViewPosition using local space tracking
            Vector3 newEyeLocalPos = scalerCore.GetEyePositionLocal();
            
            // Calculate the actual scale ratio applied to the avatar
            Vector3 newAvatarScale = avatar.transform.localScale;
            float scaleRatio = newAvatarScale.y / originalAvatarScale.y;
            
            // Scale the ViewPosition by the same ratio as the avatar
            avatar.ViewPosition = component.originalViewPosition * scaleRatio;
            
            // Apply additional tools if enabled
            if (component.applyFingerSpreading)
            {
                ImmersiveScalerFingerUtility.SpreadFingers(avatar.gameObject, 
                    component.fingerSpreadFactor, component.spareThumb);
            }
            
            if (component.applyShrinkHipBone)
            {
                ApplyHipBoneFix(avatar);
            }
            
            EditorUtility.SetDirty(avatar);
            EditorUtility.SetDirty(avatar.gameObject);
            
            // Mark preview as active
            isPreviewActive = true;
        }
        
        private void ResetPreview(ImmersiveScalerComponent component, VRCAvatarDescriptor avatar)
        {
            if (!isPreviewActive) return;
            
            // Restore ViewPosition
            if (component.hasStoredOriginalViewPosition)
            {
                avatar.ViewPosition = component.originalViewPosition;
                EditorUtility.SetDirty(avatar);
            }
            
            // Restore all transforms
            foreach (var kvp in originalTransformStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Value.RestoreTo(kvp.Key);
                    EditorUtility.SetDirty(kvp.Key);
                }
            }
            
            originalTransformStates.Clear();
            isPreviewActive = false;
            previewAvatar = null;
            
            EditorUtility.SetDirty(avatar);
            EditorUtility.SetDirty(avatar.gameObject);
        }
        
        private void ResetPreviewWithStoredReferences()
        {
            if (!isPreviewActive || previewAvatar == null) return;
            
            // Restore ViewPosition using stored reference
            previewAvatar.ViewPosition = storedOriginalViewPosition;
            EditorUtility.SetDirty(previewAvatar);
            
            // Restore all transforms
            foreach (var kvp in originalTransformStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Value.RestoreTo(kvp.Key);
                    EditorUtility.SetDirty(kvp.Key);
                }
            }
            
            originalTransformStates.Clear();
            isPreviewActive = false;
            
            EditorUtility.SetDirty(previewAvatar);
            EditorUtility.SetDirty(previewAvatar.gameObject);
            
            previewAvatar = null;
        }
        
        private void AutoPopulateValues(ImmersiveScalerComponent component)
        {
            if (scalerCore == null) return;
            
            // Get current height
            float height = component.scaleEyes ? 
                scalerCore.GetEyeHeight() - scalerCore.GetLowestPoint() :
                scalerCore.GetHighestPoint() - scalerCore.GetLowestPoint();
            component.targetHeight = height;
            
            // Get current upper body percentage using the component's selected methods
            float upperBodyRatio;
            if (component.upperBodyUseLegacy)
            {
                upperBodyRatio = scalerCore.GetUpperBodyPortion();
            }
            else
            {
                upperBodyRatio = scalerCore.GetUpperBodyRatio(component.upperBodyUseNeck, component.upperBodyTorsoUseNeck);
            }
            component.upperBodyPercentage = upperBodyRatio * 100f;
            
            // Get current scale ratio using selected measurement methods
            float armValue = scalerCore.GetArmByMethod(component.armToHeightRatioMethod);
            float heightValue = scalerCore.GetHeightByMethod(component.armToHeightHeightMethod);
            component.customScaleRatio = heightValue > 0 ? armValue / (heightValue - 0.005f) : 0.4537f;
            
            // Get current arm/leg thickness
            component.armThickness = scalerCore.GetCurrentArmThickness() * 100f;
            component.legThickness = scalerCore.GetCurrentLegThickness() * 100f;
            
            // Get current thigh percentage
            component.thighPercentage = scalerCore.GetThighPercentage() * 100f;
            
            EditorUtility.SetDirty(component);
        }
        
        private void ApplyHipBoneFix(VRCAvatarDescriptor avatar)
        {
            if (avatar == null) return;
            
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null || !animator.isHuman) return;
            
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform leftLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform rightLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            
            if (hips == null || spine == null || leftLeg == null || rightLeg == null)
            {
                Debug.LogError("Cannot find required bones for hip shrinking");
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
        
        // Gizmo drawing is now handled by the component itself
    
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmos(ImmersiveScalerComponent component, GizmoType gizmoType)
    {
        if (string.IsNullOrEmpty(component.debugMeasurement)) return;
        
        // Debug to verify this is being called
        // if (component.debugMeasurement != "")
        // {
        //     Debug.Log($"Drawing gizmo for: {component.debugMeasurement}");
        // }
        
        var scalerCore = new ImmersiveScalerCore(component.gameObject);
        var animator = component.GetComponent<Animator>();
        if (animator == null || !animator.isHuman) return;
        
        DrawMeasurementGizmo(component.debugMeasurement, scalerCore, component);
    }
    
    static void DrawGizmoLine(Vector3 start, Vector3 end, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(start, 0.02f);
        Gizmos.DrawSphere(end, 0.02f);
    }
    
    static void DrawMeasurementGizmo(string measurementKey, ImmersiveScalerCore scalerCore, ImmersiveScalerComponent component)
    {
        switch (measurementKey)
        {
            case "current_height":
                {
                    float lowest = scalerCore.GetLowestPoint();
                    Vector3 start = new Vector3(0, lowest, 0);
                    Vector3 end = component.scaleEyes ?
                        new Vector3(0, scalerCore.GetEyeHeight(), 0) :
                        new Vector3(0, scalerCore.GetHighestPoint(), 0);
                    DrawGizmoLine(start, end, Color.green);
                }
                break;
                
            case "eye_height":
            case "eye_height_debug":
                {
                    float lowest = scalerCore.GetLowestPoint();
                    Vector3 start = new Vector3(0, lowest, 0);
                    Vector3 end = new Vector3(0, scalerCore.GetEyeHeight(), 0);
                    DrawGizmoLine(start, end, Color.green);
                }
                break;
                
            case "view_position":
                {
                    var avatar = component.GetComponentInParent<VRCAvatarDescriptor>();
                    if (avatar != null)
                    {
                        Vector3 worldViewPos = avatar.transform.TransformPoint(avatar.ViewPosition);
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(worldViewPos, 0.05f);
                    }
                }
                break;
                
            case "total_height":
                {
                    float lowest = scalerCore.GetLowestPoint();
                    float highest = scalerCore.GetHighestPoint();
                    Vector3 start = new Vector3(0, lowest, 0);
                    Vector3 end = new Vector3(0, highest, 0);
                    DrawGizmoLine(start, end, Color.green);
                }
                break;
                
            case "head_to_hand":
                {
                    var head = scalerCore.GetBone(HumanBodyBones.Head);
                    var rightShoulder = scalerCore.GetBone(HumanBodyBones.RightUpperArm);
                    if (head != null && rightShoulder != null)
                    {
                        float armLength = scalerCore.GetArmLength();
                        Vector3 theoreticalHand = rightShoulder.position;
                        theoreticalHand.x -= armLength;
                        DrawGizmoLine(head.position, theoreticalHand, Color.green);
                    }
                }
                break;
                
            case "arm_length":
                {
                    var shoulder = scalerCore.GetBone(HumanBodyBones.RightUpperArm);
                    var elbow = scalerCore.GetBone(HumanBodyBones.RightLowerArm);
                    var hand = scalerCore.GetBone(HumanBodyBones.RightHand);
                    if (shoulder != null && elbow != null && hand != null)
                    {
                        DrawGizmoLine(shoulder.position, elbow.position, Color.green);
                        DrawGizmoLine(elbow.position, hand.position, Color.green);
                    }
                }
                break;
                
            case "shoulder_to_fingertip":
                {
                    var shoulder = scalerCore.GetBone(HumanBodyBones.RightUpperArm);
                    var hand = scalerCore.GetBone(HumanBodyBones.RightHand);
                    if (shoulder != null && hand != null)
                    {
                        Transform middleTip = VRCBoneMapper.FindFingerBone(hand, "RightMiddleDistal");
                        Vector3 endPoint = middleTip != null ? middleTip.position : hand.position;
                        DrawGizmoLine(shoulder.position, endPoint, Color.green);
                    }
                }
                break;
                
            case "fingertip_to_fingertip":
                {
                    var leftHand = scalerCore.GetBone(HumanBodyBones.LeftHand);
                    var rightHand = scalerCore.GetBone(HumanBodyBones.RightHand);
                    if (leftHand != null && rightHand != null)
                    {
                        Transform leftTip = VRCBoneMapper.FindFingerBone(leftHand, "LeftMiddleDistal");
                        Transform rightTip = VRCBoneMapper.FindFingerBone(rightHand, "RightMiddleDistal");
                        
                        Vector3 leftPoint = leftTip != null ? leftTip.position : leftHand.position;
                        Vector3 rightPoint = rightTip != null ? rightTip.position : rightHand.position;
                        
                        DrawGizmoLine(leftPoint, rightPoint, Color.green);
                    }
                }
                break;
                
            // Ratio measurements - draw both parts
            case "simple_arm_height":
                DrawMeasurementGizmo("arm_length", scalerCore, component);
                Gizmos.color = Color.blue;
                DrawMeasurementGizmo("total_height", scalerCore, component);
                break;
                
            case "arm_eye_height":
                DrawMeasurementGizmo("arm_length", scalerCore, component);
                Gizmos.color = Color.blue;
                DrawMeasurementGizmo("eye_height", scalerCore, component);
                break;
                
            case "current_scale_ratio":
            case "head_tpose_eye_height":
                DrawMeasurementGizmo("head_to_hand", scalerCore, component);
                Gizmos.color = Color.blue;
                DrawMeasurementGizmo("eye_height", scalerCore, component);
                break;
                
            case "shoulder_fingertip_height":
                DrawMeasurementGizmo("shoulder_to_fingertip", scalerCore, component);
                Gizmos.color = Color.blue;
                DrawMeasurementGizmo("total_height", scalerCore, component);
                break;
                
            case "shoulder_fingertip_eye_height":
                DrawMeasurementGizmo("shoulder_to_fingertip", scalerCore, component);
                Gizmos.color = Color.blue;
                DrawMeasurementGizmo("eye_height", scalerCore, component);
                break;
                
            case "upper_body_percent":
                {
                    // Show upper body portion (green)
                    var leftLeg = scalerCore.GetBone(HumanBodyBones.LeftUpperLeg);
                    var rightLeg = scalerCore.GetBone(HumanBodyBones.RightUpperLeg);
                    if (leftLeg != null && rightLeg != null)
                    {
                        float legY = (leftLeg.position.y + rightLeg.position.y) / 2f;
                        float eyeY = scalerCore.GetEyeHeight();
                        DrawGizmoLine(new Vector3(0, legY, 0), new Vector3(0, eyeY, 0), Color.green);
                    }
                    // Show full eye height (blue)
                    Gizmos.color = Color.blue;
                    DrawMeasurementGizmo("eye_height", scalerCore, component);
                }
                break;
        }
    }
    }
    
    // Reset method for when component is first added
    [UnityEditor.Callbacks.DidReloadScripts]
    public static class ImmersiveScalerComponentReset
    {
        [UnityEditor.InitializeOnLoadMethod]
        static void Init()
        {
            UnityEditor.ObjectFactory.componentWasAdded += OnComponentAdded;
        }
        
        static void OnComponentAdded(Component component)
        {
            if (component is ImmersiveScalerComponent scalerComponent)
            {
                var avatar = scalerComponent.GetComponentInParent<VRCAvatarDescriptor>();
                if (avatar != null)
                {
                    var scalerCore = new ImmersiveScalerCore(avatar.gameObject);
                    
                    // Auto-populate values
                    float height = scalerComponent.scaleEyes ? 
                        scalerCore.GetEyeHeight() - scalerCore.GetLowestPoint() :
                        scalerCore.GetHighestPoint() - scalerCore.GetLowestPoint();
                    scalerComponent.targetHeight = height;
                    // Default to legacy method when first adding component
                    scalerComponent.upperBodyUseLegacy = true;
                    scalerComponent.upperBodyPercentage = scalerCore.GetUpperBodyPortion() * 100f;
                    
                    // Calculate scale ratio using default measurement methods
                    float armValue = scalerCore.GetArmByMethod(scalerComponent.armToHeightRatioMethod);
                    float heightValue = scalerCore.GetHeightByMethod(scalerComponent.armToHeightHeightMethod);
                    scalerComponent.customScaleRatio = heightValue > 0 ? armValue / (heightValue - 0.005f) : 0.4537f;
                    scalerComponent.armThickness = scalerCore.GetCurrentArmThickness() * 100f;
                    scalerComponent.legThickness = scalerCore.GetCurrentLegThickness() * 100f;
                    scalerComponent.thighPercentage = scalerCore.GetThighPercentage() * 100f;
                    
                    EditorUtility.SetDirty(scalerComponent);
                }
            }
        }
    }
}