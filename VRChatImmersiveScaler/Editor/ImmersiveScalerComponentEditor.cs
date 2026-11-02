using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;
using VRChatImmersiveScaler.Editor.EditorUI;
using Kittyn.Tools.ImmersiveScaler;

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
        private bool showMeasurementOverrides = false;

        // Icon display
        private Texture2D _iconTexture;
        private Texture2D _iconBackground;

        // Preview state tracking
        private bool isPreviewActive = false;
        private Dictionary<Transform, TransformState> originalTransformStates = new Dictionary<Transform, TransformState>();
        private VRCAvatarDescriptor previewAvatar = null;
        private Vector3 storedOriginalViewPosition;
        private readonly ImmersiveScalerDebugPanel debugPanel = new ImmersiveScalerDebugPanel();

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

            // Load icon using shared utility
            if (_iconTexture == null)
            {
                _iconTexture = KittynIconUtility.LoadIcon("ImmersiveScaler",
                    "Packages/cat.kittyn.immersive-scaler/Editor/Icons/ImmersiveScaler.png",
                    "Assets/kittyncat_tools/cat.kittyn.immersive-scaler/Editor/Icons/ImmersiveScaler.png");
            }

            // Create icon background using shared utility
            if (_iconBackground == null)
            {
                _iconBackground = KittynIconUtility.CreateColorTexture(KittynEditorTheme.IconBackgroundColor);
            }

            var avatar = component.GetComponentInParent<VRCAvatarDescriptor>();
            if (avatar != null)
            {
                scalerCore = new ImmersiveScalerCore(avatar.gameObject);
                scalerCore.SetMeasurementRendererOverrides(
                    component.useMeasurementRendererOverrides,
                    component.measurementBodyRenderers,
                    component.measurementHeadRenderers
                );

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

        private void DrawCustomHeader()
        {
            // Create a style for the header
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            EditorGUILayout.Space(5);

            // Draw header with icon
            EditorGUILayout.BeginHorizontal();

            // Draw icon on the left
            if (_iconTexture != null)
            {
                KittynIconUtility.DisplayIcon(_iconTexture, 64, KittynEditorTheme.IconBackgroundColor);
            }

            // Draw title text next to icon
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(KittynLocalization.Get("immersive_scaler.comp_header"), headerStyle, GUILayout.Height(25));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Draw a separator line
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(5);
        }

        public override void OnInspectorGUI()
        {
            // Draw custom header
            DrawCustomHeader();

            var component = (ImmersiveScalerComponent)target;
            var avatar = component.GetComponentInParent<VRCAvatarDescriptor>();

            if (avatar == null)
            {
                EditorGUILayout.HelpBox(KittynLocalization.Get("immersive_scaler.comp_error_requires_avatar"), MessageType.Error);
                return;
            }

            // Update scaler core if needed
            if (scalerCore == null || scalerCore.avatarRoot != avatar.gameObject)
            {
                scalerCore = new ImmersiveScalerCore(avatar.gameObject);
            }

            serializedObject.Update();
            ApplyMeasurementOverridesToScalerCore();

            // Current Stats Section
            Vector3 origViewPos = isPreviewActive ? storedOriginalViewPosition : avatar.ViewPosition;
            ImmersiveScalerUIShared.DrawCurrentStatsSection(paramProvider, scalerCore, avatar, ref showCurrentStats, isPreviewActive, origViewPos);

            // Measurement Config Section
            ImmersiveScalerUIShared.DrawMeasurementConfigSection(paramProvider, scalerCore, ref showDebugMeasurements, ref showDebugRatios);

            // Measurement Overrides Section
            DrawMeasurementOverridesSection(component);

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
                if (GUILayout.Button(KittynLocalization.Get("immersive_scaler.comp_preview_scaling"), GUILayout.Height(30)))
                {
                    StartPreview(component, avatar);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                // In preview mode - show cancel button
                EditorGUILayout.HelpBox(KittynLocalization.Get("immersive_scaler.comp_preview_active"), MessageType.Info);

                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button(KittynLocalization.Get("immersive_scaler.comp_cancel_preview"), GUILayout.Height(30)))
                {
                    ResetPreview(component, avatar);
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.HelpBox(KittynLocalization.Get("immersive_scaler.comp_build_info"), MessageType.Info);

            bool settingsChanged = serializedObject.ApplyModifiedProperties();
            if (settingsChanged || GUI.changed) debugPanel.InvalidateCurrent();
            debugPanel.Draw(() => ImmersiveScalerDiagnostics.Snapshot(avatar, scalerCore,
                ImmersiveScalerDiagnostics.ParametersFrom(component),
                isPreviewActive ? "Current state: preview is active" : "Current state: edit mode",
                ImmersiveScalerDiagnostics.DescribeTools(component)));
        }

        private void ApplyMeasurementOverridesToScalerCore()
        {
            if (scalerCore == null) return;

            var enabledProp = serializedObject.FindProperty("useMeasurementRendererOverrides");
            var bodyProp = serializedObject.FindProperty("measurementBodyRenderers");
            var headProp = serializedObject.FindProperty("measurementHeadRenderers");
            if (enabledProp == null || bodyProp == null || headProp == null) return;

            bool enabled = enabledProp.boolValue;

            var body = new List<SkinnedMeshRenderer>();
            for (int i = 0; i < bodyProp.arraySize; i++)
            {
                if (bodyProp.GetArrayElementAtIndex(i).objectReferenceValue is SkinnedMeshRenderer smr)
                {
                    body.Add(smr);
                }
            }

            var head = new List<SkinnedMeshRenderer>();
            for (int i = 0; i < headProp.arraySize; i++)
            {
                if (headProp.GetArrayElementAtIndex(i).objectReferenceValue is SkinnedMeshRenderer smr)
                {
                    head.Add(smr);
                }
            }

            scalerCore.SetMeasurementRendererOverrides(enabled, body, head);
        }

        private void DrawMeasurementOverridesSection(ImmersiveScalerComponent component)
        {
            showMeasurementOverrides = EditorGUILayout.Foldout(showMeasurementOverrides, KittynLocalization.Get("immersive_scaler.measurement_overrides"), true);
            if (!showMeasurementOverrides) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var useOverridesProp = serializedObject.FindProperty("useMeasurementRendererOverrides");
            var bodyProp = serializedObject.FindProperty("measurementBodyRenderers");
            var headProp = serializedObject.FindProperty("measurementHeadRenderers");

            useOverridesProp.boolValue = EditorGUILayout.Toggle(
                new GUIContent(
                    KittynLocalization.Get("immersive_scaler.field_use_measurement_renderer_overrides"),
                    KittynLocalization.Get("immersive_scaler.use_measurement_renderer_overrides_tooltip")
                ),
                useOverridesProp.boolValue
            );

            if (useOverridesProp.boolValue)
            {
                EditorGUILayout.HelpBox(KittynLocalization.Get("immersive_scaler.measurement_overrides_help"), MessageType.Info);

                bool hasBodyRenderer = false;
                for (int i = 0; i < bodyProp.arraySize; i++)
                {
                    if (bodyProp.GetArrayElementAtIndex(i).objectReferenceValue != null)
                    {
                        hasBodyRenderer = true;
                        break;
                    }
                }

                if (!hasBodyRenderer)
                {
                    EditorGUILayout.HelpBox(KittynLocalization.Get("immersive_scaler.measurement_overrides_missing_body_warning"), MessageType.Warning);
                }

                EditorGUILayout.PropertyField(
                    bodyProp,
                    new GUIContent(
                        KittynLocalization.Get("immersive_scaler.field_measurement_body_renderers"),
                        KittynLocalization.Get("immersive_scaler.measurement_body_renderers_tooltip")
                    ),
                    true
                );
                EditorGUILayout.PropertyField(
                    headProp,
                    new GUIContent(
                        KittynLocalization.Get("immersive_scaler.field_measurement_head_renderers"),
                        KittynLocalization.Get("immersive_scaler.measurement_head_renderers_tooltip")
                    ),
                    true
                );
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void StartPreview(ImmersiveScalerComponent component, VRCAvatarDescriptor avatar)
        {
            // Store references for cleanup
            previewAvatar = avatar;
            storedOriginalViewPosition = avatar.ViewPosition;

            try
            {
                PreviewScaling(component, avatar);
            }
            catch
            {
                ResetPreview(component, avatar);
                throw;
            }
        }

        private void PreviewScaling(ImmersiveScalerComponent component, VRCAvatarDescriptor avatar)
        {
            // Store original state for manual restoration
            originalTransformStates.Clear();
            Transform[] allTransforms = avatar.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                originalTransformStates[t] = new TransformState(t);
            }

            isPreviewActive = true;

            // Also record for undo as a backup
            Undo.RecordObject(avatar.transform, "Preview Immersive Scaling");
            foreach (var t in allTransforms)
            {
                Undo.RecordObject(t, "Preview Immersive Scaling");
            }
            Undo.RecordObject(avatar, "Preview Immersive Scaling");

            // Apply scaling
            var scalerCore = new ImmersiveScalerCore(avatar.gameObject);
            scalerCore.SetMeasurementRendererOverrides(
                component.useMeasurementRendererOverrides,
                component.measurementBodyRenderers,
                component.measurementHeadRenderers
            );

            var parameters = ImmersiveScalerDiagnostics.ParametersFrom(component);
            using (var report = new ImmersiveScalerDiagnostics(avatar, scalerCore, parameters,
                "Component preview", ImmersiveScalerDiagnostics.DescribeTools(component)))
            {
                Vector3 eyeOffset = ImmersiveScalerViewpoint.CaptureEyeOffset(avatar, scalerCore);
                report.Record("Original eye offset (avatar-local): " + eyeOffset.ToString("F6"));
                scalerCore.ScaleAvatar(parameters);

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

                ImmersiveScalerViewpoint.Reconcile(avatar, scalerCore, eyeOffset);
                report.Record("Viewpoint reconciled after scaling and additional tools.");
                debugPanel.SetPreview(report.Complete());
            }
            EditorUtility.SetDirty(avatar);
            EditorUtility.SetDirty(avatar.gameObject);
        }

        private void ResetPreview(ImmersiveScalerComponent component, VRCAvatarDescriptor avatar)
        {
            if (!isPreviewActive) return;

            avatar.ViewPosition = storedOriginalViewPosition;
            EditorUtility.SetDirty(avatar);

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
            debugPanel.InvalidateCurrent();
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
            debugPanel.InvalidateCurrent();

            EditorUtility.SetDirty(previewAvatar);
            EditorUtility.SetDirty(previewAvatar.gameObject);

            previewAvatar = null;
        }

        private void AutoPopulateValues(ImmersiveScalerComponent component)
        {
            if (scalerCore == null) return;

            // Get current height
            float floor = scalerCore.GetLowestPoint(component.useBoneBasedFloorCalculation);
            float height = component.scaleEyes ?
                scalerCore.GetEyeHeight() - floor :
                scalerCore.GetHighestPoint() - floor;
            component.targetHeight = height;

            // Get current upper body percentage using the component's selected methods
            float upperBodyRatio;
            if (component.upperBodyUseLegacy)
            {
                upperBodyRatio = scalerCore.GetUpperBodyPortion(component.useBoneBasedFloorCalculation);
            }
            else
            {
                upperBodyRatio = scalerCore.GetUpperBodyRatio(component.upperBodyUseNeck, component.upperBodyTorsoUseNeck, component.useBoneBasedFloorCalculation);
            }
            component.upperBodyPercentage = upperBodyRatio * 100f;

            // Get current scale ratio using selected measurement methods
            float armValue = scalerCore.GetArmByMethod(component.armToHeightRatioMethod);
            float heightValue = scalerCore.GetHeightByMethod(component.armToHeightHeightMethod, component.useBoneBasedFloorCalculation);
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
                Debug.LogError(KittynLocalization.Get("immersive_scaler.error_cannot_find_bones_hip_shrinking"));
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

    // Gizmo handling moved to ImmersiveScalerGizmos.cs

    // Helper methods for gizmo drawing - commented out
    /*
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
                        Vector3 worldViewPos = ImmersiveScalerViewpoint.GetWorldPosition(avatar);
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
    */
    }

    // Reset method for when component is first added
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
                    float floor = scalerCore.GetLowestPoint(scalerComponent.useBoneBasedFloorCalculation);
                    float height = scalerComponent.scaleEyes ?
                        scalerCore.GetEyeHeight() - floor :
                        scalerCore.GetHighestPoint() - floor;
                    scalerComponent.targetHeight = height;
                    // Default to legacy method when first adding component
                    scalerComponent.upperBodyUseLegacy = true;
                    scalerComponent.upperBodyPercentage = scalerCore.GetUpperBodyPortion(scalerComponent.useBoneBasedFloorCalculation) * 100f;

                    // Calculate scale ratio using default measurement methods
                    float armValue = scalerCore.GetArmByMethod(scalerComponent.armToHeightRatioMethod);
                    float heightValue = scalerCore.GetHeightByMethod(scalerComponent.armToHeightHeightMethod, scalerComponent.useBoneBasedFloorCalculation);
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
