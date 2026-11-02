using DG.Tweening;
using UnityEngine;

namespace SPR;

public class AttackPlayerTest : MonoBehaviour, IDamageable, IAttackSource
{
	[SerializeField]
	private float moveSpeed;

	[SerializeField]
	private int moveDistance;

	[SerializeField]
	private int blockDamageFrame;

	[SerializeField]
	private int damage;

	[SerializeField]
	private int knockBackPower;

	[SerializeField]
	private AttackAttribute attackAttribute;

	private BoxCollider2D coll;

	private Tween moveTween;

	private int direction;

	private int currentFrame;

	private BoxCollider2D Coll => coll ?? (coll = GetComponent<BoxCollider2D>());

	private PlayerControlParameter pcp => PlayerControlParameter.Instance;

	public string Name => "AttackPlayerTest";

	public Direction FacingDirection
	{
		get
		{
			if (!(Game.Instance.Player.mover.VisualBottom.x - base.transform.position.x > 0f))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
	}

	public HitType HitType => HitType.Object;

	public Vector2 VisualCenter => base.transform.position;

	private void OnDrawGizmosSelected()
	{
		if (moveDistance != 0)
		{
			Vector3 center = base.transform.position + Vector3.right * moveDistance;
			Gizmos.color = Color.magenta;
			Gizmos.DrawCube(center, Coll.bounds.size);
		}
	}

	private void Awake()
	{
		direction = 1;
	}

	private void Update()
	{
		Move();
		if (currentFrame != 0)
		{
			currentFrame--;
			return;
		}
		int mask = LayerMask.GetMask("Player");
		ContactFilter2D contactFilter = new ContactFilter2D
		{
			useLayerMask = true,
			layerMask = mask
		};
		Collider2D[] array = new Collider2D[1];
		Physics2D.OverlapCollider(Coll, contactFilter, array);
		if (!(array[0] == null) && array[0].TryGetComponent<PlayerMover>(out var component))
		{
			component.TryTakeDamage(new HitInfo(this, damage, AttackDirection.Facing, attackAttribute, knockBackPower));
			currentFrame = blockDamageFrame;
		}
	}

	private void Move()
	{
		if ((moveTween == null || !moveTween.IsPlaying()) && moveDistance != 0)
		{
			float endValue = base.transform.localPosition.x + (float)(moveDistance * direction);
			moveTween = base.transform.DOLocalMoveX(endValue, (float)Mathf.Abs(moveDistance) / moveSpeed);
			moveTween.Play();
			direction *= -1;
		}
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		return true;
	}
}
