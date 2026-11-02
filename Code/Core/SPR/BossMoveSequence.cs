using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using UnityEngine;

namespace SPR;

[Serializable]
public class BossMoveSequence : BossAttackSequence
{
	[Header("이동 행동 설정")]
	[SerializeField]
	private bool isForward;

	[SerializeField]
	private float speed;

	[SerializeField]
	private float playerDistance;

	[SerializeField]
	private int powerRise;

	public override int GetPower(Enemy enemy)
	{
		int num = base.GetPower(enemy);
		BossCharacter bossCharacter = enemy.CastObject<BossCharacter>();
		if (isForward)
		{
			if (!bossCharacter.IsPlayerInRange(playerDistance))
			{
				num += powerRise;
			}
		}
		else if (bossCharacter.IsPlayerInRange(playerDistance))
		{
			num += powerRise;
		}
		return num;
	}

	protected override void OnStart_ForBoss(Enemy enemy)
	{
	}

	protected override List<AttackBehaviour> GenerateFrameBehaviour(Enemy enemy)
	{
		BossCharacter boss = enemy.CastObject<BossCharacter>();
		int num = Mathf.Clamp(Mathf.CeilToInt(boss.Fatigue * 60f), 1, 180);
		List<AttackBehaviour> list = Enumerable.Repeat(default(AttackBehaviour), num).ToList();
		list[0] += (Action)delegate
		{
			boss.LookPlayer();
			boss.SetAnimation(isForward ? boss.MoveForwardAnimation : boss.MoveBackwardAnimation);
		};
		for (int num2 = 0; num2 < num; num2++)
		{
			list[num2] += (Action)delegate
			{
				boss.MoveX(speed, Time.fixedDeltaTime, isForward);
			};
		}
		list[list.Count - 1] += (Action)delegate
		{
			boss.SetAnimation("idle");
		};
		return list;
	}
}
