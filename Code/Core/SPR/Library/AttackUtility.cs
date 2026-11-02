using System.Collections.Generic;
using UnityEngine;

namespace SPR.Library;

public static class AttackUtility
{
	public static List<IDamageable> AttackBox(Vector2 center, Vector2 size, HitInfo hitInfo, LayerMask attackLayer, List<IDamageable> attackedList = null)
	{
		return Attack_Internal(OverlapBoxAll(center, size, attackLayer), hitInfo, attackedList);
	}

	public static List<IDamageable> AttackBox(Vector2 center, Vector2 size, float angle, HitInfo hitInfo, LayerMask attackLayer, List<IDamageable> attackedList = null)
	{
		return Attack_Internal(OverlapBoxAll(center, size, angle, attackLayer), hitInfo, attackedList);
	}

	public static List<IDamageable> AttackCircle(Vector2 center, float radius, HitInfo hitInfo, LayerMask attackLayer, List<IDamageable> attackedList = null)
	{
		return Attack_Internal(OverlapCircleAll(center, radius, attackLayer), hitInfo, attackedList);
	}

	public static Collider2D OverlapCircle(Vector2 center, float radius, int layer)
	{
		return Physics2D.OverlapCircle(center, radius, layer);
	}

	public static Collider2D[] OverlapCircleAll(Vector2 center, float radius, int layer)
	{
		return Physics2D.OverlapCircleAll(center, radius, layer);
	}

	public static Collider2D OverlapBox(Vector2 center, Vector2 size, int layer)
	{
		return Physics2D.OverlapBox(center, size, 0f, layer);
	}

	public static Collider2D[] OverlapBoxAll(Vector2 center, Vector2 size, int layer)
	{
		return Physics2D.OverlapBoxAll(center, size, 0f, layer);
	}

	public static Collider2D[] OverlapBoxAll(Vector2 center, Vector2 size, float angle, int layer)
	{
		return Physics2D.OverlapBoxAll(center, size, angle, layer);
	}

	public static int OverlapCollider(Collider2D collider2D, ContactFilter2D filter, out List<Collider2D> results)
	{
		results = new List<Collider2D>();
		return Physics2D.OverlapCollider(collider2D, filter, results);
	}

	private static List<IDamageable> Attack_Internal(Collider2D[] overlappedColls, HitInfo hitInfo, List<IDamageable> attackedList)
	{
		List<IDamageable> list = new List<IDamageable>();
		for (int i = 0; i < overlappedColls.Length; i++)
		{
			if (overlappedColls[i].TryGetComponent<IDamageable>(out var component) && (hitInfo.TypeToAttack & component.HitType) != HitType.None && (attackedList == null || !attackedList.Contains(component)))
			{
				list.Add(component);
			}
		}
		if (list.Count == 0)
		{
			return new List<IDamageable>();
		}
		foreach (IDamageable item in list)
		{
			item.TryTakeDamage(hitInfo);
			if (attackedList != null && !hitInfo.AttackAttribute.HasFlag(AttackAttribute.Continuous))
			{
				attackedList.Add(item);
			}
		}
		return list;
	}
}
