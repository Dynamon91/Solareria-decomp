using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using Library;
using SPR.FSM;
using SPR.FSM.Base;
using SPR.Field;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class Baran : BossCharacter
{
	private enum Pattern
	{
		None,
		Attack1,
		Attack2,
		Attack3,
		Attack4,
		Attack5,
		Attack6,
		Attack7,
		Attack10
	}

	[Header("디버깅")]
	[SerializeField]
	private Pattern pattern;

	[Header("패턴")]
	[SerializeField]
	private GenericBossAttackSequence<Baran> attack1;

	[SerializeField]
	private GenericBossAttackSequence<Baran> attack2;

	[SerializeField]
	private GenericBossAttackSequence<Baran> attack3;

	[SerializeField]
	private GenericBossAttackSequence<Baran> attack4;

	[SerializeField]
	private GenericBossAttackSequence<Baran> attack5;

	[SerializeField]
	private GenericBossAttackSequence<Baran> attack5Kick;

	[SerializeField]
	private GenericBossAttackSequence<Baran> attack6;

	[SerializeField]
	private GenericBossAttackSequence<Baran> attack7;

	[SerializeField]
	private GenericBossAttackSequence<Baran> attack10;

	[Header("잡기")]
	[SerializeField]
	private Zone grabZone;

	[Header("기타 수치")]
	[SerializeField]
	private float pullPower;

	[SerializeField]
	private float windForce;

	[SerializeField]
	private int patternCancelChance;

	[Header("Attack7")]
	[SerializeField]
	private int attack7WaitDuration = 20;

	[SerializeField]
	private int totalStaggerDuration = 110;

	[Header("Attack9")]
	[SerializeField]
	private int attack9SignDuration;

	[SerializeField]
	private int attack9Duration;

	[SerializeField]
	private float attack9Gap;

	[Header("Attack10")]
	[SerializeField]
	private int jumpDuration;

	[SerializeField]
	private float jumpHeight;

	[SerializeField]
	private int landingDuration;

	[SerializeField]
	private int swordEnergyDuration;

	[SerializeField]
	private int energyAttackInterval;

	[SerializeField]
	private float windPushForce;

	[SerializeField]
	private int slashMoveDuration;

	[Header("Reference")]
	[SerializeField]
	private Transform swordTransform;

	[SerializeField]
	private Transform[] swordFollowEffects;

	[SerializeField]
	private Transform swordImpactEffect;

	[SerializeField]
	private ParticleSystem shield;

	[Header("SFX Event")]
	[SerializeField]
	private EventReference hitEvent;

	[SerializeField]
	private EventReference bounceEvent;

	[SerializeField]
	private EventReference cancelEvent;

	[SerializeField]
	private EventReference lightningEvent;

	[SerializeField]
	private EventReference shieldEvent;

	public const int OpeningStateIndex = 0;

	public const int RetryInFirstPhaseStateIndex = 1;

	public const int SecondPhaseStateIndex = 2;

	public const int RetryInSecondPhaseStateIndex = 3;

	public const int ThirdPhaseStateIndex = 4;

	public const int RetryInThirdPhaseStateIndex = 5;

	public const int DefeatStateIndex = 6;

	public const int DyingStateIndex = 7;

	public const int AfterDyingStateIndex = 8;

	public const int DyingMyosoStateIndex = 9;

	public const int AfterDyingMyosoStateIndex = 10;

	private CancellationTokenSource cts;

	private Vector3 swordDestination;

	private readonly List<LightningAttack> attack9List = new List<LightningAttack>();

	private bool isEnteringPhase3;

	private int defenseReduction;

	private bool stuckAvailable;

	private UniTask attack7WaitTask = UniTask.CompletedTask;

	private EventInstance shieldInstance;

	public override string Name => "Baran";

	public override string GroggyStartAnim => "groggy";

	public override bool CanMakeStuck
	{
		get
		{
			if (!stuckAvailable)
			{
				return IsShieldActive;
			}
			return true;
		}
	}

	public bool IsShieldActive { get; private set; }

	private PhaseTransitionCutsceneState PhaseTwoTransitionState { get; set; }

	private PhaseTransitionCutsceneState PhaseThreeTransitionState { get; set; }

	private CutsceneState DefeatState { get; set; }

	public IReadOnlyList<LightningAttack> Attack9List => attack9List;

	public bool IsAttack10Progressing
	{
		get
		{
			if (base.StateMachine.CurrentState is BossAttackState bossAttackState)
			{
				return bossAttackState.CurrentSequence == attack10;
			}
			return false;
		}
	}

	protected override int OriginDef => Mathf.Max(IsShieldActive ? base.EnemyStatData.Def[1] : (base.EnemyStatData.Def[0] - defenseReduction));

	protected override float GroggyThreshold
	{
		get
		{
			IState currentState = base.StateMachine.CurrentState;
			if (!(currentState is EnemyBounceState) && !(currentState is BaranPierceState))
			{
				return 0f;
			}
			return 1f;
		}
	}

	private Vector3 SwordLeftDestination => new Vector3(MyArea.AreaBox.Center.x - (float)MyArea.AreaBox.Size.x / 4f, base.SpawnPosition.y);

	private Vector3 SwordRightDestination => new Vector3(MyArea.AreaBox.Center.x + (float)MyArea.AreaBox.Size.x / 4f, base.SpawnPosition.y);

	private Vector2 LeftBorder => new Vector2((float)MyArea.AreaBox.LowerLeft.x + Width * 0.75f, base.SpawnPosition.y);

	private Vector2 RightBorder => new Vector2((float)MyArea.AreaBox.UpperRight.x - Width * 0.75f, base.SpawnPosition.y);

	private void LateUpdate()
	{
		if (!MyArea.AreaBox.IsInArea(Enemy.Player.transform.position))
		{
			Vector3 position = Enemy.Player.transform.position;
			position.x = Mathf.Clamp(position.x, MyArea.AreaBox.LowerLeft.x, MyArea.AreaBox.UpperRight.x);
		}
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		base.StateMachine.ChangeState(null);
		base.MoveForwardAnimation = "idle";
		base.MoveBackwardAnimation = "idle";
		PhaseTwoTransitionState = new PhaseTransitionCutsceneState(this, 2, blockControl: false, hideCanvas: false);
		PhaseThreeTransitionState = new PhaseTransitionCutsceneState(this, 4, blockControl: true);
		DefeatState = new CutsceneState(this, 6);
		base.StateMachine.ChangeState(base.IdleState);
		TimelineActions.Add("TryCancelAttack", delegate
		{
			TryCancelAttack().Forget();
		});
		AddAction(ActionTiming.Begin, "HoldPlayer", (Action)delegate
		{
			Enemy.Player.SetGravityFree(active: true);
			LazySingleton<MessageSystem>.Instance.Publish(new MyosoSupportEvent(MyosoEventState.PlayerGrabbed));
		});
		AddAction(ActionTiming.OnFrame, "HoldPlayer", HoldPlayer);
		AddAction(ActionTiming.End, "HoldPlayer", (Action)delegate
		{
			Enemy.Player.SetGravityFree(active: false);
		});
		TimelineActions.Add("ApplyPullForce", delegate
		{
			ApplyPullForce();
		});
		TimelineActions.Add("ApplyWindForce", delegate
		{
			ApplyWindForce();
		});
		TimelineActions.Add("TryKick", delegate
		{
			TryKick();
		});
		AddAction(ActionTiming.Begin, "MoveByAttack6", MoveByAttack6);
		AddAction(ActionTiming.Begin, "ActivateLightning", (Action<int, Parameter[]>)delegate
		{
			attack9List.Where((LightningAttack a) => a.IsActive).ForEach((LightningAttack a) => a.Activate(attack9SignDuration, attack9Duration));
		});
		TimelineActions.Add("ClearAttack9", delegate
		{
			ClearAttack9();
		});
		TimelineActions.Add("Jump", delegate
		{
			Jump();
		});
		TimelineActions.Add("ThrowSword", delegate
		{
			ThrowSword();
		});
		TimelineActions.Add("LandingGround", delegate
		{
			LandingGround();
		});
		TimelineActions.Add("EmitAttackSign", delegate
		{
			EmitAttackSign().Forget();
		});
		TimelineActions.Add("SlashMove", delegate
		{
			SlashMove();
		});
		TimelineActions.Add("StaggerAttack", delegate
		{
			StaggerAttack();
		});
		AddAction(ActionTiming.Begin, "CanParry", (Action)delegate
		{
			stuckAvailable = true;
		});
		AddAction(ActionTiming.End, "CanParry", (Action)delegate
		{
			stuckAvailable = false;
		});
		AddAction(ActionTiming.Begin, "ActivateShield", ActivateShield);
		AddAction(ActionTiming.Begin, "MovePlayerFront", (Action<int, Parameter[]>)delegate
		{
			base.SkeletonAnimation.Skeleton.SetAlpha(0f);
		});
		AddAction(ActionTiming.End, "MovePlayerFront", delegate(Parameter[] p)
		{
			MoveToPlayer(p[0].Float);
		});
		AddAction(ActionTiming.Begin, "RecoverStunPoint", delegate(Parameter[] p)
		{
			AddStunCount(p[0].Int);
		});
	}

	public override void OnEnterBounceState()
	{
		CancelBehaviour();
		Enemy.SoundManager.PlayOneShotAttached(bounceEvent, base.transform);
	}

	public override void OnEnterExecution()
	{
		CancelBehaviour();
		base.OnEnterExecution();
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (hitInfo.AttackName.Contains("Attack6"))
		{
			TryAttack7(attack7WaitDuration).Forget();
		}
		if (hitInfo.AttackName == "Baran_Attack10" && !Enemy.Player.IsImmortal && !Enemy.Player.PSM.StateOption.HasFlag(StateOption.Parry))
		{
			PlayerStaggerHitState newState = new PlayerStaggerHitState(Enemy.Player.PSM, null, null, delegate
			{
				Enemy.Player.TryTakeDamage(new HitInfo(this, "Attack10_1"));
				PlayStiffenVfx("VFX/Player_Stiffen_Damage.prefab", -Enemy.Player.LastHitDirection);
			})
			{
				Duration = 120,
				TargetPosition = Enemy.Player.Position
			};
			Enemy.Player.PSM.ChangeState(newState);
			PlayStiffenVfx("VFX/Player_Stiffen.prefab", base.transform.localScale.x);
		}
		static void PlayStiffenVfx(string key, float direction)
		{
			PoolableTimeLimitObject poolableTimeLimitObject = Game.Instance.ObjectPoolManager.GetObject<PoolableTimeLimitObject>(key, Enemy.Player.VisualCenter, Enemy.Player.transform);
			poolableTimeLimitObject.Initialize(key, Util.GetParticlesMaxRemainTime(poolableTimeLimitObject.GetComponent<ParticleSystem>()));
			Vector3 localScale = poolableTimeLimitObject.transform.localScale;
			int num = (Enemy.Player.IsFacingRight ? 1 : (-1));
			poolableTimeLimitObject.transform.localScale = new Vector3(Mathf.Sign(direction) * Mathf.Abs(localScale.x) / (float)num, localScale.y, localScale.z);
		}
	}

	public async UniTaskVoid ForcePause(int duration)
	{
		base.VfxController.Pause();
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(duration);
		base.VfxController.Resume();
	}

	public override void MoveX(float velocityX, float deltaTime, bool moveForward)
	{
		Vector2 vector = (moveForward ? FacingDirection.ToVector2() : FacingDirection.Opposite().ToVector2());
		float num = vector.x * velocityX * deltaTime;
		if ((vector.x < 0f && LeftBorder.x < VisualBottom.x + num) || (vector.x > 0f && RightBorder.x > VisualBottom.x + num))
		{
			base.transform.position += (Vector3)vector * (velocityX * deltaTime);
		}
		else if (base.StateMachine.CurrentState == base.IdleState)
		{
			SetAnimation("idle");
		}
	}

	public override bool TryPhaseTransition()
	{
		if (!IsGroggyPlaying && base.Phase == 1 && base.Health < base.Phase2Health)
		{
			EnterCutsceneState(2, active: true);
			return true;
		}
		return false;
	}

	public void DisableShield()
	{
		IsShieldActive = false;
		shield.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
		if (shieldInstance.isValid())
		{
			shieldInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			shieldInstance.release();
		}
	}

	public void ReduceDefense(int reduction)
	{
		defenseReduction = reduction;
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (IsShieldActive && hitInfo.AttackAttribute.HasFlag(AttackAttribute.HeavyAttack))
		{
			DisableShield();
		}
		if (hitInfo.Source is PlayerMover && CanMakeStuck && !Enemy.Player.CanParry(hitInfo))
		{
			LazySingleton<MessageSystem>.Instance.Publish(new MyosoSupportEvent(MyosoEventState.PlayerBounced));
		}
		if (IsAttack10Progressing && Groggy.Value - hitInfo.GroggyDamage < 1f)
		{
			hitInfo.SetGroggyDamage(Mathf.Max(0f, Groggy.Value - 1f));
		}
		return base.TryTakeDamage(hitInfo);
	}

	public void OnPierce()
	{
		stuckAvailable = false;
		if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState)
		{
			Enemy.Player.SetGravityFree(active: false);
			Enemy.Player.PSM.ChangeState(Enemy.Player.PSM.IdleState);
		}
	}

	protected override void OnDamaged(HitInfo hitInfo)
	{
		base.OnDamaged(hitInfo);
		Enemy.SoundManager.PlayOneShotAttached(hitEvent, base.transform);
	}

	protected override void Die()
	{
		if (!isEnteringPhase3)
		{
			if (base.Phase < 3)
			{
				EnterPhase3().Forget();
			}
			else
			{
				EnterDie().Forget();
			}
			ClearStatusEffect(force: true);
			DisableShield();
		}
	}

	protected override void OnDie()
	{
		ClearPlayerStagger();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack1, attack2, attack3, attack4, attack5, attack6, attack10 };
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		cts = null;
		if (pattern != Pattern.None)
		{
			return pattern switch
			{
				Pattern.Attack1 => new List<BossAttackSequence> { attack1 }, 
				Pattern.Attack2 => new List<BossAttackSequence> { attack2 }, 
				Pattern.Attack3 => new List<BossAttackSequence> { attack3 }, 
				Pattern.Attack4 => new List<BossAttackSequence> { attack4 }, 
				Pattern.Attack5 => new List<BossAttackSequence> { attack5 }, 
				Pattern.Attack6 => new List<BossAttackSequence> { attack6 }, 
				Pattern.Attack7 => new List<BossAttackSequence> { attack7 }, 
				Pattern.Attack10 => new List<BossAttackSequence> { attack10 }, 
				_ => new List<BossAttackSequence>(), 
			};
		}
		List<int> list = new List<int>();
		list.AddRange(Enumerable.Repeat(0, attack1.GetPower(this)));
		list.AddRange(Enumerable.Repeat(1, attack2.GetPower(this)));
		list.AddRange(Enumerable.Repeat(2, attack3.GetPower(this)));
		list.AddRange(Enumerable.Repeat(3, attack4.GetPower(this)));
		list.AddRange(Enumerable.Repeat(4, attack5.GetPower(this)));
		list.AddRange(Enumerable.Repeat(5, attack6.GetPower(this)));
		if (base.Phase >= 3)
		{
			list.AddRange(Enumerable.Repeat(6, attack10.GetPower(this)));
		}
		int num = list[CryptoRandom.Range(0, list.Count)];
		for (int i = 0; i < base.AllAttackSequences.Count; i++)
		{
			if (i != num)
			{
				base.AllAttackSequences[i].CastObject<BossAttackSequence>().RisePower();
			}
		}
		return num switch
		{
			0 => new List<BossAttackSequence> { attack1 }, 
			1 => new List<BossAttackSequence> { attack2 }, 
			2 => new List<BossAttackSequence> { attack3 }, 
			3 => new List<BossAttackSequence> { attack4 }, 
			4 => new List<BossAttackSequence> { attack5 }, 
			5 => new List<BossAttackSequence> { attack6 }, 
			6 => new List<BossAttackSequence> { attack10 }, 
			_ => new List<BossAttackSequence>(), 
		};
	}

	protected override void OnEnterGroggyState()
	{
		base.OnEnterGroggyState();
		CancelBehaviour();
	}

	protected override void OnExitGroggyState()
	{
		base.OnExitGroggyState();
		cts = null;
	}

	protected override CutsceneState GetTargetState(int index)
	{
		return index switch
		{
			-1 => DefeatState, 
			2 => PhaseTwoTransitionState, 
			4 => PhaseThreeTransitionState, 
			6 => DefeatState, 
			_ => null, 
		};
	}

	private async UniTaskVoid EnterPhase3()
	{
		isEnteringPhase3 = true;
		await UniTask.WaitUntil(() => !(base.StateMachine.CurrentState is GroggyState) && !(base.StateMachine.CurrentState is GroggyEndState));
		EnterCutsceneState(4);
		health = new ClampedInt(base.Phase3Health, 0, base.Phase3Health);
		isEnteringPhase3 = false;
	}

	private async UniTaskVoid TryCancelAttack()
	{
		if (base.Phase >= 2 && patternCancelChance > CryptoRandom.Range(1, 101))
		{
			CancelBehaviour();
			BossAttackState newState = new BossAttackState(this, () => new List<AttackSequence> { attack6 });
			base.StateMachine.ChangeState(newState);
			await EmitAttackSign();
		}
	}

	private void TryKick()
	{
		if (CryptoRandom.RandomBool())
		{
			BossAttackState newState = new BossAttackState(this, () => new List<AttackSequence> { attack5Kick });
			base.StateMachine.ChangeState(newState);
		}
	}

	private void MoveByAttack6(int duration)
	{
		if (base.Phase == 3)
		{
			SpawnAttack9();
		}
		Vector2 vector = ((FacingDirection.GetSignX() < 0) ? LeftBorder : RightBorder);
		base.transform.DOMoveX(vector.x, Util.FrameToSeconds(duration)).SetUpdate(UpdateType.Fixed).SetEase(Ease.Linear);
	}

	private void CancelBehaviour()
	{
		DOTween.Kill(base.transform);
		cts = new CancellationTokenSource();
		cts.Cancel();
		ActiveAttack9();
		attack9List.Where((LightningAttack a) => a.IsActive).ForEach(delegate(LightningAttack a)
		{
			a.ReturnToPool();
		});
		stuckAvailable = false;
	}

	private void HoldPlayer()
	{
		if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState playerStaggerHitState)
		{
			playerStaggerHitState.TargetPosition = swordTransform.position;
		}
	}

	private void ApplyPullForce()
	{
		if (Enemy.Player.TryGetComponent<Rigidbody2D>(out var component))
		{
			Vector2 vector = GetBoneWorldPosition("Shield_F_Ctl");
			Vector2 vector2 = new Vector2(-(Enemy.Player.VisualBottom - vector).normalized.x.GetSign(), 0f);
			component.AddForce(vector2 * pullPower, ForceMode2D.Force);
		}
	}

	private void ApplyWindForce()
	{
		if (Enemy.Player.TryGetComponent<Rigidbody2D>(out var component))
		{
			Vector2 vector = new Vector2((Enemy.Player.VisualBottom - VisualBottom).normalized.x.GetSign(), 0f);
			component.AddForce(vector * windForce, ForceMode2D.Force);
		}
	}

	private async UniTaskVoid PushPlayer(Vector2 position)
	{
		Enemy.Player.TryGetComponent<Rigidbody2D>(out var rb2D);
		int frame = 0;
		while (frame < swordEnergyDuration && !IsInGroggyState)
		{
			Vector2 vector = new Vector2((position - Enemy.Player.VisualBottom).normalized.x.GetSign(), 0f);
			rb2D.AddForce(vector * windPushForce, ForceMode2D.Force);
			frame++;
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
	}

	private void SpawnAttack9()
	{
		List<LightningAttack> soundList = new List<LightningAttack>();
		Vector2 vector = new Vector2(VisualBottom.x, MyArea.AreaBox.UpperRight.y);
		do
		{
			LightningAttack item = Enemy.PoolManager.GetObject<LightningAttack>("Attack9_Lightning.prefab", vector);
			attack9List.Add(item);
			soundList.Add(item);
			vector += new Vector2((float)FacingDirection.GetSignX() * attack9Gap, 0f);
		}
		while (LeftBorder.x <= vector.x && vector.x <= RightBorder.x);
		UniTaskExtension.WaitForFixedUpdatesOrCancel(attack9Duration).GetAwaiter().OnCompleted(delegate
		{
			if (soundList.Any((LightningAttack ba) => ba.gameObject.activeInHierarchy && !ba.IsActive))
			{
				Enemy.SoundManager.PlayOneShot(lightningEvent);
			}
		});
	}

	private void ActiveAttack9()
	{
		foreach (LightningAttack item in attack9List.Where((LightningAttack a) => a.IsActive))
		{
			if ((FacingDirection == Direction.Left && item.transform.position.x >= VisualBottom.x) || (FacingDirection == Direction.Right && item.transform.position.x <= VisualBottom.x))
			{
				item.Activate(attack9SignDuration, attack9Duration).Forget();
			}
		}
	}

	private void ClearAttack9()
	{
		attack9List.ForEach(delegate(LightningAttack a)
		{
			a.ReturnToPool();
		});
		attack9List.Clear();
	}

	private void Jump()
	{
		Vector3 endValue = base.transform.position + new Vector3(0f, jumpHeight);
		base.transform.DOMove(endValue, Util.FrameToSeconds(jumpDuration));
		swordDestination = (CryptoRandom.RandomBool() ? SwordLeftDestination : SwordRightDestination);
		LookAt(swordDestination);
	}

	private void ThrowSword()
	{
		Vector2 vector = GetBoneWorldPosition("Sword_Ctl");
		PoolableTimeLimitObject sword = Enemy.PoolManager.GetObject<PoolableTimeLimitObject>("BaranSword.prefab", vector);
		sword.Initialize("BaranSword.prefab", Util.FrameToSeconds(90f));
		Vector2 normalized = ((Vector2)swordDestination - vector).normalized;
		float angle = Vector2.SignedAngle(Vector2.up, normalized);
		swordFollowEffects.ForEach(delegate(Transform e)
		{
			e.rotation = Quaternion.Euler(0f, 0f, angle + (float)(-angle.GetSign()) * 90f);
			UnityEngine.Object.Destroy(e.GetComponent<BoneFollower>());
		});
		sword.transform.rotation = Quaternion.Euler(0f, 0f, angle);
		sword.transform.DOMove(swordDestination, 0.1f).OnUpdate(delegate
		{
			Transform[] array = swordFollowEffects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].transform.position = sword.transform.position;
			}
		}).OnComplete(delegate
		{
			PushPlayer(swordDestination).Forget();
			swordImpactEffect.transform.position = sword.transform.position;
		});
	}

	private void LandingGround()
	{
		base.transform.DOMove(swordDestination, Util.FrameToSeconds(landingDuration));
	}

	private async UniTask EmitAttackSign()
	{
		Vector2 vector = VisualCenter + new Vector2(0f, 4f);
		PoolableTimeLimitObject poolableTimeLimitObject = Enemy.PoolManager.GetObject<PoolableTimeLimitObject>("Attack8_Sign.prefab", vector);
		poolableTimeLimitObject.Initialize("Attack8_Sign.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		BoneFollower component = poolableTimeLimitObject.GetComponent<BoneFollower>();
		component.skeletonRenderer = base.SkeletonAnimation;
		component.boneName = "Head_Root";
		Enemy.SoundManager.PlayOneShotAttached(cancelEvent, base.transform);
		await UniTask.WaitForSeconds(Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
	}

	private void SlashMove()
	{
		Vector2 vector = Vector2.zero;
		if (swordDestination == SwordLeftDestination)
		{
			vector = RightBorder;
		}
		else if (swordDestination == SwordRightDestination)
		{
			vector = LeftBorder;
		}
		base.transform.DOMove(vector, Util.FrameToSeconds(slashMoveDuration)).SetUpdate(UpdateType.Fixed).SetEase(Ease.Linear);
	}

	private void StaggerAttack()
	{
		if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState)
		{
			Enemy.Player.TryTakeDamage(new HitInfo(this, "Attack10_Tick"));
			Enemy.SoundManager.PlayOneShot("event:/SFX/Player/Hit/SFX_Hit");
			PoolableTimeLimitObject poolableTimeLimitObject = Enemy.PoolManager.GetObject<PoolableTimeLimitObject>("Hit_Fire.prefab", Enemy.Player.VisualCenter);
			poolableTimeLimitObject.Initialize("Hit_Fire.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		}
	}

	private async UniTaskVoid TryAttack7(int waitDuration)
	{
		if (base.Phase < 2 || attack7WaitTask.Status == UniTaskStatus.Pending)
		{
			return;
		}
		bool flag = Enemy.Player.CanGuard(AttackAttribute.Default, FacingDirection.GetSignX());
		if (!(!Enemy.Player.CanHit || flag) && !Enemy.Player.PSM.StateOption.HasFlag(StateOption.Parry))
		{
			cts = new CancellationTokenSource();
			PlayerStaggerHitState newState = new PlayerStaggerHitState(Enemy.Player.PSM, delegate
			{
				Enemy.Player.AddAnimation("hit_loop");
			})
			{
				Duration = totalStaggerDuration + waitDuration,
				Animation = string.Empty,
				NextState = Enemy.Player.PSM.DownState,
				TargetPosition = swordTransform.position
			};
			Enemy.Player.PSM.ChangeState(newState);
			attack7WaitTask = UniTaskExtension.WaitForFixedUpdatesOrCancel(waitDuration, cts.Token);
			await attack7WaitTask;
			CancelBehaviour();
			BossAttackState newState2 = new BossAttackState(this, () => new List<AttackSequence> { attack7 });
			base.StateMachine.ChangeState(newState2);
			attack7WaitTask = UniTask.CompletedTask;
		}
	}

	public void ActivateShield()
	{
		if (!IsShieldActive)
		{
			IsShieldActive = true;
			shield.Play(withChildren: true);
			shieldInstance = Enemy.SoundManager.PlayOneShotAttached(shieldEvent, base.transform);
		}
	}

	public void HitSpear()
	{
		string key = (IsShieldActive ? "Baran_GuardBreak_B.prefab" : "Baran_GuardBreak_A.prefab");
		Game.Instance.ObjectPoolManager.GetObject<PoolingParticle>(key, GetBoneWorldPosition("Armor_Center_CT2")).Play();
		DisableShield();
		stuckAvailable = false;
	}

	private void MoveToPlayer(float distance)
	{
		int sign = (VisualBottom - Enemy.Player.VisualBottom).normalized.x.GetSign();
		base.transform.position = new Vector3(Enemy.Player.VisualBottom.x + (float)sign * distance, base.SpawnPosition.y);
		base.SkeletonAnimation.Skeleton.SetAlpha(1f);
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Zone zoneOf = grabZone.GetZoneOf(this);
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(zoneOf.Center, zoneOf.Size);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		DisableShield();
	}
}
