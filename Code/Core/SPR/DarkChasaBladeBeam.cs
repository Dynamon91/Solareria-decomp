using System.Collections.Generic;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class DarkChasaBladeBeam : MonoBehaviour, IPoolableObject, IAttackSource
{
	private BoxCollider2D box2D;

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	private DarkChasa darkChasa;

	public string PrefabKey => "DarkChasa_BladeBeam.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "DarkChasaBladeBeam";

	public Direction FacingDirection
	{
		get
		{
			if (!(base.transform.localScale.x > 0f))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
	}

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
		damageableList.Clear();
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void SetOwner(DarkChasa owner)
	{
		darkChasa = owner;
	}

	public void Overlap(float width)
	{
		if (AttackUtility.AttackBox((Vector2)base.transform.position + Box2D.offset, new Vector2(width, Box2D.size.y), new HitInfo(this, string.Empty), LayerUtility.PlayerLayer, damageableList).Count > 0)
		{
			darkChasa.Heal();
		}
	}
}
