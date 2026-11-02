using Library;
using SPR.FSM.Base;
using SPR.Library;
using Spine;
using UnityEngine;

namespace SPR;

public class PrimordialFlame2AirBounceState : BossState
{
	private FrameTimer timer;

	public PrimordialFlame2AirBounceState(Enemy enemy)
		: base(enemy)
	{
		base.Enemy.AddAnimationEvent(SpineObject.AnimState.Start, OnStart);
		base.Enemy.AddAnimationEvent(SpineObject.AnimState.Complete, OnEnd);
	}

	public override void Enter(IState prevState)
	{
		base.Enemy.SetAnimation("parry_air", loop: false);
		base.Enemy.AddAnimation("parry_air_end", loop: false);
		timer = new FrameTimer(25);
	}

	public override void Exit(IState nextState)
	{
		base.Enemy.ResetStunCount();
	}

	public override void FixedUpdate(float fdt)
	{
		if (timer != null)
		{
			if (timer.IsDone)
			{
				base.Enemy.SkeletonAnimation.Skeleton.SetAlpha(0f);
				timer = null;
			}
			else
			{
				timer.Tick();
			}
		}
	}

	private void OnStart(TrackEntry track)
	{
		if (track.Animation.Name == "parry_air_end")
		{
			base.Enemy.Position = new Vector2(base.Enemy.Position.x, base.Enemy.SpawnPosition.y);
			base.Enemy.SkeletonAnimation.Skeleton.SetAlpha(1f);
		}
	}

	private void OnEnd(TrackEntry track)
	{
		if (track.Animation.Name == "parry_air_end")
		{
			ChangeState(base.Enemy.DefaultState);
		}
	}
}
