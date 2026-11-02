using UnityEngine;

namespace SPR.FSM.Base;

public class MoveToState : CreatureState
{
	private readonly Vector2 destination;

	public MoveToState(Creature enemy, Vector2 destination)
		: base(enemy)
	{
		this.destination = destination;
	}

	public override void Enter(IState prevState)
	{
		base.Creature.SetAnimation("walk");
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (destination == base.Creature.Position)
		{
			base.Creature.StateMachine.ChangeState(base.Creature.DefaultState);
		}
		base.Creature.Move(destination);
	}
}
