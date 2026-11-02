using System;
using System.Collections.Generic;
using System.Linq;
using Library;

namespace SPR;

[Serializable]
public class GenericAttackSequence<T> : AttackSequence where T : Enemy
{
	public IReadOnlyList<AnimationInfo> AnimationInfos => animationList;

	protected override List<AttackBehaviour> GenerateFrameBehaviour(Enemy enemy)
	{
		if ((object)timelineEffect == null)
		{
			return new List<AttackBehaviour>();
		}
		T castEnemy = enemy.CastObject<T>();
		int num;
		if (animationList.IsNullOrEmpty())
		{
			enemy.SetAnimation(timelineEffect.name, loop: false);
			num = Util.SecondsToFrame(enemy.GetAnimDuration(timelineEffect.name));
		}
		else
		{
			num = 0;
			for (int i = 0; i < animationList.Count; i++)
			{
				num += animationList[i].duration;
			}
		}
		List<AttackBehaviour> list = ((num == 0) ? new List<AttackBehaviour>() : Enumerable.Repeat(default(AttackBehaviour), num).ToList());
		num = 0;
		foreach (AnimationInfo animInfo in animationList)
		{
			list[num] += (Action)delegate
			{
				castEnemy.SetAnimation(animInfo.name, animInfo.loop, 1f, 0, 0f, vfxEnable: false);
			};
			num += animInfo.duration;
		}
		if (list.Count > 0)
		{
			list[0] += (Action)delegate
			{
				castEnemy.EnableVFX(timelineEffect.name);
			};
		}
		return list;
	}
}
