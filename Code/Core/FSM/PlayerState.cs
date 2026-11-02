using SPR;

namespace FSM;

public abstract class PlayerState : IState
{
	protected readonly PlayerMover PlayerMover;

	protected readonly PlayerStateMachine psm;

	public virtual bool CanExitOnSceneLoading => true;

	protected static PlayerController PlayerController => Game.Instance.Player;

	protected static PlayerStat PlayerStat => PlayerController.PlayerStat;

	protected PlayerState(PlayerStateMachine machine)
	{
		psm = machine;
		PlayerMover = machine.Mover;
	}

	public abstract void Enter(IState prevState);

	public abstract void Exit(IState nextState);

	public abstract void FixedUpdate(float fdt);
}
