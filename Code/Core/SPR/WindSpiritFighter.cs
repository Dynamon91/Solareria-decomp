using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.FSM.Base;
using SPR.Field;
using Spine.Unity.Examples;
using UnityEngine;

namespace SPR;

public class WindSpiritFighter : HighCreature
{
	[Serializable]
	private class Combo
	{
		[SerializeField]
		private List<TimelineEffect> sequences;

		public IReadOnlyList<TimelineEffect> Sequences => sequences;
	}

	[Header("콤보")]
	[SerializeField]
	private List<Combo> comboList;

	[Header("위빙 스트라이크")]
	[SerializeField]
	private int weavingStrikeRise;

	[Header("Sequence")]
	[SerializeField]
	private GenericAttackSequence<WindSpiritFighter> weavingStrike;

	[SerializeField]
	private GenericAttackSequence<WindSpiritFighter> weaving;

	[SerializeField]
	private GenericAttackSequence<WindSpiritFighter> weaving2;

	[SerializeField]
	private GenericAttackSequence<WindSpiritFighter> hookLeft;

	[SerializeField]
	private GenericAttackSequence<WindSpiritFighter> hookRight;

	[SerializeField]
	private GenericAttackSequence<WindSpiritFighter> upper;

	[SerializeField]
	private GenericAttackSequence<WindSpiritFighter> slam;

	[SerializeField]
	private GenericAttackSequence<WindSpiritFighter> slamStrong;

	[SerializeField]
	private GenericAttackSequence<WindSpiritFighter> strike;

	[SerializeField]
	private GenericAttackSequence<WindSpiritFighter> backstep;

	private bool isInvincible;

	private int weavingStrikeChance;

	private PoolingObject clone;

	public override string Name => "WindSpiritFighter";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		SetAnimation("acc_spin", loop: true, 1f, 1);
		AddAction(ActionTiming.Begin, "Invincible", (Action)delegate
		{
			isInvincible = true;
		});
		AddAction(ActionTiming.End, "Invincible", (Action)delegate
		{
			isInvincible = false;
		});
		AddAction(ActionTiming.Begin, "SpawnClone", delegate(int duration, Parameter[] p)
		{
			SpawnClone(duration, p[0].Int).Forget();
		});
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (isInvincible)
		{
			return false;
		}
		if (!hitInfo.AttackAttribute.HasFlag(AttackAttribute.ParryGroggy) || base.IsBounced || IsGroggyPlaying || base.StateMachine.CurrentState == null)
		{
			return base.TryTakeDamage(hitInfo);
		}
		weavingStrikeChance = Mathf.Clamp(weavingStrikeChance + weavingStrikeRise, 0, 100);
		if (weavingStrikeChance >= CryptoRandom.Range(1, 101))
		{
			base.StateMachine.ChangeState(new AttackState(this, () => new List<AttackSequence> { weavingStrike, strike }, forceAttack: true));
			weavingStrikeChance = 0;
			return false;
		}
		if (!isInvincible)
		{
			return base.TryTakeDamage(hitInfo);
		}
		return false;
	}

	public override bool ShouldChangeToAttackState()
	{
		return IsPlayerInSightBound();
	}

	public override void OnEnterBounceState()
	{
		isInvincible = false;
	}

	public override void OnEnterExecution()
	{
		base.OnEnterExecution();
		isInvincible = false;
	}

	protected override void OnEnterGroggyState()
	{
		base.OnEnterGroggyState();
		isInvincible = false;
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { weavingStrike, weaving, weaving2, hookLeft, hookRight, upper, slam, slamStrong, strike, backstep };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return GetRandomSequence();
	}

	protected override List<AttackSequence> GetRandomSequence()
	{
		Combo combo = comboList[CryptoRandom.Range(0, comboList.Count)];
		List<AttackSequence> list = new List<AttackSequence>();
		foreach (TimelineEffect timeline in combo.Sequences)
		{
			GenericAttackSequence<WindSpiritFighter> item = base.AllAttackSequences.Find((AttackSequence s) => s.IsSameTimelineEffect(timeline)).CastObject<GenericAttackSequence<WindSpiritFighter>>();
			list.Add(item);
		}
		return list;
	}

	protected override void OnDie()
	{
		base.OnDie();
		RemoveAnimation(1);
		LegendaryArea legendaryArea = MyArea.CastObject<LegendaryArea>();
		DamageEffectAsync().Forget();
		legendaryArea.OpenWall();
		legendaryArea.PlayClearTimeline(VisualCenter).Forget();
	}

	private async UniTaskVoid SpawnClone(int duration, int fadeDuration)
	{
		clone?.ReturnToPool();
		clone = Enemy.PoolManager.GetObject<PoolingObject>("Wind Spirit Fighter_Clone.prefab", VisualBottom);
		clone.transform.Flip(base.transform.localScale.x.GetSign());
		SkeletonRenderTexture cloneRenderTex = clone.GetComponentInChildren<SkeletonRenderTexture>();
		cloneRenderTex.color = Color.white;
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(duration);
		await DOTween.To(() => cloneRenderTex.color, delegate(Color x)
		{
			cloneRenderTex.color = x;
		}, new Color(1f, 1f, 1f, 0f), Util.FrameToSeconds(fadeDuration)).WaitAsync();
		clone?.ReturnToPool();
		clone = null;
	}
}
