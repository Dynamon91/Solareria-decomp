using FMOD.Studio;
using SPR.FSM;
using SPR.Library;

namespace SPR;

public class EnemyPierceState : EnemyState
{
	public readonly HitInfo HitInfo;

	private readonly int pauseFrame;

	private readonly FrameTimer timer;

	private EventInstance loopInstance;

	public Enemy Target => base.Enemy;

	public PoolingObject Spear { get; set; }

	public Direction HitDirection { get; set; }

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	protected virtual string HitEventPath => "event:/SFX/Char/Myoso/SFX_Myoso_SpearHit_Default";

	protected EnemyPierceState(Enemy enemy, HitInfo hitInfo, int pauseFrame)
		: base(enemy)
	{
		HitInfo = hitInfo;
		this.pauseFrame = pauseFrame;
		timer = new FrameTimer(pauseFrame);
	}

	public override void Enter(IState prevState)
	{
		if (!base.Enemy.IsDead)
		{
			base.Enemy.DisableVFXAll();
			base.Enemy.TryTakeDamage(HitInfo);
			base.Enemy.VisualKnockBack(HitDirection);
			if (!base.Enemy.enabled)
			{
				base.Enemy.enabled = true;
			}
			SoundManager.PlayOneShotAttached(HitEventPath, base.Enemy.transform);
			if (!base.Enemy.IsDead)
			{
				base.Enemy.SkeletonAnimation.timeScale = 0f;
				base.Enemy.Pause(pauseFrame).Forget();
				loopInstance = SoundManager.PlayOneShotAttached("event:/SFX/Char/Myoso/SFX_Myoso_SpearHit_Shock", base.Enemy.transform);
			}
		}
	}

	public override void Exit(IState nextState)
	{
		base.Enemy.SetAnimation("idle");
		base.Enemy.SkeletonAnimation.timeScale = 1f;
		Spear.ReturnToPool();
		loopInstance.stop(STOP_MODE.ALLOWFADEOUT);
		loopInstance.release();
	}

	public override void FixedUpdate(float fdt)
	{
		if (timer.IsDone)
		{
			if (!base.Enemy.IsDead)
			{
				base.Enemy.StateMachine.ChangeState(base.Enemy.DefaultState);
			}
		}
		else
		{
			timer.Tick();
		}
	}
}
