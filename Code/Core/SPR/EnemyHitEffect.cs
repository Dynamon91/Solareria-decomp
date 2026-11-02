using UnityEngine;

namespace SPR;

public class EnemyHitEffect : PoolableTimeLimitObject
{
	[SerializeField]
	private ParticleSystem blackFogParticle;

	public void SetBlackFogParticle(bool active, float angle = 0f)
	{
		blackFogParticle.transform.rotation = Quaternion.Euler(0f, 0f, angle);
		blackFogParticle.gameObject.SetActive(active);
	}
}
