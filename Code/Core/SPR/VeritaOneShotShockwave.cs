using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class VeritaOneShotShockwave : MonoBehaviour, IPoolableObject, IAttackSource
{
	[SerializeField]
	private ParticleSystem bombParticle;

	private BoxCollider2D boxColl;

	private SpriteRenderer sr;

	private bool isHit;

	private int activeFrame;

	private int cycleFrame;

	private int currentFrame;

	public string PrefabKey => "Verita OneShot Shockwave.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "Verita_Shockwave_2";

	public Direction FacingDirection => Direction.None;

	private BoxCollider2D BoxCollider => boxColl ?? (boxColl = GetComponent<BoxCollider2D>());

	private SpriteRenderer SR => sr ?? (sr = GetComponent<SpriteRenderer>());

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Initialize(Vector2 size, int activeFrame, int cycleFrame)
	{
		BoxCollider.size = size;
		SR.size = size;
		this.activeFrame = activeFrame;
		this.cycleFrame = cycleFrame;
		currentFrame = 0;
		bombParticle.Play();
	}

	private void FixedUpdate()
	{
		if (currentFrame >= activeFrame)
		{
			if (!bombParticle.isPlaying)
			{
				ReturnToPool();
			}
			return;
		}
		currentFrame++;
		if (currentFrame % cycleFrame == 0)
		{
			Collider2D collider2D = AttackUtility.OverlapBox(base.transform.position, BoxCollider.size, LayerUtility.PlayerLayer);
			if ((bool)collider2D && collider2D.TryGetComponent<PlayerMover>(out var component))
			{
				component.TryTakeDamage(new HitInfo(this, string.Empty));
			}
		}
	}
}
