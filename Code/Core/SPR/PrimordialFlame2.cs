using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.FSM.Base;
using SPR.Library;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Serialization;

namespace SPR;

public class PrimordialFlame2 : BossCharacter
{
	[Serializable]
	private class AttackZone
	{
		[SerializeField]
		public string name;

		[SerializeField]
		public bool drawGizmo;

		[SerializeField]
		public Zone zone;
	}

	[Header("3 페이즈")]
	[SerializeField]
	private int normalEndingHealthRatio = 40;

	[SerializeField]
	private int normalEndingRecoverRatio = 30;

	[SerializeField]
	private int trueEndingHealthRatio = 20;

	[Header("중력")]
	[SerializeField]
	private float gravityAcceleration;

	[Header("Sequence")]
	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> basicAttack1;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> basicAttack2;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> basicAttack3;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> basicAttack4;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> jumpAttack;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> jumpAttack2Phase2;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> jumpAttack2Phase3;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> ashBreak;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> ashBreakCombo;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> swordOfGod;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> lightning;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> dash;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> chargeAttack;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> pyronAction;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> strongPyronAction;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame2> warmhorn;

	[Header("패시브 - 패링")]
	[SerializeField]
	private int parryChance = 30;

	[SerializeField]
	private int addParryChanceOnGuard = 1;

	[SerializeField]
	private int parryActivateFrame = 60;

	[SerializeField]
	private float onParryUpdateFatigue = 1f;

	[SerializeField]
	private int activeParryDamageCount = 2;

	[Header("패시브 - 잔열")]
	[Tooltip("일반 피해량 잔열 전환률")]
	[SerializeField]
	private int hitResidualRate = 25;

	[Tooltip("잔열 당 HP 회복량")]
	[SerializeField]
	private int heatPerHealth = 10;

	[Tooltip("잔열 최대 소모량")]
	[SerializeField]
	private int maxHeatChange;

	[Header("종말 번개")]
	[SerializeField]
	private int lightningRandomWaitDuration;

	[FormerlySerializedAs("lightningWaringDuration")]
	[SerializeField]
	private int lightningWarningDuration;

	[SerializeField]
	private int lightningWaitDuration;

	[Header("파이론 액션")]
	[SerializeField]
	private int strongPyronActionWaitFrame = 20;

	[Header("웜혼")]
	[SerializeField]
	private int normalEndingAddGroggy = 10;

	[Header("AttackZone")]
	[SerializeField]
	private List<AttackZone> attackZones;

	[Header("Transform")]
	[SerializeField]
	private Transform lanceTransform;

	[SerializeField]
	private Transform pyronActionTrail;

	[SerializeField]
	private Transform flamePosition;

	[Header("FMOD")]
	[SerializeField]
	private EventReference lightningFlashEvent;

	[SerializeField]
	private EventReference lightningEvent;

	private float gravity;

	private readonly List<IDamageable> damageList = new List<IDamageable>();

	private int residualHeatAmount;

	private int currentParryChance;

	private PrimordialFlame2ParryState parryState;

	private bool heatReduced;

	private PhaseTransitionCutsceneState phaseThreeTransitionState;

	private CutsceneState defeatState;

	private bool warmhornUsed;

	private bool skipSwordOfGodPostDelay;

	private bool lightningProgress;

	private LightningAttack lightningAttack;

	private string prevAttackName;

	private int parryCount;

	private int parriedGroundAttackIndex;

	private DG.Tweening.Sequence jumpSequence;

	private DG.Tweening.Sequence jumpAttackSequence;

	public override string ID => "PrimordialFlame_Phase2";

	public override string Name
	{
		get
		{
			if (IsPhase3)
			{
				return "PrimordialFlame_Phase3";
			}
			return "PrimordialFlame_Phase2";
		}
	}

	public override bool IsDead
	{
		get
		{
			if (health.Value <= 0)
			{
				return IsPhase3;
			}
			return false;
		}
	}

	protected override int OriginDef
	{
		get
		{
			if (base.StateMachine.CurrentState is BossAttackState bossAttackState && bossAttackState.CurrentSequence.CastObject<GenericBossAttackSequence<PrimordialFlame2>>().TimelineName.Equals("warmhorn"))
			{
				return base.EnemyStatData.Def[1];
			}
			return base.EnemyStatData.Def[0];
		}
	}

	public int Stun => base.EnemyStatData.Stun;

	public bool IsGround => Mathf.Approximately(base.Position.y, base.SpawnPosition.y);

	private bool IsPhase3 => base.Phase > 1;

	public bool IsTrueEnding => Trigger.CheckTrigger("Trigger_IsTrueEnding");

	private GenericBossAttackSequence<PrimordialFlame2> JumpAttack2
	{
		get
		{
			if (IsPhase3)
			{
				return jumpAttack2Phase3;
			}
			return jumpAttack2Phase2;
		}
	}

	private void LateUpdate()
	{
		AdjustXPosition();
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		base.MoveBackwardAnimation = "walk";
		base.IdleState = new PrimordialFlame2IdleState(this, gravityAcceleration);
		parryState = new PrimordialFlame2ParryState(this);
		phaseThreeTransitionState = new PhaseTransitionCutsceneState(this, 4, blockControl: false, hideCanvas: false);
		defeatState = new CutsceneState(this, 6);
		AddAction(ActionTiming.Begin, "JumpAttack", delegate(int duration, Parameter[] p)
		{
			JumpAttack(duration, p[0].Float, p[1].Float);
		});
		AddAction(ActionTiming.Begin, "AshBreakJump", delegate(int duration, Parameter[] p)
		{
			AshBreakJump(duration, p[0].Float, p[1].Float);
		});
		AddAction(ActionTiming.Begin, "AshBreakAttack", (Action)delegate
		{
			damageList.Clear();
		});
		AddAction(ActionTiming.OnFrame, "AshBreakAttack", AshBreakAttack);
		AddAction(ActionTiming.Begin, "SwordOfGodDrop", SwordOfGodDrop);
		AddAction(ActionTiming.Begin, "Invincible", (Action)delegate
		{
			base.IsImmortal = true;
		});
		AddAction(ActionTiming.End, "Invincible", (Action)delegate
		{
			base.IsImmortal = false;
			CheckDashNext();
		});
		AddAction(ActionTiming.Begin, "PyronAction", delegate(int duration, Parameter[] p)
		{
			PyronAction(duration, p[0].Bool, p[1].String);
		});
		AddAction(ActionTiming.Begin, "LightningStrike", delegate(Parameter[] p)
		{
			LightningStrike(p[0].Bool).Forget();
		});
		AddAction(ActionTiming.Begin, "JumpAttack2Rise", delegate(int duration, Parameter[] p)
		{
			JumpAttack2Rise(duration, p[0].Float, p[1].Float, p[2].Ease, p[3].Ease);
		});
		AddAction(ActionTiming.Begin, "JumpAttack2Drop", delegate(int duration, Parameter[] p)
		{
			JumpAttack2Drop(duration, p[0].Float, p[1].Ease, p[2].Ease);
		});
		AddAction(ActionTiming.Begin, "SkipDashPostDelay", SkipDashPostDelay);
		AddAction(ActionTiming.Begin, "SkipSwordOfGodPostDelay", SkipSwordOfGodPostDelay);
		AddAction(ActionTiming.Begin, "LanceHoldPlayer", (Action)delegate
		{
			Enemy.Player.TryTakeDamage(new HitInfo(this, "StrongPyronAction_In"));
		});
		AddAction(ActionTiming.OnFrame, "LanceHoldPlayer", LanceHoldPlayer);
		AddAction(ActionTiming.End, "LanceHoldPlayer", (Action)delegate
		{
			Enemy.Player.SetGravityFree(active: false);
		});
		AddAction(ActionTiming.Begin, "MovePlayerFront", (Action<int, Parameter[]>)delegate
		{
			base.SkeletonAnimation.Skeleton.SetAlpha(0f);
		});
		AddAction(ActionTiming.End, "MovePlayerFront", delegate(Parameter[] p)
		{
			MoveToPlayer(p[0].Float);
		});
		AddAction(ActionTiming.Begin, "UseWarmhorn", UseWarmhorn);
		AddAction(ActionTiming.Begin, "StrongPyronActionAura", (Action<int, Parameter[]>)delegate
		{
			StrongPyronActionAura();
		});
		AddAction(ActionTiming.Begin, "SwordDisappear", SwordDisappear);
		AddAction(ActionTiming.Begin, "Flame", delegate(Parameter[] p)
		{
			Flame(p[0].Int, p[1].Int, p[2].Int);
		});
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (base.IsImmortal)
		{
			return false;
		}
		int num = ((base.StateMachine.CurrentState is PhaseTransitionCutsceneState) ? 1 : CalculateDamage(hitInfo.Damage, hitInfo.FireDamage));
		IState currentState = Enemy.Player.PSM.CurrentState;
		if (currentState is PlayerParryActionState || currentState is PlayerBurstState)
		{
			if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.HyperBurst))
			{
				num = ((health.Value > num) ? num : (health.Value - 1));
			}
			AddResidualHeatAmount(-residualHeatAmount);
		}
		int value = health.Value;
		if (base.StateMachine.CurrentState is PhaseTransitionCutsceneState || (base.StateMachine.CurrentState is BossAttackState bossAttackState && bossAttackState.CurrentSequence == warmhorn))
		{
			if (HasStatusEffect<ExecutionStatusEffect>() && !IsExecutionFinished())
			{
				num = ((health.Value > num) ? num : (health.Value - 1));
			}
			health -= num;
			OnChangeHealth(health.Value - value);
			OnDamaged(hitInfo);
			return true;
		}
		if (health.Value >= 0)
		{
			if (TryParry(hitInfo))
			{
				return true;
			}
			if (HasStatusEffect<ExecutionStatusEffect>() && !IsExecutionFinished())
			{
				num = ((health.Value > num) ? num : (health.Value - 1));
			}
			health -= num;
			OnChangeHealth(health.Value - value);
			AddResidualHeatAmount(Mathf.RoundToInt((float)num * ((float)hitResidualRate / 100f)));
			if (health.Value <= 0 && (warmhornUsed || (IsTrueEnding && IsPhase3)))
			{
				Die();
				return true;
			}
		}
		if (!(base.StateMachine.CurrentState is PrimordialFlame2ParryState) || (base.StateMachine.CurrentState is BossAttackState bossAttackState2 && bossAttackState2.CurrentSequence != warmhorn) || (base.StateMachine.CurrentState is PhaseTransitionCutsceneState && Groggy.Value > phaseTransitionGroggyLimit))
		{
			TakeGroggyDamage(hitInfo);
		}
		if (num != 0)
		{
			OnDamaged(hitInfo);
		}
		return true;
	}

	public override bool TryPhaseTransition()
	{
		if (IsTrueEnding)
		{
			if (!IsPhase3 && (float)health.Value / (float)maxHealth <= (float)trueEndingHealthRatio / 100f)
			{
				EnterCutsceneState(4);
				return true;
			}
		}
		else if (!warmhornUsed && (float)health.Value / (float)maxHealth <= (float)normalEndingHealthRatio / 100f)
		{
			ExecuteUseWarmhorn();
			return true;
		}
		return false;
	}

	public override void OnIdle()
	{
		base.IsImmortal = false;
	}

	public override void OnPlayerHit(HitInfo hitinfo)
	{
		prevAttackName = hitinfo.AttackName;
		if (CheckPyronAction())
		{
			SwitchPyronAction().Forget();
		}
		if (IsPhase3 && hitinfo.AttackName.Contains("ChargeAttack") && Enemy.Player.PSM.CurrentState is PlayerHitState)
		{
			SwitchPyronAction().Forget();
		}
	}

	public override void OnAttackEnd(IState nextState)
	{
		ActiveGuardPassive(parryActivateFrame).Forget();
		if (nextState is PrimordialFlame2IdleState || nextState is EnemyBounceState || nextState is GroggyState)
		{
			BossAttackState bossAttackState = base.StateMachine.CurrentState.CastObject<BossAttackState>();
			base.Fatigue = bossAttackState.LastSequenceFatigue;
			if (IsPhase3)
			{
				LightningStrike(usePhase3: false).Forget();
			}
		}
	}

	public override void AddStunCount(int value)
	{
		if (!(base.StateMachine.CurrentState is BossAttackState bossAttackState) || bossAttackState.CurrentSequence != dash || IsGround)
		{
			base.AddStunCount(value);
		}
	}

	public override void OnEnterBounceState()
	{
		jumpSequence.Kill();
		jumpAttackSequence.Kill();
		base.OnEnterBounceState();
		base.IsImmortal = false;
	}

	public override void OnEnterExecution()
	{
		ReturnPaleBlow();
		base.StateMachine.ChangeState(null);
		DisableVFXAll();
		jumpSequence.Kill();
		jumpAttackSequence.Kill();
		ForceDrop();
		SetAnimation(GroggyStartAnim, loop: false, 1f, 0, 0f, vfxEnable: false);
		AddAnimation(GroggyLoopAnim);
	}

	protected override void OnEnterGroggyState()
	{
		base.OnEnterGroggyState();
		jumpSequence.Kill();
		jumpAttackSequence.Kill();
		ForceDrop();
		base.IsImmortal = false;
	}

	protected override void OnDie()
	{
		jumpSequence.Kill();
		jumpAttackSequence.Kill();
		ForceDrop();
		ClearPlayerStagger();
		Game.Instance.Player.StatusEffectController.Clear();
		lightningAttack?.Cancel();
		lightningAttack?.ReturnToPool();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { basicAttack1, basicAttack2, basicAttack3, basicAttack4, jumpAttack, JumpAttack2, ashBreak, swordOfGod, dash, chargeAttack };
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		parryCount = 0;
		skipSwordOfGodPostDelay = false;
		damageList.Clear();
		return GetRandomPattern();
	}

	protected override CutsceneState GetTargetState(int index)
	{
		return index switch
		{
			-1 => defeatState, 
			4 => phaseThreeTransitionState, 
			6 => defeatState, 
			_ => null, 
		};
	}

	protected override void OnChangeHealth(int delta)
	{
		base.OnChangeHealth(delta);
		AddResidualHeatAmount(0);
	}

	private List<BossAttackSequence> GetBasicAttack()
	{
		switch (CryptoRandom.Range(0, 5))
		{
		case 0:
			return new List<BossAttackSequence> { basicAttack1, basicAttack2, basicAttack3, basicAttack4 };
		case 1:
			return new List<BossAttackSequence> { basicAttack1, basicAttack2, basicAttack3, dash };
		case 2:
		{
			List<BossAttackSequence> list2 = new List<BossAttackSequence>();
			list2.Add(basicAttack1);
			list2.Add(basicAttack2);
			list2.AddRange(GetJumpAttack());
			return list2;
		}
		case 3:
		{
			List<BossAttackSequence> list = new List<BossAttackSequence>();
			list.Add(basicAttack1);
			list.Add(basicAttack2);
			list.AddRange(GetAshBreak(chainByOther: false, ashBreak));
			return list;
		}
		case 4:
			return new List<BossAttackSequence> { basicAttack1, chargeAttack };
		default:
			return new List<BossAttackSequence>();
		}
	}

	private List<BossAttackSequence> GetBasicAttackPhase3()
	{
		switch (CryptoRandom.Range(0, 13))
		{
		case 0:
			return new List<BossAttackSequence> { basicAttack1, basicAttack2, basicAttack3, basicAttack4, dash };
		case 1:
			return new List<BossAttackSequence> { basicAttack1, basicAttack2, basicAttack3, basicAttack4, chargeAttack };
		case 2:
			return new List<BossAttackSequence> { basicAttack1, basicAttack2, basicAttack3, basicAttack4 };
		case 3:
			return new List<BossAttackSequence> { basicAttack1, basicAttack2, basicAttack3, dash };
		case 4:
			return new List<BossAttackSequence> { basicAttack1, basicAttack2, basicAttack3, basicAttack4, chargeAttack };
		case 5:
		{
			List<BossAttackSequence> list4 = new List<BossAttackSequence>();
			list4.Add(basicAttack1);
			list4.Add(basicAttack2);
			list4.Add(basicAttack3);
			list4.AddRange(GetAshBreak(chainByOther: false, ashBreak));
			return list4;
		}
		case 6:
		{
			List<BossAttackSequence> list3 = new List<BossAttackSequence>();
			list3.Add(basicAttack1);
			list3.Add(basicAttack2);
			list3.AddRange(GetAshBreak(chainByOther: false, ashBreak));
			return list3;
		}
		case 7:
		{
			List<BossAttackSequence> list2 = new List<BossAttackSequence>();
			list2.Add(basicAttack1);
			list2.Add(basicAttack2);
			list2.AddRange(GetJumpAttack());
			return list2;
		}
		case 8:
			return new List<BossAttackSequence> { basicAttack1, basicAttack2, chargeAttack };
		case 9:
			return new List<BossAttackSequence> { basicAttack1, basicAttack2, dash };
		case 10:
			return new List<BossAttackSequence> { basicAttack1, dash };
		case 11:
		{
			List<BossAttackSequence> list = new List<BossAttackSequence>();
			list.Add(basicAttack1);
			list.AddRange(GetJumpAttack());
			return list;
		}
		case 12:
			return new List<BossAttackSequence> { basicAttack1, chargeAttack };
		default:
			return new List<BossAttackSequence>();
		}
	}

	private List<BossAttackSequence> GetJumpAttack()
	{
		switch (CryptoRandom.Range(0, 3))
		{
		case 0:
			return new List<BossAttackSequence> { jumpAttack, JumpAttack2 };
		case 1:
		{
			List<BossAttackSequence> list = new List<BossAttackSequence>();
			list.Add(jumpAttack);
			list.AddRange(GetSwordOfGod(chainByOther: false));
			return list;
		}
		case 2:
			return new List<BossAttackSequence> { jumpAttack, dash };
		default:
			return new List<BossAttackSequence>();
		}
	}

	private List<BossAttackSequence> GetAshBreak(bool chainByOther, GenericBossAttackSequence<PrimordialFlame2> ashBreakParam)
	{
		switch (CryptoRandom.Range(0, 3))
		{
		case 0:
			return new List<BossAttackSequence> { ashBreakParam, JumpAttack2 };
		case 1:
		{
			List<BossAttackSequence> list = new List<BossAttackSequence>();
			list.Add(ashBreakParam);
			list.AddRange(GetSwordOfGod(chainByOther));
			return list;
		}
		case 2:
			return new List<BossAttackSequence> { ashBreakParam, dash };
		default:
			return new List<BossAttackSequence>();
		}
	}

	private List<BossAttackSequence> GetSwordOfGod(bool chainByOther)
	{
		if (chainByOther)
		{
			switch ((!IsPhase3) ? CryptoRandom.Range(0, 2) : CryptoRandom.Range(0, 3))
			{
			case 0:
				return new List<BossAttackSequence> { swordOfGod, lightning };
			case 1:
				return new List<BossAttackSequence> { swordOfGod };
			case 2:
				skipSwordOfGodPostDelay = true;
				return new List<BossAttackSequence> { swordOfGod, dash };
			}
		}
		else
		{
			switch ((!IsPhase3) ? CryptoRandom.Range(0, 3) : CryptoRandom.Range(0, 4))
			{
			case 0:
			{
				skipSwordOfGodPostDelay = true;
				List<BossAttackSequence> list = new List<BossAttackSequence>();
				list.Add(swordOfGod);
				list.AddRange(GetAshBreak(chainByOther: true, ashBreakCombo));
				return list;
			}
			case 1:
				return new List<BossAttackSequence> { swordOfGod, lightning };
			case 2:
				return new List<BossAttackSequence> { swordOfGod };
			case 3:
				skipSwordOfGodPostDelay = true;
				return new List<BossAttackSequence> { swordOfGod, dash };
			}
		}
		return new List<BossAttackSequence>();
	}

	private void AddResidualHeatAmount(int value)
	{
		if (IsPhase3)
		{
			residualHeatAmount = Mathf.Clamp(residualHeatAmount + value, 0, base.MaxHealth - base.Health);
			BossCharacter.BossBoard.UpdateRegainHealthBar(residualHeatAmount, base.MaxHealth - base.Health, 0);
		}
	}

	private void JumpAttack(int duration, float jumpHeight, float distance)
	{
		ShortcutExtensions.DOMove(endValue: new Vector3(base.Position.x + (float)FacingDirection.GetSignX() * distance, base.Position.y + jumpHeight), target: base.transform, duration: Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private void AshBreakJump(int duration, float jumpHeight, float distance)
	{
		ShortcutExtensions.DOMove(endValue: new Vector3(base.Position.x + (float)(-FacingDirection.GetSignX()) * distance, base.Position.y + jumpHeight), target: base.transform, duration: Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private void AshBreakAttack()
	{
		foreach (AttackZone attackZone in attackZones)
		{
			if (attackZone.name == "ash_break")
			{
				Zone zoneOf = attackZone.zone.GetZoneOf(new Vector2(base.Position.x, base.SpawnPosition.y), 1);
				AttackUtility.AttackBox(zoneOf.Center, zoneOf.Size, new HitInfo(this, "AshBreak"), LayerUtility.PlayerLayer, damageList);
			}
		}
	}

	private bool TryParry(HitInfo hitInfo)
	{
		if (hitInfo.Damage <= 0)
		{
			return false;
		}
		if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.BlockStuck) || hitInfo.AttackAttribute.HasFlag(AttackAttribute.FixedDamage))
		{
			return false;
		}
		IState currentState = base.StateMachine.CurrentState;
		if (!(currentState is PrimordialFlame2IdleState) && !(currentState is PrimordialFlame2ParryState))
		{
			return false;
		}
		if (IsGroggyPlaying)
		{
			return false;
		}
		if (FacingDirection.GetSignX() != (Enemy.Player.Position - base.Position).x.GetSign())
		{
			return false;
		}
		currentParryChance += addParryChanceOnGuard;
		if (currentParryChance >= CryptoRandom.Range(0, 101))
		{
			if (Enemy.Player.PSM.CurrentState is PlayerGroundAttackState playerGroundAttackState)
			{
				int num = parriedGroundAttackIndex;
				if ((num == 1 || num == 2) && playerGroundAttackState.CurrentAttackIndex == parriedGroundAttackIndex)
				{
					return false;
				}
				parriedGroundAttackIndex = playerGroundAttackState.CurrentAttackIndex;
			}
			base.Fatigue = onParryUpdateFatigue;
			base.StateMachine.ChangeState(parryState);
			currentParryChance = 100;
			ChangeHeatToHealth((residualHeatAmount > maxHeatChange) ? maxHeatChange : residualHeatAmount);
			if (parryCount >= activeParryDamageCount)
			{
				Enemy.Player.TryTakeDamage(new HitInfo(this, "Parrying"));
			}
			parryCount++;
			if (Game.Instance.Player.PlayerStat.GetStat<Stamina>().Current <= 0f)
			{
				base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { (!IsPhase3) ? pyronAction : strongPyronAction }));
				if (!IsPhase3)
				{
					EnableVFX("pyron_action_trail");
				}
			}
			return true;
		}
		return false;
	}

	private void SwordOfGodDrop(int duration)
	{
		base.transform.DOMoveY(base.SpawnPosition.y, Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private async UniTaskVoid ActiveGuardPassive(int frame)
	{
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(frame);
		currentParryChance = parryChance;
	}

	private void ChangeHeatToHealth(int value)
	{
		if (IsPhase3)
		{
			AddResidualHeatAmount(-value);
			value *= heatPerHealth;
			health += value;
			OnChangeHealth(value);
		}
	}

	private bool CheckPyronAction()
	{
		if (!(Enemy.Player.PSM.CurrentState is PlayerBrokenGuardState))
		{
			return false;
		}
		if (!(base.StateMachine.CurrentState is BossAttackState { CurrentSequence: var currentSequence }))
		{
			return false;
		}
		if (currentSequence == basicAttack1 || currentSequence == basicAttack2 || currentSequence == basicAttack3 || currentSequence == basicAttack4)
		{
			return true;
		}
		if (currentSequence == swordOfGod)
		{
			return true;
		}
		if (currentSequence == ashBreak)
		{
			return true;
		}
		if (currentSequence == chargeAttack)
		{
			return true;
		}
		if (currentSequence == dash && IsGround)
		{
			return true;
		}
		if (currentSequence == JumpAttack2)
		{
			return true;
		}
		return false;
	}

	private void PyronAction(int duration, bool isStrong, string attackName)
	{
		string animation = ((Enemy.Player.PSM.CurrentState is PlayerBrokenGuardState) ? string.Empty : "hit_loop");
		ChangeHeatToHealth(residualHeatAmount);
		if (isStrong)
		{
			Enemy.Player.PSM.ChangeState(new PlayerStaggerHitState(Enemy.Player.PSM, delegate
			{
				Enemy.Player.RemoveAnimationEvent(AnimState.Complete, OnPlayerGuardBroken);
				Enemy.Player.SetGravityFree(active: true);
				Enemy.Player.AddAnimationEvent(AnimState.Complete, OnPlayerGuardBroken);
			}, null, delegate
			{
				Enemy.Player.TryTakeDamage(new HitInfo(this, attackName + "_Out"));
			})
			{
				Duration = duration,
				Animation = animation
			});
		}
		else
		{
			Enemy.Player.PSM.ChangeState(new PlayerStaggerHitState(Enemy.Player.PSM, null, null, delegate
			{
				Enemy.Player.TryTakeDamage(new HitInfo(this, attackName));
			})
			{
				Duration = duration,
				Animation = animation
			});
			pyronActionTrail.Flip((Enemy.Player.Position.x - pyronActionTrail.transform.position.x).GetSign());
			pyronActionTrail.position = new Vector3(Enemy.Player.Position.x, pyronActionTrail.transform.position.y);
		}
	}

	public async UniTaskVoid LightningStrike(bool usePhase3)
	{
		if ((!usePhase3 || IsPhase3) && !lightningProgress)
		{
			lightningProgress = true;
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(CryptoRandom.Range(0, lightningRandomWaitDuration + 1));
			LazySingleton<MessageSystem>.Instance.Publish(new WorldParticleEvent(WorldParticleEvent.WorldParticle.PrimordialFlameLightningSign));
			Enemy.SoundManager.PlayOneShot(lightningFlashEvent);
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(lightningWarningDuration + lightningWaitDuration);
			if (!IsDead && !IsGroggyPlaying)
			{
				Vector2 vector = new Vector2(Enemy.Player.VisualBottom.x, base.SpawnPosition.y);
				lightningAttack = Enemy.PoolManager.GetObject<LightningAttack>("Primordial Flame_Lightning.prefab", vector);
				lightningAttack.gameObject.SetActive(value: true);
				lightningAttack.Activate(0, 60).Forget();
				Enemy.SoundManager.PlayOneShotAttached(lightningEvent, lightningAttack.transform);
				lightningProgress = false;
			}
		}
	}

	public void UseWarmhorn()
	{
		if (IsTrueEnding)
		{
			BossCharacter.BossBoard.UpdateHealthBar(base.Phase3Health, base.Phase3Health, base.Phase3Health);
			health = new ClampedInt(base.Phase3Health, 0, base.Phase3Health);
			AddResidualHeatAmount(-residualHeatAmount);
		}
		else
		{
			int value = health.Value;
			health += Mathf.RoundToInt((float)normalEndingRecoverRatio * ((float)maxHealth / 100f));
			OnChangeHealth(health.Value - value);
			BossCharacter.BossBoard.UpdateRegainHealthBar(residualHeatAmount, maxHealth - base.Health, 0);
			Groggy.ChangeValueBy(normalEndingAddGroggy);
		}
		BossCharacter.BossBoard.PlayHealTween(loop: false, GetType());
	}

	private void CheckDashNext()
	{
		if (!Mathf.Approximately(base.Position.y, base.SpawnPosition.y))
		{
			List<BossAttackSequence> attackSequences = CryptoRandom.Range(0, 2) switch
			{
				0 => new List<BossAttackSequence> { JumpAttack2 }, 
				1 => GetSwordOfGod(chainByOther: false), 
				_ => throw new ArgumentOutOfRangeException(), 
			};
			base.StateMachine.CurrentState.CastObject<BossAttackState>().AddAttackSequence(attackSequences);
		}
	}

	private void JumpAttack2Rise(int duration, float x, float y, Ease xEase, Ease yEase)
	{
		Vector3 vector = new Vector3(base.Position.x + (float)FacingDirection.GetSignX() * x, base.Position.y + y);
		jumpSequence = DOTween.Sequence();
		jumpSequence.Append(base.transform.DOMoveX(vector.x, Util.FrameToSeconds(duration)).SetEase(xEase)).Join(base.transform.DOMoveY(vector.y, Util.FrameToSeconds(duration)).SetEase(yEase));
	}

	private void JumpAttack2Drop(int duration, float distance, Ease xEase, Ease yEase)
	{
		Vector3 vector = new Vector3(base.Position.x + (float)FacingDirection.GetSignX() * distance, base.SpawnPosition.y);
		jumpAttackSequence = DOTween.Sequence();
		jumpAttackSequence.Append(base.transform.DOMoveX(vector.x, Util.FrameToSeconds(duration)).SetEase(xEase)).Join(base.transform.DOMoveY(vector.y, Util.FrameToSeconds(duration)).SetEase(yEase));
	}

	private void SkipDashPostDelay()
	{
		if (base.Position.y > base.SpawnPosition.y && base.StateMachine.CurrentState is BossAttackState bossAttackState)
		{
			bossAttackState.CurrentSequence.ForceFinish();
		}
	}

	private void SkipSwordOfGodPostDelay()
	{
		if (base.StateMachine.CurrentState is BossAttackState bossAttackState && skipSwordOfGodPostDelay)
		{
			bossAttackState.CurrentSequence.ForceFinish();
		}
	}

	private void MoveToPlayer(float distance)
	{
		int sign = (VisualBottom - Enemy.Player.VisualBottom).normalized.x.GetSign();
		base.transform.position = new Vector3(Enemy.Player.VisualBottom.x + (float)sign * distance, base.SpawnPosition.y);
		base.SkeletonAnimation.Skeleton.SetAlpha(1f);
	}

	private static void OnPlayerGuardBroken(TrackEntry track)
	{
		if (track.Animation.Name == "guard_break")
		{
			Enemy.Player.SetAnimation("hit_loop");
		}
	}

	private async UniTaskVoid SwitchPyronAction()
	{
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(strongPyronActionWaitFrame);
		if (IsPhase3)
		{
			if (base.AttackState.LastSequence == JumpAttack2)
			{
				DisableVFXAll();
			}
		}
		else
		{
			EnableVFX("pyron_action_trail");
		}
		base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { (!IsPhase3) ? pyronAction : strongPyronAction }));
	}

	private void StrongPyronActionAura()
	{
		LazySingleton<MessageSystem>.Instance.Publish(new WorldParticleEvent(WorldParticleEvent.WorldParticle.PrimordialFlamePyronActionAura));
	}

	private void LanceHoldPlayer()
	{
		if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState playerStaggerHitState)
		{
			float x = Mathf.Clamp(lanceTransform.position.x, MyArea.AreaBox.LowerLeft.x, MyArea.AreaBox.UpperRight.x);
			playerStaggerHitState.TargetPosition = new Vector2(x, base.SpawnPosition.y + 2f);
		}
	}

	private List<BossAttackSequence> GetRandomPattern()
	{
		List<BossAttackSequence> list = new List<BossAttackSequence> { basicAttack1, jumpAttack, ashBreak, dash, chargeAttack };
		List<int> list2 = new List<int>();
		list2.AddRange(Enumerable.Repeat(0, basicAttack1.GetPower(this)));
		list2.AddRange(Enumerable.Repeat(1, jumpAttack.GetPower(this)));
		list2.AddRange(Enumerable.Repeat(2, ashBreak.GetPower(this)));
		list2.AddRange(Enumerable.Repeat(3, dash.GetPower(this)));
		list2.AddRange(Enumerable.Repeat(4, chargeAttack.GetPower(this)));
		int num = list2[CryptoRandom.Range(0, list2.Count)];
		for (int i = 0; i < list.Count; i++)
		{
			if (i != num)
			{
				list[i].RisePower();
			}
		}
		return num switch
		{
			0 => GetBasicAttack(), 
			1 => GetJumpAttack(), 
			2 => GetAshBreak(chainByOther: false, ashBreak), 
			3 => new List<BossAttackSequence> { dash }, 
			4 => new List<BossAttackSequence> { chargeAttack }, 
			_ => new List<BossAttackSequence>(), 
		};
	}

	private void ExecuteUseWarmhorn()
	{
		if (!warmhornUsed)
		{
			base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { warmhorn }));
			warmhornUsed = true;
		}
	}

	private void SwordDisappear()
	{
		GenericBossAttackSequence<PrimordialFlame2> genericBossAttackSequence = base.AttackState.CurrentSequence.CastObject<GenericBossAttackSequence<PrimordialFlame2>>();
		GenericBossAttackSequence<PrimordialFlame2> genericBossAttackSequence2 = base.AttackState.NextSequence.CastObject<GenericBossAttackSequence<PrimordialFlame2>>();
		if (genericBossAttackSequence.TimelineName.Contains("basic") && (genericBossAttackSequence2 == null || !genericBossAttackSequence2.TimelineName.Contains("basic")))
		{
			PoolingParticle poolingParticle = Enemy.PoolManager.GetObject<PoolingParticle>("vfx_Primordial Flame_Phase2_Sword_Disappear.prefab");
			PoolingParticle poolingParticle2 = Enemy.PoolManager.GetObject<PoolingParticle>("vfx_Primordial Flame_Phase2_Sword_Disappear.prefab");
			BoneFollower componentInChildren = poolingParticle.GetComponentInChildren<BoneFollower>();
			BoneFollower componentInChildren2 = poolingParticle2.GetComponentInChildren<BoneFollower>();
			componentInChildren.enabled = true;
			componentInChildren2.enabled = true;
			componentInChildren.skeletonRenderer = base.SkeletonRenderer;
			componentInChildren2.skeletonRenderer = base.SkeletonRenderer;
			componentInChildren.boneName = "Weapon_A";
			componentInChildren2.boneName = "Weapon_B";
			poolingParticle.Play();
			poolingParticle2.Play();
			componentInChildren.LateUpdate();
			componentInChildren2.LateUpdate();
			componentInChildren.enabled = false;
			componentInChildren2.enabled = false;
		}
	}

	private void Flame(int duration, int signFrame, int cycleFrame)
	{
		VeritaFlame veritaFlame = Enemy.PoolManager.GetObject<VeritaFlame>("Primordial Flame2_Flame.prefab", flamePosition.position);
		veritaFlame.Initalize(Vector2.zero, duration, signFrame, cycleFrame, base.BossName);
		_ = veritaFlame.BoxCollider.size.x / 2f;
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		foreach (AttackZone attackZone in attackZones)
		{
			if (attackZone.drawGizmo)
			{
				Zone zoneOf = attackZone.zone.GetZoneOf(base.transform);
				Gizmos.color = Color.magenta;
				Gizmos.DrawWireCube(zoneOf.Center, zoneOf.Size);
			}
		}
	}
}
