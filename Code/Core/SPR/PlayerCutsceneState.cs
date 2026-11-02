using FSM;

namespace SPR;

public class PlayerCutsceneState : PlayerState
{
	public PlayerCutsceneState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	public override void Enter(IState prevState)
	{
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
	}
}
