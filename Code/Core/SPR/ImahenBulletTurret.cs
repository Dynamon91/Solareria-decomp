using SPR.Library;

namespace SPR;

public class ImahenBulletTurret : ImahenTurretBase
{
	public override HitType HitType => HitType.Creature;

	protected override void Fire(float speed)
	{
		Enemy.PoolManager.GetObject<Projectile>("Imahen Turret 1 Bullet.prefab", GetBoneWorldPosition("Fire")).Launch(AimDirection, speed, LayerUtility.PlayerLayer);
	}
}
