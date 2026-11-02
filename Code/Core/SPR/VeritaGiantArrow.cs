using System;
using System.Collections.Generic;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class VeritaGiantArrow : MonoBehaviour, IPoolableObject, IAttackSource
{
	[Serializable]
	public struct GiantArrowParameter
	{
		[field: SerializeField]
		public int createFrame { get; private set; }

		[field: SerializeField]
		public int energyAttackCycleFrame { get; private set; }

		[field: SerializeField]
		public int explosionDelayFrame { get; private set; }

		[field: SerializeField]
		public Vector2 energyRange { get; private set; }

		[field: SerializeField]
		public float speed { get; private set; }
	}

	private enum State
	{
		Ready,
		Shoot,
		Paused,
		Finished
	}

	[SerializeField]
	private Bounds frontBounds;

	[SerializeField]
	private GameObject energy;

	private Collider2D col2D;

	private SkeletonAnimation sa;

	private int currentFrame;

	private AreaBox areaBox;

	private State state;

	private GiantArrowParameter parameter;

	private Action onExplode;

	private float waveSpeed;

	public string PrefabKey => "Verita Giant Arrow.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "Verita_Giant_Arrow";

	public Direction FacingDirection
	{
		get
		{
			if (!(Game.Instance.Player.mover.VisualCenter.x >= base.transform.position.x))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
	}

	public bool IsFinished => state == State.Finished;

	private Collider2D Collider2D => col2D ?? (col2D = GetComponent<Collider2D>());

	private SkeletonAnimation SkeletonAnimation => sa ?? (sa = GetComponentInChildren<SkeletonAnimation>());

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(base.transform.position + frontBounds.center, frontBounds.size);
	}

	public void InitializeBeforeUse()
	{
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Initialize(GiantArrowParameter parameter, AreaBox areaBox, float waveSpeed, bool facingRight, Action onExplode)
	{
		state = State.Ready;
		this.areaBox = areaBox;
		this.parameter = parameter;
		this.onExplode = onExplode;
		this.waveSpeed = waveSpeed;
		currentFrame = 0;
		Vector3 localScale = base.transform.localScale;
		localScale.x = (facingRight ? 1 : (-1));
		base.transform.localScale = localScale;
		energy.transform.localScale = parameter.energyRange;
		energy.SetActive(value: true);
		base.gameObject.SetActive(value: true);
		SkeletonAnimation.gameObject.SetActive(value: true);
		SkeletonAnimation.SetAnimation("arrow_large_start");
	}

	private void FixedUpdate()
	{
		switch (state)
		{
		case State.Ready:
		{
			if (currentFrame >= parameter.createFrame)
			{
				state = State.Shoot;
				SkeletonAnimation.SetAnimation("arrow_large", 0, loop: false, 0f, 0.5f);
				SkeletonAnimation.AnimationState.AddAnimation(0, "arrow_large_loop", loop: true, 0f);
				energy.SetActive(value: false);
				break;
			}
			currentFrame++;
			Collider2D collider2D = AttackUtility.OverlapBox(base.transform.position, parameter.energyRange, LayerUtility.PlayerLayer);
			if ((bool)collider2D && collider2D.TryGetComponent<PlayerMover>(out var component2))
			{
				component2.TryTakeDamage(new HitInfo(this, "Energy"));
			}
			break;
		}
		case State.Shoot:
		{
			ContactFilter2D filter = new ContactFilter2D
			{
				useTriggers = true,
				useLayerMask = true,
				layerMask = LayerUtility.PlayerLayer
			};
			List<Collider2D> results = new List<Collider2D>();
			if (AttackUtility.OverlapCollider(Collider2D, filter, out results) > 0)
			{
				for (int i = 0; i < results.Count; i++)
				{
					if (results[i].TryGetComponent<PlayerMover>(out var component))
					{
						component.TryTakeDamage(new HitInfo(this, string.Empty));
					}
				}
			}
			Vector3 vector = ((base.transform.localScale.x >= 0f) ? Vector3.right : Vector3.left);
			Vector3 position = base.transform.position;
			position.x += Collider2D.bounds.size.x / 2f * vector.x;
			if (!areaBox.IsInArea(position))
			{
				state = State.Paused;
				currentFrame = 0;
			}
			else
			{
				base.transform.position += vector * parameter.speed * Time.fixedDeltaTime;
			}
			break;
		}
		case State.Paused:
			if (currentFrame >= parameter.explosionDelayFrame)
			{
				state = State.Finished;
				SkeletonAnimation.gameObject.SetActive(value: false);
				SpawnWave();
				onExplode?.Invoke();
			}
			else
			{
				currentFrame++;
			}
			break;
		case State.Finished:
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private void SpawnWave()
	{
		VeritaExplosionWave veritaExplosionWave = Game.Instance.ObjectPoolManager.GetObject<VeritaExplosionWave>("Verita Explosion Wave.prefab", base.transform.position);
		veritaExplosionWave.SetData(isFlameWave: true);
		veritaExplosionWave.Emit(waveSpeed).Forget();
	}
}
