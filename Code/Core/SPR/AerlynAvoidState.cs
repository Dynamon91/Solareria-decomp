using Library;
using SPR.FSM.Base;
using UnityEngine;

namespace SPR;

public class AerlynAvoidState : CreatureState
{
	private readonly Aerlyn aerlyn;

	private Vector2 direction;

	public AerlynAvoidState(Creature enemy)
		: base(enemy)
	{
		aerlyn = enemy.CastObject<Aerlyn>();
	}

	public override void Enter(IState prevState)
	{
		aerlyn.SetAnimation("run");
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		aerlyn.Move(fdt);
	}
}
