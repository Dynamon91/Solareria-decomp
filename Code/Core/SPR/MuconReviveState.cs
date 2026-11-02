using Library;
using SPR.FSM.Base;

namespace SPR;

public class MuconReviveState : CreatureState
{
	private readonly Mucon mucon;

	public MuconReviveState(Creature enemy)
		: base(enemy)
	{
		mucon = enemy.CastObject<Mucon>();
	}

	public override void Enter(IState prevState)
	{
		mucon.Revive().Forget();
	}

	public override void Exit(IState nextState)
	{
		mucon.OnReviveExit();
	}

	public override void FixedUpdate(float fdt)
	{
		if (mucon.ReviveComplete)
		{
			mucon.StateMachine.ChangeState(mucon.DefaultState);
		}
	}
}
