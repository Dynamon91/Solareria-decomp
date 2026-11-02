using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using Library;

namespace SPR;

public class PlayerSproutInteractState : PlayerAsyncState
{
	private readonly bool hasManySprouts;

	public PlayerSproutInteractState(PlayerStateMachine psm, bool hasManySprouts)
		: base(psm)
	{
		this.hasManySprouts = hasManySprouts;
	}

	protected override void OnEnter(IState prevState)
	{
		PlayerMover.ResetRelativeVelocity();
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		if (!hasManySprouts)
		{
			PlayerMover.SetAnimation("give_small", loop: false);
		}
		else
		{
			PlayerMover.SetAnimation("give_big", loop: false);
		}
		await UniTaskExtension.WaitUntilOrCancel(() => PlayerMover.IsAnimComplete(0), token);
		if (!token.IsCancellationRequested)
		{
			psm.ChangeState(psm.IdleState);
		}
	}
}
