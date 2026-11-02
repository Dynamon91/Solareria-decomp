using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class WaterDragonWave : MonoBehaviour, IPoolableObject, IAttackSource
{
	private List<ParticleSystem> particles;

	private BoxCollider2D box2D;

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public string PrefabKey => "Water Dragon Wave.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "WaterDragonWave";

	public Direction FacingDirection => Direction.Right;

	private void Awake()
	{
		particles = GetComponentsInChildren<ParticleSystem>().ToList();
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

	public async UniTaskVoid MoveToOwner(Vector2 targetPosition, int duration)
	{
		float num = Mathf.Abs(Box2D.offset.y) + Box2D.size.y / 2f;
		targetPosition.y = base.transform.position.y + num;
		base.transform.position += new Vector3(0f, num);
		particles.ForEach(delegate(ParticleSystem p)
		{
			p.Play();
		});
		base.transform.DOMove(targetPosition, Util.FrameToSeconds(duration)).SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed)
			.OnUpdate(HandleOverlap);
		await UniTask.WaitUntil(() => particles.All((ParticleSystem p) => !p.isPlaying));
		ReturnToPool();
	}

	private void HandleOverlap()
	{
		Vector2 center = (Vector2)base.transform.position + Box2D.offset;
		Vector3 localScale = base.transform.localScale;
		Vector2 size = Box2D.size * localScale;
		AttackUtility.AttackBox(center, size, new HitInfo(this, string.Empty), LayerUtility.PlayerLayer, damageableList);
	}
}
