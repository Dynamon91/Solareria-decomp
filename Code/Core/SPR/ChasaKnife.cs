using UnityEngine;

namespace SPR;

public class ChasaKnife : Projectile
{
	public override string Name => "ChasaKnife";

	public override string PrefabKey => "ChasaKnife.prefab";

	public override void InitializeBeforeUse()
	{
		Speed = 0f;
		base.InitializeBeforeUse();
	}

	public void Throw(Vector2 direction, float speed)
	{
		MoveDirection = direction;
		Speed = speed;
		Rotate(MoveDirection);
		SetVelocity();
	}
}
