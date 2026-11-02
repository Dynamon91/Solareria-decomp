using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using Spine;
using UnityEngine;

namespace SPR.Field;

public class EarthPortStatue : StateSaveBreakableObject
{
	[Serializable]
	private struct BreakInfo
	{
		[SerializeField]
		public int count;

		[SerializeField]
		public string anim;

		[SerializeField]
		public GameObject crackEffect;
	}

	[SerializeField]
	private BreakInfo[] breakInfos;

	[SerializeField]
	private ParticleSystem damagedEffect;

	[SerializeField]
	private ParticleSystem breakEffect;

	[SerializeField]
	private EventReference breakEvent;

	private Vector3 originLocalPosition;

	private GameObject currentCrack;

	private DG.Tweening.Sequence visualKnockBackSequence;

	public override string ID => base.name;

	protected override string BreakAnimationName => "broken";

	protected override void OnLoadBrokenState()
	{
		string animationName = ((base.SkeletonAnimation.AnimationState.Data.SkeletonData.Animations.Find((Spine.Animation x) => x.Name == "idle_broken") == null) ? BreakAnimationName : "idle_broken");
		base.SkeletonAnimation.SetAnimation(animationName, 0, loop: false, 1f);
	}

	protected override void OnLoadNormalState()
	{
		originLocalPosition = base.SkeletonAnimation.transform.localPosition;
		foreach (BreakInfo item in breakInfos.Reverse())
		{
			if (item.count <= Count)
			{
				base.SkeletonAnimation.SetAnimation(item.anim);
				if (!currentCrack.IsNull())
				{
					currentCrack.gameObject.SetActive(value: false);
				}
				if (!item.crackEffect.IsNull())
				{
					currentCrack = item.crackEffect;
					currentCrack.gameObject.SetActive(value: true);
				}
			}
		}
	}

	protected override void OnDamaged()
	{
		BreakInfo[] array = breakInfos;
		for (int i = 0; i < array.Length; i++)
		{
			BreakInfo breakInfo = array[i];
			if (breakInfo.count == Count)
			{
				base.SkeletonAnimation.SetAnimation(breakInfo.anim);
				if (!currentCrack.IsNull())
				{
					currentCrack.gameObject.SetActive(value: false);
				}
				if (!breakInfo.crackEffect.IsNull())
				{
					currentCrack = breakInfo.crackEffect;
					currentCrack.gameObject.SetActive(value: true);
				}
			}
		}
		damagedEffect.Play();
		Transform tr = base.SkeletonAnimation.transform;
		tr.DOKill();
		visualKnockBackSequence = DOTween.Sequence();
		int sign = (tr.position.x - Game.Instance.Player.mover.VisualCenter.x).GetSign();
		visualKnockBackSequence.Append(tr.DOLocalMoveX(0.125f * (float)sign, 0.05f).SetEase(Ease.Linear)).Append(tr.DOLocalMoveX(originLocalPosition.x, 0.125f).SetEase(Ease.OutBack)).OnKill(delegate
		{
			tr.localPosition = originLocalPosition;
		});
	}

	protected override void OnBreak()
	{
		breakEffect.Play();
		currentCrack.gameObject.SetActive(value: false);
		ChangeState(BrokenState);
	}

	protected override void PlaySfx(bool isBreak)
	{
		(isBreak ? BreakableObject.SoundManager.PlayOneShotAttached(breakEvent, base.transform) : BreakableObject.SoundManager.PlayOneShotAttached("event:/SFX/Object/Breakable/SFX_Breakable_Debris", base.transform)).setParameterByName("Breakable_Material", (float)materialType);
	}

	protected override async UniTask PlayBreakAnimationAsync()
	{
		await visualKnockBackSequence.WaitAsync();
		await base.PlayBreakAnimationAsync();
	}
}
