using UnityEngine;

namespace SPR.Field;

public class TransparentWall : MonoBehaviour, IDamageable
{
	public HitType HitType => HitType.Platform;

	public Vector2 VisualCenter => base.transform.position;

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		return false;
	}
}
