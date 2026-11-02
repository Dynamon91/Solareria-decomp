using System;
using System.Collections.Generic;
using System.Linq;
using Library;

namespace SPR;

[Serializable]
public class GenericBossAttackSequence<T> : BossAttackSequence where T : Enemy, IPatternSelectable
{
	private int startFrame;

	public string TimelineName
	{
		get
		{
			if ((object)timelineEffect != null)
			{
				return timelineEffect.name;
			}
			return string.Empty;
		}
	}

	public TimelineEffect TimelineEffect => timelineEffect;

	public int Duration
	{
		get
		{
			if ((object)timelineEffect != null)
			{
				return Util.SecondsToFrame(timelineEffect.Duration);
			}
			return 0;
		}
	}

	public event Action OnStart;

	protected override void OnStart_ForBoss(Enemy enemy)
	{
		base.OnStart_ForBoss(enemy);
		this.OnStart?.Invoke();
	}

	protected override List<AttackBehaviour> GenerateFrameBehaviour(Enemy enemy)
	{
		if ((object)timelineEffect == null)
		{
			return new List<AttackBehaviour>();
		}
		T castEnemy = enemy.CastObject<T>();
		int num;
		if (animationList.Count <= 0)
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
		for (int j = 0; j < animationList.Count; j++)
		{
			AnimationInfo animInfo = animationList[j];
			if (string.IsNullOrEmpty(animInfo.name))
			{
				num += animInfo.duration;
				continue;
			}
			if (j > 0 && animationList[j].name == animationList[j - 1].name)
			{
				list[num] += (Action)delegate
				{
					castEnemy.SetAnimationInstantly(animInfo.name, animInfo.loop, Util.FrameToSeconds(startFrame));
				};
			}
			else
			{
				list[num] += (Action)delegate
				{
					castEnemy.SetAnimation(animInfo.name, animInfo.loop, 1f, 0, Util.FrameToSeconds(startFrame), vfxEnable: false);
				};
			}
			num += animInfo.duration;
		}
		if (list.Count > 0)
		{
			list[0] += (Action)delegate
			{
				castEnemy.EnableVFX(timelineEffect.name, loop: false, Util.FrameToSeconds(startFrame));
			};
			list[list.Count - 1] += (Action)delegate
			{
				SetStartFrame(0);
			};
		}
		return list;
	}

	public void SetStartFrame(int start)
	{
		startFrame = start;
	}
}
