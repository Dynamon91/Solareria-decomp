using System;
using Library;
using UnityEngine;

namespace SPR;

public abstract class BossAttackSequence : AttackSequence
{
	[Header("보스 패턴 구성")]
	[Tooltip("패턴의 피로도 상승 수치. 이 피로도를 다 회복하기 전까지는 Idle 상태를 유지합니다.")]
	[SerializeField]
	private float fatigue;

	[Tooltip("패턴의 영향력. 높을수록 패턴이 수행될 확률이 높아집니다.")]
	[SerializeField]
	private int power;

	[Tooltip("패턴이 실행될 경우 감소하는 영향력 수치. 다른 패턴이 수행되면 초기화됩니다.")]
	[SerializeField]
	private int powerPenalty;

	[SerializeField]
	private int penaltyCoefficient = 1;

	[SerializeField]
	private bool resetOther;

	private int? currentPower;

	public float Fatigue => fatigue;

	public virtual int GetPower(Enemy enemy)
	{
		int valueOrDefault = currentPower.GetValueOrDefault();
		if (!currentPower.HasValue)
		{
			valueOrDefault = power;
			currentPower = valueOrDefault;
		}
		return currentPower.Value;
	}

	public int GetPower(Func<int> func)
	{
		return func();
	}

	protected override void OnStart_ForBoss(Enemy enemy)
	{
		IPatternSelectable patternSelectable = enemy.CastObject<IPatternSelectable>();
		int valueOrDefault = currentPower.GetValueOrDefault();
		if (!currentPower.HasValue)
		{
			valueOrDefault = power;
			currentPower = valueOrDefault;
		}
		currentPower = Mathf.Max(currentPower.Value - powerPenalty, 0);
		patternSelectable.AddFatigue(fatigue);
		if (resetOther)
		{
			patternSelectable.ResetOtherAttackSequences(this);
		}
	}

	public virtual void RisePower()
	{
		int valueOrDefault = currentPower.GetValueOrDefault();
		if (!currentPower.HasValue)
		{
			valueOrDefault = power;
			currentPower = valueOrDefault;
		}
		if (penaltyCoefficient == 0)
		{
			penaltyCoefficient = 1;
		}
		currentPower = Mathf.Min(currentPower.Value + powerPenalty / penaltyCoefficient, power);
	}

	public void RemovePower()
	{
		currentPower = 0;
	}

	public void Reset()
	{
		currentPower = power;
	}
}
