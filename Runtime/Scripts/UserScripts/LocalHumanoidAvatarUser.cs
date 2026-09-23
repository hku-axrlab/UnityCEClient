using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine;

namespace UnityCEClient
{
    /// <summary>
    /// Sends avatar bones, on top of LocalUser data
    /// </summary>
	public class LocalHumanoidAvatarUser : LocalUser
	{
		public enum BoneCollection
		{
			All = 0,
			NoFingers = 1,
			Manual
		}

		[Tooltip("If All / NoFingers, will generate boneTransforms automatically based on attached Animator")]
		public BoneCollection boneCollection = BoneCollection.Manual;
		[Tooltip("This will be overridden by boneCollection, unless set to Manual")]
		public List<HumanBodyBones> selectedBones = new();

        public string mocapClientType = "optitrack";
        public string url = "localhost";
        public string avatarName = "Default";

		private Animator animator;

		private void Awake()
		{
			animator = GetComponent<Animator>();

            if (animator == null) {
				enabled = false;
				Debug.LogError("No Animator on LocalAvatarUser", gameObject);
			}
		}

		protected override void Start()
		{
			base.Start();
			if (animator != null)
				SetupBoneTransforms();

            userType = "Mocap";

            userCustomData.Add("MocapClientType", GetMocapClientType);
            userCustomData.Add("URL", GetURL);
            userCustomData.Add("AvatarName", GetAvatarName);
        }

        public (System.Type, object) GetMocapClientType()
        {
            return (typeof(string), mocapClientType);
        }

        public (System.Type, object) GetURL()
        {
            return (typeof(string), url);
        }

        public (System.Type, object) GetAvatarName()
        {
            return (typeof(string), avatarName);
        }

        private void SetupBoneTransforms()
		{
			switch (boneCollection)
			{
				case BoneCollection.All:
					selectedBones.Clear();
					for (int i = 0; i < (int)HumanBodyBones.LastBone; ++i)
						selectedBones.Add((HumanBodyBones)i);
					break;
				case BoneCollection.NoFingers:
					// Everything after Jaw is fingers
                    for (int i = 0; i <= (int)HumanBodyBones.Jaw; ++i)
                        selectedBones.Add((HumanBodyBones)i);
                    break;
			}

			foreach (HumanBodyBones bone in selectedBones)
			{
                Transform tBone = animator.GetBoneTransform(bone);
                if (tBone != null)
                {
                    tBone.name = HumanBodyBoneToHumanIK(bone);
                    bones.Add(tBone);
                }
			}
		}

        // Implemented as a temporary variant before we know exactly which one we'll use (could be this one)
        public static string HumanBodyBoneToHumanIK(HumanBodyBones bone) => bone switch
        {
            HumanBodyBones.Hips => "Hips",
            HumanBodyBones.Spine => "Spine",
            HumanBodyBones.Chest => "Spine1",
            HumanBodyBones.UpperChest => "Spine2",
            HumanBodyBones.Neck => "Neck",
            HumanBodyBones.Head => "Head",

            HumanBodyBones.LeftShoulder => "LeftShoulder",
            HumanBodyBones.LeftUpperArm => "LeftArm",
            HumanBodyBones.LeftLowerArm => "LeftForeArm",
            HumanBodyBones.LeftHand => "LeftHand",

            HumanBodyBones.RightShoulder => "RightShoulder",
            HumanBodyBones.RightUpperArm => "RightArm",
            HumanBodyBones.RightLowerArm => "RightForeArm",
            HumanBodyBones.RightHand => "RightHand",

            HumanBodyBones.LeftUpperLeg => "LeftUpLeg",
            HumanBodyBones.LeftLowerLeg => "LeftLeg",
            HumanBodyBones.LeftFoot => "LeftFoot",
            HumanBodyBones.LeftToes => "LeftToeBase",

            HumanBodyBones.RightUpperLeg => "RightUpLeg",
            HumanBodyBones.RightLowerLeg => "RightLeg",
            HumanBodyBones.RightFoot => "RightFoot",
            HumanBodyBones.RightToes => "RightToeBase",

            // Left fingers
            HumanBodyBones.LeftThumbProximal => "LeftHandThumb1",
            HumanBodyBones.LeftThumbIntermediate => "LeftHandThumb2",
            HumanBodyBones.LeftThumbDistal => "LeftHandThumb3",
            HumanBodyBones.LeftIndexProximal => "LeftHandIndex1",
            HumanBodyBones.LeftIndexIntermediate => "LeftHandIndex2",
            HumanBodyBones.LeftIndexDistal => "LeftHandIndex3",
            HumanBodyBones.LeftMiddleProximal => "LeftHandMiddle1",
            HumanBodyBones.LeftMiddleIntermediate => "LeftHandMiddle2",
            HumanBodyBones.LeftMiddleDistal => "LeftHandMiddle3",
            HumanBodyBones.LeftRingProximal => "LeftHandRing1",
            HumanBodyBones.LeftRingIntermediate => "LeftHandRing2",
            HumanBodyBones.LeftRingDistal => "LeftHandRing3",
            HumanBodyBones.LeftLittleProximal => "LeftHandPinky1",
            HumanBodyBones.LeftLittleIntermediate => "LeftHandPinky2",
            HumanBodyBones.LeftLittleDistal => "LeftHandPinky3",

            // Right fingers
            HumanBodyBones.RightThumbProximal => "RightHandThumb1",
            HumanBodyBones.RightThumbIntermediate => "RightHandThumb2",
            HumanBodyBones.RightThumbDistal => "RightHandThumb3",
            HumanBodyBones.RightIndexProximal => "RightHandIndex1",
            HumanBodyBones.RightIndexIntermediate => "RightHandIndex2",
            HumanBodyBones.RightIndexDistal => "RightHandIndex3",
            HumanBodyBones.RightMiddleProximal => "RightHandMiddle1",
            HumanBodyBones.RightMiddleIntermediate => "RightHandMiddle2",
            HumanBodyBones.RightMiddleDistal => "RightHandMiddle3",
            HumanBodyBones.RightRingProximal => "RightHandRing1",
            HumanBodyBones.RightRingIntermediate => "RightHandRing2",
            HumanBodyBones.RightRingDistal => "RightHandRing3",
            HumanBodyBones.RightLittleProximal => "RightHandPinky1",
            HumanBodyBones.RightLittleIntermediate => "RightHandPinky2",
            HumanBodyBones.RightLittleDistal => "RightHandPinky3",

            _ => null
        };
    }
}