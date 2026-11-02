using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class VeritaExplosionWave : MonoBehaviour, IPoolableObject, IAttackSource
{
	[SerializeField]
	private float thickness;

	[SerializeField]
	private EventReference explosionEvent;

	private float outerRadius;

	private float innerRadius;

	private bool isHit;

	private CapsuleCollider2D playerHitCollider;

	private string dataId;

	public string Name => "Verita";

	public string PrefabKey => "Verita Explosion Wave.prefab";

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

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
		isHit = false;
		innerRadius = 0f;
		outerRadius = 0f;
		Game.Instance.SoundManager.PlayOneShotAttached(explosionEvent, base.transform);
	}

	public void ReturnToPool()
	{
		DOTween.Kill(this);
		Game.Instance.ObjectPoolManager.Return(this);
	}

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
		innerRadius = outerRadius;
		outerRadius = base.transform.localScale.x;
		foreach (Vector3 vertex in GetVertices(playerHitCollider.bounds))
		{
			if (IsInCircle(vertex, base.transform.position, outerRadius) && !IsInCircle(vertex, base.transform.position, innerRadius))
			{
				Player.TryTakeDamage(new HitInfo(this, dataId));
				isHit = true;
				break;
			}
		}
	}

	public void SetData(bool isFlameWave)
	{
		dataId = (isFlameWave ? "Flame_Wave" : "Kick_Wave");
	}

	public async UniTaskVoid Emit(float speed)
	{
		await base.transform.DOScale(100f, speed).SetSpeedBased().SetUpdate(UpdateType.Fixed)
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

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Vector3 position = base.transform.position;
		Gizmos.DrawWireSphere(position, outerRadius);
		Gizmos.DrawWireSphere(position, innerRadius);
	}
}
