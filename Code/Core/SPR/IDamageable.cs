using UnityEngine;

namespace SPR;

public interface IDamageable
{
	HitType HitType { get; }

	Vector2 VisualCenter { get; }

	bool TryTakeDamage(HitInfo hitInfo);
}
