using Library;
using SPR.FSM;
using SPR.FSM.Base;
using SPR.Library;

namespace SPR;

public class MoomooHitWallInChargeState : BossState
{
	private const string StartAnimation = "dash_down2";

	private readonly Moomoo moomoo;

	private readonly FrameTimer timer;

	private readonly int downEndFrame;

	public MoomooHitWallInChargeState(Enemy enemy)
		: base(enemy)
	{
		moomoo = enemy.CastObject<Moomoo>();
		float animDuration = moomoo.GetAnimDuration("dash_down2");
		downEndFrame = Util.SecondsToFrame(animDuration);
		timer = new FrameTimer(Util.SecondsToFrame(animDuration));
	}

	public override void Enter(IState prevState)
	{
		moomoo.EmitQuake();
		moomoo.SetAnimation("dash_down2", loop: false);
		Game.Instance.SoundManager.PlayOneShotAttached(moomoo.CollisionEvent, moomoo.transform);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (EnemyState.Player.PSM.CurrentState == EnemyState.Player.PSM.BurstState)
		{
			timer.Tick();
		}
		else if (moomoo.IsAnimComplete(0, "dash_down2") && timer.IsDone)
		{
			moomoo.StateMachine.ChangeState(moomoo.PatrolState);
		}
		else
		{
			timer.Tick();
		}
	}
}
