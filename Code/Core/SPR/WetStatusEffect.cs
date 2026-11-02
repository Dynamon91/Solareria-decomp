using Cysharp.Threading.Tasks;
using FMOD.Studio;
using SPR.UI.Tutorial;
using UnityEngine;

namespace SPR;

public class WetStatusEffect : StatusEffect
{
	private bool isResidualHeatActive;

	private PoolingParticle particle;

	private static EventInstance wetLoopSound;

	public override StatusEffectType EffectType { get; protected set; } = StatusEffectType.Wet;

	public override bool IsStackable => false;

	private static PlayerController Player => Game.Instance.Player;

	public WetStatusEffect(IDamageable target, int durationFrame)
		: base(target, durationFrame)
	{
	}

	protected override void ApplyEffect()
	{
	}

	public override void OnAdd()
	{
		Player.ForceWalk = true;
		StaminaRegain stat = Player.PlayerStat.GetStat<StaminaRegain>();
		stat.AddOther((0f - stat.Max) * 0.5f);
		FireAttack stat2 = Player.PlayerStat.GetStat<FireAttack>();
		stat2.AddOther((0f - stat2.Max) * 0.5f);
		isResidualHeatActive = Player.PlayerStat.ResidualHeat.Active;
		if (isResidualHeatActive)
		{
			Player.PlayerStat.ResidualHeat.Deactivate();
		}
		Player.mover.PSM.SetIgnoreDamage(60);
		PlayEffectAsync().Forget();
		if (!wetLoopSound.isValid())
		{
			wetLoopSound = StatusEffect.SoundManager.PlayOneShot("event:/SFX/Player/Hit/SFX_WaterDebuff");
		}
		if (Game.Instance.SaveData.WanderingData.ShouldShowTutorialMessage(ActionName.WetStatusEffect))
		{
			Game.Instance.UIManager.FieldCanvas.TutorialMessageBoard.AddMessage(ActionName.WetStatusEffect);
		}
	}

	public override void Update(StatusEffect elementEffect)
	{
		if (elementEffect is WetStatusEffect wetStatusEffect)
		{
			base.DurationFrame = wetStatusEffect.DurationFrame;
			base.CurrentFrame = 0;
			Player.mover.PSM.SetIgnoreDamage(60);
		}
	}

	public override void OnRemove(StatusEffectController statusEffectController, bool forceRemove)
	{
		base.CurrentFrame = base.DurationFrame;
		Game.Instance.Player.ForceWalk = false;
		StaminaRegain stat = Player.PlayerStat.GetStat<StaminaRegain>();
		stat.AddOther(0f - stat.Other);
		FireAttack stat2 = Player.PlayerStat.GetStat<FireAttack>();
		stat2.AddOther(0f - stat2.Other);
		if (isResidualHeatActive)
		{
			Player.PlayerStat.ResidualHeat.Activate();
		}
		StopEffectAsync().Forget();
		if (wetLoopSound.isValid())
		{
			wetLoopSound.stop(STOP_MODE.ALLOWFADEOUT);
			wetLoopSound.release();
		}
	}

	public void RemoveDuration()
	{
		base.CurrentFrame += Mathf.CeilToInt((float)base.DurationFrame / 2f);
	}

	private async UniTask PlayEffectAsync()
	{
		if ((object)particle == null)
		{
			particle = await AddressableHelper.InstantiateAsync<PoolingParticle>("Water_Debuff.prefab", Game.Instance.Player.transform);
		}
		particle.transform.localPosition = Vector3.zero;
		particle.gameObject.SetActive(value: true);
		particle.ParticleSystem.SetLoop(enabled: true);
		particle.ParticleSystem.Play();
	}

	private async UniTask StopEffectAsync()
	{
		particle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		await UniTask.WaitUntil(() => !particle.ParticleSystem.isEmitting);
		particle.gameObject.SetActive(value: false);
	}
}
