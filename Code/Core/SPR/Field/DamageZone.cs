using SPR.Library;
using UnityEngine;

namespace SPR.Field;

public abstract class DamageZone : MonoBehaviour, IAttackSource
{
	[SerializeField]
	private int knockBackPower;

	[Tooltip("사용하지 않는 경우 (0,0)으로 설정하세요.")]
	[SerializeField]
	private Vector2Int fixedRelocatePosition;

	private Collider2D coll;

	private const int DelayFrame = 60;

	private int frameCount;

	private Collider2D Collider => coll ?? (coll = GetComponent<Collider2D>());

	public abstract string Name { get; }

	public Direction FacingDirection => Direction.None;

	private void FixedUpdate()
	{
		if (frameCount > 0)
		{
			frameCount--;
			return;
		}
		ContactFilter2D filter = new ContactFilter2D
		{
			useTriggers = true,
			useLayerMask = true,
			layerMask = LayerUtility.PlayerLayer
		};
		if (AttackUtility.OverlapCollider(Collider, filter, out var results) <= 0)
		{
			return;
		}
		foreach (Collider2D item in results)
		{
			if (item.TryGetComponent<PlayerMover>(out var component))
			{
				frameCount = 60;
				if (fixedRelocatePosition != Vector2Int.zero)
				{
					component.SetLastGroundPosition(fixedRelocatePosition);
				}
				HitInfo hitInfo = new HitInfo(this);
				hitInfo.SetKnockBack(new Vector2(knockBackPower, 0f));
				component.TryTakeDamage(hitInfo);
				break;
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (!(fixedRelocatePosition == Vector2Int.zero))
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawCube(fixedRelocatePosition + 1.5f * Vector2.up, new Vector3(2f, 3f, 1f));
		}
	}
}
