namespace SPR;

public class PlayerJumpDownState : IState
{
	private PlayerStateMachine psm;

	private int tryCount;

	public PlayerJumpDownState(PlayerStateMachine psm)
	{
		this.psm = psm;
	}

	public void Enter(IState prevState)
	{
		tryCount = 0;
	}

	public void Exit(IState nextState)
	{
	}

	public void FixedUpdate(float fdt)
	{
		tryCount++;
		if (psm.TryFall())
		{
			psm.ChangeState(psm.FallState);
		}
		else if (tryCount >= 5)
		{
			psm.ChangeState(psm.IdleState);
		}
	}
}
