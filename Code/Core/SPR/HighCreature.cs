using System;
using System.Collections.Generic;
using SPR.Achievement;
using SPR.FSM;
using SPR.FSM.Base;
using SPR.UI;
using UnityEngine;

namespace SPR;

public abstract class HighCreature : Creature, IPatternSelectable
{
	[Header("최초 피로도 설정")]
	[SerializeField]
	private float initialFatigue;

	private float Fatigue { get; set; }

	public override bool IsDefenseless
	{
		get
		{
			if (IsInGroggyState && base.VfxController.IsTimelineEnd("groggy_start"))
			{
				return IsInExecutionRange();
			}
			return false;
		}
	}

	public bool FatigueIsDone => Fatigue == 0f;

	public override bool IsGroggyPlaying
	{
		get
		{
			if (!IsInGroggyState)
			{
				return base.StateMachine.CurrentState is GroggyEndState;
			}
			return true;
		}
	}

	protected static BossBoard BossBoard => Game.Instance.UIManager.FieldCanvas.BossBoard;

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		base.AttackState = new AttackState(this, GenerateCurrentAttackSequences);
	}

	public override void ResetStunCount()
	{
		base.CurrentStun = base.MaxStunCount;
		BossBoard.ResetStunPoint();
	}

	public override void ActivateBurstIcon(bool value, bool isClicked = false)
	{
		BossBoard.ActivateBurstIcon(value, isClicked);
	}

	public override void ActivateExecuteLine(bool value)
	{
		BossBoard.ActivateExecuteLine(value);
	}

	public override void PlayHealTween(bool loop, Type subscriberType = null)
	{
		if ((object)subscriberType == null)
		{
			subscriberType = GetType();
		}
		BossBoard.PlayHealTween(loop, subscriberType);
	}

	public override void StopHealTween(Type subscriberType)
	{
		BossBoard.StopHealTween(subscriberType);
	}

	public void SetInitialFatigue()
	{
		AddFatigue(initialFatigue);
	}

	public void AddFatigue(float value)
	{
		Fatigue = Mathf.Max(Fatigue + value, 0f);
	}

	public void ResetOtherAttackSequences(BossAttackSequence currentSequence)
	{
		foreach (AttackSequence allAttackSequence in base.AllAttackSequences)
		{
			if (allAttackSequence != currentSequence)
			{
				(allAttackSequence as BossAttackSequence)?.Reset();
			}
		}
	}

	public override bool ShouldChangeToAttackState()
	{
		foreach (AttackSequence allAttackSequence in base.AllAttackSequences)
		{
			if (allAttackSequence is BossAttackSequence bossAttackSequence && (float)bossAttackSequence.GetPower(this) > 0f && IsPlayerInAttackBound(bossAttackSequence))
			{
				return true;
			}
		}
		return false;
	}

	protected override void OnDie()
	{
		Game.Instance.SaveData.WorldData.AddEliminatedCreature(Game.Instance.CurrentFieldID, base.PlaceholderPosition);
		AchievementHelper.CheckTriggers();
		Enemy.SoundManager.PlayOneShot("event:/SFX/Boss/SFX_Boss_Clear_Common");
	}

	protected override void OnChangeHealth(int delta)
	{
		BossBoard.UpdateHealthBar(base.Health, base.MaxHealth, delta);
	}

	protected override void OnFixedUpdate()
	{
		if (Fatigue > 0f)
		{
			AddFatigue(0f - Time.fixedDeltaTime);
		}
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return GetRandomSequence();
	}

	protected abstract List<AttackSequence> GetRandomSequence();
}
