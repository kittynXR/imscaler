using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace VRChatImmersiveScaler.Tests
{
    public class ViewpointRegressionTests
    {
        private readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = cleanup.Count - 1; i >= 0; --i)
                if (cleanup[i] != null) Object.DestroyImmediate(cleanup[i]);
            cleanup.Clear();
        }

        [TestCase(1f, 1.5f, HeightMethodType.EyeHeight)]
        [TestCase(0.5f, 1.8f, HeightMethodType.EyeHeight)]
        [TestCase(2f, 0.5f, HeightMethodType.EyeHeight)]
        [TestCase(1f, 1.5f, HeightMethodType.TotalHeight)]
        public void PreviewKeepsViewpointAtEyesWhenRootScaleChanges(float initialScale, float multiplier, HeightMethodType heightMethod)
        {
            var avatar = CreateAvatar("RootScale");
            avatar.transform.position = new Vector3(3, 0.4f, -2);
            avatar.transform.rotation = Quaternion.Euler(0, 35, 0);
            avatar.transform.localScale = Vector3.one * initialScale;
            AddRenderer(avatar, "Body", 0, 1.9f);
            var core = new ImmersiveScalerCore(avatar.gameObject);
            Vector3 offsetLocal = new Vector3(0.01f, 0.025f, 0.035f);
            avatar.ViewPosition = avatar.transform.TransformVector(core.GetEyePositionLocal() + offsetLocal);
            var component = Configure(avatar);
            component.targetHeightMethod = heightMethod;
            component.targetHeight = core.GetHeightByMethod(heightMethod, true) * multiplier;
            var editor = CreateInspector(component);

            Invoke(editor, "StartPreview", component, avatar);

            Vector3 expectedWorld = avatar.transform.TransformPoint(core.GetEyePositionLocal() + offsetLocal);
            Vector3 actualWorld = avatar.transform.position + avatar.ViewPosition;
            Assert.That(Vector3.Distance(actualWorld, expectedWorld), Is.LessThan(0.0001f),
                $"VRChat viewpoint {actualWorld} must follow the final eyes {expectedWorld}");
        }

        [Test]
        public void HipOnlyPreviewReconcilesViewpointAfterMovingHead()
        {
            var avatar = CreateAvatar("HipAdjustment");
            var core = new ImmersiveScalerCore(avatar.gameObject);
            avatar.ViewPosition = avatar.transform.TransformVector(core.GetEyePositionLocal());
            var component = Configure(avatar);
            component.skipHeightScaling = true;
            component.applyShrinkHipBone = true;
            var editor = CreateInspector(component);
            Invoke(editor, "StartPreview", component, avatar);
            Vector3 expected = avatar.transform.TransformVector(core.GetEyePositionLocal());
            Assert.That(Vector3.Distance(avatar.ViewPosition, expected), Is.LessThan(0.0001f),
                "Hip adjustment moves the head even when both rescale options are skipped.");
        }

        [Test]
        public void CancelPreviewRestoresInactiveMappedBones()
        {
            var avatar = CreateAvatar("InactiveBones");
            var component = Configure(avatar);
            component.skipMainRescale = false;
            component.skipHeightScaling = true;
            component.customScaleRatio = 0.3f;
            var hand = avatar.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftHand);
            hand.gameObject.SetActive(false);
            Vector3 originalScale = hand.localScale;
            var editor = CreateInspector(component);
            Invoke(editor, "StartPreview", component, avatar);
            Assert.That(Vector3.Distance(hand.localScale, originalScale), Is.GreaterThan(0.001f),
                "Fixture must exercise compensation on the inactive mapped hand.");
            Invoke(editor, "ResetPreview", component, avatar);
            Assert.That(Vector3.Distance(hand.localScale, originalScale), Is.LessThan(0.0001f));
        }

        [Test]
        public void ChangingWindowAvatarDoesNotOverwriteNewAvatarsViewpoint()
        {
            var first = CreateAvatar("FirstAvatar");
            var second = CreateAvatar("SecondAvatar");
            first.ViewPosition = new Vector3(0, 1.78f, 0.08f);
            second.ViewPosition = new Vector3(0.1f, 1.2f, 0.3f);
            Vector3 firstOriginal = first.ViewPosition;
            Vector3 secondOriginal = second.ViewPosition;
            var window = ScriptableObject.CreateInstance<ImmersiveScalerWindow>();
            cleanup.Add(window);
            SetField(window, "selectedAvatar", first.gameObject);
            SetField(window, "scalerCore", new ImmersiveScalerCore(first.gameObject));
            SetField(window, "parameters", new ScalingParameters { skipAdjust = true, skipFloor = true, targetHeight = 2.5f });
            Invoke(window, "StartPreview");
            SetField(window, "selectedAvatar", second.gameObject);
            Invoke(window, "ValidateAvatar");
            Assert.That(second.ViewPosition, Is.EqualTo(secondOriginal), "The newly selected avatar must be untouched.");
            Assert.That(first.ViewPosition, Is.EqualTo(firstOriginal), "Restore the descriptor that owns the preview.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NdmfPassKeepsViewpointAtFinalEyes(bool hipOnly)
        {
            var avatar = CreateAvatar("BuildViewpoint");
            var component = Configure(avatar);
            component.skipHeightScaling = hipOnly;
            component.applyShrinkHipBone = hipOnly;
            var core = new ImmersiveScalerCore(avatar.gameObject);
            Vector3 offset = new Vector3(0, 0.02f, 0.04f);
            avatar.ViewPosition = avatar.transform.TransformVector(core.GetEyePositionLocal() + offset);
            component.targetHeight = core.GetHeightByMethod(HeightMethodType.EyeHeight, true) * 1.5f;
            using (new OverrideTemporaryDirectoryScope(null))
                AvatarProcessor.ProcessAvatar(avatar.gameObject);
            Vector3 expected = avatar.transform.TransformVector(core.GetEyePositionLocal() + offset);
            Assert.That(Vector3.Distance(avatar.ViewPosition, expected), Is.LessThan(0.0001f));
            Assert.That(avatar.GetComponent<ImmersiveScalerComponent>(), Is.Null, "The NDMF pass must run and remove its component.");
            StringAssert.Contains("NDMF build", ImmersiveScalerDiagnostics.LastBuildReport);
            StringAssert.Contains("OUTPUT STATE", ImmersiveScalerDiagnostics.LastBuildReport);
        }

        [Test]
        public void SnapshotShowsSelectedBoundsAndOutliersWithoutChangingAvatar()
        {
            var avatar = CreateAvatar("BoundsDiagnostics");
            var body = AddRenderer(avatar, "Body", 0, 1.8f);
            var head = AddRenderer(avatar, "HeadMesh", 1.5f, 2.1f);
            var bad = AddRenderer(avatar, "BadBounds", -6, 10);
            var core = new ImmersiveScalerCore(avatar.gameObject);
            core.SetMeasurementRendererOverrides(true, new[] { body, bad }, new[] { head });
            Vector3 oldView = avatar.ViewPosition;
            Matrix4x4 oldMatrix = avatar.transform.localToWorldMatrix;
            var parameters = new ScalingParameters { targetHeightMethod = HeightMethodType.TotalHeight, armToHeightHeightMethod = HeightMethodType.EyeHeight };
            string report = ImmersiveScalerDiagnostics.Snapshot(avatar, core, parameters, "Read-only test");
            StringAssert.Contains("Floor accepted Body", report);
            StringAssert.Contains("Floor rejected BadBounds", report);
            StringAssert.Contains("Height rejected BadBounds", report);
            StringAssert.Contains("Height accepted HeadMesh", report);
            StringAssert.Contains("Height used for target=TotalHeight", report);
            StringAssert.Contains("Height used for arm ratio=EyeHeight", report);
            StringAssert.Contains("Read-only snapshot: scaling was not run", report);
            Assert.That(avatar.ViewPosition, Is.EqualTo(oldView));
            Assert.That(avatar.transform.localToWorldMatrix, Is.EqualTo(oldMatrix));
            Assert.That(core.DiagnosticLog, Is.Null, "The report must release its trace callback.");
        }

        [Test]
        public void PreviewReportKeepsActualTraceAfterCancel()
        {
            var avatar = CreateAvatar("PreviewReport");
            var component = Configure(avatar);
            var editor = CreateInspector(component);
            Invoke(editor, "StartPreview", component, avatar);
            var panel = editor.GetType().GetField("debugPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(editor);
            var previewField = typeof(ImmersiveScalerDebugPanel).GetField("preview", BindingFlags.NonPublic | BindingFlags.Instance);
            string captured = (string)previewField.GetValue(panel);
            StringAssert.Contains("Height scale: target=", captured);
            StringAssert.Contains("INPUT STATE", captured);
            StringAssert.Contains("OUTPUT STATE", captured);
            Invoke(editor, "ResetPreview", component, avatar);
            Assert.That(previewField.GetValue(panel), Is.EqualTo(captured));
        }

        [TestCase("ShrinkHipBone")]
        [TestCase("ResetScales")]
        public void WindowManualActionKeepsViewpointAtEyes(string action)
        {
            var avatar = CreateAvatar("ManualHip");
            avatar.transform.localScale = Vector3.one * 1.5f;
            var core = new ImmersiveScalerCore(avatar.gameObject);
            avatar.ViewPosition = avatar.transform.TransformVector(core.GetEyePositionLocal());
            var window = ScriptableObject.CreateInstance<ImmersiveScalerWindow>();
            cleanup.Add(window);
            SetField(window, "selectedAvatar", avatar.gameObject);
            SetField(window, "scalerCore", core);
            Invoke(window, action);
            Assert.That(Vector3.Distance(avatar.ViewPosition, avatar.transform.TransformVector(core.GetEyePositionLocal())), Is.LessThan(0.0001f));
        }

        private SkinnedMeshRenderer AddRenderer(VRCAvatarDescriptor avatar, string name, float minY, float maxY)
        {
            var transform = new GameObject(name).transform;
            transform.SetParent(avatar.transform, false);
            var renderer = transform.gameObject.AddComponent<SkinnedMeshRenderer>();
            var mesh = new Mesh { name = name + "Mesh" };
            cleanup.Add(mesh);
            mesh.vertices = new[] { new Vector3(-0.1f, minY, 0), new Vector3(0.1f, maxY, 0), new Vector3(0, minY, 0.1f) };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateBounds();
            renderer.sharedMesh = mesh;
            renderer.localBounds = mesh.bounds;
            var animator = avatar.GetComponent<Animator>();
            renderer.bones = new[] { animator.GetBoneTransform(HumanBodyBones.Hips), animator.GetBoneTransform(HumanBodyBones.LeftFoot) };
            return renderer;
        }

        private ImmersiveScalerComponent Configure(VRCAvatarDescriptor avatar)
        {
            var component = avatar.gameObject.AddComponent<ImmersiveScalerComponent>();
            component.targetHeight = 2.1f;
            component.skipMainRescale = true;
            component.skipMoveToFloor = true;
            component.useBoneBasedFloorCalculation = true;
            return component;
        }

        private UnityEditor.Editor CreateInspector(ImmersiveScalerComponent component)
        {
            var editor = UnityEditor.Editor.CreateEditor(component);
            cleanup.Add(editor);
            return editor;
        }

        private static void Invoke(object target, string method, params object[] arguments)
        {
            var info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(info, Is.Not.Null, method);
            try { info.Invoke(target, arguments); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }

        private static void SetField(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private VRCAvatarDescriptor CreateAvatar(string name)
        {
            var root = new GameObject(name);
            cleanup.Add(root);
            var mapped = new Dictionary<HumanBodyBones, Transform>();
            Transform Bone(HumanBodyBones bone, Transform parent, Vector3 position)
            {
                var transform = new GameObject(bone.ToString()).transform;
                transform.SetParent(parent, false);
                transform.localPosition = position;
                mapped.Add(bone, transform);
                return transform;
            }
            var hips = Bone(HumanBodyBones.Hips, root.transform, new Vector3(0, 1, 0));
            var spine = Bone(HumanBodyBones.Spine, hips, new Vector3(0, 0.2f, 0));
            var chest = Bone(HumanBodyBones.Chest, spine, new Vector3(0, 0.2f, 0));
            var neck = Bone(HumanBodyBones.Neck, chest, new Vector3(0, 0.15f, 0));
            var head = Bone(HumanBodyBones.Head, neck, new Vector3(0, 0.15f, 0));
            Bone(HumanBodyBones.LeftEye, head, new Vector3(-0.03f, 0.08f, 0.08f));
            Bone(HumanBodyBones.RightEye, head, new Vector3(0.03f, 0.08f, 0.08f));
            for (int side = 0; side < 2; ++side)
            {
                float sign = side == 0 ? -1 : 1;
                var upperArm = Bone(side == 0 ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm, chest, new Vector3(sign * 0.2f, 0.13f, 0));
                var lowerArm = Bone(side == 0 ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm, upperArm, new Vector3(sign * 0.3f, 0, 0));
                Bone(side == 0 ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand, lowerArm, new Vector3(sign * 0.25f, 0, 0));
                var upperLeg = Bone(side == 0 ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg, hips, new Vector3(sign * 0.1f, -0.05f, 0));
                var lowerLeg = Bone(side == 0 ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg, upperLeg, new Vector3(0, -0.4f, 0));
                var foot = Bone(side == 0 ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot, lowerLeg, new Vector3(0, -0.45f, 0.02f));
                Bone(side == 0 ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes, foot, new Vector3(0, -0.02f, 0.1f));
            }
            var description = new HumanDescription
            {
                human = mapped.Select(pair => new HumanBone { boneName = pair.Value.name, humanName = HumanTrait.BoneName[(int)pair.Key], limit = new HumanLimit { useDefaultValues = true } }).ToArray(),
                skeleton = root.GetComponentsInChildren<Transform>(true).Select(t => new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale }).ToArray(),
                upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
                armStretch = 0.05f, legStretch = 0.05f
            };
            var humanAvatar = AvatarBuilder.BuildHumanAvatar(root, description);
            cleanup.Add(humanAvatar);
            Assert.That(humanAvatar.isValid && humanAvatar.isHuman, Is.True, "Synthetic humanoid must be valid.");
            root.AddComponent<Animator>().avatar = humanAvatar;
            return root.AddComponent<VRCAvatarDescriptor>();
        }
    }
}
