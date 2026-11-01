using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Kittyn.Tools.ImmersiveScaler;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace VRChatImmersiveScaler
{
    /// <summary>Captures the measurements and the actual scaling trace without serializing diagnostic state on avatars.</summary>
    public sealed class ImmersiveScalerDiagnostics : IDisposable
    {
        private const string BuildReportKey = "ImmersiveScaler.LastBuildDebugReport";
        private readonly StringBuilder text = new StringBuilder();
        private readonly ImmersiveScalerCore core;
        private readonly VRCAvatarDescriptor descriptor;
        private readonly ScalingParameters parameters;
        private readonly Action<string> previousSink;
        private bool disposed;

        public static string LastBuildReport
        {
            get => SessionState.GetString(BuildReportKey, "");
            set => SessionState.SetString(BuildReportKey, value ?? "");
        }

        public ImmersiveScalerDiagnostics(VRCAvatarDescriptor descriptor, ImmersiveScalerCore core,
            ScalingParameters parameters, string context, string tools = "")
        {
            this.descriptor = descriptor;
            this.core = core;
            this.parameters = parameters;
            previousSink = core.DiagnosticLog;
            text.AppendLine("Immersive Scaler Debug Report");
            text.AppendLine("Captured UTC: " + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            text.AppendLine("Context: " + context);
            text.AppendLine("Avatar: " + descriptor.name);
            text.AppendLine("Unity: " + Application.unityVersion);
            var packages = PackageInfo.GetAllRegisteredPackages();
            foreach (string name in new[] { "cat.kittyn.immersive-scaler", "com.vrchat.avatars", "nadena.dev.ndmf", "nadena.dev.modular-avatar" })
            {
                var package = packages.FirstOrDefault(p => p.name == name);
                text.AppendLine(name + ": " + (package != null ? package.version : "not registered (may be installed under Assets)"));
            }
            text.AppendLine("ViewPosition convention: world-distance vector from the avatar root; world viewpoint = root.position + ViewPosition.");
            text.AppendLine("Eye offset is preserved in avatar-local space and transformed back after all scaling/tools.");
            text.AppendLine();
            text.AppendLine("SETTINGS");
            foreach (var field in typeof(ScalingParameters).GetFields().OrderBy(f => f.Name))
                text.AppendLine(field.Name + " = " + Convert.ToString(field.GetValue(parameters), CultureInfo.InvariantCulture));
            text.AppendLine("targetHeightMethod selects height scaling; scaleEyes is a legacy serialized field.");
            if (!string.IsNullOrEmpty(tools)) text.AppendLine(tools);
            core.DiagnosticLog = Record;
            try
            {
                AppendState("INPUT STATE");
            }
            catch
            {
                core.DiagnosticLog = previousSink;
                throw;
            }
            text.AppendLine();
            text.AppendLine("CALCULATION TRACE (captured from the scaling operations)");
        }

        public void Record(string message)
        {
            text.AppendLine(message);
            previousSink?.Invoke(message);
        }

        public string Complete(string result = "Completed")
        {
            text.AppendLine("Result: " + result);
            AppendState("OUTPUT STATE");
            return text.ToString();
        }

        public static string Snapshot(VRCAvatarDescriptor descriptor, ImmersiveScalerCore core,
            ScalingParameters parameters, string context, string tools = "")
        {
            using (var report = new ImmersiveScalerDiagnostics(descriptor, core, parameters, context, tools))
            {
                report.Record("Read-only snapshot: scaling was not run. Use Preview Scaling or a build to capture the actual sequence.");
                return report.text.ToString();
            }
        }

        public static string DescribeTools(ImmersiveScalerComponent component)
        {
            return $"Component enabled={component.enabled}, activeInHierarchy={component.gameObject.activeInHierarchy}\n" +
                $"Finger spreading={component.applyFingerSpreading}, factor={F(component.fingerSpreadFactor)}, spareThumb={component.spareThumb}\n" +
                $"Hip adjustment={component.applyShrinkHipBone}";
        }

        public static ScalingParameters ParametersFrom(ImmersiveScalerComponent c)
        {
            return new ScalingParameters
            {
                targetHeight = c.targetHeight, upperBodyPercentage = c.upperBodyPercentage,
                customScaleRatio = c.customScaleRatio, armThickness = c.armThickness, legThickness = c.legThickness,
                thighPercentage = c.thighPercentage, scaleHand = c.scaleHand, scaleFoot = c.scaleFoot,
                scaleEyes = c.scaleEyes, centerModel = c.centerModel, extraLegLength = c.extraLegLength,
                scaleRelative = c.scaleRelative, armToLegs = c.armToLegs, keepHeadSize = c.keepHeadSize,
                skipAdjust = c.skipMainRescale, skipFloor = c.skipMoveToFloor, skipScale = c.skipHeightScaling,
                useBoneBasedFloorCalculation = c.useBoneBasedFloorCalculation, targetHeightMethod = c.targetHeightMethod,
                armToHeightRatioMethod = c.armToHeightRatioMethod, armToHeightHeightMethod = c.armToHeightHeightMethod,
                upperBodyUseNeck = c.upperBodyUseNeck, upperBodyTorsoUseNeck = c.upperBodyTorsoUseNeck,
                upperBodyUseLegacy = c.upperBodyUseLegacy
            };
        }

        private void AppendState(string label)
        {
            text.AppendLine();
            text.AppendLine(label + " (positions/distances in meters; bounds are renderer culling bounds, not baked vertices)");
            Transform root = descriptor.transform;
            var animator = root.GetComponent<Animator>();
            text.AppendLine("Root Animator=" + (animator == null ? "missing" : "present; isHuman=" + animator.isHuman + "; enabled=" + animator.enabled));
            text.AppendLine("Root world position=" + V(root.position) + "; rotation=" + V(root.eulerAngles));
            text.AppendLine("Root local scale=" + V(root.localScale) + "; lossy scale=" + V(root.lossyScale));
            Vector3 eyeLocal = core.GetEyePositionLocal();
            Vector3 eyeWorld = root.TransformPoint(eyeLocal);
            Vector3 viewWorld = ImmersiveScalerViewpoint.GetWorldPosition(descriptor);
            text.AppendLine("ViewPosition (SDK vector)=" + V(descriptor.ViewPosition));
            text.AppendLine("Viewpoint world=" + V(viewWorld) + "; viewpoint avatar-local=" + V(root.InverseTransformVector(descriptor.ViewPosition)));
            text.AppendLine("Eye avatar-local=" + V(eyeLocal) + "; eye world=" + V(eyeWorld));
            text.AppendLine("Viewpoint minus eyes (world)=" + V(viewWorld - eyeWorld) + "; distance=" + F(Vector3.Distance(viewWorld, eyeWorld)));
            text.AppendLine("A nonzero viewpoint/eye offset may be intentional; compare INPUT and OUTPUT offsets.");

            Transform leftEye = core.GetBone(HumanBodyBones.LeftEye);
            Transform rightEye = core.GetBone(HumanBodyBones.RightEye);
            string eyeSource = leftEye != null && rightEye != null ? "average of humanoid LeftEye and RightEye" :
                leftEye != null ? "humanoid LeftEye only" : rightEye != null ? "humanoid RightEye only" :
                core.GetBone(HumanBodyBones.Head) != null ? "head bone fallback (no mapped eyes)" : "estimated root + 1.5m (no mapped head/eyes)";
            text.AppendLine("Eye source=" + eyeSource);
            float floor = core.GetLowestPoint(parameters.useBoneBasedFloorCalculation);
            float highest = core.GetHighestPoint();
            float eyeY = core.GetEyeHeight();
            text.AppendLine("Selected floor world Y=" + F(floor) + "; highest world Y=" + F(highest) + "; eye world Y=" + F(eyeY));
            text.AppendLine("Total height (highest-floor)=" + F(highest - floor) + "; eye height (eye-floor)=" + F(eyeY - floor));
            float targetMeasurement = core.GetHeightByMethod(parameters.targetHeightMethod, parameters.useBoneBasedFloorCalculation);
            float ratioHeight = core.GetHeightByMethod(parameters.armToHeightHeightMethod, parameters.useBoneBasedFloorCalculation);
            float arm = core.GetArmByMethod(parameters.armToHeightRatioMethod);
            text.AppendLine("Height used for target=" + parameters.targetHeightMethod + ": " + F(targetMeasurement));
            text.AppendLine("Height used for arm ratio=" + parameters.armToHeightHeightMethod + ": " + F(ratioHeight));
            text.AppendLine("Arm measurement=" + parameters.armToHeightRatioMethod + ": " + F(arm));
            text.AppendLine("Arm-derived view height = arm/customScaleRatio + 0.005 = " + F(arm / parameters.customScaleRatio + 0.005f));
            text.AppendLine("Upper body fraction=" + F(parameters.upperBodyUseLegacy ? core.GetUpperBodyPortion(parameters.useBoneBasedFloorCalculation) :
                core.GetUpperBodyRatio(parameters.upperBodyUseNeck, parameters.upperBodyTorsoUseNeck, parameters.useBoneBasedFloorCalculation)));

            text.AppendLine("MAPPED BONES (world position; local scale)");
            foreach (HumanBodyBones bone in new[] { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Neck, HumanBodyBones.Head,
                HumanBodyBones.LeftEye, HumanBodyBones.RightEye, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, HumanBodyBones.LeftUpperLeg,
                HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightUpperLeg,
                HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightToes })
            {
                Transform transform = core.GetBone(bone);
                text.AppendLine(bone + ": " + (transform == null ? "not mapped" :
                    AnimationUtility.CalculateTransformPath(transform, root) + "; " + V(transform.position) + "; " + V(transform.localScale) + "; active=" + transform.gameObject.activeInHierarchy));
            }
            text.AppendLine("ALL SKINNED RENDERERS (selection/rejection decisions are logged above)");
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                text.AppendLine(AnimationUtility.CalculateTransformPath(renderer.transform, root) +
                    "; enabled=" + renderer.enabled + "; active=" + renderer.gameObject.activeInHierarchy +
                    "; mesh=" + (renderer.sharedMesh != null ? renderer.sharedMesh.name : "none") +
                    "; updateWhenOffscreen=" + renderer.updateWhenOffscreen);
                text.AppendLine("  world bounds min=" + V(renderer.bounds.min) + ", max=" + V(renderer.bounds.max) +
                    "; local bounds center=" + V(renderer.localBounds.center) + ", size=" + V(renderer.localBounds.size));
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            core.DiagnosticLog = previousSink;
            disposed = true;
        }

        private static string F(float value) => value.ToString("G9", CultureInfo.InvariantCulture);
        private static string V(Vector3 value) => "(" + F(value.x) + ", " + F(value.y) + ", " + F(value.z) + ")";
    }

    /// <summary>Shared read-only debug foldout for the component inspector and Tools window.</summary>
    public sealed class ImmersiveScalerDebugPanel
    {
        private bool expanded;
        private int source;
        private string current;
        private string preview;
        private Vector2 scroll;
        private GUIStyle reportStyle;

        public void InvalidateCurrent() => current = null;
        public void SetPreview(string report)
        {
            preview = report;
            current = null;
            source = 1;
            scroll = Vector2.zero;
        }

        public void ClearAvatar()
        {
            current = null;
            preview = null;
            source = 0;
        }

        public void Draw(Func<string> captureCurrent)
        {
            EditorGUILayout.Space();
            expanded = EditorGUILayout.Foldout(expanded, L("debug_report_title"), true);
            if (!expanded) return;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(L("debug_report_help"), MessageType.Info);
                source = EditorGUILayout.Popup(source, new[] { L("debug_report_current"), L("debug_report_preview"), L("debug_report_build") });
                if (source == 0 && current == null) current = captureCurrent();
                string report = source == 0 ? current : source == 1 ? preview : ImmersiveScalerDiagnostics.LastBuildReport;
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(source != 0))
                    {
                        if (GUILayout.Button(L("debug_report_refresh"))) report = current = captureCurrent();
                    }
                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(report)))
                    {
                        if (GUILayout.Button(L("debug_report_copy"))) EditorGUIUtility.systemCopyBuffer = report;
                    }
                }
                if (string.IsNullOrEmpty(report))
                {
                    EditorGUILayout.HelpBox(L(source == 1 ? "debug_report_no_preview" : "debug_report_no_build"), MessageType.Info);
                    return;
                }
                if (reportStyle == null)
                    reportStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true, richText = false, fontSize = 11 };
                using (var view = new EditorGUILayout.ScrollViewScope(scroll, GUILayout.Height(280)))
                {
                    scroll = view.scrollPosition;
                    float width = Mathf.Max(100, EditorGUIUtility.currentViewWidth - 70);
                    float height = reportStyle.CalcHeight(new GUIContent(report), width) + 20;
                    EditorGUILayout.SelectableLabel(report, reportStyle, GUILayout.MinHeight(height), GUILayout.ExpandWidth(true));
                }
            }
        }

        private static string L(string key) => KittynLocalization.Get("immersive_scaler." + key);
    }
}
