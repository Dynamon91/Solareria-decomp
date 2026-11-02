namespace SPR;

public class PlayerDoubleJumpUpState : PlayerJumpUpState
{
	public PlayerDoubleJumpUpState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	protected override void SetStartAnimation()
	{
		PlayerMover.SetAnimation("double_jump_up", loop: false);
	}
}
