using System;
using SPR.Library;

namespace SPR.FSM.Base;

public class GroggyState : EnemyState
{
	private readonly FrameTimer groggyTimer;

	private readonly Action onEnter;

	private readonly Action onExit;

	public GroggyState(Enemy enemy, FrameTimer groggyTimer, Action onEnter, Action onExit)
		: base(enemy)
	{
		this.groggyTimer = groggyTimer;
		this.onEnter = onEnter;
		this.onExit = onExit;
	}

	public override void Enter(IState prevState)
	{
		groggyTimer.Reset();
		onEnter?.Invoke();
	}

	public override void Exit(IState nextState)
	{
		onExit?.Invoke();
	}

	public override void FixedUpdate(float fdt)
	{
		if (!EnemyState.Player.IsSpecialAttackTaskValid())
		{
			if (groggyTimer.IsDone)
			{
				ChangeState(new GroggyEndState(base.Enemy));
			}
			else
			{
				groggyTimer.Tick();
			}
		}
	}
}
