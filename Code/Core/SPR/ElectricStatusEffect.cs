using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using Library;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class ElectricStatusEffect : StatusEffect, IAttackSource
{
	public const string Identifier = "Shock";

	private static ParticleSystem shockParentParticle;

	private static List<ParticleSystem> shockLoopParticles;

	private static float originDuration;

	private static EventInstance shockLoopSound;

	public override StatusEffectType EffectType { get; protected set; } = StatusEffectType.Electric;

	public override bool IsStackable => true;

	public string Name => "Shock";

	public Direction FacingDirection => Game.Instance.Player.mover.FacingDirection.Opposite();

	public ElectricStatusEffect(IDamageable target, int durationFrame)
		: base(target, durationFrame)
	{
	}

	protected override void ApplyEffect()
	{
		base.StackCount = 0;
	}

	protected override void OnRunning()
	{
	}

	public override void OnAdd()
	{
		if (Target is PlayerMover playerMover)
		{
			EnableLoopVFX(playerMover.VisualCenter + Vector2.up, playerMover.transform).Forget();
			if (!shockLoopSound.isValid())
			{
				shockLoopSound = StatusEffect.SoundManager.PlayOneShot("event:/SFX/Player/Hit/SFX_ElectricShock_Loop");
			}
		}
	}

	public override void Update(StatusEffect elementEffect)
	{
		base.StackCount = Mathf.Min(base.StackCount + 1, StatusEffect.pcp.ShockMaxCount);
		base.CurrentFrame = 0;
	}

	public override void OnRemove(StatusEffectController controller, bool forceRemove)
	{
		if (controller.Count<ElectricStatusEffect>() <= 1 || forceRemove)
		{
			shockParentParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			shockLoopSound.stop(STOP_MODE.ALLOWFADEOUT);
			shockLoopSound.release();
		}
	}

	private static async UniTaskVoid EnableLoopVFX(Vector2 position, Transform parent)
	{
		if ((object)shockParentParticle == null)
		{
			shockParentParticle = await AddressableHelper.InstantiateAsync<ParticleSystem>("Electric Shock_loop.prefab", position, parent);
			shockParentParticle.gameObject.SetActive(value: true);
			shockLoopParticles = shockParentParticle.GetComponentsInChildren<ParticleSystem>().ToList();
			shockParentParticle.GetComponent<BoneFollower>().SkeletonRenderer = Game.Instance.Player.mover.SkeletonRenderer;
			shockParentParticle.GetComponentInChildren<BoneFollower>().SkeletonRenderer = Game.Instance.Player.mover.SkeletonRenderer;
		}
		shockParentParticle.Play();
	}

	public override void SetEffectLoop(bool value)
	{
		shockParentParticle.Play();
		shockLoopSound.setParameterByName("StatusEffect", value ? 1 : 0);
	}
}
