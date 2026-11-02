using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR;

public class ImahenLaserTurret : ImahenTurretBase
{
	[SerializeField]
	private int attackDuration;

	public override HitType HitType => HitType.Creature;

	protected override void Fire(float seconds)
	{
		Attack().Forget();
	}

	public override bool IsPlayerInSightBound()
	{
		Zone zoneOf = patrolSightArea.GetZoneOf(this);
		return IsPlayerInBound(zoneOf.Center, zoneOf.Size);
	}

	private async UniTaskVoid Attack()
	{
		ImahenLaser laser = Enemy.PoolManager.GetObject<ImahenLaser>("Imahen Turret Laser.prefab", GetBoneWorldPosition("Fire"));
		laser.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, AimDirection));
		laser.Play();
		for (int frame = 0; frame < attackDuration; frame++)
		{
			laser.Attack();
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
	}
}
