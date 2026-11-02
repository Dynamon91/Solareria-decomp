using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using UnityEngine;

namespace SPR;

public class PlayerDieState : PlayerAsyncState
{
	public PlayerDieState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		psm.SetStateOptionActive(StateOption.Immortal, active: true);
		PlayerMover.Flip(-PlayerMover.LastHitDirection);
		PlayerMover.SetAnimation("knockback_back_start", loop: false);
		PlayerMover.ResetRelativeVelocity();
		PlayerMover.gameObject.SetActive(value: false);
		(await AddressableHelper.InstantiateAsync<ParticleSystem>("VFX/Player_Death.prefab", PlayerMover.VisualCenter)).Play();
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/Player/System/SFX_Player_Death");
		await UniTask.CompletedTask;
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Immortal, active: false);
	}
}
