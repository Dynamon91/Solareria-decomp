using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Achievement;
using SPR.FSM;
using SPR.FSM.Base;
using SPR.UI;
using UnityEngine;

namespace SPR;

public abstract class BossCharacter : Enemy, IPatternSelectable
{
	[Header("피로도")]
	[ReadOnlyInspector]
	[SerializeField]
	private float fatigue;

	[Header("Boss")]
	[SerializeField]
	private BossName bossName;

	[SerializeField]
	private float initialFatigue;

	[SerializeField]
	private float phaseTransitionGroggyRecovery = 20f;

	[SerializeField]
	protected float phaseTransitionGroggyLimit = 20f;

	[SerializeField]
	private ParticleSystem hitEffect;

	[Header("Idle Sequence")]
	[SerializeField]
	private bool canMove = true;

	[SerializeField]
	private BossIdleSequence idle;

	[SerializeField]
	private BossMoveSequence moveForward;

	[SerializeField]
	private BossMoveSequence moveBackward;

	private BossCharacterSpeech bossCharacterSpeech;

	public BossName BossName => bossName;

	public override EnemyState DefaultState => IdleState;

	public BossIdleState IdleState { get; protected set; }

	public BossAttackState AttackState { get; protected set; }

	public int Phase2Health { get; private set; }

	public int Phase3Health { get; private set; }

	public int Phase { get; private set; }

	public float Fatigue { get; protected set; }

	public float PhaseTransitionGroggyRecovery => phaseTransitionGroggyRecovery;

	public override HitType HitType => HitType.Boss;

	public override bool IsDefenseless
	{
		get
		{
			if (IsInGroggyState && base.VfxController.IsTimelineEnd("groggy_start") && IsInExecutionRange())
			{
				return !CanMakeStuck;
			}
			return false;
		}
	}

	public bool IsImmortal { get; set; }

	public override bool IsGroggyPlaying
	{
		get
		{
			if (!IsInGroggyState)
			{
				return base.StateMachine.CurrentState is GroggyEndState;
			}
			return true;
		}
	}

	public bool CanMove => canMove;

	public BossIdleSequence Idle => idle;

	public BossMoveSequence MoveForward => moveForward;

	public BossMoveSequence MoveBackward => moveBackward;

	protected float InitialFatigue => initialFatigue;

	public virtual string IdleAnimation => "idle";

	public string MoveForwardAnimation { get; protected set; } = "walk";

	public string MoveBackwardAnimation { get; protected set; } = "walk_back";

	public bool IdleLoop { get; protected set; } = true;

	protected static BossBoard BossBoard => Game.Instance.UIManager.FieldCanvas.BossBoard;

	protected override void Awake()
	{
		base.Awake();
		Phase2Health = base.EnemyStatData.Phase2HP;
		Phase3Health = base.EnemyStatData.Phase3HP;
		bossCharacterSpeech = new BossCharacterSpeech(this);
		Phase = 1;
		AddFatigue(initialFatigue);
		IdleState = new BossIdleState(this);
		AttackState = new BossAttackState(this, GenerateCurrentAttackSequences);
	}

	protected override void Update()
	{
		base.Update();
		fatigue = Fatigue;
		if (!bossCharacterSpeech.IsDone)
		{
			bossCharacterSpeech.Update(Time.deltaTime);
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		BossBoard.HideAsync(0.1f).Forget();
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		base.StateMachine.ChangeState(IdleState);
	}

	public override void OnEnterExecution()
	{
		base.OnEnterExecution();
		ForceDrop();
	}

	public override void EnableHitBox(bool value)
	{
		IsImmortal = !value;
		base.EnableHitBox(value);
	}

	protected sealed override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return CreateBossAttackSequences().Cast<AttackSequence>().ToList();
	}

	protected abstract List<BossAttackSequence> CreateBossAttackSequences();

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (IsImmortal || Game.Instance.Player.isDead)
		{
			return false;
		}
		int num = CalculateDamage(hitInfo.Damage, hitInfo.FireDamage);
		int value = health.Value;
		if (health.Value > 0)
		{
			if (Phase == 1 && Phase2Health > 0 && health.Value - num <= 0)
			{
				OnChangeHealth(health.Value - value);
				health = new ClampedInt(1, 0, maxHealth);
				OnDamaged(hitInfo);
				return false;
			}
			if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.HyperBurst) || (HasStatusEffect<ExecutionStatusEffect>() && !IsExecutionFinished()))
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
		if (base.StateMachine.CurrentState is CutsceneState && Groggy.Value - hitInfo.GroggyDamage < phaseTransitionGroggyLimit)
		{
			hitInfo.SetGroggyDamage(Mathf.Max(0f, Groggy.Value - phaseTransitionGroggyLimit));
		}
		TakeGroggyDamage(hitInfo);
		if (num != 0)
		{
			OnDamaged(hitInfo);
		}
		return true;
	}

	public override void MoveX(float velocityX, float deltaTime, bool moveForward)
	{
		AreaBox areaBox = MyArea.AreaBox;
		float num = HitBox.size.x / 2f;
		float num2 = VisualCenter.x - num;
		float num3 = VisualCenter.x + num;
		Vector2 vector = (moveForward ? FacingDirection.ToVector2() : FacingDirection.Opposite().ToVector2());
		float num4 = vector.x * velocityX * deltaTime;
		if ((vector.x < 0f && (float)areaBox.LowerLeft.x < num2 + num4) || (vector.x > 0f && (float)areaBox.UpperRight.x > num3 + num4))
		{
			base.transform.position += (Vector3)vector * (velocityX * deltaTime);
		}
		else if (base.StateMachine.CurrentState == IdleState)
		{
			SetAnimation(IdleAnimation);
		}
	}

	public virtual bool TryPhaseTransition()
	{
		return false;
	}

	public virtual void OnIdle()
	{
	}

	protected override void OnChangeHealth(int delta)
	{
		BossBoard.UpdateHealthBar(base.Health, base.MaxHealth, delta);
	}

	protected virtual void OnIncreasePhase(int before, int after)
	{
		if (after == 3)
		{
			Groggy.ResetGroggy(includeMax: false);
		}
	}

	protected override void Die()
	{
		Enemy.PlayerStat.AddStatAppliedExperience(base.EnemyStatData.Exp);
		Game.Instance.Player.PlayerPart.OnKill();
		EnterDie().Forget();
		ClearStatusEffect(force: false);
	}

	protected virtual void OnDie()
	{
	}

	protected override void ResetStatus()
	{
		base.ResetStatus();
		Phase = 1;
		AddFatigue(initialFatigue);
		base.StateMachine.ChangeState(IdleState);
	}

	protected async UniTaskVoid EnterDie()
	{
		await UniTask.WaitUntil(() => Enemy.Player.PSM.CurrentState != Enemy.Player.PSM.BurstState);
		ClearStatusEffect(force: true);
		ForceStopDeathFire();
		EnterCutsceneState(-1);
		BossBoard.HideAsync(0.6f, deactivate: true).Forget();
		if (!Game.Instance.SaveData.EnemyFightData.IsBossCleared(bossName))
		{
			Game.Instance.SaveData.EnemyFightData.TryBreakBossClearRecord(bossName, float.MaxValue);
		}
		AchievementHelper.CheckTriggers();
		OnDie();
	}

	public void IncreasePhase()
	{
		OnIncreasePhase(Phase, Phase + 1);
		Phase++;
		bossCharacterSpeech.PhaseChanged(Phase);
	}

	public void AddFatigue(float value)
	{
		Fatigue = Mathf.Max(Fatigue + value, 0f);
	}

	public void ResetOtherAttackSequences(BossAttackSequence currentSequence)
	{
		foreach (AttackSequence allAttackSequence in base.AllAttackSequences)
		{
			if (allAttackSequence != currentSequence)
			{
				(allAttackSequence as BossAttackSequence)?.Reset();
			}
		}
	}

	public void EnterCutsceneState(int index, bool active = false)
	{
		IsImmortal = true;
		CutsceneState targetState = GetTargetState(index);
		if (targetState == null)
		{
			DebugLog.GeneralLogError($"{base.name}가 가질 수 없는 컷씬 인덱스입니다. 인덱스: {index}");
			return;
		}
		base.gameObject.SetActive(active);
		base.StateMachine.ChangeState(targetState);
	}

	public void ExitCutsceneState(Transform spineTransform)
	{
		base.transform.position = spineTransform.position - base.SkeletonAnimation.transform.localPosition;
		Direction direction = ((spineTransform.localScale.x > 0f) ? Direction.Right : Direction.Left);
		Flip(direction);
		IsImmortal = false;
		BossBoard.SetBoss(this);
		BossBoard.UpdateStunPoint(base.CurrentStun);
		base.gameObject.SetActive(value: true);
		base.StateMachine.ChangeState(DefaultState);
	}

	protected abstract CutsceneState GetTargetState(int index);

	public bool IsPlayerInRange(float value, bool ignoreY = false)
	{
		Vector2 vector = Enemy.Player.VisualCenter - VisualCenter;
		if (ignoreY)
		{
			vector.y = 0f;
		}
		return vector.magnitude <= value;
	}

	public void PlayHitEffect()
	{
		if (!hitEffect.IsNull())
		{
			hitEffect.Play();
		}
	}

	public override void ActivateBurstIcon(bool value, bool isClicked = false)
	{
		BossBoard.ActivateBurstIcon(value, isClicked);
	}

	public override void ActivateExecuteLine(bool value)
	{
		BossBoard.ActivateExecuteLine(value);
	}

	public virtual void OnAttackEnd(IState nextState)
	{
	}

	public override void AddStunCount(int value)
	{
		base.AddStunCount(value);
		BossBoard.UpdateStunPoint(base.CurrentStun);
	}

	public override void ResetStunCount()
	{
		base.ResetStunCount();
		BossBoard.ResetStunPoint();
	}
}
