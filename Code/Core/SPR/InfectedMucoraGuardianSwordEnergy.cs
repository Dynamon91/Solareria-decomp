using UnityEngine;

namespace SPR;

public class InfectedMucoraGuardianSwordEnergy : Projectile
{
	[SerializeField]
	private ParticleSystem particle;

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		particle.Play(withChildren: true);
		ReturnToPoolControl = true;
	}

	protected override void OnPlatform()
	{
		ReturnToPool();
	}
}
