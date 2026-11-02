using Library;
using SPR.FSM;
using SPR.FSM.Base;
using SPR.Library;

namespace SPR;

public class MimicPullState : CreatureState
{
	private readonly Mimic mimic;

	private readonly FrameTimer timer;

	public MimicPullState(Creature enemy, int duration)
		: base(enemy)
	{
		mimic = enemy.CastObject<Mimic>();
		timer = new FrameTimer(duration);
	}

	public override void Enter(IState prevState)
	{
		timer.Reset();
		EnemyState.Player.Stun(() => timer.IsDone);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (timer.IsDone)
		{
			ChangeState(mimic.AlertState);
		}
		mimic.PullPlayer();
		timer.Tick();
	}
}
