using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using Library;
using SPR.FSM.Base;
using SPR.Field;
using SPR.Library;
using SPR.ObjectPool;
using Spine;
using UnityEngine;

namespace SPR;

public class InfectedResearcher : Creature, IPoolableObject
{
	private enum Type
	{
		A = 1,
		B
	}

	private enum Variation
	{
		V1 = 1,
		V2,
		V3
	}

	[SerializeField]
	private Type researcherType;

	[SerializeField]
	private Variation variation;

	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<InfectedResearcher> chase;

	[SerializeField]
	private GenericAttackSequence<InfectedResearcher> explode;

	[Header("References")]
	[SerializeField]
	private ParticleSystem idleSmoke;

	[SerializeField]
	private EventReference idleEvent;

	[SerializeField]
	private SpriteRenderer[] explodeSprite;

	private Vector2 gravityVelocity;

	private float jumpElapsed;

	private int def;

	private Vector2 jumpStartPosition;

	private Vector2 jumpDestination;

	private EventInstance idleInstance;

	public override string Name => variation switch
	{
		Variation.V2 => $"InfectedResearcher_{researcherType}_2", 
		Variation.V3 => $"InfectedResearcher_{researcherType}_3", 
		_ => $"InfectedResearcher_{researcherType}", 
	};

	public override string ID => Name;

	public string PrefabKey => $"Infected Researcher_{researcherType}_{(int)variation}.prefab";

	public GameObject GameObject => base.gameObject;

	public override Bounds Bounds => HitBox.bounds;

	protected override int OriginDef => def;

	protected override bool UseDeathFireVFX => false;

	private void OnEnable()
	{
		CreatureCanvas?.Initialize(Bounds.size.y, base.MaxStunCount);
	}

	public void InitializeBeforeUse()
	{
		base.SkeletonRenderer.Skeleton.SetAlpha(1f);
		base.gameObject.SetActive(value: true);
		HitBox.enabled = true;
		SetColorAsync(Color.white, 0f, 0).Forget();
		if ((object)CreatureCanvas != null)
		{
			ResetStatus();
		}
		jumpDestination = Vector2.zero;
		base.Money = 0;
		base.Exp = 0;
		AddAnimationEvent(AnimState.Complete, OnAnimationComplete);
	}

	public void ReturnToPool()
	{
		RemoveAnimationEvent(AnimState.Complete, OnAnimationComplete);
		Enemy.PoolManager.Return(this);
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AddAction(ActionTiming.Begin, "Explode", Explode);
		AddAction(ActionTiming.OnFrame, "CheckPlayerDistance", CheckPlayerDistance);
		AddAction(ActionTiming.Begin, "Jump", (Action<int, Parameter[]>)delegate
		{
			jumpStartPosition = base.Position;
			jumpDestination = Enemy.Player.GetPositionFromGroud();
			jumpElapsed = 1f;
		});
		AddAction(ActionTiming.OnFrame, "Jump", delegate(int duration, Parameter[] p)
		{
			Jump(duration, p[0].Float, p[1].Float);
		});
		if (researcherType == Type.A)
		{
			AddAttack(chase, () => new List<AttackSequence> { chase });
		}
		else
		{
			AddAttack(chase, () => new List<AttackSequence> { explode });
		}
		idleInstance = Enemy.SoundManager.PlayOneShotAttached(idleEvent, base.transform);
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if (!IsDead && !idleInstance.isValid())
		{
			idleInstance = Enemy.SoundManager.PlayOneShotAttached(idleEvent, base.transform);
		}
	}

	public override void MoveX(float velocityX, float deltaTime, bool moveForward)
	{
		if (researcherType == Type.A)
		{
			Vector2 vector = (moveForward ? FacingDirection.ToVector2() : FacingDirection.Opposite().ToVector2());
			RaycastHit2D raycastHit2D = Physics2D.Raycast(VisualCenter, FacingDirection.ToVector2(), velocityX * deltaTime + HitBox.size.x / 2f, (int)LayerUtility.PlatformLayer | (int)LayerUtility.ObjectLayer);
			if ((bool)raycastHit2D)
			{
				base.transform.position = new Vector2(raycastHit2D.point.x + (float)(-FacingDirection.GetSignX()) * HitBox.size.x / 2f, base.transform.position.y);
			}
			else
			{
				base.transform.position += (Vector3)vector * (velocityX * deltaTime);
			}
			if (PathFinder.FindNode(VisualBottom) == null)
			{
				PathFinder.Node node = PathFinder.FindNode(Physics2D.Raycast(VisualCenter, Vector2.down, float.PositiveInfinity, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer).point);
				if (node != null)
				{
					Vector3 target = new Vector3(base.transform.position.x, node.Position.y);
					base.transform.position = Vector3.MoveTowards(base.transform.position, target, Mathf.Abs(gravityVelocity.y) * deltaTime);
					gravityVelocity += Physics2D.gravity;
				}
			}
			else
			{
				gravityVelocity = Vector2.zero;
			}
		}
		else
		{
			base.MoveX(velocityX, deltaTime, moveForward);
		}
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (Game.Instance.Player.isDead)
		{
			return false;
		}
		if (!HitBox.enabled)
		{
			return false;
		}
		def = (hitInfo.AttackName.Contains("Turret") ? base.EnemyStatData.Def[1] : base.EnemyStatData.Def[0]);
		int num = (hitInfo.AttackAttribute.HasFlag(AttackAttribute.FixedDamage) ? CalculateFixedDamage(hitInfo.Damage, hitInfo.FireDamage) : CalculateDamage(hitInfo.Damage, hitInfo.FireDamage));
		int value = health.Value;
		if (health.Value > 0)
		{
			health -= num;
			OnChangeHealth(health.Value - value);
			if (health.Value <= 0)
			{
				Die();
				return true;
			}
		}
		OnDamaged(hitInfo);
		TakeGroggyDamage(hitInfo);
		return true;
	}

	protected override void OnDie()
	{
		base.OnDie();
		DOTween.Kill(base.transform);
		idleSmoke.Stop();
		idleInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		idleInstance.release();
		SpriteRenderer[] array = explodeSprite;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].DOAlpha(0f, 0.5f).SetEase(Ease.Linear);
		}
	}

	public override bool IsPlayerInSightBound()
	{
		Zone zoneOf = patrolSightArea.GetZoneOf(this);
		if (!IsPlayerInBound(zoneOf.Center, zoneOf.Size))
		{
			return false;
		}
		Vector2 normalized = (Enemy.Player.VisualCenter - VisualCenter).normalized;
		float distance = Mathf.Min((Enemy.Player.VisualCenter - VisualCenter).magnitude, patrolSightArea.Size.x / 2f);
		return (object)Physics2D.Raycast(VisualCenter, normalized, distance, (int)LayerUtility.ObjectLayer | (int)LayerUtility.PlatformLayer).collider == null;
	}

	public override bool IsPlayerInAnyAttackBound()
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(VisualCenter, FacingDirection.ToVector2(), float.PositiveInfinity, LayerUtility.PlatformLayer);
		bool flag = Vector2.Distance(Enemy.Player.VisualBottom, VisualBottom) > raycastHit2D.distance;
		if (base.IsPlayerInAnyAttackBound())
		{
			return !flag;
		}
		return false;
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { chase, explode };
	}

	private void Explode()
	{
		if (!base.transform.parent.TryGetComponent<ImahenArea>(out var _))
		{
			Game.Instance.SaveData.WorldData.AddEliminatedCreature(Game.Instance.CurrentFieldID, base.PlaceholderPosition);
		}
		CreatureCanvas.SetActive(value: false);
		health -= health.Value;
		DOTween.Kill(base.transform);
		idleSmoke.Stop();
		base.StatusEffectController.Clear();
		base.StateMachine.ChangeState(null);
		HitBox.enabled = false;
	}

	private void Jump(int duration, float jumpHeight, float maxChaseDistance)
	{
		float num = jumpElapsed / (float)duration;
		if (Mathf.Abs(jumpStartPosition.x - Enemy.Player.GetPositionFromGroud().x) <= maxChaseDistance)
		{
			jumpDestination = Enemy.Player.GetPositionFromGroud();
		}
		Vector2 position = Vector2.Lerp(jumpStartPosition, jumpDestination, num);
		position.y += Mathf.Sin(MathF.PI * num) * jumpHeight;
		base.Position = position;
		jumpElapsed += 1f;
	}

	private void CheckPlayerDistance()
	{
		if (IsPlayerInAttackBound(explode))
		{
			base.StateMachine.ChangeState(new AttackState(this, () => new List<AttackSequence> { explode }, forceAttack: true));
		}
	}

	private void OnAnimationComplete(TrackEntry track)
	{
		string text = track.Animation.Name;
		if ((text == "death" || text == "pop") && Enemy.PoolManager.HasPool(PrefabKey))
		{
			Disappear().Forget();
		}
	}

	private async UniTaskVoid Disappear()
	{
		await base.SkeletonRenderer.Skeleton.DOAlpha(0f, 1f).SetEase(Ease.Linear).WaitAsync();
		ReturnToPool();
	}
}
