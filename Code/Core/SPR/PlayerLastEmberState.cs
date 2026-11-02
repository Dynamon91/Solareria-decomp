using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FSM;
using Library;
using UnityEngine;

namespace SPR;

public class PlayerLastEmberState : PlayerAsyncState
{
	private static readonly int AlphaOutlineBlend = Shader.PropertyToID("_AlphaOutlineBlend");

	private static readonly int AlphaOutlineGlow = Shader.PropertyToID("_AlphaOutlineGlow");

	private static readonly int Glow = Shader.PropertyToID("_Glow");

	private static readonly int GlowGlobal = Shader.PropertyToID("_GlowGlobal");

	private static readonly int HitEffectBlend = Shader.PropertyToID("_HitEffectBlend");

	private Sequence matSequence;

	private MaterialPropertyBlock block;

	public PlayerLastEmberState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	protected override void OnEnter(IState prevState)
	{
		PlayerMover.ResetRelativeVelocity();
		psm.SetStateOptionActive(StateOption.Immortal | StateOption.GravityFree, active: true);
		PlayActivateSequenceAsync().Forget();
		PlayerState.PlayerStat.GetStat<Health>().AddHealth(1);
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Immortal | StateOption.GravityFree, active: false);
		PlayerMover.DisableVFX("heal_start");
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		PlayerMover.SetAnimation("hit_start", loop: false);
		await UniTaskExtension.WaitUntilOrCancel(() => PlayerMover.IsAnimComplete(0), token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		PlayerMover.SetAnimation("last_ember", loop: false);
		await UniTaskExtension.WaitUntilOrCancel(() => PlayerMover.IsAnimComplete(0), token);
		if (!token.IsCancellationRequested)
		{
			psm.SetStateOptionActive(StateOption.Immortal | StateOption.GravityFree, active: false);
			psm.SetIgnoreDamage(60, delegate
			{
				PlayDeactivateSequenceAsync().Forget();
			});
			psm.ChangeState(psm.IdleState);
		}
	}

	private async UniTask PlayActivateSequenceAsync()
	{
		block = new MaterialPropertyBlock();
		block.SetFloat(AlphaOutlineBlend, 0f);
		block.SetFloat(AlphaOutlineGlow, 0f);
		block.SetFloat(Glow, 0f);
		block.SetFloat(GlowGlobal, 1f);
		block.SetFloat(HitEffectBlend, 0f);
		PlayerMover.SetPropertyBlock(block);
		float fixedDeltaTime = Time.fixedDeltaTime;
		matSequence?.Kill();
		matSequence = DOTween.Sequence().SetUpdate(UpdateType.Fixed);
		matSequence.Append(block.DOFloatPropertyBlock(AlphaOutlineGlow, 1f, 9f * fixedDeltaTime, PlayerMover).SetEase(Ease.Linear));
		matSequence.Append(block.DOFloatPropertyBlock(AlphaOutlineBlend, 1f, fixedDeltaTime, PlayerMover).SetEase(Ease.Linear)).Join(block.DOFloatPropertyBlock(AlphaOutlineGlow, 5f, fixedDeltaTime, PlayerMover).SetEase(Ease.Linear)).Join(block.DOFloatPropertyBlock(Glow, 10f, fixedDeltaTime, PlayerMover).SetEase(Ease.Linear))
			.Join(block.DOFloatPropertyBlock(GlowGlobal, 4f, fixedDeltaTime, PlayerMover).SetEase(Ease.Linear))
			.Join(block.DOFloatPropertyBlock(HitEffectBlend, 1f, fixedDeltaTime, PlayerMover).SetEase(Ease.Linear));
		matSequence.Append(block.DOFloatPropertyBlock(AlphaOutlineGlow, 2f, 5f * fixedDeltaTime, PlayerMover).SetEase(Ease.Linear)).Join(block.DOFloatPropertyBlock(HitEffectBlend, 0f, 5f * fixedDeltaTime, PlayerMover).SetEase(Ease.Linear));
		matSequence.Append(block.DOFloatPropertyBlock(Glow, 1f, 20f * fixedDeltaTime, PlayerMover).SetEase(Ease.Linear));
		matSequence.Play();
		await matSequence.WaitAsync();
		PlayLoopSequence();
	}

	private void PlayLoopSequence()
	{
		matSequence?.Kill();
		matSequence = DOTween.Sequence().SetUpdate(UpdateType.Fixed);
		block.SetFloat(GlowGlobal, 4f);
		matSequence.Append(block.DOFloatPropertyBlock(GlowGlobal, 7f, Time.fixedDeltaTime * 30f, PlayerMover)).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
		matSequence.Play();
	}

	private async UniTask PlayDeactivateSequenceAsync()
	{
		float duration = 5f * Time.fixedDeltaTime;
		matSequence?.Kill();
		matSequence = DOTween.Sequence().SetUpdate(UpdateType.Fixed);
		matSequence.Append(block.DOFloatPropertyBlock(AlphaOutlineBlend, 0f, duration, PlayerMover).SetEase(Ease.Linear)).Join(block.DOFloatPropertyBlock(AlphaOutlineGlow, 1f, duration, PlayerMover).SetEase(Ease.Linear)).Join(block.DOFloatPropertyBlock(Glow, 0f, duration, PlayerMover).SetEase(Ease.Linear))
			.Join(block.DOFloatPropertyBlock(GlowGlobal, 1f, duration, PlayerMover).SetEase(Ease.Linear))
			.Join(block.DOFloatPropertyBlock(HitEffectBlend, 0f, duration, PlayerMover).SetEase(Ease.Linear));
		matSequence.Play();
		await matSequence.WaitAsync();
		ResetPlayerMaterial(block);
	}

	private void ResetPlayerMaterial(MaterialPropertyBlock block = null)
	{
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		block.SetFloat(AlphaOutlineBlend, 0f);
		block.SetFloat(AlphaOutlineGlow, 1f);
		block.SetFloat(Glow, 0f);
		block.SetFloat(GlowGlobal, 1f);
		block.SetFloat(HitEffectBlend, 0f);
		PlayerMover.SetPropertyBlock(block);
	}
}
