using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using Library;
using UnityEngine;

namespace SPR;

public class PlayerBoxInteractState : PlayerAsyncState
{
	private bool canControl;

	private readonly float startPosX;

	private readonly float boxPosX;

	private readonly float speedFactor;

	private readonly int pushDelayFrame;

	private readonly int interactFrame;

	private readonly int pushFrame;

	private float horizontal;

	public bool IsBoxPushed { get; private set; }

	public PlayerBoxInteractState(PlayerStateMachine machine, float startPosX, float boxPosX, float speedFactor, int pushDelayFrame, int pushFrame, int interactFrame)
		: base(machine)
	{
		this.startPosX = startPosX;
		this.boxPosX = boxPosX;
		this.speedFactor = speedFactor;
		this.pushDelayFrame = pushDelayFrame;
		this.interactFrame = interactFrame;
		this.pushFrame = pushFrame;
		canControl = true;
	}

	protected override void OnFixedUpdate(float fdt)
	{
		if (canControl && psm.RollingInputItem.CanExecute(Time.time))
		{
			psm.RollingInputItem.Execute();
		}
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		UniTask moveTask = MoveXPositionAsync(startPosX, speedFactor, token);
		int sign = (startPosX - PlayerMover.transform.position.x).GetSign();
		PlayerMover.Flip(sign);
		PlayerMover.SetAnimation("walk");
		while (true)
		{
			if (token.IsCancellationRequested)
			{
				return;
			}
			if (moveTask.Status != UniTaskStatus.Pending)
			{
				break;
			}
			PlayerMover.HorizontalMove(horizontal);
			await PlayerUniTaskUtil.WaitOneFrame();
		}
		if (token.IsCancellationRequested)
		{
			return;
		}
		PlayerMover.HorizontalMove(0f);
		PlayerMover.Flip((boxPosX - startPosX).GetSign());
		PlayerMover.SetAnimation("push", loop: false);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(pushDelayFrame, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		IsBoxPushed = true;
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(pushFrame, token);
		if (!token.IsCancellationRequested)
		{
			canControl = false;
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(Mathf.Max(interactFrame - pushFrame, 0), token);
			if (!token.IsCancellationRequested)
			{
				psm.ChangeState(psm.IdleState);
			}
		}
	}

	private async UniTask MoveXPositionAsync(float targetX, float moveSpeed, CancellationToken token = default(CancellationToken))
	{
		float x = PlayerMover.transform.position.x;
		float initialDelta = targetX - x;
		if (Mathf.Abs(initialDelta) < 0.01f)
		{
			return;
		}
		int moveDirection = initialDelta.GetSign();
		while (true)
		{
			if (token.IsCancellationRequested)
			{
				horizontal = 0f;
				return;
			}
			if (initialDelta.GetSign() != (targetX - PlayerMover.transform.position.x).GetSign())
			{
				break;
			}
			horizontal = (float)moveDirection * moveSpeed;
			await UniTask.Yield();
		}
		PlayerMover.transform.position = new Vector2(targetX, PlayerMover.transform.position.y);
		horizontal = 0f;
	}
}
