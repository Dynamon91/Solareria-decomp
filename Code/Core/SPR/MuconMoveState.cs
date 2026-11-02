using Library;
using SPR.FSM.Base;

namespace SPR;

public class MuconMoveState : CreatureState
{
	private readonly Mucon mucon;

	public MuconMoveState(Creature enemy)
		: base(enemy)
	{
		mucon = enemy.CastObject<Mucon>();
	}

	public override void Enter(IState prevState)
	{
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (mucon.CheckDeadCreature())
		{
			ChangeState(mucon.ReviveState);
			return;
		}
		mucon.CheckArrival();
		if (!mucon.IsNewBorn)
		{
			mucon.Move(fdt);
		}
	}
}
