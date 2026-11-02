using FMODUnity;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class VeritaFlame : PoolingObject, IAttackSource
{
	[SerializeField]
	private ParticleSystem flameParticle;

	[SerializeField]
	private EventReference signEvent;

	[SerializeField]
	private EventReference flameEvent;

	private BoxCollider2D boxColl;

	private int signFrame;

	private int attackDurationFrame;

	private int currentFrame;

	private int attackCheckFrame;

	private int attackCycleFrame;

	private BossName owner;

	private bool playSound = true;

	public string Name => $"{owner}_Flame";

	public Direction FacingDirection
	{
		get
		{
			if (!(base.transform.localScale.x >= 0f))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
	}

	public BoxCollider2D BoxCollider => boxColl ?? (boxColl = GetComponent<BoxCollider2D>());

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public override void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Initalize(Vector2 size, int attackFrame, int signFrame, int cycleFrame, BossName flameOwner, bool sound = true)
	{
		attackDurationFrame = attackFrame;
		this.signFrame = signFrame;
		attackCycleFrame = cycleFrame;
		currentFrame = 0;
		attackCheckFrame = 0;
		owner = flameOwner;
		playSound = sound;
		if (size != Vector2.zero)
		{
			Vector2 size2 = new Vector2(size.x, size.y);
			BoxCollider.size = size2;
		}
		if (!flameParticle.IsNull())
		{
			Vector3 localPosition = flameParticle.transform.localPosition;
			localPosition.y = (0f - size.y) / 2f;
			flameParticle.transform.localPosition = localPosition;
			flameParticle.gameObject.SetActive(value: false);
		}
		base.gameObject.SetActive(value: true);
		if (!signEvent.IsNull)
		{
			SoundManager.PlayOneShotAttached(signEvent, base.transform);
		}
	}

	private void FixedUpdate()
	{
		if (currentFrame >= attackDurationFrame + signFrame)
		{
			if (!flameParticle.IsNull() && !flameParticle.isPlaying)
			{
				ReturnToPool();
			}
			return;
		}
		if (currentFrame == signFrame)
		{
			if (!flameParticle.IsNull())
			{
				flameParticle.gameObject.SetActive(value: true);
			}
			if (playSound)
			{
				SoundManager.PlayOneShot(flameEvent);
			}
		}
		currentFrame++;
		if (currentFrame <= signFrame || !CanAttack())
		{
			return;
		}
		Collider2D[] array = AttackUtility.OverlapBoxAll((Vector2)base.transform.position + BoxCollider.offset, BoxCollider.size, LayerUtility.PlayerLayer);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].TryGetComponent<PlayerMover>(out var component))
			{
				component.TryTakeDamage(new HitInfo(this));
				if (attackCheckFrame == 0)
				{
					attackCheckFrame = 1;
				}
			}
		}
	}

	private bool CanAttack()
	{
		if (attackCheckFrame == 0)
		{
			return true;
		}
		return attackCheckFrame++ % attackCycleFrame == 0;
	}
}
