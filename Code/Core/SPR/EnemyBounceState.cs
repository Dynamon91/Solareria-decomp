using SPR.FSM;
using SPR.Library;

namespace SPR;

public class EnemyBounceState : EnemyState
{
	private readonly int duration;

	private FrameTimer timer;

	private bool shouldSetGroggy;

	private readonly bool useVFX;

	private readonly bool resetStun;

	public bool ResetStun => resetStun;

	public EnemyState NextState { get; set; }

	private EnemyBounceState(Enemy enemy)
		: base(enemy)
	{
		NextState = base.Enemy.DefaultState;
	}

	public EnemyBounceState(Enemy enemy, int duration, bool useVFX = true, bool resetStun = true)
		: this(enemy)
	{
		timer = new FrameTimer((duration == 0) ? 90 : duration);
		this.useVFX = useVFX;
		this.resetStun = resetStun;
	}

	public override void Enter(IState prevState)
	{
		shouldSetGroggy = false;
		base.Enemy.OnEnterBounceState();
		base.Enemy.DisableVFXAll();
		timer.Reset();
		base.Enemy.SetAnimation(base.Enemy.BounceAnim, loop: false, 1f, 0, 0f, useVFX);
	}

	public override void Exit(IState nextState)
	{
		if (resetStun)
		{
			base.Enemy.ResetStunCount();
		}
		if (shouldSetGroggy)
		{
			base.Enemy.Groggy.ChangeValueBy(0f - base.Enemy.Groggy.Value + 0.1f);
		}
	}

	public override void FixedUpdate(float fdt)
	{
		if (timer != null)
		{
			if (base.Enemy.IsDead)
			{
				timer = null;
			}
			else if (timer.IsDone)
			{
				ChangeState(NextState);
			}
			else
			{
				timer.Tick();
			}
		}
	}

	public void ChangeGroggyOnExit(bool active)
	{
		shouldSetGroggy = active;
	}
}
