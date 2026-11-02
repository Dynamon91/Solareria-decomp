using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR;

public class PlayerShield : IAttackSource
{
	private int blockedDamage;

	private PlayerShieldEffect endParticle;

	private static readonly int AlphaOutlineBlend = Shader.PropertyToID("_AlphaOutlineBlend");

	private static readonly int AlphaOutlineGlow = Shader.PropertyToID("_AlphaOutlineGlow");

	private static readonly int Glow = Shader.PropertyToID("_Glow");

	private static readonly int GlowGlobal = Shader.PropertyToID("_GlowGlobal");

	private static readonly int HitEffectBlend = Shader.PropertyToID("_HitEffectBlend");

	private Sequence matSequence;

	public string Name => "Player Shield";

	public Direction FacingDirection => Direction.None;

	public int Health { get; private set; }

	private static PlayerMover PlayerMover => Game.Instance.Player.mover;

	private static int Maximum => Game.Instance.Player.PlayerStat.GetStat<ShieldMaximum>().CurrentMaximum;

	public void AddShield(int health)
	{
		int health2 = Health;
		Health += health;
		UpdateShield();
		if (Health > 0)
		{
			PlayerMover.EnableVFX("shield_start");
			if (health2 == 0)
			{
				PlayActivateSequenceAsync().GetAwaiter().OnCompleted(PlayLoopSequence);
			}
		}
	}

	public void UpdateShield()
	{
		Health = Mathf.Min(Health, Maximum);
		UpdateUI();
	}

	public bool TryTakeDamage(int damage)
	{
		if (Health <= 0)
		{
			return false;
		}
		Health -= damage;
		blockedDamage += damage;
		if (Health <= 0)
		{
			Destroy();
		}
		UpdateUI();
		return true;
	}

	public void UpdateUI()
	{
		int maxHealth = Game.Instance.Player.PlayerStat.GetStat<Health>().MaxHealth;
		Game.Instance.UIManager.FieldCanvas.PlayerStatusBoard.UpdateShieldBar(Health, maxHealth);
	}

	public void Reset(bool inactive = false)
	{
		Health = 0;
		blockedDamage = 0;
		PlayDeactivateSequenceAsync().Forget();
		UpdateUI();
		if (!endParticle.IsNull())
		{
			endParticle.Deactivate();
			if (inactive)
			{
				Object.Destroy(endParticle);
				endParticle = null;
			}
		}
	}

	private void Destroy()
	{
		Health = 0;
		Game.Instance.Player.PlayerPart.OnShieldDestroyed(blockedDamage);
		blockedDamage = 0;
		PlayDeactivateSequenceAsync().Forget();
		PlayEndEffectAsync().Forget();
	}

	private async UniTask PlayEndEffectAsync()
	{
		if ((object)endParticle == null)
		{
			endParticle = await AddressableHelper.InstantiateAsync<PlayerShieldEffect>("Shield_End", PlayerMover.VisualCenter, PlayerMover.transform);
			endParticle.Initialize();
		}
		endParticle.Activate(playOnAwake: false);
		endParticle.ParticleSystem.Play();
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(6);
	}

	private async UniTask PlayActivateSequenceAsync()
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(AlphaOutlineBlend, 0f);
		materialPropertyBlock.SetFloat(AlphaOutlineGlow, 0f);
		materialPropertyBlock.SetFloat(Glow, 0f);
		materialPropertyBlock.SetFloat(GlowGlobal, 1f);
		materialPropertyBlock.SetFloat(HitEffectBlend, 0f);
		PlayerMover.SetPropertyBlock(materialPropertyBlock);
		float fixedDeltaTime = Time.fixedDeltaTime;
		matSequence?.Kill();
		matSequence = DOTween.Sequence().SetUpdate(UpdateType.Fixed);
		matSequence.Append(materialPropertyBlock.DOFloatPropertyBlock(AlphaOutlineGlow, 1f, 9f * fixedDeltaTime, PlayerMover).SetEase(Ease.Linear));
		matSequence.Append(materialPropertyBlock.DOFloatPropertyBlock(AlphaOutlineBlend, 1f, fixedDeltaTime, PlayerMover).SetEase(Ease.Linear)).Join(materialPropertyBlock.DOFloatPropertyBlock(AlphaOutlineGlow, 5f, fixedDeltaTime, PlayerMover).SetEase(Ease.Linear)).Join(materialPropertyBlock.DOFloatPropertyBlock(Glow, 10f, fixedDeltaTime, PlayerMover).SetEase(Ease.Linear))
			.Join(materialPropertyBlock.DOFloatPropertyBlock(GlowGlobal, 2f, fixedDeltaTime, PlayerMover).SetEase(Ease.Linear))
			.Join(materialPropertyBlock.DOFloatPropertyBlock(HitEffectBlend, 1f, fixedDeltaTime, PlayerMover).SetEase(Ease.Linear));
		matSequence.Append(materialPropertyBlock.DOFloatPropertyBlock(AlphaOutlineGlow, 2f, 5f * fixedDeltaTime, PlayerMover).SetEase(Ease.Linear)).Join(materialPropertyBlock.DOFloatPropertyBlock(HitEffectBlend, 0f, 5f * fixedDeltaTime, PlayerMover).SetEase(Ease.Linear));
		matSequence.Append(materialPropertyBlock.DOFloatPropertyBlock(Glow, 1f, 20f * fixedDeltaTime, PlayerMover).SetEase(Ease.Linear));
		matSequence.Play();
		await matSequence.WaitAsync();
	}

	private void PlayLoopSequence()
	{
		matSequence?.Kill();
		matSequence = DOTween.Sequence().SetUpdate(UpdateType.Fixed);
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(GlowGlobal, 2f);
		matSequence.Append(materialPropertyBlock.DOFloatPropertyBlock(GlowGlobal, 5f, Time.fixedDeltaTime * 30f, PlayerMover)).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
	}

	private async UniTask PlayDeactivateSequenceAsync()
	{
		MaterialPropertyBlock block = new MaterialPropertyBlock();
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
