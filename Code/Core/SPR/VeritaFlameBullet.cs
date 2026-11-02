using System;
using System.Collections.Generic;
using FMODUnity;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class VeritaFlameBullet : MonoBehaviour, IPoolableObject, IAttackSource
{
	private enum State
	{
		Bullet,
		Delay,
		Flame
	}

	[SerializeField]
	private GameObject bulletObject;

	[SerializeField]
	private BoxCollider2D flameCollider;

	[SerializeField]
	private EventReference projectileEvent;

	[SerializeField]
	private EventReference flameEvent;

	private Animator animator;

	private List<IDamageable> damaged;

	private ParticleSystem flameParticle;

	private float speed;

	private int flameAppearDelayFrame;

	private int flameDurationFrame;

	private int flameCycleFrame;

	private int currentFrame;

	private Vector2 endPos;

	private Vector2 direction;

	private State state;

	private static readonly int Activate = Animator.StringToHash("Activate");

	public string Name => "Verita_Flame_Bullet";

	public Direction FacingDirection
	{
		get
		{
			if (!(Player.transform.position.x > base.transform.position.x))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
	}

	public string PrefabKey => "Verita Flame Bullet.prefab";

	public GameObject GameObject => base.gameObject;

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	private static PlayerController Player => Game.Instance.Player;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private void Awake()
	{
		flameParticle = flameCollider.GetComponent<ParticleSystem>();
	}

	public void InitializeBeforeUse()
	{
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Initialize(float speed, int flameDelay, int flameDuration, int flameCycleFrame, Vector2 endPos, List<IDamageable> damaged)
	{
		this.damaged = damaged;
		flameAppearDelayFrame = flameDelay;
		flameDurationFrame = flameDuration;
		this.flameCycleFrame = flameCycleFrame;
		currentFrame = 0;
		this.speed = speed;
		this.endPos = endPos;
		direction = (endPos - (Vector2)base.transform.position).normalized;
		Animator.Rebind();
		state = State.Bullet;
		bulletObject.SetActive(value: true);
		flameCollider.gameObject.SetActive(value: false);
		base.gameObject.SetActive(value: true);
		SoundManager.PlayOneShotAttached(projectileEvent, base.transform);
	}

	private void FixedUpdate()
	{
		switch (state)
		{
		case State.Bullet:
		{
			Vector2 vector = base.transform.position;
			vector += direction * speed * Time.fixedDeltaTime;
			if (vector.y <= endPos.y)
			{
				vector = endPos;
				state = State.Delay;
				Animator.SetTrigger(Activate);
			}
			base.transform.position = vector;
			break;
		}
		case State.Delay:
			if (currentFrame >= flameAppearDelayFrame)
			{
				ReturnToPool();
			}
			else
			{
				currentFrame++;
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
