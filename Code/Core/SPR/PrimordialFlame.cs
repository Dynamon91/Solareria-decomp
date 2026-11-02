using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.FSM.Base;
using SPR.Field;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class PrimordialFlame : BossCharacter
{
	public enum Pattern
	{
		None,
		Annihilation,
		BasicAttack1,
		BasicAttack2,
		BasicAttack3,
		BasicAttack4,
		BasicAttackOnly,
		BasicAttackCombo,
		Dash,
		ChargeAttack,
		SwordOfGod
	}

	[Header("디버깅")]
	[SerializeField]
	private Pattern patternDebug;

	[Header("신의 검")]
	[SerializeField]
	private float swordSpawnDistance;

	[SerializeField]
	private float swordSpawnHeight;

	[Header("종말")]
	[SerializeField]
	private int theEndLightningDuration = 30;

	[SerializeField]
	private int theEndWarningDuration = 10;

	[SerializeField]
	private int theEndWaitFrame = 30;

	[SerializeField]
	private float lightningActivateFatigue = 1f;

	[Header("섬멸")]
	[SerializeField]
	private float pullPower;

	[SerializeField]
	private int maxTemporaryHealth;

	[Header("기본 공격 연계 확률")]
	[SerializeField]
	private int swordOfGodChance = 20;

	[SerializeField]
	private int annihilationChance = 20;

	[SerializeField]
	private int dashChance = 20;

	[SerializeField]
	private int chargeAttackChance = 20;

	[Header("Sequence")]
	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame> annihilation;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame> annihilationGrab;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame> basicAttack1;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame> basicAttack2;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame> basicAttack3;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame> basicAttack4;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame> swordOfGod0;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame> swordOfGod1;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame> holyExplosion;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame> dash;

	[SerializeField]
	private GenericBossAttackSequence<PrimordialFlame> chargeAttack;

	[Header("SFX")]
	[SerializeField]
	private EventReference theEndFlashEvent;

	[SerializeField]
	private EventReference theEndLightningEvent;

	[SerializeField]
	private EventReference distortionEvent;

	[Header("Reference")]
	[SerializeField]
	private Transform lanceTransform;

	[SerializeField]
	private Transform annihilationTransform;

	private PrimordialFlameArea area;

	private PrimordialFlameSword swordOfGod;

	private List<BossAttackSequence> lastAttackSequences;

	public const int OpeningStateIndex = 0;

	public const int RetryInFirstPhaseStateIndex = 1;

	public const int SecondPhaseStateIndex = 2;

	public const int RetryInSecondPhaseStateIndex = 3;

	public const int ThirdPhaseStateIndex = 4;

	public const int RetryInThirdPhaseStateIndex = 5;

	public const int DyingStateIndex = 6;

	private bool swordOfGodAgain;

	private CancellationTokenSource cts;

	private LightningAttack lightning;

	private Vector2 SwordSpawnPosition => new Vector2(VisualBottom.x + (float)FacingDirection.GetSignX() * swordSpawnDistance, base.SpawnPosition.y + swordSpawnHeight);

	private Vector2 AnnihilationPosition
	{
		get
		{
			Vector3 position = annihilationTransform.position;
			AreaBox areaBox = area.AreaBox;
			return new Vector2(Mathf.Clamp(position.x, areaBox.LowerLeft.x, areaBox.UpperRight.x), position.y);
		}
	}

	public override string Name => "PrimordialFlame";

	private PhaseTransitionCutsceneState PhaseTwoTransitionState { get; set; }

	private CutsceneState DefeatState { get; set; }

	private void LateUpdate()
	{
		AdjustXPosition();
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		PhaseTwoTransitionState = new PhaseTransitionCutsceneState(this, 2, blockControl: true);
		base.StateMachine.ChangeState(base.IdleState);
		area = MyArea.CastObject<PrimordialFlameArea>();
		AddAction(ActionTiming.Begin, "SpawnSword", (Action)delegate
		{
			SpawnSword(ref swordOfGod, SwordSpawnPosition, goldenSword: false);
		});
		AddAction(ActionTiming.Begin, "DropSword", DropSword);
		AddAction(ActionTiming.Begin, "PullOutSword", PullOutSword);
		AddAction(ActionTiming.Begin, "ResetDisplay", delegate(int duration)
		{
			area.TurnWhite(0f, Util.FrameToSeconds(duration));
		});
		AddAction(ActionTiming.Begin, "Invisible", delegate(int duration)
		{
			base.IsImmortal = true;
			base.SkeletonAnimation.skeleton.DOAlpha(0f, Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
		});
		AddAction(ActionTiming.End, "Invisible", (Action)delegate
		{
			base.transform.position = new Vector3(area.AreaBox.Center.x, area.AreaBox.UpperRight.y);
		});
		AddAction(ActionTiming.Begin, "Visible", delegate(int duration)
		{
			base.SkeletonAnimation.skeleton.DOAlpha(1f, Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
		});
		AddAction(ActionTiming.OnFrame, "Pulling", Pulling);
		AddAction(ActionTiming.Begin, "FreePlayer", (Action)delegate
		{
			Enemy.Player.SetGravityFree(active: false);
		});
		AddAction(ActionTiming.Begin, "Dash", (Action)delegate
		{
			Dash(immortal: true);
		});
		AddAction(ActionTiming.End, "Dash", (Action)delegate
		{
			Dash(immortal: false);
		});
		AddAction(ActionTiming.Begin, "SwordOfGodAgain", delegate(Parameter[] p)
		{
			SwordOfGodAgain(p[0].Int);
		});
		AddAction(ActionTiming.Begin, "SwordDisappear", SwordDisappear);
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (!base.IsImmortal)
		{
			return base.TryTakeDamage(hitInfo);
		}
		return base.IsImmortal;
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (hitInfo.AttackName.Contains("TryGrab") && !(Enemy.Player.PSM.CurrentState is PlayerStaggerHitState) && Enemy.Player.CanHit && Enemy.Player.CanGrab)
		{
			PlayerStaggerHitState playerStaggerHitState = new PlayerStaggerHitState(Enemy.Player.PSM, delegate
			{
				Enemy.Player.SetGravityFree(active: true);
			}, delegate
			{
				Enemy.Player.SetGravityFree(active: false);
			})
			{
				Duration = 120
			};
			Enemy.Player.PSM.ChangeState(playerStaggerHitState);
			playerStaggerHitState.TargetPosition = AnnihilationPosition;
			BossAttackState bossAttackState = base.StateMachine.CurrentState.CastObject<BossAttackState>();
			bossAttackState.AddAttackSequence(new List<BossAttackSequence> { annihilationGrab });
			bossAttackState.ShiftNextSequence();
		}
	}

	public override void OnEnterBounceState()
	{
		base.IsImmortal = false;
		base.SkeletonRenderer.skeleton.SetAlpha(1f);
	}

	public override void OnEnterExecution()
	{
		base.OnEnterExecution();
		base.IsImmortal = false;
		base.SkeletonRenderer.skeleton.SetAlpha(1f);
	}

	public void ReturnAllSwords()
	{
		swordOfGod?.ReturnToPool();
	}

	public override void OnAttackEnd(IState nextState)
	{
		float num = ((base.AttackState.CurrentSequence == swordOfGod1) ? swordOfGod0.Fatigue : ((base.AttackState.CurrentSequence != annihilationGrab) ? base.AttackState.CurrentSequence.CastObject<BossAttackSequence>().Fatigue : annihilation.Fatigue));
		if (nextState == base.IdleState && num >= lightningActivateFatigue)
		{
			LightningStrike().Forget();
		}
	}

	protected override void OnEnterGroggyState()
	{
		base.OnEnterGroggyState();
		base.IsImmortal = false;
		base.SkeletonRenderer.skeleton.SetAlpha(1f);
	}

	protected override void Die()
	{
		ClearPlayerStagger();
		ReturnAllSwords();
		StopDeathFireAsync().Forget();
		Enemy.PoolManager.DisposePool("vfx_Primordial Flame_Phase2_Sword_Disappear.prefab");
		cts?.Cancel();
		lightning?.Cancel();
		EnterCutsceneState(2);
		Game.Instance.FieldController.RemoveEnemy(this);
		ClearStatusEffect(force: true);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { basicAttack1, basicAttack2, basicAttack3, basicAttack4, annihilation, dash, chargeAttack, swordOfGod0, swordOfGod1 };
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		swordOfGodAgain = false;
		if (patternDebug != Pattern.None)
		{
			return GetSequence(patternDebug);
		}
		List<int> list = new List<int>();
		list.AddRange(Enumerable.Repeat(0, annihilation.GetPower(this)));
		list.AddRange(Enumerable.Repeat(1, basicAttack1.GetPower(this)));
		list.AddRange(Enumerable.Repeat(2, dash.GetPower(this)));
		list.AddRange(Enumerable.Repeat(3, chargeAttack.GetPower(this)));
		list.AddRange(Enumerable.Repeat(4, swordOfGod0.GetPower(this)));
		int num = list[CryptoRandom.Range(0, list.Count)];
		List<BossAttackSequence> list2 = new List<BossAttackSequence> { annihilation, basicAttack1, dash, chargeAttack, swordOfGod0 };
		for (int i = 0; i < list2.Count; i++)
		{
			if (i != num)
			{
				list2[i].CastObject<BossAttackSequence>().RisePower();
			}
		}
		lastAttackSequences = num switch
		{
			0 => new List<BossAttackSequence> { annihilation }, 
			1 => GetBasicAttackSequences(), 
			2 => new List<BossAttackSequence> { dash }, 
			3 => new List<BossAttackSequence> { chargeAttack }, 
			4 => new List<BossAttackSequence> { swordOfGod0, swordOfGod1 }, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		return lastAttackSequences;
	}

	protected override CutsceneState GetTargetState(int index)
	{
		ReturnAllSwords();
		return index switch
		{
			-1 => DefeatState, 
			2 => PhaseTwoTransitionState, 
			6 => DefeatState, 
			_ => null, 
		};
	}

	private List<BossAttackSequence> GetSequence(Pattern pattern)
	{
		lastAttackSequences = pattern switch
		{
			Pattern.BasicAttack1 => new List<BossAttackSequence> { basicAttack1 }, 
			Pattern.BasicAttack2 => new List<BossAttackSequence> { basicAttack2 }, 
			Pattern.BasicAttack3 => new List<BossAttackSequence> { basicAttack3 }, 
			Pattern.BasicAttack4 => new List<BossAttackSequence> { basicAttack4 }, 
			Pattern.BasicAttackOnly => new List<BossAttackSequence> { basicAttack1, basicAttack2, basicAttack3, basicAttack4 }, 
			Pattern.BasicAttackCombo => GetBasicAttackSequences(), 
			Pattern.Dash => new List<BossAttackSequence> { dash }, 
			Pattern.ChargeAttack => new List<BossAttackSequence> { chargeAttack }, 
			Pattern.SwordOfGod => new List<BossAttackSequence> { swordOfGod0, swordOfGod1 }, 
			Pattern.Annihilation => new List<BossAttackSequence> { annihilation }, 
			_ => new List<BossAttackSequence>(), 
		};
		return lastAttackSequences;
	}

	private List<BossAttackSequence> GetBasicAttackSequences()
	{
		List<int> list = new List<int>();
		int count = 100 - swordOfGodChance - annihilationChance - dashChance - chargeAttackChance;
		list.AddRange(Enumerable.Repeat(0, count));
		list.AddRange(Enumerable.Repeat(1, swordOfGodChance));
		list.AddRange(Enumerable.Repeat(2, annihilationChance));
		list.AddRange(Enumerable.Repeat(3, dashChance));
		list.AddRange(Enumerable.Repeat(4, chargeAttackChance));
		return list[CryptoRandom.Range(0, list.Count)] switch
		{
			0 => new List<BossAttackSequence> { basicAttack1, basicAttack2, basicAttack3, basicAttack4 }, 
			1 => new List<BossAttackSequence> { basicAttack1, basicAttack2, swordOfGod0, swordOfGod1 }, 
			2 => new List<BossAttackSequence> { basicAttack1, basicAttack2, annihilation }, 
			3 => new List<BossAttackSequence> { basicAttack1, dash }, 
			4 => new List<BossAttackSequence> { basicAttack1, chargeAttack }, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private static void SpawnSword(ref PrimordialFlameSword sword, Vector2 position, bool goldenSword)
	{
		sword = Enemy.PoolManager.GetObject<PrimordialFlameSword>("Primordial Flame_Sword.prefab", position);
		if (!goldenSword)
		{
			sword.EnableVFX("sword_of_god_0");
			sword.SetAnimation("god_ready", loop: false);
			sword.AddAnimation("idle_god", loop: false);
		}
		else
		{
			sword.EnableVFX("sword_of_god_2");
			sword.SetAnimation("idle_god", loop: false);
		}
	}

	private void DropSword(int duration)
	{
		swordOfGod.transform.DOMoveY(base.SpawnPosition.y, Util.FrameToSeconds(duration)).SetEase(Ease.InCubic).OnComplete(delegate
		{
			swordOfGod.ImpactEffect();
			swordOfGod.Drop();
		});
	}

	private void PullOutSword(int duration)
	{
		swordOfGod.EnableVFX("sword_of_god_1");
		swordOfGod.transform.DOMoveY(base.SpawnPosition.y + swordSpawnHeight, Util.FrameToSeconds(duration)).SetEase(Ease.OutSine).OnComplete(delegate
		{
			swordOfGod.ReturnSword("sword_of_god_1").Forget();
			swordOfGod = null;
		});
	}

	private void Drop(int duration)
	{
		base.IsImmortal = false;
		base.transform.DOMoveY(base.SpawnPosition.y + HitBox.size.y / 2f, Util.FrameToSeconds(duration)).SetEase(Ease.OutQuad);
		LightningStrikeSelf(30);
	}

	private async UniTaskVoid LightningStrike()
	{
		cts = new CancellationTokenSource();
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(CryptoRandom.Range(0, theEndLightningDuration + 1));
		LazySingleton<MessageSystem>.Instance.Publish(new WorldParticleEvent(WorldParticleEvent.WorldParticle.PrimordialFlameLightningSign));
		Enemy.SoundManager.PlayOneShot(theEndFlashEvent);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(theEndWarningDuration + theEndWaitFrame);
		if (!cts.IsCancellationRequested)
		{
			Vector2 vector = new Vector2(Enemy.Player.VisualBottom.x, base.SpawnPosition.y);
			lightning = Enemy.PoolManager.GetObject<LightningAttack>("Primordial Flame_Lightning.prefab", vector);
			lightning.gameObject.SetActive(value: true);
			lightning.Activate(0, theEndLightningDuration).Forget();
			Enemy.SoundManager.PlayOneShotAttached(theEndLightningEvent, lightning.transform);
		}
	}

	private void LightningStrikeSelf(int duration)
	{
		area.Flash(Color.yellow, 1f, Util.FrameToSeconds(theEndWarningDuration));
		Enemy.SoundManager.PlayOneShot(theEndFlashEvent);
		Vector2 vector = new Vector2(VisualBottom.x, base.SpawnPosition.y);
		lightning = Enemy.PoolManager.GetObject<LightningAttack>("Primordial Flame_Lightning.prefab", vector);
		lightning.gameObject.SetActive(value: true);
		lightning.Activate(0, duration).Forget();
		Enemy.SoundManager.PlayOneShotAttached(theEndLightningEvent, lightning.transform);
	}

	private void Pulling()
	{
		if (Enemy.Player.TryGetComponent<Rigidbody2D>(out var component))
		{
			Vector2 vector = new Vector2((AnnihilationPosition - Enemy.Player.VisualBottom).normalized.x.GetSign(), 0f);
			component.AddForce(vector * pullPower, ForceMode2D.Force);
		}
	}

	private void SwordOfGodAgain(int chance)
	{
		if (!swordOfGodAgain && CryptoRandom.Range(1, 101) <= chance)
		{
			base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { swordOfGod0, swordOfGod1 }));
			swordOfGodAgain = true;
		}
	}

	private void Dash(bool immortal)
	{
		base.IsImmortal = immortal;
		base.SkeletonRenderer.skeleton.SetAlpha(immortal ? 0f : 1f);
	}

	private void SwordDisappear()
	{
		GenericBossAttackSequence<PrimordialFlame> genericBossAttackSequence = base.AttackState.CurrentSequence.CastObject<GenericBossAttackSequence<PrimordialFlame>>();
		GenericBossAttackSequence<PrimordialFlame> genericBossAttackSequence2 = base.AttackState.NextSequence?.CastObject<GenericBossAttackSequence<PrimordialFlame>>();
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
}
