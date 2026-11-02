using System.Collections.Generic;
using SPR.Field;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class FallingRock : MonoBehaviour, IPoolableObject, IAttackSource, IRideable, IGround, IUnclimbable
{
	[SerializeField]
	private Vector2 size;

	[SerializeField]
	private Bounds hitBounds;

	[SerializeField]
	private SoundManager.GroundMaterialType groundMaterial;

	[Header("Sprites")]
	[SerializeField]
	private List<Sprite> quarrySprites;

	private Rigidbody2D rb2D;

	private SpriteRenderer sr;

	private BoxCollider2D coll;

	private Area area;

	public static Vector2 Size;

	public string Name => "FallingRock";

	public Direction FacingDirection => Direction.None;

	public Vector2 Velocity => Rb2D.velocity;

	public SoundManager.GroundMaterialType GroundMaterial => groundMaterial;

	private Rigidbody2D Rb2D => rb2D ?? (rb2D = GetComponent<Rigidbody2D>());

	private SpriteRenderer SpriteRenderer => sr ?? (sr = GetComponent<SpriteRenderer>());

	private BoxCollider2D BoxColl => coll ?? (coll = GetComponent<BoxCollider2D>());

	private Vector3 HitBoundCenter => base.transform.position + hitBounds.center;

	public string PrefabKey => "Falling Rock.prefab";

	public GameObject GameObject => base.gameObject;

	public void OnStartRide()
	{
	}

	public void OnEndRide()
	{
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(HitBoundCenter, hitBounds.size);
	}

	private void OnValidate()
	{
		Size = size;
		SpriteRenderer.size = Size;
		BoxColl.size = Size;
		Vector3 extents = hitBounds.extents;
		extents.x = Size.x / 2f - 0.05f;
		extents.y = 1f / 32f;
		hitBounds.extents = extents;
		Vector3 center = hitBounds.center;
		center.y = (0f - Size.y) / 2f + extents.y;
		hitBounds.center = center;
	}

	public void Initialize(float speed, Area area)
	{
		base.gameObject.SetActive(value: true);
		Rb2D.velocity = Vector2.down * speed;
		SpriteRenderer.sprite = quarrySprites[Random.Range(0, quarrySprites.Count)];
		this.area = area;
	}

	private void FixedUpdate()
	{
		if (base.transform.position.y + Size.y < (float)area.AreaBox.LowerLeft.y)
		{
			ReturnToPool();
			return;
		}
		LayerMask playerLayer = LayerUtility.PlayerLayer;
		Collider2D[] array = AttackUtility.OverlapBoxAll(HitBoundCenter, hitBounds.size, playerLayer);
		if (array.Length == 0)
		{
			return;
		}
		Collider2D[] array2 = array;
		foreach (Collider2D collider2D in array2)
		{
			if ((object)collider2D.gameObject != base.gameObject && collider2D.TryGetComponent<PlayerMover>(out var component))
			{
				HitInfo hitInfo = new HitInfo(this, string.Empty);
				component.TryTakeDamage(hitInfo);
				ReturnToPool();
				break;
			}
		}
	}

	public void InitializeBeforeUse()
	{
	}

	public void ReturnToPool()
	{
		base.gameObject.SetActive(value: false);
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
