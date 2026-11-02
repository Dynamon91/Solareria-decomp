using System;
using Spine.Unity;
using UnityEngine;

namespace SPR.Library;

public class DynamicBoneFollower : MonoBehaviour
{
	[Serializable]
	private class BoneInfo
	{
		[SerializeField]
		private string boneName;

		[SerializeField]
		private string slotName;

		[SerializeField]
		private string attachmentName;

		public string BoneName => boneName;

		public string SlotName => slotName;

		public string AttachmentName => attachmentName;
	}

	[SerializeField]
	private BoneInfo[] boneInfos;

	private SkeletonRenderer skeletonRenderer;

	private int index;

	private BoneFollower boneFollower;

	private SkeletonAnimation skeletonAnimation;

	private BoneFollower BoneFollower => boneFollower ?? (boneFollower = GetComponent<BoneFollower>());

	private void Awake()
	{
		skeletonRenderer = BoneFollower.SkeletonRenderer;
	}

	private void OnEnable()
	{
		index = 0;
	}

	private void Update()
	{
		if (index < boneInfos.Length)
		{
			if (skeletonRenderer.Skeleton.FindSlot(boneInfos[index].SlotName).Attachment == null && index + 1 < boneInfos.Length)
			{
				index++;
			}
			boneFollower.bone = skeletonRenderer.Skeleton.FindBone(boneInfos[index].BoneName);
		}
	}
}
