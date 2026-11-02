using System.Threading;
using Cysharp.Threading.Tasks;
using LapinLibrary;
using SPR;
using UnityEngine;

namespace FSM;

public abstract class PlayerAsyncState : PlayerState
{
	private CancellationTokenSource tokenSource;

	protected static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	protected PlayerAsyncState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	public sealed override void Enter(IState prevState)
	{
		OnEnter(prevState);
		ProgressAsync(TaskUtil.RefreshToken(ref tokenSource));
	}

	protected virtual void OnEnter(IState prevState)
	{
	}

	protected abstract UniTask ProgressAsync(CancellationToken token);

	public sealed override void Exit(IState nextState)
	{
		tokenSource?.Cancel();
		OnExit(nextState);
	}

	protected virtual void OnExit(IState nextState)
	{
	}

	public sealed override void FixedUpdate(float fdt)
	{
		OnFixedUpdate(fdt);
	}

	protected virtual void OnFixedUpdate(float fdt)
	{
	}

	protected async UniTask PlayAnimationAsync(string animName, bool loop = true, CancellationToken token = default(CancellationToken))
	{
		PlayerMover.SetAnimation(animName, loop);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(Util.SecondsToFrame(PlayerMover.GetAnimDuration(animName)), token);
	}

	protected async UniTask KnockBackAsync(int direction, Vector2 velocity, int jerk, CancellationToken token)
	{
		if (direction != 0)
		{
			PlayerMover.ApplyRelativeVelocity(velocity.x * (float)direction, velocity.y, faceJump: false);
		}
		else
		{
			PlayerMover.ResetRelativeVelocity();
		}
		float elapsedTime = 0f;
		while (true)
		{
			await PlayerUniTaskUtil.WaitOneFrame();
			if (token.IsCancellationRequested || (PlayerMover.IsAnimComplete(0) && PlayerMover.IsOnGround))
			{
				break;
			}
			elapsedTime += Time.fixedDeltaTime;
			PlayerMover.DecelerateHorizontal(direction, elapsedTime * (float)jerk);
		}
	}

	protected async UniTask HitKnockBackAsync(int direction, Vector2 knockBackDelta, CancellationToken token = default(CancellationToken))
	{
		int n = pcp.KnockBackFrame;
		float x = knockBackDelta.x;
		float dt = Time.fixedDeltaTime;
		float num = 3f * x / (float)(n + 2) / dt;
		float a0 = 2f * num / (float)(n + 1) / dt;
		float w = a0 / (float)n / dt;
		if (direction != 0)
		{
			PlayerMover.ApplyRelativeVelocity(num * (float)direction, knockBackDelta.y, faceJump: false);
		}
		else
		{
			PlayerMover.ResetRelativeVelocity();
		}
		for (int frame = 0; frame < n; frame++)
		{
			await PlayerUniTaskUtil.WaitOneFrame();
			if (token.IsCancellationRequested)
			{
				break;
			}
			float accel = a0 - w * (float)frame * dt;
			PlayerMover.DecelerateHorizontal(direction, accel);
		}
	}

	protected async UniTask KnockBackAsync(int duration, int direction, Vector2 velocity, CancellationToken token = default(CancellationToken))
	{
		if (direction != 0)
		{
			PlayerMover.ApplyRelativeVelocity(velocity.x * (float)direction, velocity.y, faceJump: false);
		}
		else
		{
			PlayerMover.ResetRelativeVelocity();
		}
		for (int frame = 0; frame < duration; frame++)
		{
			await PlayerUniTaskUtil.WaitOneFrame();
			if (token.IsCancellationRequested)
			{
				break;
			}
			PlayerMover.DecelerateHorizontal(direction, PlayerMover.LastHitInfo.KnockBack.x * 2f);
		}
	}

	protected int GetAnimFrame(string animName)
	{
		return Util.SecondsToFrame(PlayerMover.GetAnimDuration(animName));
	}
}
