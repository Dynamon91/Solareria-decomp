using System;
using SPR.FSM.Base;
using SPR.Library;
using UnityEngine;

namespace SPR.FSM;

public class StoneGuardFlyingState : CreatureState
{
	private FrameTimer flyingTimer;

	private int flyingDuration;

	private float flyingHeight;

	private Vector2 startPos;

	private Vector2 groundingPos;

	private readonly Creature stoneGuard;

	public StoneGuardFlyingState(Creature enemy)
		: base(enemy)
	{
		stoneGuard = enemy;
	}

	public override void Enter(IState prevState)
	{
		stoneGuard.SetAnimation("stone_loop");
		flyingTimer?.Reset();
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (flyingTimer == null)
		{
			flyingTimer = new FrameTimer(flyingDuration);
			startPos = base.Creature.VisualBottom;
		}
		flyingTimer.Tick();
		float num = startPos.y - groundingPos.y;
		float normalizedTime = flyingTimer.NormalizedTime;
		float x = Vector2.Lerp(startPos, groundingPos, normalizedTime).x;
		float y = groundingPos.y + num * Mathf.Cos(normalizedTime * MathF.PI / 2f);
		base.Creature.ChangeVisualBottomPositionTo(new Vector2(x, y));
		if (flyingTimer.IsDone)
		{
			ChangeState(new StoneGuardHatchingState(stoneGuard));
		}
	}

	public void SetParameter(int durationFrame, float height, Vector2 targetPos)
	{
		flyingDuration = durationFrame;
		flyingHeight = height;
		groundingPos = targetPos;
	}
}
