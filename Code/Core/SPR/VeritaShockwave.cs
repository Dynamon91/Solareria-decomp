using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class VeritaShockwave : MonoBehaviour, IPoolableObject, IAttackSource
{
	[SerializeField]
	private ParticleSystem particle;

	[SerializeField]
	private EventReference shockwaveEvent;

	private BoxCollider2D boxColl;

	private int activeFrame;

	private int cycleFrame;

	private int currentFrame;

	private List<ParticleSystem> particles;

	public string PrefabKey => "Verita Shockwave.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "Verita_Shockwave";

	public Direction FacingDirection => Direction.None;

	private BoxCollider2D BoxCollider => boxColl ?? (boxColl = GetComponent<BoxCollider2D>());

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Initialize(float width, int activeFrame, int cycleFrame)
	{
		if (particles.IsNullOrEmpty())
		{
			particles = particle.GetComponentsInChildren<ParticleSystem>().ToList();
		}
		particles.ForEach(delegate(ParticleSystem p)
		{
			p.SetLoop(enabled: true);
		});
		Vector2 size = BoxCollider.size;
		size.x = width;
		BoxCollider.size = size;
		this.activeFrame = activeFrame;
		this.cycleFrame = cycleFrame;
		currentFrame = 0;
		Vector3 position = base.transform.position;
		position.y += size.y / 2f;
		base.transform.position = position;
		Game.Instance.SoundManager.PlayOneShotAttached(shockwaveEvent, base.transform);
	}

	private void FixedUpdate()
	{
		if (currentFrame == activeFrame)
		{
			particles.ForEach(delegate(ParticleSystem p)
			{
				p.SetLoop(enabled: false);
			});
		}
		if (currentFrame > activeFrame)
		{
			if (!particle.isPlaying)
			{
				ReturnToPool();
			}
			return;
		}
		currentFrame++;
		if (currentFrame % cycleFrame == 0)
		{
			AttackUtility.AttackBox(BoxCollider.bounds.center, BoxCollider.size, new HitInfo(this, string.Empty), LayerUtility.PlayerLayer);
		}
	}
}
