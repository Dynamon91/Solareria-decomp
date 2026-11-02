using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using UnityEngine;

namespace SPR;

public class PlayerKnockBackState : PlayerAsyncState
{
	private int knockBackDirection;

	private Vector2 knockBackVelocity;

	private float waitSeconds;

	public PlayerKnockBackState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	protected override void OnExit(IState nextState)
	{
		waitSeconds = 0f;
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		PlayerMover.Flip(-knockBackDirection);
		PlayerMover.SetAnimation("knockback_back_start", loop: false, 1f, 0, 0f, vfxEnable: false);
		await KnockBackAsync(knockBackDirection, knockBackVelocity, PlayerAsyncState.pcp.KnockBackAccelJerk, token);
		await UniTask.WaitForSeconds(waitSeconds, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		PlayerMover.SetAnimation("knockback_back_end", loop: false, 1f, 0, 0f, vfxEnable: false);
		psm.ChangeState(psm.IdleState);
	}

	public void SetKnockBackVelocity(int direction, Vector2 velocity)
	{
		knockBackDirection = direction;
		knockBackVelocity = velocity;
	}

	public void SetWaitSeconds(float seconds)
	{
		waitSeconds = seconds;
	}
}
