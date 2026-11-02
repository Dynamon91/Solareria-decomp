using UnityEngine;

namespace SPR;

public class KnifeProjectile : Projectile
{
	[SerializeField]
	private string nameKey;

	[SerializeField]
	private string addressableKey;

	public override string Name => nameKey;

	public override string PrefabKey => addressableKey;

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
