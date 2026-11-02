using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using UnityEngine;

namespace SPR;

[Serializable]
public class BossIdleSequence : BossAttackSequence
{
	protected override void OnStart_ForBoss(Enemy enemy)
	{
	}

	protected override List<AttackBehaviour> GenerateFrameBehaviour(Enemy enemy)
	{
		BossCharacter boss = enemy.CastObject<BossCharacter>();
		int num = Mathf.CeilToInt(boss.Fatigue * 60f);
		List<AttackBehaviour> list = Enumerable.Repeat(default(AttackBehaviour), num).ToList();
		if (num > 0)
		{
			list[0] += (Action)delegate
			{
				boss.SetAnimation(boss.IdleAnimation);
			};
		}
		return list;
	}
}
