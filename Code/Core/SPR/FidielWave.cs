using System.Collections.Generic;
using DG.Tweening;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class FidielWave : MonoBehaviour, IPoolableObject, IAttackSource
{
	private BoxCollider2D box2D;

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	public string PrefabKey => "Fidiel_Wave.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "FidielWave";

	public Direction FacingDirection { get; private set; }

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public void InitializeBeforeUse()
	{
		damageableList.Clear();
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Initialize(Vector2 destination, float speed)
	{
		FacingDirection = ((!((destination - (Vector2)base.transform.position).normalized.x < 0f)) ? Direction.Right : Direction.Left);
		base.transform.DOMove(destination, speed).SetEase(Ease.Linear).SetSpeedBased(isSpeedBased: true)
			.OnUpdate(HandleOverlap);
	}

	private void HandleOverlap()
	{
		AttackUtility.AttackBox(base.transform.position + new Vector3(Mathf.Abs(Box2D.offset.x) * (float)(-FacingDirection.GetSignX()), Box2D.offset.y), Box2D.size, new HitInfo(this, string.Empty), LayerUtility.PlayerLayer, damageableList);
	}
}
