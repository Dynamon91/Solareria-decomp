using Cysharp.Threading.Tasks;
using FSM;

namespace SPR;

public class PlayerIdleRestState : PlayerState
{
	private PlayerRestControllable restControllable;

	private static InputManager InputManager => Game.Instance.InputManager;

	private PlayerRestControllable RestControllable => restControllable ?? (restControllable = new PlayerRestControllable());

	public PlayerIdleRestState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	public override void Enter(IState prevState)
	{
		PlayerMover.SetAnimation("sit_start_slow", loop: false);
		PlayerMover.AddAnimation("sleep_loop");
		InputManager.StackControlTarget(RestControllable).Forget();
	}

	public override void Exit(IState nextState)
	{
		InputManager.RemoveControlTarget(RestControllable).Forget();
	}

	public override void FixedUpdate(float fdt)
	{
		if (!(PlayerMover.GetCurrentAnim(0).Name != "sleep_loop") && RestControllable.buttonDown)
		{
			psm.ChangeState(psm.IdleState);
		}
	}
}
