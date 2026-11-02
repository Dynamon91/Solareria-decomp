using System.Collections.Generic;
using StaticData;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class EnemyAttackBehaviour : PlayableBehaviour
{
	public Enemy Enemy;

	public string Key;

	public int Interval;

	public bool IgnoreStun;

	public bool SplitDirection;

	public List<Zone> Zones;

	public List<IDamageable> SharedDamageableList;

	protected HitInfo HitInfo;

	private int frameCount;

	private List<IDamageable> damageableList;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		damageableList = new List<IDamageable>();
		frameCount = 0;
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		double duration = playable.GetDuration();
		double time = playable.GetTime();
		double num = time + (double)info.deltaTime;
		if (info.effectivePlayState != PlayState.Paused || !(num > duration))
		{
			Mathf.Approximately((float)time, (float)duration);
		}
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		HitInfo = new HitInfo(Enemy, Key);
		HitInfo.SetIgnoreStun(IgnoreStun);
		if (HitInfo.AttackAttribute.HasFlag(AttackAttribute.Continuous))
		{
			if (frameCount % Interval == 0)
			{
				foreach (Zone zone in Zones)
				{
					Attack(zone);
				}
			}
			frameCount++;
			return;
		}
		foreach (Zone zone2 in Zones)
		{
			Attack(zone2);
		}
	}

	protected void Attack(Zone zone)
	{
		List<IDamageable> list = new List<IDamageable>();
		float num = Enemy.VisualCenter.x - Enemy.VisualBottom.x;
		if (Game.Instance.SaveData.Difficulty == Difficulty.Main)
		{
			num += Enemy.HitBoxSize.x / 2f;
		}
		if (SplitDirection)
		{
			foreach (Zone item in zone.SplitByX(num))
			{
				if (item.Center.x < num)
				{
					HitInfo hitInfo = HitInfo;
					hitInfo.AttackDirection = AttackDirection.Both;
					hitInfo.KnockBack = Vector2.zero;
					hitInfo.ParryKnockBack = Vector2.zero;
					hitInfo.GuardKnockBack = Vector2.zero;
					hitInfo.FixDirection = true;
					list.AddRange(Enemy.AttackSquare(item, hitInfo, SharedDamageableList ?? damageableList));
				}
				else
				{
					list.AddRange(Enemy.AttackSquare(item, HitInfo, SharedDamageableList ?? damageableList));
				}
			}
		}
		else
		{
			list = Enemy.AttackSquare(zone, HitInfo, SharedDamageableList ?? damageableList);
		}
		foreach (IDamageable item2 in list)
		{
			if (item2 is PlayerMover)
			{
				Enemy.OnPlayerHit(HitInfo);
			}
			if (item2.HitType == HitType.Platform)
			{
				Enemy.OnPlatformHit(HitInfo);
			}
		}
	}
}
