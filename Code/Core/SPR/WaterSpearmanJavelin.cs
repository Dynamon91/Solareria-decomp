using FMODUnity;
using UnityEngine;

namespace SPR;

public class WaterSpearmanJavelin : Projectile
{
	[SerializeField]
	private EventReference landEvent;

	public void Throw(Vector2 direction, float speed)
	{
		AttackAvailable = true;
		MoveDirection = direction;
		Rotate(MoveDirection);
		base.OverlapSize = new Vector2(base.Box2D.size.x, base.Box2D.size.y * ((speed <= 60f) ? 1f : (speed * Time.fixedDeltaTime)));
		SetAnimation("throw", loop: false);
	}

	public void OnPlatform(RaycastHit2D hit)
	{
		PoolableTimeLimitObject poolableTimeLimitObject = Game.Instance.ObjectPoolManager.GetObject<PoolableTimeLimitObject>("WaterSpearman_Shockwave.prefab", hit.point);
		poolableTimeLimitObject.Initialize("WaterSpearman_Shockwave.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		if (hit.normal.x != 0f)
		{
			poolableTimeLimitObject.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, (0f - hit.normal.x) * 90f));
		}
		else if (hit.normal.y != 0f)
		{
			poolableTimeLimitObject.transform.rotation = Quaternion.Euler((hit.normal.y >= 1f) ? new Vector3(0f, 0f, 0f) : new Vector3(0f, 0f, 180f));
		}
		Game.Instance.SoundManager.PlayOneShotAttached(landEvent, base.transform);
		ReturnToPool();
	}
}
