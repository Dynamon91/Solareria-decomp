using Library;
using SPR.FSM.Base;
using SPR.Library;

namespace SPR;

public class MoomooHitPlayerInChargeState : BossState
{
	private readonly Moomoo moomoo;

	private readonly FrameTimer timer;

	public MoomooHitPlayerInChargeState(Enemy enemy)
		: base(enemy)
	{
		moomoo = enemy.CastObject<Moomoo>();
		timer = new FrameTimer(Util.SecondsToFrame(moomoo.GetAnimDuration("dash_hit")));
	}

	public override void Enter(IState prevState)
	{
		timer.Reset();
		moomoo.SetAnimation("dash_hit", loop: false);
		Game.Instance.SoundManager.PlayOneShotAttached(moomoo.CollisionEvent, moomoo.transform);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (timer.IsDone)
		{
			moomoo.StateMachine.ChangeState(moomoo.PatrolState);
		}
		else
		{
			timer.Tick();
		}
	}
}
