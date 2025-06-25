using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Avatars.Components;

namespace VRChatImmersiveScaler
{
    // Window parameter provider wrapper
    internal class WindowParameterProvider : ImmersiveScalerUIShared.IParameterProvider
    {
        private ScalingParameters parameters;
        private ImmersiveScalerWindow window;
        
        // Measurement method selections from window
        private HeightMethodType _targetHeightMethod;
        private ArmMethodType _armToHeightRatioMethod;
        private HeightMethodType _armToHeightHeightMethod;
        private bool _upperBodyUseNeck;
        private bool _upperBodyTorsoUseNeck;
        private bool _upperBodyUseLegacy;
        private string _debugMeasurement = "";
        
        public WindowParameterProvider(ScalingParameters param, ImmersiveScalerWindow win)
        {
            parameters = param;
            window = win;
            _targetHeightMethod = win.targetHeightMethod;
            _armToHeightRatioMethod = win.armToHeightRatioMethod;
            _armToHeightHeightMethod = win.armToHeightHeightMethod;
            _upperBodyUseNeck = win.upperBodyUseNeck;
            _upperBodyTorsoUseNeck = win.upperBodyTorsoUseNeck;
            _upperBodyUseLegacy = win.upperBodyUseLegacy;
        }
        
        // Basic Settings
        public float targetHeight
        {
            get => parameters.targetHeight;
            set => parameters.targetHeight = value;
        }
        
        public float upperBodyPercentage
        {
            get => parameters.upperBodyPercentage;
            set => parameters.upperBodyPercentage = value;
        }
        
        public float customScaleRatio
        {
            get => parameters.customScaleRatio;
            set => parameters.customScaleRatio = value;
        }
        
        // Body Proportions
        public float armThickness
        {
            get => parameters.armThickness;
            set => parameters.armThickness = value;
        }
        
        public float legThickness
        {
            get => parameters.legThickness;
            set => parameters.legThickness = value;
        }
        
        public float thighPercentage
        {
            get => parameters.thighPercentage;
            set => parameters.thighPercentage = value;
        }
        
        // Scaling Options
        public bool scaleHand
        {
            get => parameters.scaleHand;
            set => parameters.scaleHand = value;
        }
        
        public bool scaleFoot
        {
            get => parameters.scaleFoot;
            set => parameters.scaleFoot = value;
        }
        
        public bool scaleEyes
        {
            get => parameters.scaleEyes;
            set => parameters.scaleEyes = value;
        }
        
        public bool centerModel
        {
            get => parameters.centerModel;
            set => parameters.centerModel = value;
        }
        
        // Advanced Options
        public float extraLegLength
        {
            get => parameters.extraLegLength;
            set => parameters.extraLegLength = value;
        }
        
        public bool scaleRelative
        {
            get => parameters.scaleRelative;
            set => parameters.scaleRelative = value;
        }
        
        public float armToLegs
        {
            get => parameters.armToLegs;
            set => parameters.armToLegs = value;
        }
        
        public bool keepHeadSize
        {
            get => parameters.keepHeadSize;
            set => parameters.keepHeadSize = value;
        }
        
        // Debug Options
        public bool skipMainRescale
        {
            get => parameters.skipAdjust;
            set => parameters.skipAdjust = value;
        }
        
        public bool skipMoveToFloor
        {
            get => parameters.skipFloor;
            set => parameters.skipFloor = value;
        }
        
        public bool skipHeightScaling
        {
            get => parameters.skipScale;
            set => parameters.skipScale = value;
        }
        
        public bool useBoneBasedFloorCalculation
        {
            get => parameters.useBoneBasedFloorCalculation;
            set => parameters.useBoneBasedFloorCalculation = value;
        }
        
        // Additional Tools
        public bool applyFingerSpreading
        {
            get => false; // Window handles this differently
            set { } // No-op
        }
        
        public float fingerSpreadFactor
        {
            get => parameters.fingerSpreadFactor;
            set => parameters.fingerSpreadFactor = value;
        }
        
        public bool spareThumb
        {
            get => parameters.spareThumb;
            set => parameters.spareThumb = value;
        }
        
        public bool applyShrinkHipBone
        {
            get => false; // Window handles this differently
            set { } // No-op
        }
        
        // Measurement methods
        public HeightMethodType targetHeightMethod
        {
            get => _targetHeightMethod;
            set { _targetHeightMethod = value; window.targetHeightMethod = value; }
        }
        
        public ArmMethodType armToHeightRatioMethod
        {
            get => _armToHeightRatioMethod;
            set { _armToHeightRatioMethod = value; window.armToHeightRatioMethod = value; }
        }
        
        public HeightMethodType armToHeightHeightMethod
        {
            get => _armToHeightHeightMethod;
            set { _armToHeightHeightMethod = value; window.armToHeightHeightMethod = value; }
        }
        
        public bool upperBodyUseNeck
        {
            get => _upperBodyUseNeck;
            set { _upperBodyUseNeck = value; window.upperBodyUseNeck = value; }
        }
        
        public bool upperBodyTorsoUseNeck
        {
            get => _upperBodyTorsoUseNeck;
            set { _upperBodyTorsoUseNeck = value; window.upperBodyTorsoUseNeck = value; }
        }
        
        public bool upperBodyUseLegacy
        {
            get => _upperBodyUseLegacy;
            set { _upperBodyUseLegacy = value; window.upperBodyUseLegacy = value; }
        }
        
        // Debug visualization
        public string debugMeasurement
        {
            get => _debugMeasurement;
            set { _debugMeasurement = value; window.debugMeasurement = value; }
        }
        
        public void SetDirty()
        {
            // Window doesn't need SetDirty
        }
    }
    
    public class ImmersiveScalerWindow : EditorWindow
    {
        private GameObject selectedAvatar;
        private ImmersiveScalerCore scalerCore;
        private ScalingParameters parameters = new ScalingParameters();
        private WindowParameterProvider paramProvider;
        
        // UI State
        private bool showCurrentStats = true;
        private bool showMeasurementConfig = false;
        private bool showBodyProportions = false;
        private bool showScalingOptions = false;
        private bool showAdvancedOptions = false;
        private bool showAdditionalTools = false;
        private bool showDebugRatios = false;
        private bool showNDMFOptions = true;
        
        // Visualization
        internal string debugMeasurement = "";
        
        // Measurement method selections
        internal HeightMethodType targetHeightMethod = HeightMethodType.EyeHeight;
        internal ArmMethodType armToHeightRatioMethod = ArmMethodType.HeadToHand;
        internal HeightMethodType armToHeightHeightMethod = HeightMethodType.EyeHeight;
        internal bool upperBodyUseNeck = true;
        internal bool upperBodyTorsoUseNeck = true;
        internal bool upperBodyUseLegacy = true;
        
        // Preview state
        private bool isPreviewActive = false;
        private Vector3 originalViewPosition;
        private Dictionary<Transform, TransformState> originalTransformStates = new Dictionary<Transform, TransformState>();
        private VRCAvatarDescriptor previewDescriptor;
        
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
        
        // Check if NDMF is available
        private static bool IsNDMFAvailable()
        {
#if NDMF_AVAILABLE
            return true;
#else
            return false;
#endif
        }
        
        [MenuItem("Tools/Immersive Avatar Scaler")]
        public static void ShowWindow()
        {
            var window = GetWindow<ImmersiveScalerWindow>("Immersive Avatar Scaler");
            window.minSize = new Vector2(400, 600);
        }
        
        private void OnDestroy()
        {
            // Cancel preview if window is closed while in preview mode
            if (isPreviewActive)
            {
                CancelPreview();
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Immersive Avatar Scaler", titleStyle);
            EditorGUILayout.Space(10);
            
            // Initialize parameter provider if needed
            if (paramProvider == null)
            {
                paramProvider = new WindowParameterProvider(parameters, this);
            }
            
            // Avatar Selection
            DrawAvatarSelection();
            
            if (selectedAvatar == null)
            {
                EditorGUILayout.HelpBox("Please select a VRChat avatar with a Humanoid animator.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.Space(10);
            
            // Only show the rest of the UI if not showing NDMF options
            if (!showNDMFOptions)
            {
            
            // Draw visualization in scene
            if (!string.IsNullOrEmpty(debugMeasurement) && scalerCore != null)
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                SceneView.duringSceneGui += OnSceneGUI;
            }
            
            var avatar = selectedAvatar?.GetComponent<VRCAvatarDescriptor>();
            if (avatar != null)
            {
                // Current Stats Section
                ImmersiveScalerUIShared.DrawCurrentStatsSection(paramProvider, scalerCore, avatar, ref showCurrentStats);
                
                EditorGUILayout.Space(5);
                
                // Measurement Config Section
                ImmersiveScalerUIShared.DrawMeasurementConfigSection(paramProvider, scalerCore, ref showMeasurementConfig, ref showDebugRatios);
            }
            
            EditorGUILayout.Space(5);
            
            // Basic Settings
            ImmersiveScalerUIShared.DrawBasicSettingsSection(paramProvider, scalerCore);
            
            EditorGUILayout.Space(5);
            
            // Body Proportions - show as foldout in window
            showBodyProportions = EditorGUILayout.Foldout(showBodyProportions, "Body Proportions", true);
            if (showBodyProportions)
            {
                ImmersiveScalerUIShared.DrawBodyProportionsSection(paramProvider, scalerCore);
            }
            
            EditorGUILayout.Space(5);
            
            // Scaling Options - show as foldout in window
            showScalingOptions = EditorGUILayout.Foldout(showScalingOptions, "Scaling Options", true);
            if (showScalingOptions)
            {
                ImmersiveScalerUIShared.DrawScalingOptionsSection(paramProvider);
            }
            
            EditorGUILayout.Space(5);
            
            // Advanced Options
            ImmersiveScalerUIShared.DrawAdvancedOptionsSection(paramProvider, ref showAdvancedOptions);
            
            EditorGUILayout.Space(10);
            
            // Action Buttons
            DrawActionButtons();
            
            EditorGUILayout.Space(10);
            
            // Additional Tools - custom implementation for window
            DrawAdditionalTools();
            }
        }
        
        private void DrawAvatarSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Avatar Selection", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            selectedAvatar = (GameObject)EditorGUILayout.ObjectField(
                "Avatar", 
                selectedAvatar, 
                typeof(GameObject), 
                true
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                ValidateAvatar();
            }
            
            EditorGUILayout.EndVertical();
            
            // Show NDMF options if avatar is selected
            if (selectedAvatar != null && scalerCore != null)
            {
                EditorGUILayout.Space(10);
                DrawNDMFOptions();
            }
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            if (string.IsNullOrEmpty(debugMeasurement)) return;
            if (scalerCore == null) return;
            
            var avatar = selectedAvatar?.GetComponent<VRCAvatarDescriptor>();
            if (avatar == null) return;
            
            // Use shared visualization method
            ImmersiveScalerUIShared.DrawMeasurementWithHandles(debugMeasurement, scalerCore, paramProvider, avatar);
        }
        
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            
            // Cancel preview if window loses focus or is closed
            if (isPreviewActive)
            {
                CancelPreview();
            }
        }
        
        
        
        
        
        
        
        
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            parameters.centerModel = EditorGUILayout.Toggle(
                new GUIContent("Center Model", "Move avatar to X=0, Z=0"),
                parameters.centerModel
            );
            
            EditorGUILayout.Space(5);
            
            if (!isPreviewActive)
            {
                // Not in preview mode - show preview and reset buttons
                EditorGUILayout.BeginHorizontal();
                
                GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                if (GUILayout.Button("Preview Scaling", GUILayout.Height(30)))
                {
                    StartPreview();
                }
                
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("Reset Scales", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    ResetScales();
                }
                
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // In preview mode - show apply and cancel buttons
                EditorGUILayout.HelpBox("Preview Mode Active - Changes are temporary", MessageType.Info);
                
                EditorGUILayout.BeginHorizontal();
                
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                if (GUILayout.Button("Apply Changes", GUILayout.Height(30)))
                {
                    ApplyPreview();
                }
                
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("Cancel Preview", GUILayout.Height(30)))
                {
                    CancelPreview();
                }
                
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAdditionalTools()
        {
            showAdditionalTools = EditorGUILayout.Foldout(showAdditionalTools, "Additional Tools", true);
            if (showAdditionalTools)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField("Finger Spreading", EditorStyles.boldLabel);
                
                paramProvider.spareThumb = EditorGUILayout.Toggle(
                    new GUIContent("Ignore Thumb", "Don't spread the thumb"),
                    paramProvider.spareThumb
                );
                
                paramProvider.fingerSpreadFactor = EditorGUILayout.Slider(
                    new GUIContent("Spread Factor", "How much to spread fingers apart"),
                    paramProvider.fingerSpreadFactor, 0f, 2f
                );
                
                if (GUILayout.Button("Apply Finger Spreading"))
                {
                    SpreadFingers();
                }
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.LabelField("Hip Bone Fix", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Shrink Hip Bone"))
                {
                    ShrinkHipBone();
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawNDMFOptions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (IsNDMFAvailable())
            {
                EditorGUILayout.LabelField("Non-Destructive Setup Available", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);
                
                // Check if component already exists
                var existingComponent = selectedAvatar.GetComponentInChildren<ImmersiveScalerComponent>();
                if (existingComponent != null)
                {
                    EditorGUILayout.HelpBox("Immersive Scaler component already exists on this avatar.", MessageType.Info);
                    
                    GUI.backgroundColor = Color.cyan;
                    if (GUILayout.Button("Select Component", GUILayout.Height(30)))
                    {
                        Selection.activeGameObject = existingComponent.gameObject;
                        EditorGUIUtility.PingObject(existingComponent);
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Add as NDMF Component (Recommended)", GUILayout.Height(40)))
                    {
                        AddNDMFComponent();
                    }
                    GUI.backgroundColor = Color.white;
                }
                
                EditorGUILayout.Space(10);
                
                GUI.backgroundColor = Color.yellow;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Use Destructive Version Instead", GUILayout.Height(30)))
                {
                    showNDMFOptions = false;
                }
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.HelpBox("⚠️ The destructive version makes permanent changes to your avatar. Make sure to duplicate your avatar before using it!", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Destructive Mode Only", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("NDMF is not installed in this project. The tool will make permanent changes to your avatar.", MessageType.Warning);
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("⚠️ It is highly recommended to duplicate your avatar before proceeding as changes cannot be undone once the scene is saved!", MessageType.Warning);
                EditorGUILayout.Space(5);
                
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Continue with Destructive Version", GUILayout.Height(30)))
                {
                    showNDMFOptions = false;
                }
                GUI.backgroundColor = Color.white;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void AddNDMFComponent()
        {
            // Find or create a child object for the component
            Transform componentHolder = selectedAvatar.transform.Find("ImmersiveScaler");
            if (componentHolder == null)
            {
                GameObject holder = new GameObject("ImmersiveScaler");
                holder.transform.SetParent(selectedAvatar.transform);
                holder.transform.localPosition = Vector3.zero;
                holder.transform.localRotation = Quaternion.identity;
                holder.transform.localScale = Vector3.one;
                componentHolder = holder.transform;
            }
            
            // Add the component
            var component = componentHolder.gameObject.AddComponent<ImmersiveScalerComponent>();
            
            // Auto-populate values
            if (scalerCore != null)
            {
                AutoPopulateComponent(component);
            }
            
            // Select the component
            Selection.activeGameObject = componentHolder.gameObject;
            EditorGUIUtility.PingObject(component);
            
            Debug.Log("Added Immersive Scaler component to avatar. The component will be applied non-destructively during avatar build.");
        }
        
        private void AutoPopulateComponent(ImmersiveScalerComponent component)
        {
            // Get current height
            float height = parameters.scaleEyes ? 
                scalerCore.GetEyeHeight() - scalerCore.GetLowestPoint() :
                scalerCore.GetHighestPoint() - scalerCore.GetLowestPoint();
            component.targetHeight = height;
            
            // Set legacy mode and get current upper body percentage
            component.upperBodyUseLegacy = upperBodyUseLegacy;
            component.upperBodyPercentage = upperBodyUseLegacy ? 
                scalerCore.GetUpperBodyPortion() * 100f :
                scalerCore.GetUpperBodyRatio(upperBodyUseNeck, upperBodyTorsoUseNeck) * 100f;
            
            // Calculate scale ratio using measurement methods
            float armValue = scalerCore.GetArmByMethod(armToHeightRatioMethod);
            float heightValue = scalerCore.GetHeightByMethod(armToHeightHeightMethod);
            component.customScaleRatio = heightValue > 0 ? armValue / (heightValue - 0.005f) : 0.4537f;
            
            // Copy measurement method settings
            component.targetHeightMethod = targetHeightMethod;
            component.armToHeightRatioMethod = armToHeightRatioMethod;
            component.armToHeightHeightMethod = armToHeightHeightMethod;
            component.upperBodyUseNeck = upperBodyUseNeck;
            component.upperBodyTorsoUseNeck = upperBodyTorsoUseNeck;
            
            // Copy other parameters
            component.armThickness = scalerCore.GetCurrentArmThickness() * 100f;
            component.legThickness = scalerCore.GetCurrentLegThickness() * 100f;
            component.thighPercentage = scalerCore.GetThighPercentage() * 100f;
            component.scaleEyes = parameters.scaleEyes;
            
            // Mark as dirty
            EditorUtility.SetDirty(component);
        }
        
        private void ValidateAvatar()
        {
            // Cancel any active preview when changing avatars
            if (isPreviewActive)
            {
                CancelPreview();
            }
            
            if (selectedAvatar == null)
            {
                scalerCore = null;
                showNDMFOptions = true;
                return;
            }
            
            // Check for VRCAvatarDescriptor
            var descriptor = selectedAvatar.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null)
            {
                Debug.LogError("Selected object must be a VRChat avatar with VRCAvatarDescriptor!");
                selectedAvatar = null;
                scalerCore = null;
                showNDMFOptions = true;
                return;
            }
            
            Animator animator = selectedAvatar.GetComponent<Animator>();
            if (animator == null || !animator.isHuman)
            {
                Debug.LogError("Selected avatar must have a Humanoid animator!");
                selectedAvatar = null;
                scalerCore = null;
                showNDMFOptions = true;
                return;
            }
            
            scalerCore = new ImmersiveScalerCore(selectedAvatar);
            showNDMFOptions = true; // Reset to show NDMF options for new avatar
            
            // Auto-populate values when avatar is selected
            AutoPopulateValues();
        }
        
        private void AutoPopulateValues()
        {
            if (scalerCore == null) return;
            
            // Get current values
            parameters.targetHeight = scalerCore.GetHeightByMethod(targetHeightMethod);
            parameters.upperBodyPercentage = upperBodyUseLegacy ? 
                scalerCore.GetUpperBodyPortion() * 100f :
                scalerCore.GetUpperBodyRatio(upperBodyUseNeck, upperBodyTorsoUseNeck) * 100f;
            
            // Calculate arm ratio
            float armValue = scalerCore.GetArmByMethod(armToHeightRatioMethod);
            float heightValue = scalerCore.GetHeightByMethod(armToHeightHeightMethod);
            parameters.customScaleRatio = heightValue > 0 ? armValue / (heightValue - 0.005f) : 0.4537f;
            
            parameters.thighPercentage = scalerCore.GetThighPercentage() * 100f;
            parameters.armThickness = scalerCore.GetCurrentArmThickness() * 100f;
            parameters.legThickness = scalerCore.GetCurrentLegThickness() * 100f;
        }
        
        private void StartPreview()
        {
            if (scalerCore == null || selectedAvatar == null)
            {
                Debug.LogError("No valid avatar selected!");
                return;
            }
            
            var descriptor = selectedAvatar.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null)
            {
                Debug.LogError("No VRCAvatarDescriptor found on avatar!");
                return;
            }
            
            // Store references for cleanup
            previewDescriptor = descriptor;
            
            // Store original state
            originalViewPosition = descriptor.ViewPosition;
            originalTransformStates.Clear();
            
            Transform[] allTransforms = selectedAvatar.GetComponentsInChildren<Transform>();
            foreach (var t in allTransforms)
            {
                originalTransformStates[t] = new TransformState(t);
            }
            
            // Apply scaling
            RescaleAvatar();
            
            isPreviewActive = true;
            Repaint();
        }
        
        private void ApplyPreview()
        {
            // Simply exit preview mode - changes are already applied
            isPreviewActive = false;
            originalTransformStates.Clear();
            previewDescriptor = null;
            Repaint();
        }
        
        private void CancelPreview()
        {
            if (!isPreviewActive) return;
            
            // Try to get descriptor from selected avatar first
            var descriptor = selectedAvatar != null ? selectedAvatar.GetComponent<VRCAvatarDescriptor>() : null;
            
            // Fall back to stored descriptor if needed
            if (descriptor == null && previewDescriptor != null)
            {
                descriptor = previewDescriptor;
            }
            
            if (descriptor != null)
            {
                descriptor.ViewPosition = originalViewPosition;
                EditorUtility.SetDirty(descriptor);
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
            
            isPreviewActive = false;
            originalTransformStates.Clear();
            previewDescriptor = null;
            
            if (selectedAvatar != null)
            {
                EditorUtility.SetDirty(selectedAvatar);
            }
            
            Repaint();
        }
        
        private void RescaleAvatar()
        {
            if (scalerCore == null)
            {
                Debug.LogError("No valid avatar selected!");
                return;
            }
            
            var descriptor = selectedAvatar.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null)
            {
                Debug.LogError("No VRCAvatarDescriptor found on avatar!");
                return;
            }
            
            Undo.RecordObject(selectedAvatar.transform, "Rescale Avatar");
            Undo.RecordObject(descriptor, "Rescale Avatar");
            
            // Record all child transforms for undo
            Transform[] allTransforms = selectedAvatar.GetComponentsInChildren<Transform>();
            foreach (var t in allTransforms)
            {
                Undo.RecordObject(t, "Rescale Avatar");
            }
            
            // Measure original eye height and position before scaling
            float originalEyeHeight = scalerCore.GetEyeHeight();
            Vector3 originalEyeLocalPos = scalerCore.GetEyePositionLocal();
            
            // Store original avatar scale
            Vector3 originalAvatarScale = selectedAvatar.transform.localScale;
            
            // Pass measurement configuration to parameters
            parameters.targetHeightMethod = targetHeightMethod;
            parameters.armToHeightRatioMethod = armToHeightRatioMethod;
            parameters.armToHeightHeightMethod = armToHeightHeightMethod;
            parameters.upperBodyUseNeck = upperBodyUseNeck;
            parameters.upperBodyTorsoUseNeck = upperBodyTorsoUseNeck;
            parameters.upperBodyUseLegacy = upperBodyUseLegacy;
            
            scalerCore.ScaleAvatar(parameters);
            
            // Update ViewPosition using local space tracking
            Vector3 newEyeLocalPos = scalerCore.GetEyePositionLocal();
            
            // Calculate the actual scale ratio applied to the avatar
            Vector3 newAvatarScale = selectedAvatar.transform.localScale;
            float scaleRatio = newAvatarScale.y / originalAvatarScale.y;
            
            // Scale the ViewPosition by the same ratio as the avatar
            descriptor.ViewPosition = originalViewPosition * scaleRatio;
            
            EditorUtility.SetDirty(selectedAvatar);
            EditorUtility.SetDirty(descriptor);
        }
        
        
        // Original methods updated
        private void SpreadFingers()
        {
            if (selectedAvatar == null) return;
            
            Undo.RecordObject(selectedAvatar.transform, "Spread Fingers");
            
            Transform[] allTransforms = selectedAvatar.GetComponentsInChildren<Transform>();
            foreach (var t in allTransforms)
            {
                Undo.RecordObject(t, "Spread Fingers");
            }
            
            // Use the utility method to spread fingers
            ImmersiveScalerFingerUtility.SpreadFingers(selectedAvatar, paramProvider.fingerSpreadFactor, paramProvider.spareThumb);
            
            EditorUtility.SetDirty(selectedAvatar);
        }
        
        private void ShrinkHipBone()
        {
            if (selectedAvatar == null) return;
            
            Animator animator = selectedAvatar.GetComponent<Animator>();
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
            
            Undo.RecordObject(hips, "Shrink Hip Bone");
            
            float legStartY = (leftLeg.position.y + rightLeg.position.y) / 2f;
            float spineStartY = spine.position.y;
            
            // Move hip 90% of the way between legs and spine
            Vector3 newPosition = hips.position;
            newPosition.y = legStartY + (spineStartY - legStartY) * 0.9f;
            newPosition.x = spine.position.x;
            newPosition.z = spine.position.z;
            
            hips.position = newPosition;
            
            EditorUtility.SetDirty(selectedAvatar);
        }
        
        private void ResetScales()
        {
            if (selectedAvatar == null) return;
            
            Undo.RecordObject(selectedAvatar.transform, "Reset Avatar Scales");
            
            Transform[] allTransforms = selectedAvatar.GetComponentsInChildren<Transform>();
            foreach (var t in allTransforms)
            {
                Undo.RecordObject(t, "Reset Avatar Scales");
            }
            
            ImmersiveScalerUtilities.ResetAvatarScales(selectedAvatar);
            
            EditorUtility.SetDirty(selectedAvatar);
        }
    }
}