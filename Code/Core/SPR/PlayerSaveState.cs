using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;

namespace SPR;

public class PlayerSaveState : PlayerAsyncState
{
	public PlayerSaveState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		PlayerMover.HorizontalMove(0f);
		await PlayAnimationAsync("save_start", loop: false, token);
		await PlayAnimationAsync("sleep_start", loop: false, token);
		PlayerMover.SetAnimation("sleep_loop");
	}
}
