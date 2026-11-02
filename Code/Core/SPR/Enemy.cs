using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.FSM;
using SPR.FSM.Base;
using SPR.Field;
using SPR.Library;
using SPR.Save;
using SPR.UI;
using SPR.UI.Tutorial;
using UnityEngine;

namespace SPR;

public abstract class Enemy : Character, IDamageable, IEquatable<Enemy>
{
	public enum ActionTiming
	{
		Begin,
		OnFrame,
		End
	}

	[SerializeField]
	private bool ignoreDeadList;

	[ReadOnlyInspector]
	[SerializeField]
	protected int maxHealth;

	protected ClampedInt health;

	[Header("그로기")]
	[SerializeField]
	[ReadOnlyInspector]
	private float groggyValue;

	[Header("Stun")]
	[ReadOnlyInspector]
	[SerializeField]
	private int maxStun;

	private List<AttackSequence> allAttackSequences;

	private List<AttackSequence> currentAttackSequences;

	private AttackState attackState;

	private BoxCollider2D hitBox;

	private Area myArea;

	protected readonly Dictionary<string, Action<int>> TimelineBeginActions = new Dictionary<string, Action<int>>();

	protected readonly Dictionary<string, Action<int>> TimelineEndActions = new Dictionary<string, Action<int>>();

	protected readonly Dictionary<string, Action<int>> TimelineActions = new Dictionary<string, Action<int>>();

	protected readonly Dictionary<string, Action<int, Parameter[]>> BeginParamActions = new Dictionary<string, Action<int, Parameter[]>>();

	protected readonly Dictionary<string, Action<int, Parameter[]>> EndParamActions = new Dictionary<string, Action<int, Parameter[]>>();

	protected readonly Dictionary<string, Action<int, Parameter[]>> ParamActions = new Dictionary<string, Action<int, Parameter[]>>();

	private PoolingParticle redFogObject;

	private CancellationTokenSource paleBlowCancelTokenSource;

	private CancellationTokenSource hyperBurstTokenSource;

	protected PoolingObject DeathFireObject;

	protected CancellationTokenSource DeathFireTokenSource;

	protected EnemyState BounceState;

	private Groggy groggy;

	private Sequence visualKnockBackSequence;

	public int Exp { get; protected set; }

	public int Money { get; protected set; }

	public virtual string GroggyStartAnim => "groggy_start";

	public virtual string GroggyLoopAnim => "groggy_loop";

	public virtual string GroggyEndAnim => "groggy_end";

	public string BounceAnim { get; protected set; } = "parry";

	public int MaxHealth => health.Max;

	public int Health => health.Value;

	public virtual Groggy Groggy => groggy ?? (groggy = new Groggy(ID));

	public virtual bool IsInGroggyState => base.StateMachine.CurrentState == GroggyState;

	public virtual Area MyArea => GetArea(2);

	public virtual bool CanMakeStuck { get; protected set; }

	public virtual bool IsDefenseless => false;

	public virtual bool IsGroggyPlaying => base.VfxController.IsTimelineEnd(GroggyStartAnim);

	public virtual bool CanPullPlayer => true;

	public Vector2 NearestEdgePoint => new Vector2(Player.IsFacingRight ? HitBox.bounds.min.x : HitBox.bounds.max.x, VisualCenter.y);

	public virtual bool IsDead => health.Value <= 0;

	public bool IsBounced => base.StateMachine.CurrentState is EnemyBounceState;

	public abstract EnemyState DefaultState { get; }

	public abstract HitType HitType { get; }

	public Vector2 SpawnPosition { get; set; }

	public Vector2 HitBoxSize => HitBox.size;

	public Vector2 PlaceholderPosition { get; private set; }

	public bool IgnoreDeadList => ignoreDeadList;

	protected GroggyState GroggyState { get; private set; }

	protected static PlayerMover Player => Game.Instance.Player.mover;

	protected static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	protected static Platform Platform => Game.Instance.FieldController.Platform;

	protected static ObjectPoolManager PoolManager => Game.Instance.ObjectPoolManager;

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	private static UIManager UIManager => Game.Instance.UIManager;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	protected List<AttackSequence> AllAttackSequences => allAttackSequences ?? (allAttackSequences = GetAllAttackSequences());

	protected virtual BoxCollider2D HitBox => hitBox ?? (hitBox = GetComponent<BoxCollider2D>());

	public EnemyStatData EnemyStatData => Game.DataManager.GetEnemyStatDataByClearCount(Game.Instance.SaveData.ClearCount, ID);

	public virtual string ID => GetType().Name;

	protected virtual int OriginDef => EnemyStatData.Def[0];

	protected int Def => Mathf.CeilToInt(GetDefenseRate() * (float)OriginDef);

	protected virtual float Width => HitBox.size.x;

	protected virtual float GroggyThreshold => 0f;

	protected StatusEffectController StatusEffectController { get; private set; }

	public IReadOnlyDictionary<string, Action<int>> ReadOnlyTimelineBeginActions => TimelineBeginActions;

	public IReadOnlyDictionary<string, Action<int>> ReadOnlyTimelineEndActions => TimelineEndActions;

	public IReadOnlyDictionary<string, Action<int>> ReadOnlyTimelineActions => TimelineActions;

	public IReadOnlyDictionary<string, Action<int, Parameter[]>> ROBeginParamActions => BeginParamActions;

	public IReadOnlyDictionary<string, Action<int, Parameter[]>> ROEndParamActions => EndParamActions;

	public IReadOnlyDictionary<string, Action<int, Parameter[]>> ROParamActions => ParamActions;

	public virtual Vector2 GroundPosition => SpawnPosition;

	public int MaxStunCount => maxStun;

	public int CurrentStun { get; protected set; }

	protected virtual float VisualKnockBackValue => 0.3f;

	protected virtual float VisualKnockBackDuration => 1f / 30f;

	public virtual void Activate()
	{
	}

	public virtual void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.SortingOrder = sortingOrder;
		base.transform.position = spawnPos;
		SpawnPosition = spawnPos;
		PlaceholderPosition = spawnPos;
		CurrentStun = maxStun;
		BounceState = new EnemyBounceState(this, Util.SecondsToFrame(GetAnimDuration(BounceAnim)));
	}

	protected override void Awake()
	{
		DeathFireTokenSource = new CancellationTokenSource();
		(from p in base.VfxController.GetComponentsInChildren<ParticleSystem>()
			select p.main).ForEach((ParticleSystem.MainModule m) => m.playOnAwake = false);
		base.Awake();
		maxHealth = Mathf.CeilToInt((float)EnemyStatData.HP * Game.DataManager.GetEnemyMultiplier(Game.Instance.SaveData.ClearCount).Health);
		health = new ClampedInt(maxHealth, 0, maxHealth);
		Exp = EnemyStatData.Exp;
		Money = EnemyStatData.Money;
		maxStun = EnemyStatData.Stun;
		GroggyState = new GroggyState(this, new FrameTimer(Groggy.Duration), OnEnterGroggyState, OnExitGroggyState);
		StatusEffectController = new StatusEffectController(this);
		paleBlowCancelTokenSource = new CancellationTokenSource();
		AddAction(ActionTiming.Begin, "RedFear", delegate(int duration)
		{
			PaleBlow(duration).Forget();
		});
	}

	protected sealed override void FixedUpdate()
	{
		base.FixedUpdate();
		if (groggy != null)
		{
			Groggy.OnFixedUpdate(this);
			groggyValue = Groggy.Value;
		}
		base.VfxController.UpdateTimeline(Time.fixedDeltaTime);
		StatusEffectController?.Running();
		OnFixedUpdate();
	}

	protected virtual void OnFixedUpdate()
	{
	}

	protected abstract List<AttackSequence> GetAllAttackSequences();

	protected abstract List<AttackSequence> GenerateCurrentAttackSequences();

	protected virtual void OnEnterGroggyState()
	{
		ReturnPaleBlow();
		SoundManager.PlayOneShot("event:/SFX/Player/System/SFX_Player_Groggy");
		SetAnimation(GroggyStartAnim, loop: false);
		AddAnimation(GroggyLoopAnim);
		Game.Instance.RuntimeData.AddCanHyperBurst();
		if (WanderingData.ShouldShowTutorial(ActionName.Groggy))
		{
			UniTask.WaitUntil(() => base.VfxController.IsTimelineEnd(GroggyStartAnim)).GetAwaiter().OnCompleted(delegate
			{
				UIManager.TutorialCanvas.SetData(ActionName.Groggy);
				Game.Instance.InputManager.StackControlTarget(UIManager.TutorialCanvas).Forget();
				TryShowExecutionTutorialMessage();
			});
		}
		else
		{
			TryShowExecutionTutorialMessage();
		}
		static void TryShowExecutionTutorialMessage()
		{
			if (WanderingData.ShouldShowTutorialMessage(ActionName.Execution))
			{
				UIManager.FieldCanvas.TutorialMessageBoard.AddMessage(ActionName.Execution);
			}
		}
	}

	protected virtual void OnExitGroggyState()
	{
		Groggy.PerfectRecoverFromGroggy();
	}

	public virtual void OnHitBurst()
	{
	}

	public virtual void OnEnterBounceState()
	{
	}

	public virtual void OnExitBounceState()
	{
	}

	public virtual void OnPlayerHit(HitInfo hitInfo)
	{
	}

	public virtual void OnPlatformHit(HitInfo hitInfo)
	{
	}

	public int CalculateDamage(int attack, int fireAttack)
	{
		int def = Def;
		float num = (IsInGroggyState ? Groggy.DamageMultiplier : 1f);
		float num2 = attack;
		if (def != 0)
		{
			float num3 = 100f - 100f / (1f + (float)def / 100f);
			float num4 = 1f - num3 / 100f;
			num2 = (float)attack * num4;
		}
		return Mathf.CeilToInt((num2 + (float)fireAttack) * num);
	}

	public void LookPlayer()
	{
		if (!CheckPlayerInBounds())
		{
			PlayerController player = Game.Instance.Player;
			LookAt(player.transform.position);
		}
	}

	protected int CalculateFixedDamage(int attack, int fireAttack)
	{
		float num = (IsInGroggyState ? Groggy.DamageMultiplier : 1f);
		return Mathf.CeilToInt((float)(attack + fireAttack) * num);
	}

	protected float CalculateGroggyDamage(float groggyDamage, bool isHeavyAttack)
	{
		int def = Def;
		if (def <= 0 || isHeavyAttack)
		{
			return groggyDamage;
		}
		float num = 100f - 100f / (1f + (float)def / 100f);
		return groggyDamage * (1f - num / 100f);
	}

	protected int GetRawDamage(int attack, int fireAttack)
	{
		return attack + fireAttack;
	}

	protected virtual void AdjustXPosition()
	{
		Vector3 position = base.transform.position;
		float x = Mathf.Clamp(position.x, (float)MyArea.AreaBox.LowerLeft.x + Width, (float)MyArea.AreaBox.UpperRight.x - Width);
		base.transform.position = new Vector3(x, position.y);
	}

	protected void AdjustXPosition(AreaBox areaBox)
	{
		Vector3 position = base.transform.position;
		float x = Mathf.Clamp(position.x, (float)areaBox.LowerLeft.x + Width, (float)areaBox.UpperRight.x - Width);
		base.transform.position = new Vector3(x, position.y);
	}

	public virtual bool TryTakeDamage(HitInfo hitInfo)
	{
		if (Game.Instance.Player.isDead)
		{
			return false;
		}
		if (!HitBox.enabled)
		{
			return false;
		}
		int num = (hitInfo.AttackAttribute.HasFlag(AttackAttribute.FixedDamage) ? CalculateFixedDamage(hitInfo.Damage, hitInfo.FireDamage) : CalculateDamage(hitInfo.Damage, hitInfo.FireDamage));
		int value = health.Value;
		if (health.Value > 0)
		{
			if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.HyperBurst) || (StatusEffectController.Contain<ExecutionStatusEffect>() && !IsExecutionFinished()))
			{
				num = ((health.Value > num) ? num : (health.Value - 1));
			}
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

	public void StartBurning(int totalFrame, int cycleFrame, float originDamage, float originFire)
	{
		if (Health > 0)
		{
			StatusEffectController.AddEffect(new EnemyBurnStatusEffect(this, originDamage, originFire, cycleFrame, totalFrame));
		}
	}

	public bool HasStatusEffect<T>() where T : StatusEffect
	{
		return StatusEffectController.Contain<T>();
	}

	public void ClearStatusEffect(bool force)
	{
		StatusEffectController.Clear(force);
	}

	public void AddDefenseDebuff(int durationFrame, float debuffRate)
	{
		if (Health > 0)
		{
			StatusEffectController.AddEffect(new DefenseDebuffStatusEffect(this, durationFrame, debuffRate));
		}
	}

	public async UniTaskVoid Pause(int duration)
	{
		base.VfxController.Pause();
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(duration);
		base.VfxController.Resume();
	}

	protected virtual void OnChangeHealth(int delta)
	{
	}

	protected void TakeGroggyDamage(HitInfo hitInfo)
	{
		if (Groggy.MaxValue <= 0 || IsInGroggyState)
		{
			return;
		}
		float groggyDamage = hitInfo.GroggyDamage;
		float num = (CanMakeStuck ? CalculateGroggyDamage(groggyDamage, hitInfo.AttackAttribute.HasFlag(AttackAttribute.HeavyAttack)) : groggyDamage);
		if (GroggyThreshold > 0f && Groggy.Value - num < 0f)
		{
			num = Groggy.Value - GroggyThreshold;
		}
		Groggy.ChangeValueBy(0f - num);
		if (Groggy.Value == 0f)
		{
			if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.ParryGroggy) && base.StateMachine.CurrentState is EnemyBounceState { ResetStun: not false } enemyBounceState)
			{
				enemyBounceState.ChangeGroggyOnExit(active: true);
			}
			else if (base.StateMachine.CurrentState is StiffState stiffState)
			{
				stiffState.SetGroggy(GroggyState);
			}
			else
			{
				base.StateMachine.ChangeState(GroggyState);
			}
		}
		Groggy.ResetTimer();
	}

	protected virtual void OnDamaged(HitInfo hitInfo)
	{
		if (hitInfo.IsDirectAttack)
		{
			TryExplodeDashFlame();
			if ((hitInfo.Source is PlayerMover playerMover && playerMover.IsSpecialAttackTaskValid()) || !IsDead)
			{
				DamageEffectAsync().Forget();
			}
		}
	}

	protected virtual void Die()
	{
		ReturnPaleBlow();
		base.VfxController.StopAll();
		PlayerStat.AddStatAppliedExperience(Exp);
		Game.Instance.Player.PlayerPart.OnKill();
		Game.Instance.FieldController.RemoveEnemy(this);
		StatusEffectController.Clear();
		paleBlowCancelTokenSource = null;
		hyperBurstTokenSource?.Cancel();
		base.SortingOrder = -Mathf.Abs(base.SortingOrder);
	}

	protected bool IsPlayerInAttackBound(AttackSequence seq)
	{
		Zone sightZone = seq.GetSightZone(this);
		return IsPlayerInBound(sightZone.Center, sightZone.Size, seq.CheckPlatform);
	}

	protected virtual bool IsPlayerInBound(Vector2 center, Vector2 size, bool checkPlatform = false)
	{
		return new Bounds(center, size).Intersects(Player.ColliderBounds);
	}

	protected bool IsTargetInBound<T>(Vector2 center, Vector2 size, LayerMask targetLayer) where T : MonoBehaviour
	{
		Collider2D collider2D = Physics2D.OverlapBox(center, size, 0f, targetLayer);
		if (collider2D.IsNull())
		{
			return false;
		}
		T component;
		return collider2D.TryGetComponent<T>(out component);
	}

	public List<IDamageable> AttackSquare(Zone localAttackArea, HitInfo hitInfo, List<IDamageable> attackedList)
	{
		Vector2 center = localAttackArea.GetZoneOf(this).Center;
		Vector2 size = localAttackArea.Size;
		return AttackUtility.AttackBox(center, size, hitInfo, LayerUtility.EnemyDefaultAttackLayer | (int)LayerUtility.ObjectLayer, attackedList);
	}

	public List<IDamageable> AttackCircle(float radius, HitInfo hitInfo, List<IDamageable> attackedList)
	{
		return AttackUtility.AttackCircle(VisualCenter, radius, hitInfo, LayerUtility.EnemyDefaultAttackLayer | (int)LayerUtility.ObjectLayer, attackedList);
	}

	protected virtual void ResetStatus()
	{
		health = new ClampedInt(maxHealth, 0, maxHealth);
		Groggy.ResetGroggy(includeMax: true);
	}

	protected void ForceDrop()
	{
		DOTween.Kill(base.transform);
		base.transform.DOMoveY(SpawnPosition.y, 0.5f).SetEase(Ease.OutQuart).SetUpdate(UpdateType.Fixed);
	}

	protected void ForceDrop(float yPos, float speed)
	{
		DOTween.Kill(base.transform);
		base.transform.DOMoveY(yPos, speed).SetEase(Ease.OutQuart).SetUpdate(UpdateType.Fixed)
			.SetSpeedBased(isSpeedBased: true);
	}

	public int GetTimelineDuration(string key)
	{
		return base.VfxController.GetDuration(key);
	}

	public virtual void MoveX(float velocityX, float deltaTime, bool moveForward)
	{
	}

	public virtual void SetVisible(bool visible)
	{
		base.SkeletonRenderer.Skeleton.SetAlpha(visible ? 1 : 0);
	}

	public virtual void AddStunCount(int value)
	{
		if (maxStun != 0)
		{
			int currentStun = CurrentStun;
			CurrentStun = Mathf.Clamp(CurrentStun + value, 0, maxStun);
			if (CurrentStun > currentStun)
			{
				SoundManager.PlayOneShotAttached("event:/Creature/SFX_Resistance_Regen", base.transform);
			}
			if (CurrentStun == 0 && !IsDead)
			{
				DisableVFXAll();
				base.StateMachine.ChangeState(GetBounceState());
				Player.PSM.ActivateParryAction(this);
			}
		}
	}

	public virtual void ResetStunCount()
	{
		CurrentStun = maxStun;
	}

	public void PlayDeathFire()
	{
		if ((object)DeathFireObject == null)
		{
			DeathFireObject = PoolManager.GetObject<PoolingObject>("Death Fire.prefab", base.transform.position);
		}
		ParticleSystem component = DeathFireObject.GetComponent<ParticleSystem>();
		ParticleSystem[] componentsInChildren = DeathFireObject.GetComponentsInChildren<ParticleSystem>();
		ParticleSystem.ShapeModule shape = component.shape;
		shape.meshRenderer = MeshRenderer;
		ParticleSystem[] array = componentsInChildren;
		for (int i = 0; i < array.Length; i++)
		{
			ParticleSystem.ShapeModule shape2 = array[i].shape;
			shape2.meshRenderer = MeshRenderer;
		}
		component.Play(withChildren: true);
	}

	public async UniTask StopDeathFireAsync()
	{
		if (DeathFireObject.IsNull())
		{
			return;
		}
		ParticleSystem component = DeathFireObject.GetComponent<ParticleSystem>();
		if (component.isPlaying)
		{
			component.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			await UniTask.WaitWhile(component.IsAlive, PlayerLoopTiming.Update, DeathFireTokenSource.Token);
			if (!DeathFireObject.IsNull())
			{
				DeathFireObject.ReturnToPool();
				DeathFireObject = null;
			}
		}
	}

	public void ForceStopDeathFire()
	{
		DeathFireTokenSource.Cancel();
		if (!DeathFireObject.IsNull())
		{
			DeathFireObject.ReturnToPool();
			DeathFireObject = null;
		}
	}

	public virtual void VisualKnockBack(Direction direction)
	{
		visualKnockBackSequence?.Kill();
		visualKnockBackSequence = DOTween.Sequence();
		Transform tr = base.SkeletonAnimation.transform;
		visualKnockBackSequence.Append(tr.DOLocalMoveX(VisualKnockBackValue * (float)FacingDirection.GetSignX() * direction.ToVector2().x, VisualKnockBackDuration).SetEase(Ease.Linear)).Append(tr.DOLocalMoveX(0f, VisualKnockBackDuration).SetEase(Ease.Linear)).OnKill(delegate
		{
			tr.localPosition = Vector3.zero;
		});
	}

	private void OnDrawGizmos()
	{
		foreach (Zone item in from z in base.VfxController.GetAllAttackZones()
			select z.GetZoneOf(this))
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(item.Center, item.Size);
		}
	}

	protected override void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(VisualCenter, 0.2f);
		foreach (AttackSequence allAttackSequence in AllAttackSequences)
		{
			if (allAttackSequence.ShowInGizmos)
			{
				Gizmos.color = Color.blue;
				Zone sightZone = allAttackSequence.GetSightZone(this);
				Gizmos.DrawWireCube(sightZone.Center, sightZone.Size);
			}
		}
	}

	protected virtual bool IsInExecutionRange()
	{
		if (!Player.IsOnGround)
		{
			return false;
		}
		return (Player.IsFacingRight ? (NearestEdgePoint.x - Player.VisualBottom.x) : (Player.VisualBottom.x - NearestEdgePoint.x)) <= (float)PlayerControlParameter.Instance.ExecutionEnabledDistance;
	}

	protected void ReturnPaleBlow()
	{
		if ((object)redFogObject != null)
		{
			redFogObject.ParticleSystem.Stop();
			if (paleBlowCancelTokenSource != null)
			{
				paleBlowCancelTokenSource.Cancel();
				paleBlowCancelTokenSource = new CancellationTokenSource();
			}
		}
	}

	public virtual void ActivateBurstIcon(bool value, bool isClicked = false)
	{
	}

	public virtual void ActivateExecuteLine(bool value)
	{
	}

	public void AddHyperBurstExecutionEffect()
	{
		StatusEffectController.AddEffect(new ExecutionStatusEffect(this, int.MaxValue));
	}

	public virtual void OnEnterExecution()
	{
		ReturnPaleBlow();
		base.StateMachine.ChangeState(null);
		DisableVFXAll();
		SetAnimation(GroggyStartAnim, loop: false, 1f, 0, 0f, vfxEnable: false);
		AddAnimation(GroggyLoopAnim);
	}

	public void OnExitExecution()
	{
		StatusEffectController.Clear();
		if (!IsDead)
		{
			base.StateMachine.ChangeState(new GroggyEndState(this));
		}
	}

	public virtual void AddPlayerDashAttackEffect(int durationFrame)
	{
		if (!IsDead)
		{
			StatusEffectController.AddEffect(new DashFlameStatusEffect(this, MeshRenderer, durationFrame, delegate
			{
				DamageEffectAsync().Forget();
			}));
		}
	}

	protected bool IsExecutionFinished()
	{
		return StatusEffectController.GetEffect<ExecutionStatusEffect>().IsFinished;
	}

	protected void TryExplodeDashFlame()
	{
		DashFlameStatusEffect effect = StatusEffectController.GetEffect<DashFlameStatusEffect>();
		if (effect != null)
		{
			effect.Explode();
			StatusEffectController.RemoveEffect<DashFlameStatusEffect>();
		}
	}

	public bool CheckPlayerInBounds()
	{
		float num = HitBox.size.x / 2f;
		float num2 = base.Position.x - num;
		float num3 = base.Position.x + num;
		if (Player.Position.x >= num2 && Player.Position.x <= num3)
		{
			return true;
		}
		int signX = FacingDirection.GetSignX();
		int num4 = ((Player.Position.x - base.Position.x > 0f) ? 1 : (-1));
		return signX == num4;
	}

	public virtual void EnableHitBox(bool value)
	{
		HitBox.enabled = value;
	}

	protected void AddAttackSequence(AttackSequence seq)
	{
		if (allAttackSequences == null)
		{
			allAttackSequences = new List<AttackSequence>();
		}
		if (!allAttackSequences.Contains(seq))
		{
			allAttackSequences.Add(seq);
		}
	}

	protected void RemoveAttackSequence(AttackSequence seq)
	{
		allAttackSequences.Remove(seq);
	}

	protected void AddAction(ActionTiming timing, string actionName, Action action)
	{
		switch (timing)
		{
		case ActionTiming.Begin:
			TimelineBeginActions.TryAdd(actionName, delegate
			{
				action();
			});
			break;
		case ActionTiming.OnFrame:
			TimelineActions.TryAdd(actionName, delegate
			{
				action();
			});
			break;
		case ActionTiming.End:
			TimelineEndActions.TryAdd(actionName, delegate
			{
				action();
			});
			break;
		default:
			throw new ArgumentOutOfRangeException("timing", timing, null);
		}
	}

	protected void AddAction(ActionTiming timing, string actionName, Action<int> action)
	{
		switch (timing)
		{
		case ActionTiming.Begin:
			TimelineBeginActions.TryAdd(actionName, action);
			break;
		case ActionTiming.OnFrame:
			TimelineActions.TryAdd(actionName, action);
			break;
		case ActionTiming.End:
			TimelineEndActions.TryAdd(actionName, action);
			break;
		default:
			throw new ArgumentOutOfRangeException("timing", timing, null);
		}
	}

	protected void AddAction(ActionTiming timing, string actionName, Action<Parameter[]> action)
	{
		switch (timing)
		{
		case ActionTiming.Begin:
			BeginParamActions.TryAdd(actionName, delegate(int _, Parameter[] param)
			{
				action(param);
			});
			break;
		case ActionTiming.OnFrame:
			ParamActions.TryAdd(actionName, delegate(int _, Parameter[] param)
			{
				action(param);
			});
			break;
		case ActionTiming.End:
			EndParamActions.TryAdd(actionName, delegate(int _, Parameter[] param)
			{
				action(param);
			});
			break;
		default:
			throw new ArgumentOutOfRangeException("timing", timing, null);
		}
	}

	protected void AddAction(ActionTiming timing, string actionName, Action<int, Parameter[]> action)
	{
		switch (timing)
		{
		case ActionTiming.Begin:
			BeginParamActions.TryAdd(actionName, action);
			break;
		case ActionTiming.OnFrame:
			ParamActions.TryAdd(actionName, action);
			break;
		case ActionTiming.End:
			EndParamActions.TryAdd(actionName, action);
			break;
		default:
			throw new ArgumentOutOfRangeException("timing", timing, null);
		}
	}

	protected void SpreadMoney()
	{
		Money *= Mathf.CeilToInt(PlayerStat.GetStat<CollectingPower>().Rate);
		if (Money == 0)
		{
			return;
		}
		int minInclusive = Mathf.RoundToInt((float)Money * 0.7f);
		int num = Mathf.RoundToInt((float)Money * 1.3f);
		foreach (int randomAmount in SPR.Field.Money.GetRandomAmounts(CryptoRandom.Range(minInclusive, num + 1)))
		{
			Money money = Game.Instance.ObjectPoolManager.GetObject<Money>("Money.prefab", VisualCenter);
			money.Initialize(randomAmount);
			money.gameObject.SetActive(value: true);
		}
	}

	protected async UniTaskVoid PaleBlow(int duration, MeshRenderer meshRenderer)
	{
		redFogObject = PoolManager.GetObject<PoolingParticle>("PaleBlow_Fog.prefab", VisualCenter);
		ParticleSystem particleSystem = redFogObject.ParticleSystem;
		ParticleSystem.ShapeModule shape = particleSystem.shape;
		shape.meshRenderer = meshRenderer;
		ParticleSystem.MainModule main = particleSystem.main;
		main.duration = Util.FrameToSeconds(duration);
		main.simulationSpeed = 1.2f;
		if (this is Charon)
		{
			main.maxParticles = 100;
			ParticleSystem.EmissionModule emission = particleSystem.emission;
			emission.rateOverTime = 80f;
		}
		particleSystem.Play();
		for (int frame = 0; frame < duration; frame++)
		{
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate, paleBlowCancelTokenSource.Token);
		}
		main.simulationSpeed = 4f;
	}

	protected Area GetArea(int depth)
	{
		return myArea ?? (myArea = base.transform.GetComponentInParentWithDepth<Area>(depth));
	}

	protected virtual EnemyState GetBounceState()
	{
		return BounceState;
	}

	protected void ClearPlayerStagger()
	{
		if (Player.PSM.CurrentState is PlayerStaggerHitState)
		{
			DOTween.Kill(Player.transform);
			Player.SetGravityFree(active: false);
			Player.PSM.ChangeState(Player.PSM.IdleState);
		}
	}

	private async UniTaskVoid PaleBlow(int duration)
	{
		redFogObject = PoolManager.GetObject<PoolingParticle>("PaleBlow_Fog.prefab", VisualCenter);
		ParticleSystem particleSystem = redFogObject.ParticleSystem;
		ParticleSystem.ShapeModule shape = particleSystem.shape;
		shape.meshRenderer = MeshRenderer;
		ParticleSystem.MainModule main = particleSystem.main;
		main.duration = Util.FrameToSeconds(duration);
		main.simulationSpeed = 1.2f;
		if (this is Charon)
		{
			main.maxParticles = 100;
			ParticleSystem.EmissionModule emission = particleSystem.emission;
			emission.rateOverTime = 80f;
		}
		particleSystem.Play();
		for (int frame = 0; frame < duration; frame++)
		{
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate, paleBlowCancelTokenSource.Token);
		}
		main.simulationSpeed = 4f;
	}

	private float GetDefenseRate()
	{
		return StatusEffectController.GetEffect<DefenseDebuffStatusEffect>()?.DefenseRate ?? 1f;
	}

	private void OnDisable()
	{
		hyperBurstTokenSource?.Cancel();
		ReturnPaleBlow();
	}

	protected virtual void OnDestroy()
	{
		ReturnPaleBlow();
		StatusEffectController.Clear();
		StatusEffectController.Running();
		if (base.StateMachine.CurrentState is EnemyPierceState)
		{
			base.StateMachine.ChangeState(DefaultState);
		}
	}

	public bool Equals(Enemy other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)this != other)
		{
			return SpawnPosition.Equals(other.SpawnPosition);
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (obj != null)
		{
			return this == obj;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public static bool operator ==(Enemy left, Enemy right)
	{
		return object.Equals(left, right);
	}

	public static bool operator !=(Enemy left, Enemy right)
	{
		return !object.Equals(left, right);
	}
}
