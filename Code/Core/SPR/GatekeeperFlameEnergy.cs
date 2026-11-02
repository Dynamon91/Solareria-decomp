using System.Collections.Generic;
using DG.Tweening;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class GatekeeperFlameEnergy : MonoBehaviour, IPoolableObject, IAttackSource
{
	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	private Vector2 lastPosition;

	private float delta;

	private BoxCollider2D box2D;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public string PrefabKey => "Gatekeeper Flame Energy.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "GatekeeperFlameEnergy";

	public Direction FacingDirection
	{
		get
		{
			if (!(base.transform.localScale.x < 0f))
			{
				return Direction.Right;
			}
			return Direction.Left;
		}
	}

	public void InitializeBeforeUse()
	{
		damageableList.Clear();
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Discharge(float speed, Vector2 end)
	{
		lastPosition = base.transform.position;
		delta = speed * Time.fixedDeltaTime;
		base.transform.DOMove(end, speed).SetSpeedBased(isSpeedBased: true).SetEase(Ease.Linear)
			.SetUpdate(UpdateType.Fixed)
			.OnUpdate(HandleOverlap)
			.OnComplete(ReturnToPool);
	}

	private void HandleOverlap()
	{
		int sign = base.transform.localScale.x.GetSign();
		lastPosition = base.transform.position;
		lastPosition.x += (float)(-sign) * (Box2D.size.x * delta / 2f);
		AttackUtility.AttackBox(size: new Vector2(Box2D.size.x * delta, Box2D.size.y), center: lastPosition, hitInfo: new HitInfo(this, string.Empty), attackLayer: LayerUtility.PlayerLayer, attackedList: damageableList);
	}
}
