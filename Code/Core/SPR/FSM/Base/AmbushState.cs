namespace SPR.FSM.Base;

public class AmbushState : CreatureState
{
	public AmbushState(Creature enemy)
		: base(enemy)
	{
	}

	public override void Enter(IState prevState)
	{
		base.Creature.SetAnimation(base.Creature.IdleAnim);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		base.Creature.TryAmbushAttack();
	}
}
