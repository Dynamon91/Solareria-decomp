using System;

namespace SPR.FSM.Base;

public class DeathState : EnemyState
{
	private readonly Action onEnter;

	public DeathState(Creature enemy, Action onEnter)
		: base(enemy)
	{
		this.onEnter = onEnter;
	}

	public override void Enter(IState prevState)
	{
		base.Enemy.SetAnimation("death", loop: false);
		onEnter?.Invoke();
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
	}
}
