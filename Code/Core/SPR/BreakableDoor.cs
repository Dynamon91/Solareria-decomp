using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Field;
using UnityEngine;

namespace SPR;

public class BreakableDoor : StateSaveBreakableObject
{
	[SerializeField]
	private List<SpriteRenderer> beforeBreaks;

	private const string IdleAnimName = "idle";

	private const string HitAnimName = "hit";

	public override string ID => "BreakableDoor_" + Game.Instance.CurrentFieldID.ToString();

	protected override void OnDamaged()
	{
		base.OnDamaged();
		base.SkeletonAnimation.AnimationState.SetAnimation(0, "hit", loop: false);
	}

	protected override void OnLoadBrokenState()
	{
		base.gameObject.SetActive(value: false);
	}

	protected override void OnLoadNormalState()
	{
		base.gameObject.SetActive(value: true);
		beforeBreaks.ForEach(delegate(SpriteRenderer x)
		{
			x.DOKill();
			x.gameObject.SetActive(value: true);
		});
		base.SkeletonAnimation.AnimationState.SetAnimation(0, "idle", loop: true);
	}

	protected override async UniTask PlayBreakAnimationAsync()
	{
		base.SkeletonAnimation.AnimationState.SetAnimation(0, "hit", loop: false);
		await UniTask.WaitUntil(() => base.SkeletonAnimation.AnimationState.GetCurrent(0).IsComplete, PlayerLoopTiming.Update, EnableToken);
		beforeBreaks.ForEach(delegate(SpriteRenderer x)
		{
			x.DOAlpha(0f, 0.5f);
		});
		base.SkeletonAnimation.AnimationState.SetAnimation(0, BreakAnimationName, loop: false);
		await UniTask.WaitUntil(() => base.SkeletonAnimation.AnimationState.GetCurrent(0).IsComplete, PlayerLoopTiming.Update, EnableToken);
	}
}
