using SPR.Library;

namespace SPR.FSM;

public class GroggyEndState : EnemyState
{
	private FrameTimer timer;

	public GroggyEndState(Enemy enemy)
		: base(enemy)
	{
	}

	public override void Enter(IState prevState)
	{
		int targetFrame = ((!string.IsNullOrEmpty(base.Enemy.GroggyEndAnim)) ? Util.SecondsToFrame(base.Enemy.GetAnimDuration(base.Enemy.GroggyEndAnim)) : 0);
		timer = new FrameTimer(targetFrame);
		base.Enemy.SetAnimation(base.Enemy.GroggyEndAnim, loop: false);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (timer.IsDone)
		{
			ChangeState(base.Enemy.DefaultState);
		}
		timer.Tick();
	}
}
