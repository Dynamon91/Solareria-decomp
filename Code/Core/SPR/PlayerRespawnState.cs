using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;

namespace SPR;

public class PlayerRespawnState : PlayerAsyncState
{
	public PlayerRespawnState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	protected override void OnEnter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.GravityFree, active: true);
		PlayerMover.SetAnimationInstantly("rez_start");
		PlayerMover.EnableVFX("rez_start");
		PlayerMover.ResetHitMaterial();
		PlayerMover.ResetRelativeVelocity();
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Immortal, active: false);
		psm.SetStateOptionActive(StateOption.GravityFree, active: false);
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		await PlayAnimationAsync("rez_start", loop: false, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		PlayerMover.SetAnimation("rez_loop");
		await UniTask.WaitForSeconds(0.2f, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		if (!token.IsCancellationRequested)
		{
			psm.SetStateOptionActive(StateOption.GravityFree, active: false);
			await PlayAnimationAsync("rez_end", loop: false, token);
			if (!token.IsCancellationRequested)
			{
				psm.ChangeState(psm.IdleState);
			}
		}
	}
}
