using System;
using System.Linq;
using System.Reflection;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace VRChatImmersiveScaler.Tests
{
    public class LimbScalingRegressionTests
    {
        private ViewpointRegressionTests factory;
        private VRCAvatarDescriptor avatar;
        private ImmersiveScalerCore core;

        [SetUp]
        public void SetUp()
        {
            factory = new ViewpointRegressionTests();
            avatar = factory.CreateAvatar("Limb scaling regression");
            core = new ImmersiveScalerCore(avatar.gameObject);
        }

        [TearDown]
        public void TearDown() => factory.TearDown();

        private ScalingParameters CurrentParameters()
        {
            float height = core.GetHeightByMethod(HeightMethodType.EyeHeight, true);
            return new ScalingParameters {
                targetHeight = height,
                upperBodyUseLegacy = true,
                upperBodyPercentage = core.GetUpperBodyPortion(true) * 100,
                thighPercentage = core.GetThighPercentage() * 100,
                customScaleRatio = core.GetArmByMethod(ArmMethodType.HeadToHand) / (height - 0.005f),
                useBoneBasedFloorCalculation = true,
                skipFloor = true
            };
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void ArmRatioChangePreservesBodyLegsFeetAndAuthoredScales(bool authoredScales, bool build)
        {
            if (authoredScales)
            {
                foreach (var bone in new[] { HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg })
                    core.GetBone(bone).localScale = new Vector3(0.85f, 0.65f, 0.9f);
                foreach (var bone in new[] { HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg })
                    core.GetBone(bone).localScale = new Vector3(1.1f, 0.8f, 1.05f);
                foreach (var bone in new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
                    core.GetBone(bone).localScale = new Vector3(1.05f, 0.9f, 1.15f);
            }

            // Reproduce the user's approximate starting ratio with only arm lengths adjusted.
            float height = core.GetHeightByMethod(HeightMethodType.EyeHeight, true);
            var shoulder = core.GetBone(HumanBodyBones.RightUpperArm);
            var head = core.GetBone(HumanBodyBones.Head);
            float desiredHeadToWrist = 0.4f * (height - 0.005f);
            float vertical = head.position.y - shoulder.position.y;
            float desiredArm = Mathf.Sqrt(desiredHeadToWrist * desiredHeadToWrist - vertical * vertical) - Mathf.Abs(shoulder.position.x - head.position.x);
            float armMultiplier = desiredArm / core.GetArmLength();
            foreach (var bone in new[] { HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
                core.GetBone(bone).localPosition *= armMultiplier;

            var parameters = CurrentParameters();
            Assert.That(parameters.customScaleRatio, Is.EqualTo(0.4f).Within(0.0001f));
            parameters.customScaleRatio = 0.375f;
            var preservedBones = new[] { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.Head,
                HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
                HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightToes };
            var matrices = preservedBones.Select(b => core.GetBone(b).localToWorldMatrix).ToArray();
            float oldArmLength = core.GetArmLength();
            if (build)
            {
                var component = avatar.gameObject.AddComponent<ImmersiveScalerComponent>();
                component.targetHeight = parameters.targetHeight;
                component.customScaleRatio = parameters.customScaleRatio;
                component.upperBodyPercentage = parameters.upperBodyPercentage;
                component.thighPercentage = parameters.thighPercentage;
                component.upperBodyUseLegacy = true;
                component.useBoneBasedFloorCalculation = true;
                avatar.ViewPosition = avatar.transform.TransformVector(core.GetEyePositionLocal());
                using (new OverrideTemporaryDirectoryScope(null)) AvatarProcessor.ProcessAvatar(avatar.gameObject);
                Assert.That(avatar.GetComponent<ImmersiveScalerComponent>(), Is.Null, "NDMF must execute the scaler pass");
                AssertVector(core.GetEyePositionLocal(), avatar.transform.InverseTransformVector(avatar.ViewPosition), "Final viewpoint");
            }
            else core.ScaleAvatar(parameters);

            TestContext.Out.WriteLine($"Arm-only reproduction: authoredScales={authoredScales}, NDMF={build}, ratio=0.4 -> 0.375, armLength={oldArmLength:F6} -> {core.GetArmLength():F6}, rootScale={avatar.transform.localScale}, measuredRatio={core.GetArmByMethod(ArmMethodType.HeadToHand) / (core.GetHeightByMethod(HeightMethodType.EyeHeight, true) - 0.005f):F6}");

            for (int i = 0; i < preservedBones.Length; i++)
                AssertMatrix(matrices[i], core.GetBone(preservedBones[i]).localToWorldMatrix, preservedBones[i].ToString());
            Assert.That(core.GetArmLength(), Is.LessThan(oldArmLength));
            Assert.That(core.GetArmByMethod(ArmMethodType.HeadToHand) / (core.GetHeightByMethod(HeightMethodType.EyeHeight, true) - 0.005f),
                Is.EqualTo(0.375f).Within(0.001f), "The measured result must match the chosen arm-ratio method");
        }

        [TestCase(0.8f, false)]
        [TestCase(1.2f, false)]
        [TestCase(0.8f, true)]
        public void EachArmSegmentGetsOneScaleAndHandsRespectToggle(float multiplier, bool scaleHands)
        {
            float upper = Distance(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm);
            float lower = Distance(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);
            var hand = core.GetBone(HumanBodyBones.RightHand);
            var handBasis = BasisLengths(hand);
            Invoke("ScaleArms", multiplier, 1f, scaleHands);
            Assert.That(Distance(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm), Is.EqualTo(upper * multiplier).Within(0.0001f));
            Assert.That(Distance(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand), Is.EqualTo(lower * multiplier).Within(0.0001f), "Forearm must not receive the scale twice");
            AssertVector(handBasis * (scaleHands ? multiplier : 1f), BasisLengths(hand), "Hand basis");
        }

        [TestCase(0.8f, false)]
        [TestCase(1.2f, false)]
        [TestCase(0.8f, true)]
        public void EachLegSegmentGetsOneScaleAndFeetRespectToggle(float multiplier, bool scaleFeet)
        {
            // Put the toe at ankle height to isolate segment scaling from fixed sole height.
            foreach (var id in new[] { HumanBodyBones.LeftToes, HumanBodyBones.RightToes })
            {
                var toes = core.GetBone(id);
                toes.localPosition = new Vector3(toes.localPosition.x, 0, toes.localPosition.z);
            }
            float upper = Distance(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg);
            var foot = core.GetBone(HumanBodyBones.LeftFoot);
            var footBasis = BasisLengths(foot);
            float toeLength = Distance(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes);
            Invoke("ScaleLegs", multiplier, 1f, scaleFeet, core.GetThighPercentage(), true);
            Assert.That(Distance(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg), Is.EqualTo(upper * multiplier).Within(0.0001f));
            // The synthetic shin has a small forward offset; assert the vertical length independently.
            float vertical = Mathf.Abs(core.GetBone(HumanBodyBones.LeftLowerLeg).position.y - foot.position.y);
            Assert.That(vertical, Is.EqualTo(0.45f * multiplier).Within(0.0001f), "Shin must not receive the scale twice");
            AssertVector(footBasis * (scaleFeet ? multiplier : 1f), BasisLengths(foot), "Foot basis");
            Assert.That(Distance(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes), Is.EqualTo(toeLength * (scaleFeet ? multiplier : 1f)).Within(0.0001f));
        }

        [TestCase(0.8f, false)]
        [TestCase(1.2f, false)]
        [TestCase(0.8f, true)]
        public void LegHeightAndThighPercentageRespectAnUnscaledFoot(float multiplier, bool scaleFeet)
        {
            var thigh = core.GetBone(HumanBodyBones.LeftUpperLeg);
            float height = thigh.position.y - core.GetLowestPoint(true);
            var foot = core.GetBone(HumanBodyBones.LeftFoot);
            var basis = BasisLengths(foot);
            Invoke("ScaleLegs", multiplier, 1f, scaleFeet, 0.6f, true);
            Assert.That(thigh.position.y - core.GetLowestPoint(true), Is.EqualTo(height * multiplier).Within(0.0001f));
            Assert.That(core.GetThighPercentage(), Is.EqualTo(0.6f).Within(0.0001f));
            AssertVector(basis * (scaleFeet ? multiplier : 1f), BasisLengths(foot), "Foot size");
        }

        [TestCase(ArmMethodType.HeadToElbowVRC)]
        [TestCase(ArmMethodType.HeadToHand)]
        [TestCase(ArmMethodType.ArmLength)]
        [TestCase(ArmMethodType.ShoulderToFingertip)]
        [TestCase(ArmMethodType.CenterToHand)]
        [TestCase(ArmMethodType.CenterToFingertip)]
        public void ArmSolverUsesTheSelectedMeasurement(ArmMethodType method)
        {
            foreach (var id in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
            {
                var tip = new GameObject(id == HumanBodyBones.LeftHand ? "LeftMiddleDistal" : "RightMiddleDistal").transform;
                tip.SetParent(core.GetBone(id), false);
                tip.localPosition = new Vector3(id == HumanBodyBones.LeftHand ? -0.1f : 0.1f, 0, 0);
            }
            var p = CurrentParameters();
            p.armToHeightRatioMethod = method;
            p.customScaleRatio = core.GetArmByMethod(method) / (p.targetHeight - 0.005f) * 0.9f;
            p.scaleHand = true;
            core.ScaleAvatar(p);
            float ratio = core.GetArmByMethod(method) / (core.GetHeightByMethod(HeightMethodType.EyeHeight, true) - 0.005f);
            Assert.That(ratio, Is.EqualTo(p.customScaleRatio).Within(0.001f), method.ToString());
        }

        [Test]
        public void RotatedBoneAxesAndFirstChildHelpersDoNotChangeTheScaledSegment()
        {
            foreach (var id in new[] { HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm })
            {
                var bone = core.GetBone(id);
                var children = bone.Cast<Transform>().ToArray();
                var positions = children.Select(t => t.position).ToArray();
                var rotations = children.Select(t => t.rotation).ToArray();
                bone.localRotation *= Quaternion.Euler(0, 0, 90);
                for (int i = 0; i < children.Length; i++) children[i].SetPositionAndRotation(positions[i], rotations[i]);
                var helper = new GameObject("Zero length helper").transform;
                helper.SetParent(bone, false);
                helper.SetSiblingIndex(0);
            }
            var hand = core.GetBone(HumanBodyBones.RightHand);
            var basis = BasisLengths(hand);
            float upper = Distance(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm);
            float lower = Distance(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);
            Invoke("ScaleArms", 0.8f, 1.1f, false);
            Assert.That(Distance(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm), Is.EqualTo(upper * 0.8f).Within(0.0001f));
            Assert.That(Distance(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand), Is.EqualTo(lower * 0.8f).Within(0.0001f));
            AssertVector(basis, BasisLengths(hand), "Rotated hand basis");
        }

        private float Distance(HumanBodyBones a, HumanBodyBones b) => Vector3.Distance(core.GetBone(a).position, core.GetBone(b).position);
        private static Vector3 BasisLengths(Transform t) => new Vector3(t.TransformVector(Vector3.right).magnitude, t.TransformVector(Vector3.up).magnitude, t.TransformVector(Vector3.forward).magnitude);
        private static void AssertVector(Vector3 expected, Vector3 actual, string label) => Assert.That(Vector3.Distance(expected, actual), Is.LessThan(0.0001f), label + ": expected " + expected + ", got " + actual);
        private static void AssertMatrix(Matrix4x4 expected, Matrix4x4 actual, string label)
        {
            for (int i = 0; i < 16; i++) Assert.That(actual[i], Is.EqualTo(expected[i]).Within(0.0001f), label + " matrix[" + i + "]");
        }
        private void Invoke(string method, params object[] arguments)
        {
            try { typeof(ImmersiveScalerCore).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(core, arguments); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }
    }
}
