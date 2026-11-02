using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace SPR.Field;

public class TerradinEntranceElevator : EntranceElevator
{
	[SerializeField]
	private List<SkeletonAnimation> doorSkeletonAnimations;

	private List<BoxCollider2D> doorColliders;

	private const string CloseAnimName = "close";

	private const string CloseIdleAnimName = "close_idle";

	private const string OpenAnimName = "open";

	private const string OpenIdleAnimName = "open_idle";

	protected override void Awake()
	{
		base.Awake();
		doorColliders = new List<BoxCollider2D>(doorSkeletonAnimations.Count);
		foreach (SkeletonAnimation doorSkeletonAnimation in doorSkeletonAnimations)
		{
			doorColliders.Add(doorSkeletonAnimation.GetComponent<BoxCollider2D>());
		}
	}

	protected override void LoadState_Internal(int state)
	{
		base.LoadState_Internal(state);
		for (int i = 0; i < doorSkeletonAnimations.Count; i++)
		{
			doorSkeletonAnimations[i].AnimationState.SetAnimation(0, "open_idle", loop: true);
			doorColliders[i].enabled = false;
		}
	}

	protected override void OnEnter()
	{
		for (int i = 0; i < doorSkeletonAnimations.Count; i++)
		{
			doorSkeletonAnimations[i].AnimationState.SetAnimation(0, "close_idle", loop: true);
			doorColliders[i].enabled = true;
		}
	}

	protected override void OnStartMoving()
	{
		base.OnStartMoving();
		for (int i = 0; i < doorSkeletonAnimations.Count; i++)
		{
			int collIndex = i;
			SkeletonAnimation skeletonAnimation = doorSkeletonAnimations[i];
			if (skeletonAnimation.AnimationName == "close_idle")
			{
				doorColliders[collIndex].enabled = true;
				continue;
			}
			if (skeletonAnimation.AnimationName == "open")
			{
				skeletonAnimation.AnimationState.AddAnimation(0, "close", loop: false, 0f).Start += delegate
				{
					doorColliders[collIndex].enabled = true;
				};
			}
			else
			{
				skeletonAnimation.AnimationState.SetAnimation(0, "close", loop: false);
				doorColliders[collIndex].enabled = true;
			}
			skeletonAnimation.AnimationState.AddAnimation(0, "close_idle", loop: true, 0f);
		}
	}

	protected override void OnCompleteMoving()
	{
		base.OnCompleteMoving();
		for (int i = 0; i < doorSkeletonAnimations.Count; i++)
		{
			SkeletonAnimation obj = doorSkeletonAnimations[i];
			TrackEntry trackEntry = doorSkeletonAnimations[i].AnimationState.SetAnimation(0, "open", loop: false);
			int collIndex = i;
			trackEntry.End += delegate
			{
				doorColliders[collIndex].enabled = false;
			};
			obj.AnimationState.AddAnimation(0, "open_idle", loop: true, 0f);
		}
	}

	protected override IEnumerator BeforeMoveRoutine()
	{
		yield return base.BeforeMoveRoutine();
		yield return new WaitUntil(() => doorSkeletonAnimations.TrueForAll((SkeletonAnimation x) => x.AnimationName == "close_idle"));
	}
}
