using UnityEngine;

namespace SPR;

public class SeedArcherArrow : Projectile
{
	public override string Name => "SeedArcherArrow";

	public override string PrefabKey => "Seed Archer Arrow.prefab";

	public override void InitializeBeforeUse()
	{
		Speed = 0f;
		base.InitializeBeforeUse();
	}

	public void Shoot(Vector3 dir, float speed)
	{
		MoveDirection = dir.normalized;
		Speed = speed;
		Rotate(MoveDirection);
		SetVelocity();
	}
}
