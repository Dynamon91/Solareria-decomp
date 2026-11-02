using System;
using UnityEngine;

namespace SPR.Field;

public class TiedPlatformRope : MonoBehaviour, IDamageable
{
	private BoxCollider2D ropeCollider;

	private TiedPlatform tiedPlatform;

	private Action onHit;

	private Action onFalling;

	public HitType HitType => HitType.Object | HitType.Platform;

	public Vector2 VisualCenter => base.transform.position;

	public BoxCollider2D RopeCollider => ropeCollider ?? (ropeCollider = GetComponent<BoxCollider2D>());

	public void Initialize(TiedPlatform tiedPlatform, Action onHit, Action onFalling)
	{
		this.tiedPlatform = tiedPlatform;
		this.onHit = onHit;
		this.onFalling = onFalling;
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		if (tiedPlatform.CurrentState != 0)
		{
			return false;
		}
		if (hitInfo.AttackerType != HitType.Player)
		{
			return false;
		}
		if (!hitInfo.AttackAttribute.HasFlag(AttackAttribute.HeavyAttack))
		{
			onHit?.Invoke();
			return false;
		}
		onFalling?.Invoke();
		return true;
	}
}
