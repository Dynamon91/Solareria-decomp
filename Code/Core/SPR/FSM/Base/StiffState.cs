namespace SPR.FSM.Base;

public class StiffState : EnemyState
{
	private bool shouldSetGroggy;

	private EnemyState nextState;

	public StiffState(Enemy enemy)
		: base(enemy)
	{
	}

	public override void Enter(IState prevState)
	{
		base.Enemy.SkeletonAnimation.timeScale = 0.0625f;
		nextState = base.Enemy.DefaultState;
		shouldSetGroggy = false;
	}

	public override void Exit(IState nextState)
	{
		ResetTimeScale();
	}

	public override void FixedUpdate(float fdt)
	{
	}

	public void ResetTimeScale()
	{
		base.Enemy.SkeletonAnimation.timeScale = 1f;
	}

	public bool TryChangeGroggy()
	{
		if (shouldSetGroggy)
		{
			base.Enemy.Groggy.ChangeValueBy(0f - base.Enemy.Groggy.Value);
			ChangeState(nextState);
			return true;
		}
		return false;
	}

	public void SetGroggy(GroggyState groggyState)
	{
		shouldSetGroggy = true;
		nextState = groggyState;
	}
}
