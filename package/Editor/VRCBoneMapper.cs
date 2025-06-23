using System.Collections.Generic;
using UnityEngine;

namespace VRChatImmersiveScaler
{
    public static class VRCBoneMapper
    {
        // VRChat bone name mappings based on the Blender version
        private static readonly Dictionary<HumanBodyBones, string[]> boneNameMappings = new Dictionary<HumanBodyBones, string[]>
        {
            // Head
            { HumanBodyBones.Head, new[] { "head" } },
            { HumanBodyBones.Neck, new[] { "neck" } },
            { HumanBodyBones.LeftEye, new[] { "eyel", "lefteye", "eyeleft", "lefteye001" } },
            { HumanBodyBones.RightEye, new[] { "eyer", "righteye", "eyeright", "righteye001" } },
            
            // Spine
            { HumanBodyBones.Hips, new[] { "hips", "hip", "pelvis", "root" } },
            { HumanBodyBones.Spine, new[] { "spine" } },
            { HumanBodyBones.Chest, new[] { "chest" } },
            { HumanBodyBones.UpperChest, new[] { "upperchest", "upper_chest" } },
            
            // Left Arm
            { HumanBodyBones.LeftShoulder, new[] { "leftshoulder", "shoulderl", "lshoulder" } },
            { HumanBodyBones.LeftUpperArm, new[] { "leftarm", "arml", "larm", "upperarml", "leftupperarm" } },
            { HumanBodyBones.LeftLowerArm, new[] { "leftelbow", "elbowl", "lelbow", "lowerarml", "leftlowerarm", "forearml" } },
            { HumanBodyBones.LeftHand, new[] { "leftwrist", "wristl", "lwrist", "handl", "lefthand" } },
            
            // Right Arm
            { HumanBodyBones.RightShoulder, new[] { "rightshoulder", "shoulderr", "rshoulder" } },
            { HumanBodyBones.RightUpperArm, new[] { "rightarm", "armr", "rarm", "upperarmr", "rightupperarm" } },
            { HumanBodyBones.RightLowerArm, new[] { "rightelbow", "elbowr", "relbow", "lowerarmr", "rightlowerarm", "forearmr" } },
            { HumanBodyBones.RightHand, new[] { "rightwrist", "wristr", "rwrist", "handr", "righthand" } },
            
            // Left Leg
            { HumanBodyBones.LeftUpperLeg, new[] { "leftleg", "legl", "lleg", "upperlegl", "thighl", "leftupperleg", "lupperleg" } },
            { HumanBodyBones.LeftLowerLeg, new[] { "leftknee", "kneel", "lknee", "lowerlegl", "calfl", "shinl", "leftlowerleg" } },
            { HumanBodyBones.LeftFoot, new[] { "leftankle", "anklel", "lankle", "leftfoot", "footl" } },
            { HumanBodyBones.LeftToes, new[] { "toesl", "toel", "lefttoes" } },
            
            // Right Leg
            { HumanBodyBones.RightUpperLeg, new[] { "rightleg", "legr", "rleg", "upperlegr", "thighr", "rightupperleg", "rupperleg" } },
            { HumanBodyBones.RightLowerLeg, new[] { "rightknee", "kneer", "rknee", "lowerlegr", "calfr", "rightlowerleg", "shinr" } },
            { HumanBodyBones.RightFoot, new[] { "rightankle", "ankler", "rankle", "rightfoot", "footr" } },
            { HumanBodyBones.RightToes, new[] { "toesr", "toer", "righttoes" } }
        };
        
        // Finger mappings for VRChat
        private static readonly Dictionary<string, string[]> fingerBoneMappings = new Dictionary<string, string[]>
        {
            // Left Hand
            { "LeftThumbProximal", new[] { "thumbproximall", "leftthumbproximal" } },
            { "LeftThumbIntermediate", new[] { "thumbintermediatel", "leftthumbintermediate" } },
            { "LeftThumbDistal", new[] { "thumbdistall", "leftthumbdistal" } },
            
            { "LeftIndexProximal", new[] { "indexproximall", "leftindexproximal" } },
            { "LeftIndexIntermediate", new[] { "indexintermediatel", "leftindexintermediate" } },
            { "LeftIndexDistal", new[] { "indexdistall", "leftindexdistal" } },
            
            { "LeftMiddleProximal", new[] { "middleproximall", "leftmiddleproximal" } },
            { "LeftMiddleIntermediate", new[] { "middleintermediatel", "leftmiddleintermediate" } },
            { "LeftMiddleDistal", new[] { "middledistall", "leftmiddledistal" } },
            
            { "LeftRingProximal", new[] { "ringproximall", "leftringproximal" } },
            { "LeftRingIntermediate", new[] { "ringintermediatel", "leftringintermediate" } },
            { "LeftRingDistal", new[] { "ringdistall", "leftringdistal" } },
            
            { "LeftLittleProximal", new[] { "littleproximall", "leftpinkyproximal" } },
            { "LeftLittleIntermediate", new[] { "littleintermediatel", "leftpinkyintermediate" } },
            { "LeftLittleDistal", new[] { "littledistall", "leftpinkydistal" } },
            
            // Right Hand
            { "RightThumbProximal", new[] { "thumbproximalr", "rightthumbproximal" } },
            { "RightThumbIntermediate", new[] { "thumbintermediater", "rightthumbintermediate" } },
            { "RightThumbDistal", new[] { "thumbdistalr", "rightthumbdistal" } },
            
            { "RightIndexProximal", new[] { "indexproximalr", "rightindexproximal" } },
            { "RightIndexIntermediate", new[] { "indexintermediater", "rightindexintermediate" } },
            { "RightIndexDistal", new[] { "indexdistalr", "rightindexdistal" } },
            
            { "RightMiddleProximal", new[] { "middleproximalr", "rightmiddleproximal" } },
            { "RightMiddleIntermediate", new[] { "middleintermediater", "rightmiddleintermediate" } },
            { "RightMiddleDistal", new[] { "middledistalr", "rightmiddledistal" } },
            
            { "RightRingProximal", new[] { "ringproximalr", "rightringproximal" } },
            { "RightRingIntermediate", new[] { "ringintermediater", "rightringintermediate" } },
            { "RightRingDistal", new[] { "ringdistalr", "rightringdistal" } },
            
            { "RightLittleProximal", new[] { "littleproximalr", "rightpinkyproximal" } },
            { "RightLittleIntermediate", new[] { "littleintermediater", "rightpinkyintermediate" } },
            { "RightLittleDistal", new[] { "littledistalr", "rightpinkydistal" } }
        };
        
        public static Transform GetBoneTransform(Animator animator, HumanBodyBones bone)
        {
            if (animator == null || !animator.isHuman)
                return null;
                
            return animator.GetBoneTransform(bone);
        }
        
        public static Transform FindBone(Transform root, HumanBodyBones boneType)
        {
            if (!boneNameMappings.ContainsKey(boneType))
                return null;
                
            string[] possibleNames = boneNameMappings[boneType];
            return FindBoneByNames(root, possibleNames);
        }
        
        public static Transform FindFingerBone(Transform root, string fingerBoneName)
        {
            if (!fingerBoneMappings.ContainsKey(fingerBoneName))
                return null;
                
            string[] possibleNames = fingerBoneMappings[fingerBoneName];
            return FindBoneByNames(root, possibleNames);
        }
        
        private static Transform FindBoneByNames(Transform root, string[] possibleNames)
        {
            foreach (string name in possibleNames)
            {
                Transform bone = FindBoneRecursive(root, name);
                if (bone != null)
                    return bone;
            }
            return null;
        }
        
        private static Transform FindBoneRecursive(Transform parent, string boneName)
        {
            string searchName = boneName.ToLower().Replace("_", "").Replace("-", "").Replace(" ", "").Replace(".", "");
            
            if (parent.name.ToLower().Replace("_", "").Replace("-", "").Replace(" ", "").Replace(".", "") == searchName)
                return parent;
                
            foreach (Transform child in parent)
            {
                Transform result = FindBoneRecursive(child, boneName);
                if (result != null)
                    return result;
            }
            
            return null;
        }
        
        public static Dictionary<HumanBodyBones, Transform> GetAllBones(Animator animator)
        {
            var bones = new Dictionary<HumanBodyBones, Transform>();
            
            if (animator == null || !animator.isHuman)
                return bones;
                
            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone)
                    continue;
                    
                Transform boneTransform = animator.GetBoneTransform(bone);
                if (boneTransform != null)
                    bones[bone] = boneTransform;
            }
            
            return bones;
        }
    }
}