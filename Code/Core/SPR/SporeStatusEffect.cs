using Cysharp.Threading.Tasks;
using FMOD.Studio;
using Library;
using UnityEngine;

namespace SPR;

public class SporeStatusEffect : StatusEffect
{
	private PoolingParticle effect;

	private EventInstance sporeLoopSound;

	public override StatusEffectType EffectType { get; protected set; } = StatusEffectType.Spore;

	public override bool IsStackable => true;

	public SporeStatusEffect(IDamageable target)
		: base(target, 10)
	{
	}

	protected override void ApplyEffect()
	{
		ClearStack();
	}

	public override void OnAdd()
	{
		if (Target is PlayerMover playerMover)
		{
			if ((object)effect == null)
			{
				PlayEffectAsync(playerMover.VisualCenter, playerMover.transform).Forget();
			}
			if (!sporeLoopSound.isValid())
			{
				sporeLoopSound = StatusEffect.SoundManager.PlayOneShot("event:/SFX/Player/Hit/SFX_SporeInfected");
			}
		}
	}

	public override void Update(StatusEffect elementEffect)
	{
		if (elementEffect is SporeStatusEffect)
		{
			base.StackCount = Mathf.Min(base.StackCount + 1, StatusEffect.pcp.SporeMaxCount);
			base.CurrentFrame = 0;
		}
	}

	public override void OnRemove(StatusEffectController statusEffectController, bool forceRemove)
	{
		if (!effect.IsNull())
		{
			effect.StopWithDestroy(withChildren: true).Forget();
			effect = null;
		}
		if (sporeLoopSound.isValid())
		{
			sporeLoopSound.stop(STOP_MODE.ALLOWFADEOUT);
			sporeLoopSound.release();
		}
	}

	protected override void OnRunning()
	{
		base.CurrentFrame = 0;
	}

	private async UniTaskVoid PlayEffectAsync(Vector2 position, Transform parent)
	{
		effect = await AddressableHelper.InstantiateAsync<PoolingParticle>("Spore_Debuff.prefab", position, parent);
		effect.transform.localPosition = Vector3.zero;
		effect.ParticleSystem.Play();
	}
}
