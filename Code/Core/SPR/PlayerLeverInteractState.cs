using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using Library;
using UnityEngine;

namespace SPR;

public class PlayerLeverInteractState : PlayerAsyncState
{
	private float horizontal;

	private readonly float startPosX;

	private readonly float leverPosX;

	private readonly int pullFrameCount;

	private readonly int startFrameCount;

	private readonly float speedFactor;

	private const int MoveSpeed = 2;

	private const string StartAnimName = "lever_start";

	private const string PullAnimName = "lever_pull";

	private const string EndAnimName = "lever_end";

	public bool IsPulling { get; private set; }

	public bool CanControl { get; private set; }

	public PlayerLeverInteractState(PlayerStateMachine machine, float leverPosX, float deltaX, float speedFactor, int startFrameCount = 60, int pullFrameCount = 60)
		: base(machine)
	{
		this.leverPosX = leverPosX;
		startPosX = leverPosX + deltaX;
		this.speedFactor = speedFactor;
		this.startFrameCount = startFrameCount;
		this.pullFrameCount = pullFrameCount;
	}

	protected override void OnEnter(IState prevState)
	{
		CanControl = true;
		IsPulling = false;
	}

	protected override void OnExit(IState nextState)
	{
		IsPulling = false;
		CanControl = false;
		PlayerMover.ResetRelativeVelocity();
	}

	protected override void OnFixedUpdate(float fdt)
	{
		if (CanControl && psm.RollingInputItem.CanExecute(Time.time))
		{
			psm.RollingInputItem.Execute();
		}
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		UniTask moveTask = MoveXPositionAsync(startPosX, speedFactor, token);
		PlayerMover.Flip(startPosX - PlayerMover.transform.position.x);
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
		PlayerMover.Flip(leverPosX - startPosX);
		PlayerMover.SetAnimation("idle");
		PlayerMover.SetAnimation("lever_start", loop: false, (float)GetAnimFrame("lever_start") / (float)startFrameCount);
		PlayerMover.ApplyRelativeVelocity(PlayerMover.FacingDirection, 2f, faceJump: false);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(startFrameCount, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		PlayerMover.ResetRelativeVelocity();
		PlayerMover.SetAnimation("lever_pull", loop: false, (float)GetAnimFrame("lever_pull") / (float)pullFrameCount);
		IsPulling = true;
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(pullFrameCount, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		PlayerMover.SetAnimation("lever_end", loop: false);
		int waitFrame = GetAnimFrame("lever_end");
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(8, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		waitFrame -= 8;
		PlayerMover.ApplyRelativeVelocity(PlayerMover.FacingDirection.Opposite(), 5f, faceJump: false);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(12, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		waitFrame -= 12;
		PlayerMover.ResetRelativeVelocity();
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(20, token);
		if (!token.IsCancellationRequested)
		{
			waitFrame -= 20;
			CanControl = false;
			PlayerMover.ApplyRelativeVelocity(PlayerMover.FacingDirection.Opposite(), 4f, faceJump: false);
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(waitFrame, token);
			if (!token.IsCancellationRequested)
			{
				PlayerMover.ResetRelativeVelocity();
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
