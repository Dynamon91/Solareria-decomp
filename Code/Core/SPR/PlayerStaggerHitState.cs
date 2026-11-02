using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using UnityEngine;

namespace SPR;

public class PlayerStaggerHitState : PlayerAsyncState
{
	private Vector2 targetVelocity;

	private Vector2 targetPosition;

	private bool isInProgress;

	public int Duration { get; set; }

	public IState NextState { get; set; }

	public bool KnockBackAllowed { get; set; }

	public string Animation { get; set; } = "hit_loop";

	public bool AnimationLoop { get; set; } = true;

	public bool CancelByOtherAttack { get; set; }

	public Vector2 TargetVelocity
	{
		get
		{
			return targetVelocity;
		}
		set
		{
			if (isInProgress)
			{
				PlayerMover.ApplyRelativeVelocity(value, faceJump: false);
				targetVelocity = value;
			}
		}
	}

	public Vector2 TargetPosition
	{
		get
		{
			return targetPosition;
		}
		set
		{
			if (isInProgress)
			{
				PlayerMover.Position = value;
				targetPosition = value;
			}
		}
	}

	public event Action OnEnterAction;

	public event Action OnExitAction;

	public event Action OnChangeState;

	public PlayerStaggerHitState(PlayerStateMachine psm)
		: base(psm)
	{
		Duration = 120;
		NextState = psm.DownState;
	}

	public PlayerStaggerHitState(PlayerStateMachine psm, Action onEnter = null, Action onExit = null, Action onChangeState = null)
		: this(psm)
	{
		this.OnEnterAction = onEnter;
		this.OnExitAction = onExit;
		this.OnChangeState = onChangeState;
	}

	protected override void OnEnter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.BlockHeavyCharge, active: true);
		PlayerMover.ResetRelativeVelocity();
		PlayerMover.Flip(-PlayerMover.LastHitDirection);
		this.OnEnterAction?.Invoke();
		isInProgress = true;
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.BlockHeavyCharge, active: false);
		this.OnExitAction?.Invoke();
		isInProgress = false;
		targetVelocity = Vector2.zero;
		targetPosition = Vector2.zero;
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		if (!string.IsNullOrEmpty(Animation))
		{
			PlayerMover.SetAnimation(Animation, AnimationLoop);
		}
		if (KnockBackAllowed)
		{
			KnockBackAsync(30, PlayerMover.LastHitDirection, PlayerMover.LastHitInfo.KnockBack, token).Forget();
		}
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(Duration, token);
		if (!token.IsCancellationRequested && NextState != null && (float)PlayerState.PlayerStat.GetStat<Health>().CurrentHealth > 0f)
		{
			psm.ChangeState(NextState);
			this.OnChangeState?.Invoke();
		}
	}
}
