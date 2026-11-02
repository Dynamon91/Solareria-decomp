using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class MoomooStompWave : MonoBehaviour, IPoolableObject, IAttackSource
{
	[SerializeField]
	private string id;

	[SerializeField]
	private string address;

	[SerializeField]
	private float thickness;

	private float outerRadius;

	private float innerRadius;

	private bool isHit;

	private CapsuleCollider2D playerHitCollider;

	private float speed;

	public string Name => id;

	public string PrefabKey => address;

	public GameObject GameObject => base.gameObject;

	public Direction FacingDirection
	{
		get
		{
			if ((Player.VisualCenter - (Vector2)base.transform.position).ToDirection().GetSignX() >= 0)
			{
				return Direction.Right;
			}
			return Direction.Left;
		}
	}

	private static PlayerMover Player => Game.Instance.Player.mover;

	private void Awake()
	{
		if (Game.Instance.Player.TryGetComponent<CapsuleCollider2D>(out var component))
		{
			playerHitCollider = component;
		}
	}

	private void FixedUpdate()
	{
		if (isHit)
		{
			return;
		}
		outerRadius = base.transform.localScale.x - speed * Time.fixedDeltaTime;
		innerRadius = outerRadius - thickness;
		foreach (Vector3 vertex in GetVertices(playerHitCollider.bounds))
		{
			if (IsInCircle(vertex, base.transform.position, outerRadius) && !IsInCircle(vertex, base.transform.position, innerRadius))
			{
				Player.TryTakeDamage(new HitInfo(this, string.Empty));
				isHit = true;
				break;
			}
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Vector3 position = base.transform.position;
		Gizmos.DrawWireSphere(position, outerRadius);
		Gizmos.DrawWireSphere(position, innerRadius);
	}

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
		isHit = false;
	}

	public void ReturnToPool()
	{
		DOTween.Kill(this);
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public async UniTaskVoid Emit(float speed)
	{
		this.speed = speed;
		if (thickness < speed / 60f)
		{
			thickness = speed / 60f;
		}
		await base.transform.DOScale(100f, speed).SetSpeedBased(isSpeedBased: true).SetUpdate(UpdateType.Fixed)
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
