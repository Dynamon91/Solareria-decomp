using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.FSM;
using SPR.FSM.Base;
using UnityEngine;

namespace SPR;

public class DarkChasa : BossCharacter
{
	public const int OpeningStateIndex = 0;

	public const int RetryInFirstPhaseStateIndex = 1;

	public const int SecondPhaseStateIndex = 2;

	public const int RetryInSecondPhaseStateIndex = 3;

	public const int ThirdPhaseStateIndex = 4;

	public const int RetryInThirdPhaseStateIndex = 5;

	public const int EndingStateIndex = 6;

	public const int FuneralStateIndex = 7;

	public const int AfterEndingStateIndex = 8;

	private const string TeleportAppear = "DarkChasa_Teleport_Appear.prefab";

	private const string TeleportDisappear = "DarkChasa_Teleport_Disappear.prefab";

	[Header("피로도")]
	[SerializeField]
	private float phase3Fatigue;

	[Header("공중 베기")]
	[SerializeField]
	private int aerialTeleportHeight;

	[Header("강제 텔레포트")]
	[SerializeField]
	private Transform playerForceTeleportTransform;

	[Header("그림자 베기")]
	[SerializeField]
	private Transform slashShadowAboveTransform;

	[Header("패링")]
	[SerializeField]
	private float parryingChance;

	[SerializeField]
	private float parryingChanceRise;

	[Header("찌르기")]
	[SerializeField]
	private Transform playerThrowTransform;

	[SerializeField]
	private Transform teleportAboveTransform;

	[Header("모아베기")]
	[SerializeField]
	private Transform shadowXSlashTransform;

	[Header("C 연격")]
	[SerializeField]
	private float comboCTeleportDistance;

	[Header("F 연격")]
	[SerializeField]
	private float comboFJumpHeight;

	[Header("H 연격")]
	[SerializeField]
	private float comboHJumpHeight;

	[Header("발도")]
	[SerializeField]
	private int battoConsumeHealth;

	[SerializeField]
	private int battoRegenHealth;

	[Header("Sequence")]
	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> teleport;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> poseA;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> poseB;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> slashAerial;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> forceTeleport;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> forceTeleportWait;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> slashShadow;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> parrying;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> pincer;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> pierce;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> pierceThrow;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> xSlash;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> teleportToPlayer;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> comboA;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> comboB;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> comboC;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> comboD;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> comboE;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> comboG;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> comboH;

	[SerializeField]
	private GenericBossAttackSequence<DarkChasa> batto;

	[Header("References")]
	[SerializeField]
	private ParticleSystem[] phase3IdleEffects;

	[Header("Sound")]
	[SerializeField]
	private EventReference hitEvent;

	[SerializeField]
	private EventReference teleportEvent;

	private PlayerStaggerHitState playerStaggerHitState;

	private DarkChasaShadow shadow;

	private readonly List<DarkChasaShadow> shadowList = new List<DarkChasaShadow>();

	private float parryChanceSum;

	private bool alreadyTeleportAgain;

	private Vector3 lastPosition;

	private bool isEnteringPhase3;

	public override string Name => "DarkChasa";

	private CutsceneState PhaseTwoTransitionState { get; set; }

	private CutsceneState PhaseThreeTransitionState { get; set; }

	private CutsceneState DefeatState { get; set; }

	private List<BossAttackSequence> SlashAerial
	{
		get
		{
			if (base.Phase != 1)
			{
				return new List<BossAttackSequence>
				{
					slashAerial,
					CryptoRandom.RandomBool() ? poseA : poseB
				};
			}
			return new List<BossAttackSequence> { slashAerial, teleport };
		}
	}

	public override string IdleAnimation
	{
		get
		{
			if (base.Phase >= 3)
			{
				return "phase3_idle";
			}
			return "idle";
		}
	}

	public override string GroggyStartAnim
	{
		get
		{
			if (base.Phase >= 3)
			{
				return "phase3_groggy_start";
			}
			return "groggy_enter";
		}
	}

	public override string GroggyLoopAnim
	{
		get
		{
			if (base.Phase >= 3)
			{
				return "phase3_groggy_loop";
			}
			return "groggy_loop";
		}
	}

	public override string GroggyEndAnim
	{
		get
		{
			if (base.Phase >= 3)
			{
				return "phase3_groggy_end";
			}
			return "groggy_end";
		}
	}

	protected override float Width => base.Width * 2f;

	private void LateUpdate()
	{
		AdjustXPosition();
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		base.BounceAnim = "parry_ph_1";
		PhaseTwoTransitionState = new PhaseTransitionCutsceneState(this, 2, blockControl: false, hideCanvas: false);
		PhaseThreeTransitionState = new PhaseTransitionCutsceneState(this, 4, blockControl: true);
		DefeatState = new CutsceneState(this, 6);
		parryChanceSum = parryingChance;
		base.StateMachine.ChangeState(base.IdleState);
		AddAction(ActionTiming.Begin, "RandomTeleport", RandomTeleport);
		AddAction(ActionTiming.End, "RandomTeleport", (Action)delegate
		{
			PlayTeleportEffect("DarkChasa_Teleport_Appear.prefab");
		});
		AddAction(ActionTiming.Begin, "SlashAerialRandomTeleport", (Action)delegate
		{
			if (base.Phase > 1)
			{
				RandomTeleport();
			}
		});
		AddAction(ActionTiming.End, "SlashAerialRandomTeleport", (Action)delegate
		{
			if (base.Phase > 1)
			{
				PlayTeleportEffect("DarkChasa_Teleport_Appear.prefab");
			}
		});
		AddAction(ActionTiming.Begin, "TeleportToPlayer", (Action<int, Parameter[]>)delegate
		{
			SetVisible(visible: false);
			PlayTeleportEffect("DarkChasa_Teleport_Disappear.prefab");
		});
		AddAction(ActionTiming.End, "TeleportToPlayer", delegate(Parameter[] p)
		{
			TeleportAroundPlayer(p[0].Float);
			PlayTeleportEffect("DarkChasa_Teleport_Appear.prefab");
		});
		AddAction(ActionTiming.Begin, "TeleportToAerial", (Action)delegate
		{
			PlayTeleportEffect("DarkChasa_Teleport_Disappear.prefab");
			base.transform.position = new Vector3(Enemy.Player.VisualBottom.x, base.SpawnPosition.y + (float)aerialTeleportHeight);
		});
		AddAction(ActionTiming.End, "TeleportToAerial", (Action)delegate
		{
			PlayTeleportEffect("DarkChasa_Teleport_Disappear.prefab");
		});
		TimelineBeginActions.Add("AerialSlashMove", delegate(int duration)
		{
			base.transform.DOMoveY(base.SpawnPosition.y, Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
		});
		AddAction(ActionTiming.Begin, "ForceTeleportFade", delegate(int duration, Parameter[] p)
		{
			ForceTeleportFade(duration).Forget();
		});
		AddAction(ActionTiming.Begin, "ForceTeleport", delegate(int duration, Parameter[] p)
		{
			ForceTeleport(duration, p[0].Float);
		});
		AddAction(ActionTiming.End, "ForceTeleport", (Action<int, Parameter[]>)delegate
		{
			ForceTeleportRandomPattern();
		});
		TimelineBeginActions.Add("ShadowSlashAbove", delegate
		{
			ShadowSlashAbove();
		});
		TimelineBeginActions.Add("ShadowSlashCenter", delegate
		{
			ShadowSlashCenter();
		});
		TimelineBeginActions.Add("ShadowPincer", delegate
		{
			ShadowPincer();
		});
		TimelineEndActions.Add("ThrowPlayer", delegate
		{
			ShadowPierce();
		});
		TimelineBeginActions.Add("LockPlayerControl", delegate(int duration)
		{
			playerStaggerHitState = (PlayerStaggerHitState)Enemy.Player.PSM.StaggerHitState;
			playerStaggerHitState.Duration = duration;
			playerStaggerHitState.NextState = Enemy.Player.PSM.DownState;
			Enemy.Player.PSM.ChangeState(playerStaggerHitState);
			playerStaggerHitState.TargetPosition = VisualCenter;
		});
		TimelineBeginActions.Add("HoldPlayer", delegate
		{
			Enemy.Player.SetGravityFree(active: true);
		});
		TimelineEndActions.Add("HoldPlayer", delegate
		{
			Enemy.Player.SetGravityFree(active: false);
		});
		TimelineActions.Add("HoldPlayer", delegate
		{
			Vector3 boneWorldPosition = GetBoneWorldPosition("player");
			boneWorldPosition.x = Mathf.Clamp(boneWorldPosition.x, MyArea.AreaBox.LowerLeft.x, MyArea.AreaBox.UpperRight.x);
			boneWorldPosition.y = Mathf.Clamp(boneWorldPosition.y, MyArea.AreaBox.LowerLeft.y, MyArea.AreaBox.UpperRight.y);
			playerStaggerHitState.TargetPosition = boneWorldPosition;
		});
		AddAction(ActionTiming.Begin, "TeleportAbove", (Action)delegate
		{
			SetVisible(visible: false);
		});
		AddAction(ActionTiming.End, "TeleportAbove", (Action)delegate
		{
			Vector3 position = base.transform.position;
			position.x = Enemy.Player.VisualBottom.x;
			base.transform.position = position;
			SetVisible(visible: true);
		});
		TimelineBeginActions.Add("PierceDown", PierceDown);
		AddAction(ActionTiming.Begin, "TeleportCenter", delegate(Parameter[] p)
		{
			base.transform.position = new Vector3(MyArea.AreaBox.Center.x, base.SpawnPosition.y + p[0].Float);
		});
		TimelineBeginActions.Add("ShadowBothSide", delegate
		{
			ShadowBothSide();
		});
		TimelineBeginActions.Add("TeleportWithShadow", delegate
		{
			TeleportWithShadow();
		});
		TimelineBeginActions.Add("ComboFJump", delegate(int duration)
		{
			ComboJump(duration, comboFJumpHeight);
		});
		TimelineBeginActions.Add("ComboLanding", ComboLanding);
		TimelineBeginActions.Add("ComboHJump", delegate(int duration)
		{
			ComboJump(duration, comboHJumpHeight);
		});
		AddAction(ActionTiming.Begin, "BattoTeleport", BattoTeleport);
		AddAction(ActionTiming.End, "BattoTeleport", (Action)delegate
		{
			PlayTeleportEffect("DarkChasa_Teleport_Appear.prefab");
		});
		AddAction(ActionTiming.Begin, "DischargeBladeBeam", delegate(Parameter[] p)
		{
			DischargeBladeBeam(p[0].Float);
		});
		AddAction(ActionTiming.Begin, "PlayerStuck", PlayerStuck);
		AddAction(ActionTiming.End, "PlayerStuck", ParryEffect);
		AddAction(ActionTiming.Begin, "TeleportEffect", (Action)delegate
		{
			PlayTeleportEffect("DarkChasa_Teleport_Disappear.prefab");
		});
		AddAction(ActionTiming.End, "TeleportEffect", (Action)delegate
		{
			PlayTeleportEffect("DarkChasa_Teleport_Appear.prefab");
		});
		AddAction(ActionTiming.Begin, "ShadowA", ShadowA);
		AddAction(ActionTiming.Begin, "ShadowB", ShadowB);
		Enemy.PoolManager.CreatePoolAsync("DarkChasa_BladeBeam.prefab").Forget();
	}

	public override bool TryPhaseTransition()
	{
		if (!IsGroggyPlaying && base.Phase == 1 && base.Health < base.Phase2Health)
		{
			EnterCutsceneState(2);
			return true;
		}
		return false;
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (hitInfo.AttackName.Equals("DarkChasa_Pierce") && Enemy.Player.CanGrab && Enemy.Player.PSM.CurrentState == Enemy.Player.PSM.HitState && !Enemy.Player.CanParry(hitInfo))
		{
			BossAttackState bossAttackState = base.StateMachine.CurrentState.CastObject<BossAttackState>();
			bossAttackState.AddAttackSequence(new List<BossAttackSequence> { pierceThrow, teleport });
			bossAttackState.ShiftNextSequence();
			base.Fatigue = pierce.Fatigue;
		}
	}

	public override void OnAttackEnd(IState nextState)
	{
		BossAttackState bossAttackState = base.StateMachine.CurrentState.CastObject<BossAttackState>();
		if (bossAttackState.GetFirstSequence() == forceTeleport)
		{
			base.Fatigue = forceTeleport.Fatigue;
		}
		if (nextState is BossIdleState || nextState is EnemyBounceState || nextState is GroggyState)
		{
			base.Fatigue = bossAttackState.LastSequenceFatigue;
		}
	}

	protected override void OnIncreasePhase(int before, int after)
	{
		base.OnIncreasePhase(before, after);
		if (after >= 3)
		{
			base.BounceAnim = "parry";
			phase3IdleEffects.ForEach(delegate(ParticleSystem p)
			{
				p.Play();
			});
			base.Fatigue = phase3Fatigue;
		}
	}

	protected override void Die()
	{
		if (!isEnteringPhase3)
		{
			if (base.Phase < 3)
			{
				ClearPlayerStagger();
				EnterPhase3().Forget();
			}
			else
			{
				EnterDie().Forget();
			}
			ClearStatusEffect(force: true);
		}
	}

	protected override void OnDie()
	{
		ClearPlayerStagger();
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (TryParrying(hitInfo))
		{
			hitInfo = new HitInfo(this, 0f, AttackDirection.Facing, AttackAttribute.Default);
		}
		return base.TryTakeDamage(hitInfo);
	}

	public override void SetVisible(bool visible)
	{
		base.SetVisible(visible);
		base.IsImmortal = !visible;
	}

	public void Heal()
	{
		health += battoRegenHealth;
		OnChangeHealth(battoRegenHealth);
		EnableVFX("batto_heal");
		BossCharacter.BossBoard.PlayHealTween(loop: false, GetType());
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { pierce };
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		return GetRandomAttackPattern();
	}

	public override void OnEnterBounceState()
	{
		DisableVFXAll();
		DOTween.Kill(base.transform);
		shadowList.ForEach(delegate(DarkChasaShadow x)
		{
			x.ReturnToPool();
		});
		base.transform.position = new Vector3(base.transform.position.x, base.SpawnPosition.y);
	}

	public override void OnEnterExecution()
	{
		SetVisible(visible: true);
		DOTween.Kill(base.transform);
		shadowList.ForEach(delegate(DarkChasaShadow x)
		{
			x.ReturnToPool();
		});
		base.transform.position = new Vector3(base.transform.position.x, base.SpawnPosition.y);
		base.OnEnterExecution();
	}

	protected override void OnEnterGroggyState()
	{
		SetVisible(visible: true);
		DisableVFXAll();
		DOTween.Kill(base.transform);
		shadowList.ForEach(delegate(DarkChasaShadow x)
		{
			x.ReturnToPool();
		});
		base.transform.position = new Vector3(base.transform.position.x, base.SpawnPosition.y);
		EnableVFX("groggy_start");
		base.OnEnterGroggyState();
	}

	protected override CutsceneState GetTargetState(int index)
	{
		if (!shadow.IsNull() && shadow.gameObject.activeSelf)
		{
			shadow?.DisableVFXAll();
			shadow?.ReturnToPool();
		}
		return index switch
		{
			-1 => DefeatState, 
			2 => PhaseTwoTransitionState, 
			4 => PhaseThreeTransitionState, 
			6 => DefeatState, 
			_ => null, 
		};
	}

	private List<BossAttackSequence> GetRandomAttackPattern()
	{
		if (base.Phase < 3)
		{
			List<BossAttackSequence> list = new List<BossAttackSequence> { poseA, poseB, slashAerial, slashShadow, pincer, pierce };
			if (base.Phase == 2)
			{
				list.Add(forceTeleport);
			}
			List<int> list2 = new List<int>();
			for (int i = 0; i < list.Count; i++)
			{
				list2.AddRange(Enumerable.Repeat(i, list[i].GetPower(this)));
			}
			int num = list2[CryptoRandom.Range(0, list2.Count)];
			for (int j = 0; j < list.Count; j++)
			{
				if (j != num)
				{
					list[j].CastObject<BossAttackSequence>().RisePower();
				}
			}
			return num switch
			{
				0 => new List<BossAttackSequence> { poseA, teleport }, 
				1 => new List<BossAttackSequence> { poseB, teleport }, 
				2 => SlashAerial, 
				3 => new List<BossAttackSequence>
				{
					slashShadow,
					CryptoRandom.RandomBool() ? poseA : poseB
				}, 
				4 => new List<BossAttackSequence>
				{
					pincer,
					CryptoRandom.RandomBool() ? poseA : poseB
				}, 
				5 => new List<BossAttackSequence> { pierce }, 
				6 => new List<BossAttackSequence> { forceTeleport }, 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}
		List<BossAttackSequence> list3 = new List<BossAttackSequence> { xSlash, comboA, comboB, comboC, comboD, comboE, comboG, comboH, batto };
		List<int> list4 = new List<int>();
		for (int k = 0; k < list3.Count; k++)
		{
			list4.AddRange(Enumerable.Repeat(k, list3[k].GetPower(this)));
		}
		int num2 = list4[CryptoRandom.Range(0, list4.Count)];
		for (int l = 0; l < list3.Count; l++)
		{
			if (l != num2)
			{
				list3[l].CastObject<BossAttackSequence>().RisePower();
			}
		}
		return num2 switch
		{
			0 => XSlashChainPattern(), 
			1 => new List<BossAttackSequence> { comboA, teleport }, 
			2 => GetComboBSequence(), 
			3 => new List<BossAttackSequence> { comboC }, 
			4 => new List<BossAttackSequence> { comboD }, 
			5 => new List<BossAttackSequence> { comboE }, 
			6 => new List<BossAttackSequence> { comboG, xSlash }, 
			7 => new List<BossAttackSequence> { comboH }, 
			8 => new List<BossAttackSequence> { batto, teleport }, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private void RandomTeleport()
	{
		PlayTeleportEffect("DarkChasa_Teleport_Disappear.prefab");
		SetAnimation(IdleAnimation);
		int num = CryptoRandom.Range(MyArea.AreaBox.LowerLeft.x + Mathf.RoundToInt(Width), MyArea.AreaBox.UpperRight.x - Mathf.RoundToInt(Width));
		base.transform.position = new Vector3(num, base.SpawnPosition.y);
	}

	private void TeleportAroundPlayer(float distance)
	{
		int num = -FacingDirection.GetSignX();
		num = ((num != 0) ? num : (MyArea.AreaBox.Center - VisualCenter).normalized.x.GetSign());
		base.transform.position = new Vector3(Enemy.Player.VisualBottom.x + (float)num * distance, base.SpawnPosition.y);
		SetVisible(visible: true);
	}

	private async UniTaskVoid ForceTeleportFade(int duration)
	{
		float seconds = Util.FrameToSeconds(duration);
		LazySingleton<MessageSystem>.Instance.Publish(new WorldParticleEvent(WorldParticleEvent.WorldParticle.DarkChasaForceTeleportStart));
		await Game.Instance.UIManager.FadeCanvas.FadeOut(seconds / 2f);
		LazySingleton<MessageSystem>.Instance.Publish(new WorldParticleEvent(WorldParticleEvent.WorldParticle.DarkChasaForceTeleportEnd));
		await Game.Instance.UIManager.FadeCanvas.FadeIn(seconds / 2f);
	}

	private void ForceTeleport(int duration, float boundsWidth)
	{
		int num = CryptoRandom.Range(MyArea.AreaBox.LowerLeft.x + Mathf.RoundToInt(Width), MyArea.AreaBox.UpperRight.x - Mathf.RoundToInt(Width));
		base.transform.position = new Vector3(num, base.SpawnPosition.y);
		float x = Mathf.Clamp(playerForceTeleportTransform.position.x, (float)MyArea.AreaBox.LowerLeft.x + boundsWidth, (float)MyArea.AreaBox.UpperRight.x - boundsWidth);
		Enemy.Player.transform.position = new Vector3(x, base.SpawnPosition.y);
		UniTask task = UniTaskExtension.WaitForFixedUpdatesOrCancel(duration);
		Game.Instance.InputManager.BlockControlForTask(task, "ForceTeleport").Forget();
	}

	private void ForceTeleportRandomPattern()
	{
		LookPlayer();
		List<BossAttackSequence> attackSequences = CryptoRandom.Range(0, 4) switch
		{
			0 => new List<BossAttackSequence> { poseA, teleport }, 
			1 => new List<BossAttackSequence> { poseB, teleport }, 
			2 => SlashAerial, 
			_ => new List<BossAttackSequence>(), 
		};
		base.StateMachine.CurrentState.CastObject<BossAttackState>().AddAttackSequence(attackSequences);
	}

	private void ShadowSlashAbove()
	{
		shadow = GenerateShadow(slashShadowAboveTransform.position);
		shadow.SetOwner(this);
		shadow.SlashAbove();
	}

	private void ShadowSlashCenter()
	{
		shadow = GenerateShadow(VisualBottom);
		shadow.SetOwner(this);
		shadow.SlashCenter();
	}

	private bool TryParrying(HitInfo hitInfo)
	{
		if (base.Phase >= 3)
		{
			return false;
		}
		if (base.StateMachine.CurrentState != base.IdleState)
		{
			return false;
		}
		IState currentState = Enemy.Player.PSM.CurrentState;
		if (currentState is PlayerBurstState || currentState is PlayerParryActionState)
		{
			return false;
		}
		if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.BlockStuck))
		{
			return false;
		}
		if (hitInfo.Source is EnemyBurnStatusEffect)
		{
			return false;
		}
		float num = (float)CryptoRandom.Range(1, 101) + (float)CryptoRandom.Range(0, 10) / 10f;
		if (parryChanceSum >= num)
		{
			Enemy.SoundManager.PlayOneShotAttached(hitEvent, base.transform);
			List<AttackSequence> sequences = new List<AttackSequence> { parrying };
			sequences.AddRange(SlashAerial);
			base.StateMachine.ChangeState(new BossAttackState(this, () => sequences));
			parryChanceSum = parryingChance;
			return true;
		}
		parryChanceSum += parryingChanceRise;
		return false;
	}

	private void ShadowPincer()
	{
		shadow = GenerateShadow(VisualBottom);
		shadow.SetOwner(this);
		shadow.Pincer();
	}

	private void ThrowPlayer(int duration)
	{
		Enemy.Player.transform.DOMove(playerThrowTransform.position, Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private void ShadowPierce()
	{
		shadow = GenerateShadow(VisualBottom);
		shadow.SetOwner(this);
		shadow.Pierce();
	}

	private void PierceDown(int duration)
	{
		Enemy.Player.ResetLastHitDirection();
	}

	private void ShadowBothSide()
	{
		DarkChasaShadow darkChasaShadow = GenerateShadow(new Vector3(VisualBottom.x, base.SpawnPosition.y));
		DarkChasaShadow darkChasaShadow2 = GenerateShadow(new Vector3(VisualBottom.x, base.SpawnPosition.y));
		darkChasaShadow.SetOwner(this);
		darkChasaShadow2.SetOwner(this);
		darkChasaShadow.Flip(Direction.Left);
		darkChasaShadow2.Flip(Direction.Right);
		darkChasaShadow.ComboCSideSlash();
		darkChasaShadow2.ComboCSideSlash();
	}

	private void TeleportWithShadow()
	{
		int signX = Enemy.Player.FacingDirection.GetSignX();
		Vector3 position = new Vector3(Enemy.Player.VisualBottom.x + (float)signX * comboCTeleportDistance, base.SpawnPosition.y);
		Vector3 position2 = new Vector3(Enemy.Player.VisualBottom.x + (float)(-signX) * comboCTeleportDistance, base.SpawnPosition.y);
		base.transform.position = position;
		shadow = GenerateShadow(position2);
		shadow.SetOwner(this);
	}

	private void ComboJump(int duration, float height)
	{
		base.transform.DOMoveY(VisualBottom.y + height, Util.FrameToSeconds(duration)).SetEase(Ease.OutSine);
	}

	private void ComboLanding(int duration)
	{
		base.transform.DOMoveY(base.SpawnPosition.y, Util.FrameToSeconds(duration)).SetEase(Ease.InSine);
	}

	private void BattoTeleport()
	{
		PlayTeleportEffect("DarkChasa_Teleport_Disappear.prefab");
		Vector2 visualCenter = Enemy.Player.VisualCenter;
		float num = HitBox.size.y / 2f;
		Vector3 position = ((visualCenter.x < MyArea.AreaBox.Center.x) ? new Vector3((float)MyArea.AreaBox.UpperRight.x - Width, MyArea.AreaBox.Center.y - num) : new Vector3((float)MyArea.AreaBox.LowerLeft.x + Width, MyArea.AreaBox.Center.y - num));
		base.transform.position = position;
	}

	private void DischargeBladeBeam(float speed)
	{
		int signX = FacingDirection.GetSignX();
		int x = MyArea.AreaBox.Size.x;
		Vector3 vector = ((FacingDirection.GetSignX() < 0) ? new Vector3(MyArea.AreaBox.LowerLeft.x - x, VisualCenter.y) : new Vector3(MyArea.AreaBox.UpperRight.x + x, VisualCenter.y));
		if (health.Value - battoConsumeHealth <= 1)
		{
			int num = Mathf.Max(0, health.Value - 1);
			TryTakeDamage(new HitInfo(this, num, AttackDirection.Both, AttackAttribute.Default));
		}
		else
		{
			TryTakeDamage(new HitInfo(this, battoConsumeHealth, AttackDirection.Both, AttackAttribute.Default));
		}
		DarkChasaBladeBeam beam = Enemy.PoolManager.GetObject<DarkChasaBladeBeam>("DarkChasa_BladeBeam.prefab", VisualCenter);
		beam.SetOwner(this);
		float num2 = (float)x * 2f / (speed / 60f);
		float width = (float)x * 2f / num2;
		beam.Overlap(width);
		beam.transform.localScale = new Vector3(signX, 1f, 1f);
		beam.transform.DOMoveX(vector.x, speed).SetSpeedBased(isSpeedBased: true).SetEase(Ease.Linear)
			.SetUpdate(UpdateType.Fixed)
			.OnUpdate(delegate
			{
				beam.Overlap(width);
			})
			.OnComplete(beam.ReturnToPool);
	}

	private async UniTaskVoid PushPlayer(int duration, float force)
	{
		int frameCount = 0;
		Vector2 velocity = new Vector2((float)(Enemy.Player.VisualCenter - VisualCenter).normalized.x.GetSign() * force, 1f);
		for (; frameCount < duration; frameCount++)
		{
			Enemy.Player.GetComponent<Rigidbody2D>().AddForce(velocity, ForceMode2D.Force);
			Enemy.Player.ApplyRelativeVelocity(velocity, faceJump: false);
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
	}

	private DarkChasaShadow GenerateShadow(Vector3 position)
	{
		shadowList.RemoveAll((DarkChasaShadow x) => !x.gameObject.activeSelf);
		position.x = Mathf.Clamp(position.x, (float)MyArea.AreaBox.LowerLeft.x + Width, (float)MyArea.AreaBox.UpperRight.x - Width);
		DarkChasaShadow darkChasaShadow = Enemy.PoolManager.GetObject<DarkChasaShadow>("Dark Chasa_Shadow.prefab", position);
		shadowList.Add(darkChasaShadow);
		return darkChasaShadow;
	}

	private List<BossAttackSequence> XSlashChainPattern()
	{
		List<BossAttackSequence> list = new List<BossAttackSequence> { xSlash, comboA, comboC, comboD, comboE };
		List<BossAttackSequence> list2 = new List<BossAttackSequence> { xSlash };
		switch (CryptoRandom.Range(0, list.Count))
		{
		case 1:
			list2.AddRange(new List<BossAttackSequence> { comboA, teleport });
			break;
		case 2:
			list2.AddRange(new List<BossAttackSequence> { comboC });
			break;
		case 3:
			list2.Add(comboD);
			break;
		case 4:
			list2.Add(comboE);
			break;
		}
		return list2;
	}

	private List<BossAttackSequence> GetComboBSequence()
	{
		List<BossAttackSequence> list = new List<BossAttackSequence>();
		list.Add(comboB);
		list.AddRange(XSlashChainPattern());
		return list;
	}

	private async UniTaskVoid EnterPhase3()
	{
		isEnteringPhase3 = true;
		await UniTask.WaitUntil(() => !(base.StateMachine.CurrentState is GroggyState) && !(base.StateMachine.CurrentState is GroggyEndState));
		EnterCutsceneState(4);
		health = new ClampedInt(base.Phase3Health, 0, base.Phase3Health);
		isEnteringPhase3 = false;
	}

	private static void PlayerStuck()
	{
		Enemy.Player.PSM.ChangeState(Enemy.Player.PSM.AttackStuckState);
	}

	private void ParryEffect()
	{
		float num = Vector3.Distance(VisualBottom, Enemy.Player.VisualBottom);
		Vector2 vector = new Vector2(VisualCenter.x + (float)FacingDirection.GetSignX() * num, VisualCenter.y);
		PoolingParticle poolingParticle = Enemy.PoolManager.GetObject<PoolingParticle>("vfx_DarkChasa_Parrying.prefab", vector);
		poolingParticle.transform.GetChild(0).Flip(FacingDirection.GetSignX());
		poolingParticle.Play();
	}

	private void PlayTeleportEffect(string address)
	{
		Vector3 boneWorldPosition = GetBoneWorldPosition("Body_0");
		Enemy.PoolManager.GetObject<PoolingParticle>(address, boneWorldPosition).Play();
		if (address.Equals("DarkChasa_Teleport_Disappear.prefab"))
		{
			Enemy.SoundManager.PlayOneShotAttached(teleportEvent, base.transform);
		}
	}

	private void ShadowA()
	{
		if (base.Phase >= 2)
		{
			shadow = GenerateShadow(VisualBottom);
			shadow.SetOwner(this);
			shadow.PoseA();
		}
	}

	private void ShadowB()
	{
		if (base.Phase >= 2)
		{
			shadow = GenerateShadow(VisualBottom);
			shadow.SetOwner(this);
			shadow.PoseB();
		}
	}
}
