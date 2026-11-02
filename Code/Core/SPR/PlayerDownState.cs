using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using UnityEngine;

namespace SPR;

public class PlayerDownState : PlayerAsyncState
{
	private bool isOnGround;

	private bool postDelay;

	private readonly Vector2 dustOffset = new Vector2(0.5f, -1f);

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public bool DownForwardCurrentPosition { get; set; }

	public PlayerDownState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	protected override void OnEnter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.BlockHeavyCharge, active: true);
		isOnGround = false;
		postDelay = false;
		SoundManager.PlayOneShot("event:/SFX/Player/Hit/SFX_KnockDown");
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		bool isMovingForward = PlayerMover.transform.localScale.x >= 0f == PlayerMover.LastHitDirection > 0;
		if (PlayerMover.LastHitDirection == 0)
		{
			PlayerMover.SetAnimation(DownForwardCurrentPosition ? "knockback_front_start" : "knockback_back_start", loop: false);
		}
		else
		{
			PlayerMover.SetAnimation(isMovingForward ? "knockback_front_start" : "knockback_back_start", loop: false);
		}
		UniTask task = KnockBackAsync(PlayerMover.LastHitDirection, PlayerAsyncState.pcp.KnockDownVelocity, PlayerAsyncState.pcp.KnockDownAccelJerk, token);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(20, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		while (true)
		{
			if (token.IsCancellationRequested)
			{
				return;
			}
			if (task.Status != UniTaskStatus.Pending)
			{
				break;
			}
			if (PlayerMover.IsOnGround)
			{
				Vector3 pos = PlayerMover.transform.position + new Vector3(dustOffset.x * (float)(PlayerMover.IsFacingRight ? 1 : (-1)), dustOffset.y);
				PoolableTimeLimitObject poolableTimeLimitObject = Game.Instance.ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Knockback_LandingDust.prefab", pos, PlayerMover.transform);
				poolableTimeLimitObject.Initialize("VFX/Knockback_LandingDust.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject.gameObject));
				isOnGround = true;
				break;
			}
			await PlayerUniTaskUtil.WaitOneFrame();
		}
		await task;
		if (!token.IsCancellationRequested)
		{
			if (PlayerMover.LastHitDirection == 0)
			{
				PlayerMover.SetAnimation(DownForwardCurrentPosition ? "knockback_front_end" : "knockback_back_end", loop: false);
			}
			else
			{
				PlayerMover.SetAnimation(isMovingForward ? "knockback_front_end" : "knockback_back_end", loop: false);
			}
			postDelay = true;
		}
	}

	protected override void OnExit(IState nextState)
	{
		DownForwardCurrentPosition = false;
		psm.SetStateOptionActive(StateOption.BlockHeavyCharge, active: false);
		Game.SetTimeScale(1f);
	}

	protected override void OnFixedUpdate(float fdt)
	{
		float time = Time.time;
		if (isOnGround)
		{
			if (psm.DownRollingInputItem.CanExecute(time))
			{
				psm.DownRollingInputItem.Execute();
				return;
			}
			if (psm.OverheatInputItem.CanExecute(time))
			{
				psm.OverheatInputItem.Execute();
				return;
			}
			if (psm.OverheatInputItem.CanExecute(Time.time))
			{
				psm.OverheatInputItem.Execute();
				return;
			}
		}
		if (postDelay)
		{
			if (psm.RollingInputItem.CanExecute(time))
			{
				psm.RollingInputItem.Execute();
			}
			else if (psm.GuardInputItem.CanExecute(time))
			{
				psm.GuardInputItem.Execute();
			}
			else if (PlayerMover.IsAnimComplete(0))
			{
				psm.ChangeState(psm.IdleState);
			}
		}
	}
}
