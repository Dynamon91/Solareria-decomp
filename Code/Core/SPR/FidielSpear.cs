using FMODUnity;
using UnityEngine;

namespace SPR;

public class FidielSpear : Projectile
{
	[Header("Aerial Strike")]
	[SerializeField]
	private Zone aerialStrikeZone;

	[Header("Effect")]
	[SerializeField]
	private ParticleSystem[] effects;

	[SerializeField]
	private ParticleSystem[] aerialStrikeEffects;

	[SerializeField]
	private ParticleSystem[] strongJumpEffects;

	[Header("Hit Effect")]
	[SerializeField]
	private ParticleSystem hitEffect;

	[SerializeField]
	private ParticleSystem aerialStrikeHitEffect;

	[SerializeField]
	private EventReference hitEvent;

	public Vector2 AerialStrikeCenter => aerialStrikeZone.GetZoneOf(this).Center;

	public Vector2 AerialStrikeSize => aerialStrikeZone.GetZoneOf(this).Size;

	public void EnableEffect(bool value)
	{
		ParticleSystem[] array = effects;
		foreach (ParticleSystem particleSystem in array)
		{
			if (value)
			{
				particleSystem.Play();
			}
			else
			{
				particleSystem.Stop();
			}
		}
	}

	public void EnableAerialStrikeEffect(bool value)
	{
		ParticleSystem[] array = aerialStrikeEffects;
		foreach (ParticleSystem particleSystem in array)
		{
			if (value)
			{
				particleSystem.Play();
			}
			else
			{
				particleSystem.Stop();
			}
		}
	}

	public void EnableStrongJumpEffect(bool value)
	{
		ParticleSystem[] array = strongJumpEffects;
		foreach (ParticleSystem particleSystem in array)
		{
			if (value)
			{
				particleSystem.Play();
			}
			else
			{
				particleSystem.Stop();
			}
		}
	}

	public void EmitHitEffect(bool isHitFloor)
	{
		EnableEffect(value: false);
		hitEffect.Play();
		PoolingParticle poolingParticle = Game.Instance.ObjectPoolManager.GetObject<PoolingParticle>("Fidiel_Spear_Hit_2.prefab", base.transform.position);
		poolingParticle.transform.rotation = Quaternion.identity;
		if (!isHitFloor)
		{
			poolingParticle.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
		}
		poolingParticle.Play();
	}

	public void EmitAerialStrikeHitEffect()
	{
		EnableAerialStrikeEffect(value: false);
		aerialStrikeHitEffect.Play();
		Game.Instance.SoundManager.PlayOneShotAttached(hitEvent, base.transform);
		PoolingParticle poolingParticle = Game.Instance.ObjectPoolManager.GetObject<PoolingParticle>("Fidiel_Spear_Hit_2.prefab", base.transform.position);
		poolingParticle.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
		poolingParticle.Play();
	}

	protected override void OnDrawGizmosSelected()
	{
		Zone zoneOf = aerialStrikeZone.GetZoneOf(this);
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(zoneOf.Center, zoneOf.Size);
	}
}
