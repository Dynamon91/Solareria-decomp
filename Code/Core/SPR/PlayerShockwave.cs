using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public abstract class PlayerShockwave : MonoBehaviour, IPoolableObject, IAttackSource
{
	[SerializeField]
	private CircleCollider2D outerCircleCollider2D;

	[SerializeField]
	private CircleCollider2D innerCircleCollider2D;

	private ContactFilter2D filter;

	private float minRadius;

	private float maxRadius;

	public abstract string Name { get; }

	public abstract string PrefabKey { get; }

	public GameObject GameObject => base.gameObject;

	public Direction FacingDirection => Direction.None;

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		DOTween.Kill(this);
		Game.Instance.ObjectPoolManager.Return(this);
		OnReturn();
	}

	protected void SetRadius(float min, float max)
	{
		minRadius = min;
		maxRadius = max;
		base.transform.localScale = new Vector3(minRadius, minRadius);
	}

	protected virtual void OnReturn()
	{
	}

	private void Awake()
	{
		filter = new ContactFilter2D
		{
			useTriggers = true,
			useLayerMask = true,
			layerMask = LayerUtility.MonsterLayer
		};
	}

	private void FixedUpdate()
	{
		AttackUtility.OverlapCollider(outerCircleCollider2D, filter, out var results);
		foreach (Collider2D item in results)
		{
			foreach (Vector3 vertex in GetVertices(item.bounds))
			{
				if (!IsInCircle(vertex, innerCircleCollider2D.bounds.center, innerCircleCollider2D.radius * base.transform.lossyScale.x) && item.TryGetComponent<IDamageable>(out var component))
				{
					OnContactDamageable(component);
					break;
				}
			}
		}
	}

	protected virtual void OnContactDamageable(IDamageable damageable)
	{
	}

	public async UniTask EmitAsync(float speed)
	{
		await base.transform.DOScale(maxRadius, speed).SetSpeedBased().SetEase(Ease.Linear)
			.SetUpdate(UpdateType.Fixed)
			.WaitAsync();
		base.transform.localScale = Vector3.one * 0.1f;
		ReturnToPool();
	}

	private static bool IsInCircle(Vector3 target, Vector3 center, float radius)
	{
		return Mathf.Pow(target.x - center.x, 2f) + Mathf.Pow(target.y - center.y, 2f) < Mathf.Pow(radius, 2f);
	}

	private IEnumerable<Vector3> GetVertices(Bounds bounds)
	{
		return new Vector3[4]
		{
			bounds.min,
			new Vector3(bounds.max.x, bounds.min.y),
			new Vector3(bounds.min.x, bounds.max.y),
			bounds.max
		};
	}
}
