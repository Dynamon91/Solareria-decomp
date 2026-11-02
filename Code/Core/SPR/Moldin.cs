using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Dreamteck.Splines;
using LapinLibrary;
using Library;
using SPR.FSM.Base;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class Moldin : BossCharacter
{
	public const int OpeningStateIndex = 0;

	public const int RetryInFirstPhaseStateIndex = 1;

	public const int AfterRetryInFirstPhaseStateIndex = 2;

	public const int SecondPhaseStateIndex = 3;

	public const int RetryInSecondPhaseStateIndex = 4;

	public const int EndingStateIndex = 5;

	public const int AfterEndingStateIndex = 6;

	[Header("파괴 망치")]
	[SerializeField]
	private Transform thornSpawnTransform;

	[Header("톱날 연성")]
	[SerializeField]
	private float sawStart;

	[SerializeField]
	private float sawEnd;

	[Header("Sequence")]
	[SerializeField]
	private GenericBossAttackSequence<Moldin> hammer1;

	[SerializeField]
	private GenericBossAttackSequence<Moldin> hammer2;

	[SerializeField]
	private GenericBossAttackSequence<Moldin> spearhead;

	[SerializeField]
	private GenericBossAttackSequence<Moldin> slashCheck1;

	[SerializeField]
	private GenericBossAttackSequence<Moldin> slashCheck2;

	[SerializeField]
	private GenericBossAttackSequence<Moldin> slashCheck3;

	[SerializeField]
	private GenericBossAttackSequence<Moldin> slashBackhand;

	[SerializeField]
	private GenericBossAttackSequence<Moldin> slashBackhandSphere;

	[SerializeField]
	private GenericBossAttackSequence<Moldin> createSaw;

	[SerializeField]
	private GenericBossAttackSequence<Moldin> dagger;

	[SerializeField]
	private GenericBossAttackSequence<Moldin> controlGravity;

	[SerializeField]
	private GenericBossAttackSequence<Moldin> controlGravityGrab;

	[Header("파괴 창날")]
	[SerializeField]
	private float spearRotateOffset;

	[SerializeField]
	private Ease returnRotateEase = Ease.Linear;

	[Header("References")]
	[SerializeField]
	private Transform playerGrabTransform;

	[SerializeField]
	private Transform spearheadTransform;

	[SerializeField]
	private Transform gravityMachineTransform;

	[SerializeField]
	private SplineComputer spearheadSpline;

	private MoldinWeapon weapon;

	private MoldinSaw saw;

	private MoldinGravityMachine gravityMachine;

	private CancellationTokenSource weaponsCancellationTokenSource;

	public override string Name => "Moldin";

	private PhaseTransitionCutsceneState PhaseTransitionState { get; set; }

	private CutsceneState DyingState { get; set; }

	private void LateUpdate()
	{
		AdjustXPosition();
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(new Vector3(sawStart, 0f), 1f);
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(new Vector3(sawEnd, 0f), 1f);
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		PhaseTransitionState = new PhaseTransitionCutsceneState(this, 3, blockControl: false, hideCanvas: false);
		DyingState = new CutsceneState(this, 5, blockControl: true);
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		spearheadSpline.transform.SetParent(null);
		base.StateMachine.ChangeState(null);
		AddAction(ActionTiming.Begin, "SpawnBackhandSphere", delegate(int _, Parameter[] param)
		{
			SpawnBackhandSphere(param[0].Float, param[1].Float, param[2].Int, param[3].Int, param[4].Int).Forget();
		});
		AddAction(ActionTiming.Begin, "CreateSaw", delegate(Parameter[] p)
		{
			CreateSaw(p[0].Int);
		});
		AddAction(ActionTiming.End, "CreateSaw", delegate(Parameter[] p)
		{
			MoveSaw(p[1].Int, p[2].Float, p[3].Float);
		});
		AddAction(ActionTiming.Begin, "Invisible", (Action)delegate
		{
			Invisible(active: true);
		});
		AddAction(ActionTiming.End, "Invisible", (Action)delegate
		{
			Invisible(active: false);
		});
		AddAction(ActionTiming.Begin, "CreateGravityMachine", CreateGravityMachine);
		AddAction(ActionTiming.End, "CreateGravityMachine", (Action)delegate
		{
			gravityMachine.PlayEffect(MoldinGravityMachine.EffectTarget.Wall).Forget();
			gravityMachine.PlayEffect(MoldinGravityMachine.EffectTarget.Ground).Forget();
		});
		AddAction(ActionTiming.Begin, "AdjustPlayerPos", delegate(int duration)
		{
			Enemy.Player.transform.DOMove(new Vector3(Enemy.Player.VisualBottom.x, playerGrabTransform.position.y), Util.FrameToSeconds(duration));
		});
		AddAction(ActionTiming.Begin, "HoldPlayer", HoldPlayerBegin);
		AddAction(ActionTiming.OnFrame, "HoldPlayerPosition", HoldPlayer);
		AddAction(ActionTiming.End, "HoldPlayer", (Action)delegate
		{
			Enemy.Player.TryTakeDamage(new HitInfo(this, "DropPlayer"));
		});
		AddAction(ActionTiming.Begin, "ReturnGravityMachine", ReturnGravityMachine);
		AddAction(ActionTiming.Begin, "SpawnWeapon", delegate(int duration, Parameter[] p)
		{
			SpawnWeapon(p[0].String, duration, p[1].Int, p[2].Int, p[3].Int).Forget();
		});
		AddAction(ActionTiming.Begin, "MoldinRotate", MoldinRotate);
		AddAction(ActionTiming.Begin, "SetSpearRotate", SetSpearRotate);
		AddAction(ActionTiming.Begin, "ReturnRotate", ReturnRotate);
		AddAction(ActionTiming.End, "ReturnRotate", (Action)delegate
		{
			base.transform.rotation = Quaternion.identity;
			weapon.transform.rotation = Quaternion.identity;
		});
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { hammer1, controlGravity };
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		return GetRandomPattern();
	}

	public override bool TryPhaseTransition()
	{
		if (!IsGroggyPlaying && base.Phase == 1 && base.Health < base.Phase2Health)
		{
			EnterCutsceneState(3);
			return true;
		}
		return false;
	}

	public override void OnEnterBounceState()
	{
		WeaponDisappear();
		DOTween.Kill(base.transform);
		base.transform.rotation = Quaternion.identity;
		ForceDrop();
	}

	public override void OnEnterExecution()
	{
		WeaponDisappear();
		DOTween.Kill(base.transform);
		base.transform.rotation = Quaternion.identity;
		ForceDrop();
		base.OnEnterExecution();
	}

	public override void OnAttackEnd(IState nextState)
	{
		if (nextState is BossIdleState || nextState is EnemyBounceState || nextState is GroggyState)
		{
			BossAttackState bossAttackState = base.StateMachine.CurrentState.CastObject<BossAttackState>();
			base.Fatigue = bossAttackState.LastSequenceFatigue;
		}
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (hitInfo.AttackName.Equals("Moldin_Gravity_Grab") && Enemy.Player.CanHit && Enemy.Player.CanGrab)
		{
			gravityMachine?.PlayBoundSfx();
			base.VfxController.Stop(controlGravity.TimelineName);
			base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { controlGravityGrab }));
		}
	}

	protected override void OnEnterGroggyState()
	{
		WeaponDisappear();
		DOTween.Kill(base.transform);
		base.transform.rotation = Quaternion.identity;
		ForceDrop();
		base.OnEnterGroggyState();
	}

	protected override void Die()
	{
		EnterDie().Forget();
	}

	protected override void OnDie()
	{
		WeaponDisappear();
		ClearPlayerStagger();
	}

	protected override CutsceneState GetTargetState(int index)
	{
		return index switch
		{
			-1 => DyingState, 
			3 => PhaseTransitionState, 
			5 => DyingState, 
			_ => null, 
		};
	}

	public void WeaponDisappear()
	{
		LazySingleton<MessageSystem>.Instance.Publish(new MoldinWeaponDisappearEvent());
		weaponsCancellationTokenSource?.Cancel();
		base.transform.rotation = Quaternion.identity;
	}

	private List<BossAttackSequence> GetRandomPattern()
	{
		List<BossAttackSequence> list = new List<BossAttackSequence> { hammer1, spearhead, slashCheck1, slashBackhand, createSaw, dagger };
		if (base.Phase >= 2)
		{
			list.AddRange(new List<BossAttackSequence> { controlGravity });
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
			0 => GetHammerCombo(), 
			1 => GetSpearheadCombo(), 
			2 => GetCheckSlashCombo(), 
			3 => GetSlashBackhandCombo(), 
			4 => GetSawCombo(), 
			5 => GetDaggerCombo(), 
			6 => new List<BossAttackSequence> { controlGravity }, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private List<BossAttackSequence> GetHammerCombo()
	{
		List<BossAttackSequence> list = new List<BossAttackSequence> { hammer1 };
		int maxExclusive = ((base.Phase == 1) ? 4 : 5);
		switch (CryptoRandom.Range(0, maxExclusive))
		{
		case 0:
			list.Add(hammer2);
			break;
		case 1:
			list.AddRange(GetCheckSlashCombo());
			break;
		case 2:
			list.Add(slashBackhandSphere);
			break;
		case 3:
			list.Add(createSaw);
			break;
		case 4:
			list.Add(controlGravity);
			break;
		}
		return list;
	}

	private List<BossAttackSequence> GetSpearheadCombo()
	{
		List<BossAttackSequence> list = new List<BossAttackSequence> { spearhead };
		int maxExclusive = ((base.Phase == 1) ? 3 : 4);
		switch (CryptoRandom.Range(0, maxExclusive))
		{
		case 0:
			list.AddRange(GetCheckSlashCombo());
			break;
		case 1:
			list.AddRange(GetSlashBackhandCombo());
			break;
		case 2:
			list.Add(createSaw);
			break;
		case 3:
			list.Add(controlGravity);
			break;
		}
		return list;
	}

	private List<BossAttackSequence> GetCheckSlashCombo()
	{
		List<BossAttackSequence> list = new List<BossAttackSequence> { slashCheck1 };
		switch (CryptoRandom.Range(0, 4))
		{
		case 0:
			list.Add(slashCheck2);
			break;
		case 1:
			list.Add(slashCheck3);
			break;
		case 2:
			list.Add(hammer2);
			break;
		case 3:
			list.Add(slashBackhandSphere);
			break;
		}
		return list;
	}

	private List<BossAttackSequence> GetSlashBackhandCombo()
	{
		List<BossAttackSequence> list = new List<BossAttackSequence> { slashBackhand };
		int maxExclusive = ((base.Phase == 1) ? 1 : 2);
		switch (CryptoRandom.Range(0, maxExclusive))
		{
		case 0:
			list.Add(slashBackhandSphere);
			break;
		case 1:
			list.Add(controlGravity);
			break;
		}
		return list;
	}

	private List<BossAttackSequence> GetSawCombo()
	{
		List<BossAttackSequence> list = new List<BossAttackSequence> { createSaw };
		if (base.Phase == 1)
		{
			return list;
		}
		switch (CryptoRandom.Range(0, 2))
		{
		case 1:
			list.Add(controlGravity);
			break;
		case 2:
			list.Add(slashBackhandSphere);
			break;
		}
		return list;
	}

	private List<BossAttackSequence> GetDaggerCombo()
	{
		List<BossAttackSequence> list = new List<BossAttackSequence> { dagger };
		int maxExclusive = ((base.Phase == 1) ? 4 : 5);
		switch (CryptoRandom.Range(0, maxExclusive))
		{
		case 0:
			list.Add(hammer2);
			break;
		case 1:
			list.AddRange(GetCheckSlashCombo());
			break;
		case 2:
			list.Add(createSaw);
			break;
		case 3:
			list.Add(slashBackhandSphere);
			break;
		case 4:
			list.Add(controlGravity);
			break;
		}
		return list;
	}

	private async UniTaskVoid SpawnBackhandSphere(float height, float speed, int waitFrame, int explosionWaitDuration, int explosionChance)
	{
		MoldinBackhandSphere backhandSphere = Enemy.PoolManager.GetObject<MoldinBackhandSphere>("Moldin_BackhandSphere.prefab", Enemy.Player.VisualBottom + new Vector2(0f, height));
		bool isPhase2Sphere = CryptoRandom.Range(1, 101) <= explosionChance;
		backhandSphere.EnableVFX((base.Phase == 1) ? "Phase1" : (isPhase2Sphere ? "Phase2" : "Phase1"));
		backhandSphere.ApplyFade(appear: true, Util.FrameToSeconds(waitFrame));
		for (int frameCount = 0; frameCount < waitFrame; frameCount++)
		{
			Vector3 position = backhandSphere.transform.position;
			position.x = Enemy.Player.VisualBottom.x;
			backhandSphere.transform.position = position;
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
		backhandSphere.Drop(GroundPosition.y, speed, base.Phase >= 2 && isPhase2Sphere, explosionWaitDuration);
	}

	private void CreateSaw(int appearFrame)
	{
		LookPlayer();
		Vector2 vector = VisualBottom;
		vector.x += (float)FacingDirection.GetSignX() * sawStart;
		vector = new Vector2(Mathf.Clamp(vector.x, MyArea.AreaBox.LowerLeft.x, MyArea.AreaBox.UpperRight.x), base.SpawnPosition.y);
		saw = Enemy.PoolManager.GetObject<MoldinSaw>("Moldin_Saw.prefab", vector);
		saw.Flip(FacingDirection.Opposite());
		saw.SetAnimation("saw_ready", loop: false);
		saw.ApplyFade(appear: true, Util.FrameToSeconds(appearFrame));
	}

	private void MoveSaw(int disappearFrame, float phase1Speed, float phase2Speed)
	{
		PoolingParticle effect = Enemy.PoolManager.GetObject<PoolingParticle>("vfx_Moldin_Saw_Tail.prefab", saw.transform.position);
		effect.GetComponentInChildren<BoneFollower>().SkeletonRenderer = saw.SkeletonRenderer;
		effect.transform.Flip(saw.FacingDirection.GetSignX());
		saw.SetAnimation("saw_loop", loop: false);
		saw.HandleOverlap((base.Phase > 1) ? "Phase1" : "Phase2");
		saw.transform.DOMoveX(VisualBottom.x + (float)FacingDirection.GetSignX() * sawEnd, (base.Phase == 1) ? phase1Speed : phase2Speed).SetSpeedBased(isSpeedBased: true).SetEase(Ease.Linear)
			.OnUpdate(delegate
			{
				saw.HandleOverlap((base.Phase > 1) ? "Phase1" : "Phase2");
			})
			.OnComplete(delegate
			{
				saw.SetAnimation("saw_end", loop: false);
				saw.ApplyFade(appear: false, Util.FrameToSeconds(disappearFrame), Util.FrameToSeconds(disappearFrame), "saw_ready").Forget();
				saw = null;
				effect.ReturnToPool();
			});
	}

	private void Invisible(bool active)
	{
		base.SkeletonRenderer.Skeleton.SetAlpha((!active) ? 1 : 0);
		base.IsImmortal = active;
	}

	private void CreateGravityMachine()
	{
		gravityMachine = Enemy.PoolManager.GetObject<MoldinGravityMachine>("Moldin_Weapon_2.prefab", gravityMachineTransform.position);
		gravityMachine.SetWeapon("gravity");
		gravityMachine.ApplyFade(appear: true, Util.FrameToSeconds(20f));
		gravityMachine.PlayEffect(MoldinGravityMachine.EffectTarget.Machine).Forget();
	}

	private void ReturnGravityMachine()
	{
		gravityMachine.ApplyFade(appear: false, Util.FrameToSeconds(20f), null, autoReturn: false);
		gravityMachine.StopEffect(MoldinGravityMachine.EffectTarget.Machine);
		gravityMachine.StopEffect(MoldinGravityMachine.EffectTarget.Wall);
		gravityMachine.StopEffect(MoldinGravityMachine.EffectTarget.Ground);
		gravityMachine.StopBoundSfx();
		gravityMachine.ReturnToPoolAsync().Forget();
	}

	private void HoldPlayerBegin(int duration)
	{
		Enemy.Player.PSM.ChangeState(new PlayerStaggerHitState(Enemy.Player.PSM, delegate
		{
			Enemy.Player.SetGravityFree(active: true);
		}, delegate
		{
			Enemy.Player.SetGravityFree(active: false);
		})
		{
			Duration = duration,
			NextState = Enemy.Player.PSM.DownState,
			Animation = "struggle",
			TargetPosition = Enemy.Player.Position
		});
	}

	private void HoldPlayer()
	{
		if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState playerStaggerHitState)
		{
			playerStaggerHitState.TargetPosition = new Vector3(Enemy.Player.VisualBottom.x, playerGrabTransform.position.y);
		}
	}

	private async UniTaskVoid SpawnWeapon(string weaponName, int duration, int waitFrame, int appearFrame, int disappearFrame)
	{
		weapon = Enemy.PoolManager.GetObject<MoldinWeapon>("Moldin_Weapon.prefab", base.transform.position, base.transform);
		weapon.SetWeapon(weaponName);
		weapon.ApplyFade(appear: true, Util.FrameToSeconds(appearFrame), Util.FrameToSeconds(waitFrame)).Forget();
		for (int frame = 0; frame < duration; frame++)
		{
			weapon.SetLayerOrder(weaponName, frame);
			weapon.transform.Flip(1f);
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate, TaskUtil.RefreshToken(ref weaponsCancellationTokenSource));
		}
		weapon.StopFollowing(disappearFrame);
	}

	private void MoldinRotate(int duration)
	{
		Vector2 normalized = (Enemy.Player.VisualCenter - VisualCenter).normalized;
		base.transform.DORotate(new Vector3(0f, 0f, Vector2.SignedAngle(Vector2.down, normalized) + (float)FacingDirection.GetSignX() * spearRotateOffset), Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private void SetSpearRotate()
	{
		weapon.transform.rotation = base.transform.rotation;
	}

	private void ReturnRotate(int duration)
	{
		weapon.transform.DORotate(Vector3.zero, Util.FrameToSeconds(duration)).SetEase(returnRotateEase);
	}
}
