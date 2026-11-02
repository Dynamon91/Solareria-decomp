using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using LapinLibrary;
using Library;
using SPR;
using SPR.Cutscene;
using SPR.FSM;
using SPR.Field;
using SPR.Graphic;
using SPR.Library;
using SPR.Save;
using SPR.UI.Memory;
using SPR.UI.Tutorial;
using StaticData;
using UnityEngine;

public class PlayerMover : Character, IDamageable
{
	public struct AttackParameter
	{
		public readonly IAttackSource AttackSource;

		public readonly IDamageable AttackTarget;

		public readonly AttackAttribute AttackAttribute;

		public readonly int Damage;

		public readonly int FireDamage;

		public readonly float Groggy;

		public AttackParameter(IDamageable damageable, string skillDataID, int index, AttackAttribute attackAttribute, IAttackSource attackSource = null, bool random = true, float multiplier = 0f)
		{
			AttackSource = attackSource ?? Game.Instance.Player.mover;
			AttackTarget = damageable;
			SkillData skillData = Game.DataManager.GetSkillData(skillDataID);
			bool flag = skillData.FireCoefficients[index] > skillData.Coefficients[index];
			AttackAttribute = (AttackAttribute)((int)attackAttribute | (flag ? 262144 : 0));
			(int, int) damage = GetDamage(skillDataID, index, attackAttribute, random, multiplier);
			Damage = damage.Item1;
			FireDamage = damage.Item2;
			Groggy = ((multiplier == 0f) ? PlayerStat.GetCurrentGroggyDamage(skillDataID, index) : PlayerStat.GetMultipliedGroggyDamage(skillDataID, index, multiplier));
		}

		private static (int damage, int fireDamage) GetDamage(string id, int index, AttackAttribute attackAttribute, bool random = true, float multiplier = 0f)
		{
			if (attackAttribute.HasFlag(AttackAttribute.Counter))
			{
				float rate = PlayerStat.GetStat<CounterAttack>().Rate;
				multiplier = ((multiplier == 0f) ? rate : (rate * multiplier));
			}
			if (multiplier == 0f)
			{
				if (!random)
				{
					return PlayerStat.GetDamageResultWithoutRandom(id, index);
				}
				return PlayerStat.GetDamageResult(id, index);
			}
			return PlayerStat.GetMultipliedDamageResult(id, index, multiplier);
		}
	}

	[Header("Movement")]
	[ReadOnlyInspector]
	[SerializeField]
	protected Vector2 relativeVelocity;

	[ReadOnlyInspector]
	[SerializeField]
	protected float targetGravity;

	[ReadOnlyInspector]
	[SerializeField]
	private bool isOnGround;

	[ReadOnlyInspector]
	[SerializeField]
	private float slopeAngle;

	[ReadOnlyInspector]
	[SerializeField]
	private bool isUnderCeiling;

	[ReadOnlyInspector]
	[SerializeField]
	private bool isRelocating;

	[SerializeField]
	private CircleCollider2D groundCheckCollider;

	[Header("Attack")]
	[SerializeField]
	private Transform hitBoxParent;

	[Header("Visual")]
	[SerializeField]
	private PlayerHeadParticleController headController;

	[SerializeField]
	private CinemachineImpulseSource attackImpulseSource;

	[SerializeField]
	private CinemachineImpulseSource downImpulseSource;

	[Header("Reference")]
	[SerializeField]
	private Transform motionEffect;

	private readonly List<IDamageable> damaged = new List<IDamageable>();

	private readonly List<Collider2D> hitColliders = new List<Collider2D>();

	private List<IRideable> newRideables = new List<IRideable>();

	private IRideable currentRideable;

	private Vector2 rideableVelocity;

	private PlayerStateMachine psm;

	private CapsuleCollider2D cc;

	private Rigidbody2D rb2D;

	private RideableBone ridingBone;

	private CancellationTokenSource knockBackCancellationTokenSource;

	private ContactFilter2D groundFilter;

	private const float SlopeCheckDistance = 0.9f;

	private readonly Vector2 ceilingBox = new Vector2(0.7f, 0.06f);

	private readonly Vector2 parkourCheckPos = new Vector2(0.5f, 1.5f);

	private readonly Vector2 parkourCheckSize = new Vector2(1f, 0.1f);

	private MeshRenderer firstMeshRenderer;

	private bool isFourthEffectPlaying;

	private int heatBurstIndex;

	private const string HitAfterheat = "HitAfterheat";

	public override string Name => "Player";

	public PlayerStateMachine PSM => psm ?? (psm = new PlayerStateMachine(this));

	public float LastGroundedTime { get; private set; }

	public float LastHitTime { get; private set; }

	public int LastAttackDirection { get; private set; }

	public int LastHitDirection { get; private set; }

	public HitInfo LastHitInfo { get; private set; }

	public PlayerShield PlayerShield { get; private set; } = new PlayerShield();

	public float MovementSpeedFactor { get; set; }

	public IReadOnlyList<IDamageable> Damaged => damaged;

	private static LayerMask WhatIsGround => (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer;

	private static LayerMask WhatIsCeiling => LayerUtility.PlatformLayer;

	private static ObjectPoolManager ObjectPoolManager => Game.Instance.ObjectPoolManager;

	private static GraphicManager GraphicManager => Game.Instance.GraphicManager;

	private static FieldController FieldController => Game.Instance.FieldController;

	private static SettingData SettingData => Game.Instance.SettingData;

	private static EnemyFightData EnemyFightData => Game.Instance.SaveData.EnemyFightData;

	private static MenuMemoryPanel MemoryPanel => Game.Instance.UIManager.MenuCanvas.MemoryPanel;

	private CapsuleCollider2D CapsuleCollider => cc ?? (cc = GetComponent<CapsuleCollider2D>());

	private bool IsGravityFree => PSM.StateOption.HasFlag(StateOption.GravityFree);

	private float Acceleration => (InputManager.IsNull() || InputManager.IsCurrentControlTarget(PlayerController)) ? (isOnGround ? pcp.GroundAccel : pcp.AirAccel) : 0;

	public float Gravity => targetGravity;

	private Vector2 CeilingCheckPos
	{
		get
		{
			Vector2 offset = CapsuleCollider.offset;
			Vector2 size = CapsuleCollider.size;
			return offset + new Vector2(0f, 1.05f) * size / 2f + (Vector2)base.transform.position;
		}
	}

	private Vector2 ParkourCheckPos => (Vector2)base.transform.position + new Vector2((float)(IsFacingRight ? 1 : (-1)) * parkourCheckPos.x, parkourCheckPos.y);

	private static PlayerController PlayerController => Game.Instance.Player;

	private static PlayerStat PlayerStat => PlayerController.PlayerStat;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	public Bounds VisualBounds
	{
		get
		{
			if ((object)firstMeshRenderer == null)
			{
				firstMeshRenderer = MeshRenderer.transform.GetChild(0).GetComponent<MeshRenderer>();
			}
			return firstMeshRenderer.bounds;
		}
	}

	public Bounds ColliderBounds => CapsuleCollider.bounds;

	public Vector2 ColliderSize => CapsuleCollider.size;

	public Vector2 LastGroundPosition { get; private set; }

	public bool IsOnOneWayPlatform { get; private set; }

	public bool IsOnGround => isOnGround;

	public bool IsRelocating => isRelocating;

	public Vector2 RelativeVelocity => relativeVelocity;

	public bool IsFacingRight => base.transform.localScale.x > 0f;

	public HitType HitType => HitType.Player;

	public bool IsRiding => currentRideable != null;

	public bool CanHit
	{
		get
		{
			if (!IsImmortal && !IsDodge)
			{
				return !PlayerController.IsInSceneLoadSequence;
			}
			return false;
		}
	}

	public bool CanGrab
	{
		get
		{
			if (!IsImmortal)
			{
				return !IgnoreDamage;
			}
			return false;
		}
	}

	public bool IsImmortal
	{
		get
		{
			if (!PSM.StateOption.HasFlag(StateOption.Immortal))
			{
				return Game.Instance.DevController.IsImmortal;
			}
			return true;
		}
	}

	public bool IgnoreDamage => PSM.IgnoreDamageFrame > 0;

	private bool IsDodge => PSM.StateOption.HasFlag(StateOption.Dodge);

	public override Vector2 VisualBottom => base.transform.position + base.SkeletonAnimation.transform.localPosition;

	private Vector2 Parkour1stRayPos => (Vector2)base.transform.position + new Vector2(0f, 1.5f);

	private Vector2 Parkour2ndRayPos => (Vector2)base.transform.position + new Vector2((float)(IsFacingRight ? 1 : (-1)) * 1f, 1.5f);

	public event Action<IAttackSource> OnDie = delegate
	{
	};

	public void OnDeactivate()
	{
		psm.CancelInput();
		psm.ChangeState(psm.IdleState);
		ResetHitMaterial();
		PlayerShield.Reset(inactive: true);
	}

	protected override void Awake()
	{
		base.Awake();
		rb2D = GetComponent<Rigidbody2D>();
		targetGravity = pcp.OriginGravity;
		hitColliders.Clear();
		hitColliders.AddRange(hitBoxParent.GetComponentsInChildren<Collider2D>());
		hitColliders.ForEach(delegate(Collider2D x)
		{
			x.gameObject.SetActive(value: false);
		});
		groundFilter = new ContactFilter2D
		{
			layerMask = WhatIsGround,
			useLayerMask = true,
			useTriggers = true
		};
		(from p in motionEffect.GetComponentsInChildren<ParticleSystem>()
			select p.main).ForEach((ParticleSystem.MainModule m) => m.playOnAwake = false);
	}

	protected override void CullingSpine()
	{
	}

	private void CalculateRigidbodyVelocity()
	{
		targetGravity = GravityControl();
		relativeVelocity = GetNextRelativeVelocity();
		base.transform.rotation = Quaternion.identity;
		Vector2 velocity;
		if (Mathf.Abs(slopeAngle - 0f) < 0.01f || Mathf.Abs(slopeAngle - 180f) < 0.01f)
		{
			velocity = relativeVelocity;
		}
		else
		{
			velocity = slopeAngle.ToDirectionVector() * Mathf.Abs(relativeVelocity.x);
			velocity.y += relativeVelocity.y;
		}
		if (!ridingBone.IsNull())
		{
			ridingBone.ManualFixedUpdate();
			velocity += ridingBone.GetVelocity();
		}
		velocity += rideableVelocity;
		rb2D.velocity = velocity;
	}

	private Vector2 GetNextRelativeVelocity()
	{
		Vector2 vector = Physics2D.gravity * (targetGravity * Time.fixedDeltaTime);
		Vector2 result = relativeVelocity + vector;
		if (result.y < (float)(-pcp.TerminalSpeed))
		{
			result.y = -pcp.TerminalSpeed;
		}
		if (isOnGround && result.y < 0f)
		{
			result.y = 0f;
		}
		if (isUnderCeiling && result.y > 5f)
		{
			result.y = 5f;
		}
		return result;
	}

	private bool IsThereGround()
	{
		List<Collider2D> list = new List<Collider2D>();
		Physics2D.OverlapCollider(groundCheckCollider, groundFilter, list);
		if (base.gameObject.layer == LayerUtility.PlayerLayerIndex && list.Count > 0 && relativeVelocity.y < 1f)
		{
			UpdateRideable(list);
			if (!isOnGround)
			{
				GroundingEvent();
			}
			slopeAngle = CheckSlope();
			LastGroundedTime = Time.time;
			PathFinder.Node node = PathFinder.FindNode(VisualBottom);
			if (node != null && !isRelocating)
			{
				SetLastGroundPosition(node.Position);
			}
			IsOnOneWayPlatform = IsAllOneWayPlatform(list);
			return true;
		}
		slopeAngle = 0f;
		IsOnOneWayPlatform = false;
		return false;
	}

	public void SetLastGroundPosition(Vector2 position)
	{
		LastGroundPosition = position;
	}

	private float CheckSlope()
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(VisualBottom, Vector2.down, 0.9f, WhatIsGround);
		Vector2 vector = new Vector2((rb2D.velocity.x > 0f) ? 0.125f : (-0.125f), 0f);
		RaycastHit2D raycastHit2D2 = Physics2D.Raycast(VisualBottom + vector, Vector2.down, 0.9f, WhatIsGround);
		if (!raycastHit2D && !raycastHit2D2)
		{
			return 0f;
		}
		return Util.CalculateSlopeAngle((rb2D.velocity.x != 0f) ? raycastHit2D2.normal : raycastHit2D.normal, relativeVelocity.x > 0f);
	}

	public void UnloadRideable()
	{
		if (IsRiding && currentRideable is OneWayPlatform)
		{
			currentRideable = null;
		}
	}

	private void UpdateRideable(List<Collider2D> colls)
	{
		foreach (Collider2D coll in colls)
		{
			if (coll.TryGetComponent<IRideable>(out var component))
			{
				newRideables.Add(component);
			}
		}
	}

	private void CheckRideable()
	{
		IRideable rideable = currentRideable;
		currentRideable = null;
		if (ShouldChangeRideable(rideable, out var newRide))
		{
			rideable?.OnEndRide();
			if (newRide != null)
			{
				currentRideable = newRide;
				if (!(currentRideable is OneWayPlatform))
				{
					currentRideable.OnStartRide();
				}
			}
		}
		else if (rideable != null)
		{
			currentRideable = rideable;
		}
		rideableVelocity = currentRideable?.Velocity ?? Vector2.zero;
		newRideables.Clear();
	}

	private bool ShouldChangeRideable(IRideable currentRide, out IRideable newRide)
	{
		newRide = null;
		if (currentRide == null)
		{
			if (newRideables.Count == 0)
			{
				return false;
			}
			List<IRideable> list = newRideables;
			newRide = list[list.Count - 1];
			return true;
		}
		if (newRideables.Count == 0)
		{
			return true;
		}
		List<IRideable> list2 = newRideables;
		newRide = list2[list2.Count - 1];
		return currentRide != newRide;
	}

	private bool IsAllOneWayPlatform(List<Collider2D> colls)
	{
		OneWayPlatform oneWayPlatform = null;
		foreach (Collider2D coll in colls)
		{
			if (coll.gameObject.layer != LayerUtility.OneWayPlatformLayerIndex)
			{
				return false;
			}
			if (coll.TryGetComponent<OneWayPlatform>(out var component))
			{
				oneWayPlatform = component;
			}
		}
		if (oneWayPlatform.IsNull())
		{
			return false;
		}
		oneWayPlatform.OnContact();
		return true;
	}

	private bool IsThereCeiling()
	{
		Collider2D[] array = Physics2D.OverlapBoxAll(CeilingCheckPos, ceilingBox, 0f, WhatIsCeiling);
		if (relativeVelocity.y >= 0f)
		{
			return array.Length != 0;
		}
		return false;
	}

	public bool TryGetParkourEdgePos(out Vector2 edgePos)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(Parkour1stRayPos, Vector2.down, 1.5f, LayerUtility.OneWayPlatformLayer);
		RaycastHit2D raycastHit2D2 = Physics2D.Raycast(Parkour2ndRayPos, Vector2.down, 1.5f, LayerUtility.PlatformLayer);
		bool flag = !raycastHit2D.collider.IsNull() && raycastHit2D.distance > 0.01f;
		IUnclimbable component;
		bool flag2 = !raycastHit2D2.collider.IsNull() && !raycastHit2D2.collider.TryGetComponent<IUnclimbable>(out component) && raycastHit2D2.distance > 0.01f;
		if (!isOnGround && relativeVelocity.y < -0.01f && (flag || flag2))
		{
			edgePos = (flag ? new Vector2(VisualCenter.x, raycastHit2D.centroid.y + 1.1f) : new Vector2(VisualCenter.x + (float)(IsFacingRight ? 1 : (-1)), raycastHit2D2.centroid.y + 1.1f));
			return true;
		}
		edgePos = Vector2.zero;
		return false;
	}

	private void GroundingEvent()
	{
		if (PSM.CurrentState != PSM.DownState)
		{
			PoolableTimeLimitObject poolableTimeLimitObject = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("Landing dust.prefab", VisualBottom);
			poolableTimeLimitObject.Initialize("Landing dust.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject.gameObject));
		}
	}

	private float GravityControl()
	{
		if (IsGravityFree)
		{
			return 0f;
		}
		return pcp.OriginGravity;
	}

	internal void Move()
	{
		isOnGround = IsThereGround();
		isUnderCeiling = IsThereCeiling();
		PSM.FixedUpdate(Time.fixedDeltaTime);
		CheckRideable();
		CalculateRigidbodyVelocity();
		headController.Update(relativeVelocity);
	}

	public void ResetRelativeVelocity()
	{
		relativeVelocity = Vector2.zero;
	}

	public void JumpCancel()
	{
		if (relativeVelocity.y > 0f)
		{
			relativeVelocity.y = 0f;
		}
	}

	public void HorizontalMove(float dir)
	{
		float num = relativeVelocity.x;
		float num2 = dir * (float)pcp.RunSpeed * MovementSpeedFactor;
		if (Acceleration != 0f && dir != 0f)
		{
			if (Mathf.Abs(num - num2) < 0.05f)
			{
				return;
			}
			if (num * dir < 0f)
			{
				num = 0f;
			}
			else if (num2 > num)
			{
				num += Acceleration * Time.fixedDeltaTime;
				if (num > num2)
				{
					num = num2;
				}
			}
			else if (num2 < num)
			{
				num -= Acceleration * Time.fixedDeltaTime;
				if (num < num2)
				{
					num = num2;
				}
			}
		}
		else
		{
			num = num2;
		}
		relativeVelocity.x = num;
		MovementSpeedFactor = 1f;
	}

	public void DecelerateHorizontal(int movingDirection, float accel)
	{
		float x = relativeVelocity.x;
		float num = x - (float)movingDirection * accel * Time.fixedDeltaTime;
		x = ((num.GetSign() != movingDirection) ? 0f : num);
		relativeVelocity.x = x;
	}

	public void ApplyRelativeVelocity(Vector2 velocity, bool faceJump)
	{
		relativeVelocity = velocity;
		if (faceJump)
		{
			Flip(velocity.x);
		}
	}

	public void ApplyRelativeVelocity(float x, float y, bool faceJump)
	{
		ApplyRelativeVelocity(new Vector2(x, y), faceJump);
	}

	public void ApplyRelativeVelocity(Direction direction, float speed, bool faceJump)
	{
		Vector2 vector = direction.ToVector2() * speed;
		ApplyRelativeVelocity(vector.x, vector.y, faceJump);
	}

	public async UniTask KnockBackAsync(int duration, Vector2 velocity, CancellationToken token)
	{
		token = ((token != CancellationToken.None) ? token : TaskUtil.RefreshToken(ref knockBackCancellationTokenSource));
		int num = ((LastHitDirection != 0) ? LastHitDirection : FacingDirection.GetSignX());
		velocity.x = velocity.x * 60f / (float)duration;
		if (num != 0)
		{
			ApplyRelativeVelocity(velocity.x * (float)num, velocity.y, faceJump: false);
		}
		else
		{
			ResetRelativeVelocity();
		}
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(duration, token);
		ResetRelativeVelocity();
	}

	public void CancelKnockBack()
	{
		knockBackCancellationTokenSource?.Cancel();
	}

	private int CalculateDamage(int attack, bool ignoreDefense)
	{
		float num = (float)(PlayerController.StatusEffectController.Count<ElectricStatusEffect>() * pcp.RatioPerShockCount) / 100f;
		int num2 = Mathf.RoundToInt((float)attack * num);
		attack += num2;
		float num3 = 100f - 100f / (1f + PlayerStat.GetStat<Defense>().Rate);
		return Mathf.CeilToInt((float)attack * (ignoreDefense ? 1f : (1f - num3 / 100f)));
	}

	public bool CanParry(HitInfo hitInfo)
	{
		return CanParry(hitInfo.AttackAttribute, hitInfo.GetDirection(VisualBottom).GetSignX());
	}

	private bool CanParry(AttackAttribute attackAttribute, int hitDirection)
	{
		Difficulty difficulty = Game.Instance.SaveData.Difficulty;
		if (difficulty == Difficulty.Story && CanGuard(attackAttribute, hitDirection))
		{
			return true;
		}
		if (attackAttribute.HasFlag(AttackAttribute.Continuous))
		{
			return false;
		}
		if (attackAttribute.HasFlag(AttackAttribute.IgnoreGuard))
		{
			return false;
		}
		if (difficulty == Difficulty.Main && hitDirection != 0 && IsFacingRight == hitDirection > 0)
		{
			return false;
		}
		if (!PSM.ParryInputItem.CanExecute(Time.time))
		{
			return PSM.StateOption.HasFlag(StateOption.Parry);
		}
		return true;
	}

	public bool CanGuard(AttackAttribute attackAttribute, int hitDirection)
	{
		Difficulty difficulty = Game.Instance.SaveData.Difficulty;
		if (attackAttribute.HasFlag(AttackAttribute.Continuous))
		{
			return false;
		}
		if (attackAttribute.HasFlag(AttackAttribute.IgnoreGuard))
		{
			return false;
		}
		if (difficulty == Difficulty.Main && hitDirection != 0 && IsFacingRight == hitDirection > 0)
		{
			return false;
		}
		return psm.StateOption.HasFlag(StateOption.Guard);
	}

	private bool CanDodge(AttackAttribute attackAttribute)
	{
		if (!IsDodge)
		{
			return false;
		}
		if (attackAttribute.HasFlag(AttackAttribute.RedFear) && PlayerController.PlayerPart.CanDodgePaleblow())
		{
			return true;
		}
		if (attackAttribute.HasFlag(AttackAttribute.IgnoreDodge))
		{
			return false;
		}
		return true;
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		if (PlayerController.IsInSceneLoadSequence)
		{
			return false;
		}
		LastHitInfo = hitInfo;
		if (IsImmortal)
		{
			return false;
		}
		if (CanDodge(hitInfo.AttackAttribute))
		{
			return false;
		}
		LastAttackDirection = hitInfo.GetDirection(VisualBottom).GetSignX();
		if (hitInfo.Source is MonoBehaviour monoBehaviour && !hitInfo.FixDirection)
		{
			LastHitDirection = ((LastAttackDirection == 0) ? (VisualCenter.x - monoBehaviour.transform.position.x).GetSign() : LastAttackDirection);
		}
		else if (hitInfo.Source is Enemy enemy && hitInfo.FixDirection)
		{
			LastHitDirection = enemy.FacingDirection.GetSignX();
		}
		else
		{
			LastHitDirection = LastAttackDirection;
		}
		int num = ((!psm.StateOption.HasFlag(StateOption.IgnoreDamage)) ? CalculateDamage(hitInfo.Damage, hitInfo.AttackAttribute.HasFlag(AttackAttribute.FixedDamage)) : 0);
		if (CanParry(hitInfo.AttackAttribute, LastAttackDirection))
		{
			Parry(hitInfo);
			return false;
		}
		LastHitTime = Time.time;
		if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.Stamina))
		{
			PlayerStat.GetStat<Stamina>().AddStamina(-hitInfo.Damage);
			ChangeStateOnDamaged(hitInfo);
			return false;
		}
		IAttackSource source = hitInfo.Source;
		bool flag = source is PrimordialFlame || source is PrimordialFlame2 || source is PrimordialFlameSword || hitInfo.AttackName.Equals("PrimordialFlame_Lightning");
		bool flag2 = CanGuard(hitInfo.AttackAttribute, LastAttackDirection);
		if (flag2)
		{
			if (num != 0)
			{
				GuardStaminaReduceRate stat = PlayerStat.GetStat<GuardStaminaReduceRate>();
				float num2 = (float)num * stat.StaminaRate;
				num2 = ((num2 < 1f) ? 1f : num2);
				if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.RedFear))
				{
					num2 *= 2f;
				}
				num2 = (flag ? (num2 + pcp.PrimordialFlame2FixedDamage) : num2);
				PlayerStat.GetStat<Stamina>().AddStamina(0f - num2);
			}
			if (num != 0)
			{
				num = (int)((float)num * PlayerStat.GetStat<GuardDamageReduceRate>().GuardDamageRate);
				num = ((num < 1) ? 1 : num);
			}
			if (PlayerStat.GetStat<Stamina>().Current > 0f && PSM.CurrentState is PlayerGuardState playerGuardState)
			{
				playerGuardState.PlayGuard();
			}
			if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.RedFear))
			{
				LazySingleton<MessageSystem>.Instance.Publish(new RageFloodTutorialEvent());
			}
		}
		int health = PlayerShield.Health;
		int num3 = (PlayerShield.TryTakeDamage(num) ? Mathf.Max(num - health, 0) : num);
		PlayerStat.GetStat<Health>().AddHealth(-num3);
		if (flag2 && num != 0)
		{
			PlayerStat.ResidualHeat.ChangeDamageToHeat(num);
		}
		PlayerController.PlayerPart.OnDamaged(hitInfo.Source, num);
		OnHealthChangeEvent();
		TutorialCanvas.ObservePlayerHealth();
		GraphicManager.ApplyHitEffect();
		if (!hitInfo.AttackAttribute.HasFlag(AttackAttribute.Continuous) && PlayerController.StatusEffectController.Contain<ElectricStatusEffect>())
		{
			ElectricEffect();
		}
		PlayerStat.GetStat<Health>().Darkness += hitInfo.DarkDamage;
		PlayerStat.GetStat<Health>().Darkness += PlayerController.StatusEffectController.Count<SporeStatusEffect>() * pcp.DarknessDamagePerCount;
		if (hitInfo.DarkDamage > 0f)
		{
			Game.Instance.RuntimeData.AddDarknessStatusEffect();
			SoundManager.PlayOneShot("event:/SFX/Player/Hit/SFX_Darkness");
		}
		bool flag3 = hitInfo.AttackAttribute.HasFlag(AttackAttribute.Relocate);
		bool flag4 = false;
		if (PlayerStat.GetStat<Health>().CurrentHealth <= 0)
		{
			if (!PlayerHeat.CanUseSkill(HeatSkillType.LastEmber))
			{
				Die(hitInfo.Source);
				return true;
			}
			flag4 = true;
			if (!flag3)
			{
				PlayerHeat.OnLastEmber = true;
			}
		}
		else
		{
			ChangeStateOnDamaged(hitInfo);
		}
		if (flag3)
		{
			Relocate(onEnd: (!(PSM.CurrentState.GetType() == typeof(PlayerLastEmberState))) ? (flag4 ? ((Action)delegate
			{
				PlayerHeat.OnLastEmber = true;
			}) : null) : ((Action)delegate
			{
				psm.ChangeState(new PlayerLastEmberState(PSM));
			}), source: hitInfo.Source, x: hitInfo.KnockBack.x);
		}
		if (flag2)
		{
			return true;
		}
		if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.RedFear) && Game.Instance.SaveData.WanderingData.ShouldShowTutorialMessage(ActionName.Red))
		{
			Game.Instance.UIManager.FieldCanvas.TutorialMessageBoard.AddMessage(ActionName.Red);
		}
		if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.Wet))
		{
			EnemyWetData enemyWetData = Game.DataManager.GetEnemyWetData(hitInfo.AttackName);
			PlayerController.StatusEffectController.AddEffect(new WetStatusEffect(this, enemyWetData.Duration));
		}
		if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.Electricity))
		{
			PlayerController.StatusEffectController.AddEffect(new ElectricStatusEffect(this, pcp.ShockDuration));
		}
		if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.Spore))
		{
			int count = Game.DataManager.GetEnemySporeData(hitInfo.AttackName).Count;
			for (int num4 = 0; num4 < count; num4++)
			{
				PlayerController.StatusEffectController.AddEffect(new SporeStatusEffect(this));
			}
		}
		return true;
	}

	public void Parry(HitInfo hitInfo)
	{
		string animationName = "dagger/parry";
		Enemy enemy = hitInfo.Source as Enemy;
		int num = 0;
		if (psm.CurrentState is ICounter counter)
		{
			if (!enemy.IsNull())
			{
				animationName = "counter";
				counter.OnParried(enemy);
				num = counter.GetStunCount(enemy, hitInfo.AttackAttribute);
			}
			SoundManager.PlayOneShot("event:/SFX/Player/Combat/SFX_Player_Counter");
		}
		else
		{
			psm.ParryInputItem.Execute();
			SoundManager.PlayOneShot("event:/SFX/Player/Combat/SFX_Player_Parrying");
		}
		GenerateDownImpulse();
		EnableVFX(animationName);
		Stamina stat = PlayerStat.GetStat<Stamina>();
		stat.AddStamina((float)stat.MaxStamina * 0.1f);
		PlayerStat.ResidualHeat.HealOnParry();
		if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.RedFear))
		{
			LazySingleton<MessageSystem>.Instance.Publish(new RageFloodTutorialEvent());
		}
		if (enemy.IsNull())
		{
			return;
		}
		PlayerController.PlayerPart.OnParry(enemy);
		if (!hitInfo.AttackAttribute.HasFlag(AttackAttribute.IgnoreSource) && !hitInfo.IgnoreStun)
		{
			if (enemy.MaxStunCount != 0 && !hitInfo.IgnoreStun)
			{
				int num2 = ((num <= 0) ? 1 : num);
				enemy.AddStunCount(-num2 * pcp.StunMultiplier);
			}
			float groggyDamage = (PlayerStat.GetStat<GroggyAttack>().Current + (float)(10 * hitInfo.Damage) / (float)(hitInfo.Damage + 100)) / 2f * PlayerStat.GetStat<ParryGroggy>().Current;
			enemy.TryTakeDamage(new HitInfo(this, 0f, 0f, groggyDamage, AttackDirection.Both, AttackAttribute.Default | AttackAttribute.ParryGroggy));
		}
	}

	public void GenerateDownImpulse()
	{
		if (SettingData.CombatCamera)
		{
			downImpulseSource.GenerateImpulse();
		}
	}

	public void Stun(Func<bool> condition = null)
	{
		((PlayerStiffState)PSM.StiffState).ClearCondition = condition;
		PSM.ChangeState(PSM.StiffState);
	}

	private void ChangeStateOnDamaged(HitInfo hitInfo)
	{
		if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.Continuous))
		{
			return;
		}
		bool flag = CanGuard(hitInfo.AttackAttribute, LastAttackDirection);
		if (!flag && hitInfo.AttackAttribute.HasFlag(AttackAttribute.Default))
		{
			EnableVFX("hit_start");
		}
		if (psm.CurrentState is PlayerStaggerHitState { CancelByOtherAttack: false })
		{
			TryPlayBulletTimeEffect();
		}
		else
		{
			if (psm.StateOption.HasFlag(StateOption.SuperArmor))
			{
				return;
			}
			IState newState;
			if (flag)
			{
				if (!(PlayerStat.GetStat<Stamina>().Current <= 0f))
				{
					return;
				}
				newState = psm.BrokenGuardState;
				base.VfxController.StopAll();
			}
			else
			{
				TryPlayBulletTimeEffect();
				newState = PSM.HitState;
				if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.Down))
				{
					newState = PSM.DownState;
				}
				else if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.Stiff))
				{
					newState = PSM.StiffState;
				}
				else if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.Stagger))
				{
					newState = PSM.StaggerHitState;
				}
			}
			PSM.ChangeState(newState);
		}
		void TryPlayBulletTimeEffect()
		{
			if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.BulletTime))
			{
				EnableVFX("bullet_time");
			}
		}
	}

	public void TakeHighJump(int jumpSpeed)
	{
		PSM.ChangeState(new PlayerHighJumpUpState(PSM, jumpSpeed));
	}

	private void Die(IAttackSource source)
	{
		this.OnDie?.Invoke(source);
		bool flag = false;
		foreach (CutsceneProducer cutsceneProducer in FieldController.GetCutsceneProducers())
		{
			if (cutsceneProducer is ImahenCutsceneProducer imahenCutsceneProducer)
			{
				imahenCutsceneProducer.StopChaserSfx();
			}
			if (cutsceneProducer is BossCutsceneProducer { BossName: var bossName } bossCutsceneProducer)
			{
				if (bossName == BossName.Imahen)
				{
					continue;
				}
				if (bossCutsceneProducer.IsFighting && EnemyFightData.GetBossTryCount(bossName) == 1)
				{
					MemoryPanel.UpdateMemory(bossName, isClear: false);
				}
			}
			if (cutsceneProducer.TryCutsceneOnPlayerDie())
			{
				flag = true;
			}
		}
		if (!flag)
		{
			if (source is Creature creature)
			{
				EnemyFightData.AddCreaturePlayerKill(creature.Name);
				MemoryPanel.UpdateMemoryCreature(creature.Name);
			}
			PlayerController.KillPlayer(source).Forget();
		}
	}

	public void ClearDamagedList()
	{
		damaged.Clear();
	}

	public void ShootFlame(HeatFlame.FlameParameter parameter)
	{
		Enemy targetEnemy = parameter.TargetEnemy;
		if (!(targetEnemy is DarkChasaShadow) && !(targetEnemy is ChasaShadow))
		{
			Game.Instance.ObjectPoolManager.GetObject<HeatFlame>("Fever Flame.prefab", VisualCenter).Initialize(parameter);
		}
	}

	public bool TryGetDamageblesInHitBox(string skillDataID, int index, out List<Collider2D> results)
	{
		string hitCollName = ((index == 0) ? skillDataID : $"{skillDataID}_{index + 1}");
		Collider2D collider2D = hitColliders.FirstOrDefault((Collider2D x) => x.name == hitCollName);
		if ((object)collider2D == null)
		{
			collider2D = hitColliders[0];
		}
		collider2D.gameObject.SetActive(value: true);
		int num = AttackUtility.OverlapCollider(collider2D, default(ContactFilter2D).NoFilter(), out results);
		collider2D.gameObject.SetActive(value: false);
		return num > 0;
	}

	public void HandleAttackHitBox(string skillDataID, int index, AttackAttribute attackAttribute, float multiplier = 0f, IAttackSource attackSource = null, List<IDamageable> damageables = null)
	{
		if (!TryGetDamageblesInHitBox(skillDataID, index, out var results))
		{
			return;
		}
		foreach (Collider2D item in results)
		{
			if (item.TryGetComponent<IDamageable>(out var component))
			{
				AttackParameter parameter = new AttackParameter(component, skillDataID, index, attackAttribute, attackSource, random: true, multiplier);
				Attack(parameter, damageables);
			}
		}
	}

	public void HandleAttackHitBoxFrom(Vector2 center, string skillDataID, int attackIndex, AttackAttribute attackAttribute, string collName = null, bool random = true, float multiplier = 0f)
	{
		if (string.IsNullOrEmpty(collName))
		{
			collName = skillDataID;
		}
		Collider2D collider2D = hitColliders.FirstOrDefault((Collider2D x) => x.name == collName);
		if ((object)collider2D == null)
		{
			collider2D = hitColliders[0];
		}
		collider2D.gameObject.SetActive(value: true);
		Vector3 vector = collider2D.bounds.center - collider2D.transform.position;
		center += (Vector2)vector;
		Collider2D[] array = AttackUtility.OverlapBoxAll(center, collider2D.bounds.size, ~(int)LayerUtility.PlayerLayer);
		collider2D.gameObject.SetActive(value: false);
		if (array.Length == 0)
		{
			return;
		}
		Collider2D[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			if (array2[num].TryGetComponent<IDamageable>(out var component))
			{
				Attack(new AttackParameter(component, skillDataID, attackIndex, attackAttribute, this, random, multiplier));
			}
		}
	}

	public bool Attack(AttackParameter parameter, List<IDamageable> damagedList = null)
	{
		IDamageable damageable = parameter.AttackTarget;
		bool flag = (damageable.HitType & (HitType.Creature | HitType.Boss)) != 0;
		if (damageable is DynamicHitBox dynamicHitBox)
		{
			damageable = dynamicHitBox.Enemy;
		}
		if (damagedList == null)
		{
			damagedList = damaged;
		}
		if (damagedList.Contains(damageable))
		{
			return false;
		}
		if (damageable == this)
		{
			return false;
		}
		if (damageable.HitType == HitType.Platform)
		{
			return false;
		}
		if (flag && parameter.AttackAttribute.HasFlag(AttackAttribute.DestroyWall) && !parameter.AttackAttribute.HasFlag(AttackAttribute.Default))
		{
			return false;
		}
		HitInfo hitInfo = new HitInfo(parameter.AttackSource, parameter.Damage, parameter.FireDamage, parameter.Groggy, AttackDirection.Facing, parameter.AttackAttribute);
		if (!Attack_Internal(damageable, hitInfo, damagedList))
		{
			return false;
		}
		return true;
	}

	private bool Attack_Internal(IDamageable damageable, HitInfo hitInfo, List<IDamageable> damagedList)
	{
		bool flag = (damageable.HitType & (HitType.Creature | HitType.Boss)) != 0;
		if (!damageable.TryTakeDamage(hitInfo))
		{
			return false;
		}
		damagedList.Add(damageable);
		GenerateAttackImpulse();
		if (flag)
		{
			Enemy enemy = damageable.CastObject<Enemy>();
			if (enemy.CanMakeStuck && !CanBlockStuck(hitInfo))
			{
				psm.ChangeState(psm.AttackStuckState);
				return true;
			}
			PlayEnemyHitEffect(enemy, hitInfo.IsDirectAttack);
		}
		else
		{
			if (damageable is CharonRock)
			{
				Vector2 visualCenter = damageable.VisualCenter;
				Vector2 vector = VisualCenter - visualCenter;
				float num = 1f;
				num = ((num > vector.magnitude) ? 1f : num);
				Vector2 vector2 = visualCenter + vector.normalized * (num * UnityEngine.Random.Range(0.3f, 0.6f));
				PoolableTimeLimitObject poolableTimeLimitObject = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Charon_RockHit.prefab", vector2);
				poolableTimeLimitObject.Initialize("VFX/Charon_RockHit.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
			}
			if (damageable is StuckableObject && !CanBlockStuck(hitInfo))
			{
				psm.ChangeState(psm.AttackStuckState);
			}
		}
		return true;
	}

	private void GenerateAttackImpulse()
	{
		if (SettingData.CombatCamera)
		{
			CinemachineImpulseManager.Instance.Clear();
			attackImpulseSource.GenerateImpulse();
		}
	}

	public bool CanBlockStuck(HitInfo hitInfo)
	{
		if (!psm.StateOption.HasFlag(StateOption.BlockStuck))
		{
			return hitInfo.AttackAttribute.HasFlag(AttackAttribute.BlockStuck);
		}
		return true;
	}

	private void PlayEnemyHitEffect(Enemy enemy, bool isDirectAttack)
	{
		Vector3 pos;
		if (psm.CurrentState == psm.BurstState)
		{
			Vector2 visualCenter = psm.BurstInputItem.TargetEnemy.VisualCenter;
			Collider2D collider2D = hitColliders.First((Collider2D collider2D2) => collider2D2.name == "dagger/execution");
			collider2D.gameObject.SetActive(value: true);
			Vector3 size = collider2D.bounds.size;
			pos = (Vector3)visualCenter + new Vector3(UnityEngine.Random.Range((0f - size.x) / 2f, size.x / 2f), UnityEngine.Random.Range((0f - size.y) / 2f, size.y / 2f));
			collider2D.gameObject.SetActive(value: false);
		}
		else if (psm.CurrentState == psm.HyperBurstState)
		{
			Vector2 visualCenter2 = psm.HyperBurstInputItem.TargetEnemy.VisualCenter;
			IReadOnlyList<Vector2> burstPositions = PlayerController.HeatEffectController.BurstPositions;
			Vector2 vector = burstPositions[heatBurstIndex];
			vector.x *= (IsFacingRight ? 1 : (-1));
			pos = visualCenter2 + vector;
			heatBurstIndex++;
			if (heatBurstIndex >= burstPositions.Count)
			{
				heatBurstIndex = 0;
			}
		}
		else
		{
			Vector2 visualCenter3 = enemy.VisualCenter;
			Vector2 vector2 = VisualCenter - visualCenter3;
			float x = enemy.Bounds.extents.x;
			x = ((x > vector2.magnitude) ? 1f : x);
			pos = visualCenter3 + vector2.normalized * (x * UnityEngine.Random.Range(0.3f, 0.6f));
		}
		bool isLastAttack = ((PlayerGroundAttackState)psm.BasicGroundAttackState).IsLastAttack;
		bool flag = !isFourthEffectPlaying;
		if (isDirectAttack)
		{
			if (flag && isLastAttack)
			{
				isFourthEffectPlaying = true;
				PoolableTimeLimitObject poolableTimeLimitObject = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Enemy_Damaged_4.prefab", pos);
				poolableTimeLimitObject.Initialize("VFX/Enemy_Damaged_4.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
			}
			else
			{
				isFourthEffectPlaying = false;
				PoolableTimeLimitObject poolableTimeLimitObject2 = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Enemy_Damaged.prefab", pos);
				poolableTimeLimitObject2.Initialize("VFX/Enemy_Damaged.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject2));
				PoolableTimeLimitObject poolableTimeLimitObject3 = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Hit_Circle.prefab", pos);
				poolableTimeLimitObject3.Initialize("VFX/Hit_Circle.prefab", Util.GetCurrentAnimDuration(poolableTimeLimitObject3));
			}
		}
		else
		{
			PoolableTimeLimitObject poolableTimeLimitObject4 = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Enemy_Damaged_Indirect.prefab", pos);
			poolableTimeLimitObject4.Initialize("VFX/Enemy_Damaged_Indirect.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject4));
		}
		if (enemy is Charon charon)
		{
			charon.PlayHitEffect();
		}
		else if (enemy is SeedKeeper)
		{
			Bounds bounds = enemy.Bounds;
			Vector2 vector3 = new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y / 2f);
			PoolableTimeLimitObject poolableTimeLimitObject5 = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Enemy_BlackFog.prefab", vector3);
			Vector3 localScale = poolableTimeLimitObject5.transform.localScale;
			int sign = (enemy.VisualCenter - VisualCenter).x.GetSign();
			poolableTimeLimitObject5.transform.localScale = new Vector3(localScale.x * (float)sign, localScale.y, localScale.z);
			poolableTimeLimitObject5.Initialize("VFX/Enemy_BlackFog.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject5));
			SoundManager.PlayOneShotAttached("event:/Creature/SFX_Creature_Hit_Blood", enemy.transform);
		}
		else if ((!isLastAttack || flag) && isDirectAttack)
		{
			Bounds bounds2 = enemy.Bounds;
			Vector2 vector4 = bounds2.center;
			int num = 0;
			while (vector4.y < bounds2.center.y && num < 10)
			{
				vector4 = bounds2.center + UnityEngine.Random.onUnitSphere * bounds2.extents.magnitude;
				num++;
			}
			PoolableTimeLimitObject poolableTimeLimitObject6 = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Enemy_BlackFog.prefab", vector4);
			Vector3 localScale2 = poolableTimeLimitObject6.transform.localScale;
			int sign2 = (enemy.VisualCenter - VisualCenter).x.GetSign();
			poolableTimeLimitObject6.transform.localScale = new Vector3(Mathf.Abs(localScale2.x) * (float)sign2, localScale2.y, localScale2.z);
			poolableTimeLimitObject6.Initialize("VFX/Enemy_BlackFog.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject6));
			poolableTimeLimitObject6.GetComponent<ParticleSystem>().Play();
			SoundManager.PlayOneShotAttached("event:/Creature/SFX_Creature_Hit_Blood", enemy.transform);
		}
	}

	private void Relocate(IAttackSource source, float x, Action onEnd)
	{
		isRelocating = true;
		Vector2 vector = LastGroundPosition;
		PathFinder.Node node = PathFinder.FindNode(vector);
		if (node != null)
		{
			PathFinder.Node node2 = node.Shift(-1, 1, allowNull: true);
			PathFinder.Node node3 = node.Shift(1, 1, allowNull: true);
			if (node2 == null)
			{
				vector = node3.Position;
			}
			if (node3 == null)
			{
				vector = node2.Position;
			}
		}
		Vector2 position = vector - (Vector2)base.SkeletonAnimation.transform.localPosition;
		onEnd = (Action)Delegate.Combine(onEnd, (Action)delegate
		{
			isRelocating = false;
		});
		PlayerController.SetPositionAsync(position, onEnd, IsFacingRight).Forget();
	}

	public Vector2 GetPositionFromGroud()
	{
		Bounds colliderBounds = ColliderBounds;
		Vector2 vector = new Vector2(colliderBounds.min.x, colliderBounds.center.y);
		Vector2 vector2 = new Vector2(colliderBounds.center.x, colliderBounds.center.y);
		Vector2 vector3 = new Vector2(colliderBounds.max.x, colliderBounds.center.y);
		Vector2[] obj = new Vector2[3] { vector2, vector, vector3 };
		Vector2 down = Vector2.down;
		LayerMask layerMask = (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer;
		Vector2[] array = obj;
		for (int i = 0; i < array.Length; i++)
		{
			RaycastHit2D raycastHit2D = Physics2D.Raycast(array[i], down, 1.5f, layerMask);
			if ((bool)raycastHit2D.collider)
			{
				return raycastHit2D.point;
			}
		}
		return Physics2D.Raycast(vector2, down, float.PositiveInfinity, layerMask).point;
	}

	public void SetGravityFree(bool active)
	{
		PSM.SetStateOptionActive(StateOption.GravityFree, active);
	}

	public void SetRideBone(RideableBone boneFollower)
	{
		ridingBone = boneFollower;
	}

	public void SetSkillActive(Type inputItemType, bool active)
	{
		InputItem inputItem = PSM.GetInputItem(inputItemType);
		inputItem.Active = active;
		Game.Instance.SaveData.PlayerData.SetSkillActive(inputItem, active);
		DebugLog.GeneralLog($"{inputItemType.Name} 해금 변경 {active}");
	}

	public void UpgradeState<T>(bool upgrade = true) where T : PlayerUpgradeableState
	{
		FieldInfo fieldInfo = PSM.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault((FieldInfo x) => x.FieldType == typeof(T));
		if (!(fieldInfo == null))
		{
			((T)fieldInfo.GetValue(PSM)).Upgrade(upgrade);
		}
	}

	protected override void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(ParkourCheckPos, parkourCheckSize);
	}

	public void PlayTempAttackAnimation(int targetFrame, string animName = "stone/attack_1", bool vfxEnable = true)
	{
		int num = Util.SecondsToFrame(GetAnimDuration(animName));
		if (targetFrame == 0)
		{
			targetFrame = num;
		}
		float timeScale = (float)num / (float)targetFrame;
		if (base.SkeletonAnimation.AnimationState.GetCurrent(0) != null && GetCurrentAnim(0).Name == animName)
		{
			SetAnimationInstantly(animName, loop: false, 0f, timeScale);
			if (vfxEnable)
			{
				EnableVFX(animName);
			}
		}
		else
		{
			SetAnimation(animName, loop: false, timeScale, 0, 0f, vfxEnable);
		}
	}

	public void ResetHitMaterial()
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat("_Hit Effect Blend", 0f);
		SetPropertyBlock(materialPropertyBlock);
	}

	public void ApplyKnockBack(Transform source, Vector2 velocity, float seconds = 0f)
	{
		PlayerKnockBackState playerKnockBackState = PSM.KnockBackState as PlayerKnockBackState;
		int direction = ((!(VisualCenter.x < source.position.x)) ? 1 : (-1));
		playerKnockBackState.SetKnockBackVelocity(direction, velocity);
		playerKnockBackState.SetWaitSeconds(seconds);
		PSM.ChangeState(playerKnockBackState);
	}

	public void CancelIfKnockBack(bool instantly = false)
	{
		if (PSM.CurrentState == PSM.KnockBackState)
		{
			PSM.ChangeState(PSM.IdleState);
			if (instantly)
			{
				SetAnimationInstantly("idle");
			}
		}
	}

	public void ResetLastHitDirection()
	{
		LastHitDirection = 0;
	}

	public bool IsSpecialAttackTaskValid()
	{
		bool isTaskValid = psm.PyronAttackState.CastObject<ITaskValid>().IsTaskValid;
		bool isTaskValid2 = psm.HyperPyronAttackState.CastObject<ITaskValid>().IsTaskValid;
		bool isTaskValid3 = psm.BurstState.CastObject<ITaskValid>().IsTaskValid;
		bool isTaskValid4 = psm.HyperBurstState.CastObject<ITaskValid>().IsTaskValid;
		return isTaskValid || isTaskValid2 || isTaskValid3 || isTaskValid4;
	}

	public void PlayOverheatBurstExplosion()
	{
		PoolableTimeLimitObject poolableTimeLimitObject = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("Overheat Burst Explosion.prefab", PSM.HyperBurstInputItem.TargetEnemy.VisualCenter);
		poolableTimeLimitObject.Initialize(Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		poolableTimeLimitObject.GetComponent<ParticleSystem>().Play();
	}

	private void OnHealthChangeEvent()
	{
		if (FieldController.CurrentArea is BaranArea baranArea && (float)PlayerStat.GetStat<Health>().MaxHealth * baranArea.SupportHealthPercentage >= (float)PlayerStat.GetStat<Health>().CurrentHealth)
		{
			LazySingleton<MessageSystem>.Instance.Publish(new MyosoSupportEvent(MyosoEventState.PlayerHealthLow));
		}
	}

	private void ElectricEffect()
	{
		SoundManager.PlayOneShot("event:/SFX/Player/Hit/SFX_ElectricShock_Burst");
		Vector2 visualCenter = VisualCenter;
		PoolableTimeLimitObject poolableTimeLimitObject = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("Electric Burst.prefab", visualCenter, base.transform);
		poolableTimeLimitObject.Initialize("Electric Burst.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		poolableTimeLimitObject.GetComponent<ParticleSystem>().Play();
		poolableTimeLimitObject.gameObject.SetActive(value: true);
	}

	private void AddEnemyStun(HitInfo hitInfo, Enemy enemy)
	{
		if (PlayerStat.GetStat<Stamina>().Current != 0f && !enemy.IsNull() && !enemy.IsNull && !hitInfo.AttackAttribute.HasFlag(AttackAttribute.IgnoreSource) && enemy.MaxStunCount != 0 && !hitInfo.IgnoreStun)
		{
			enemy.AddStunCount(-1);
		}
	}

	public void ApplyDifficulty(Difficulty difficulty)
	{
		if (difficulty != Difficulty.None)
		{
			bool flag = Game.Instance.SaveData.PlayerData.GetAcquiredItemCount("GuardianStone") > 0;
			if (difficulty != Difficulty.Story && flag)
			{
				Game.Instance.SaveData.PlayerData.TryRemoveAcquiredItem("GuardianStone");
			}
			else if (difficulty == Difficulty.Story && !flag)
			{
				new PureItem("GuardianStone").AddToPlayerData();
			}
		}
	}
}
