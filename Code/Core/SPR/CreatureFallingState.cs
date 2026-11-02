using DG.Tweening;
using SPR.FSM.Base;
using Spine;
using UnityEngine;

namespace SPR;

public class CreatureFallingState : CreatureState
{
	private readonly Vector2 groundPosition;

	public CreatureFallingState(Creature enemy, Vector2 groundPosition)
		: base(enemy)
	{
		this.groundPosition = groundPosition;
	}

	public override void Enter(IState prevState)
	{
		base.Creature.transform.DOMoveY(groundPosition.y, 0.5f).SetEase(Ease.InCubic).OnComplete(delegate
		{
			base.Creature.SetAnimation("landing", loop: false);
		});
		base.Creature.SetAnimation("falling");
		base.Creature.AddAnimationEvent(SpineObject.AnimState.Complete, OnAnimationComplete);
	}

	public override void Exit(IState nextState)
	{
		base.Creature.SetConnectedPath();
		base.Creature.RemoveAnimationEvent(SpineObject.AnimState.Complete, OnAnimationComplete);
	}

	public override void FixedUpdate(float fdt)
	{
	}

	private void OnAnimationComplete(TrackEntry track)
	{
		if (track.Animation.Name == "landing")
		{
			ChangeState(base.Creature.DefaultState);
		}
	}
}
