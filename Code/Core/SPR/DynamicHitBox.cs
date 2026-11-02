using Spine.Unity;
using UnityEngine;

namespace SPR;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(BoneFollower))]
public class DynamicHitBox : MonoBehaviour, IDamageable
{
	private BoxCollider2D box2D;

	public HitType HitType => Enemy.HitType;

	public Vector2 VisualCenter => Enemy.VisualCenter;

	public BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public Enemy Enemy { get; private set; }

	public void SetEnemy(Enemy enemy)
	{
		Enemy = enemy;
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		return Enemy.TryTakeDamage(hitInfo);
	}
}
