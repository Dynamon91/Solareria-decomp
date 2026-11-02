using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.FSM.Base;
using SPR.Field;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class Fidiel : BossCharacter
{
	[Header("2페이즈")]
	[SerializeField]
	private float phase2StartFatigue = 1f;

	[Header("캔슬")]
	[SerializeField]
	private int grabGhosting = 30;

	[SerializeField]
	private int throwSpearGhosting = 20;

	[Header("투창")]
	[SerializeField]
	private float jumpPower;

	[Header("강한 점프")]
	[SerializeField]
	private float waveSpeed;

	[SerializeField]
	private Transform strongJumpThrowTransform;

	[Header("고속 잡기")]
	[SerializeField]
	private float leftOffset;

	[SerializeField]
	private float rightOffset;

	[Header("Sequence")]
	[SerializeField]
	private GenericBossAttackSequence<Fidiel> throwSpearWall;

	[SerializeField]
	private GenericBossAttackSequence<Fidiel> attack1;

	[SerializeField]
	private GenericBossAttackSequence<Fidiel> attack2;

	[SerializeField]
	private GenericBossAttackSequence<Fidiel> attack3;

	[SerializeField]
	private GenericBossAttackSequence<Fidiel> aerialStrike;

	[SerializeField]
	private GenericBossAttackSequence<Fidiel> aerialStrikeAttack3;

	[SerializeField]
	private GenericBossAttackSequence<Fidiel> strongJump;

	[SerializeField]
	private GenericBossAttackSequence<Fidiel> highSpeedGrab;

	[SerializeField]
	private GenericBossAttackSequence<Fidiel> highSpeedGrabThrow;

	[SerializeField]
	private GenericBossAttackSequence<Fidiel> jumpToCenter;

	[Header("Reference")]
	[SerializeField]
	private Transform throwDestination;

	[SerializeField]
	private Transform strongJumpDirection;

	public const int OpeningStateIndex = 0;

	public const int RetryInFirstPhaseStateIndex = 1;

	public const int SecondPhaseStateIndex = 2;

	public const int RetryInSecondPhaseStateIndex = 3;

	public const int EndingStateIndex = 4;

	public const int AfterEndingStateIndex = 5;

	private FidielSpear spear;

	private Vector2 spearJumpDestination;

	private GenericBossAttackSequence<Fidiel> throwSpearNextBehavior;

	private Zone areaZone;

	private Vector2 playerThrowDestination;

	private SkeletonGhost skeletonGhost;

	private readonly LayerMask spearLayer = (int)LayerUtility.PlatformLayer | (int)LayerUtility.ObjectLayer;

	public override string Name => "Fidiel";

	protected override int OriginDef
	{
		get
		{
			if (base.Phase >= 2)
			{
				return base.EnemyStatData.Def[1];
			}
			return base.EnemyStatData.Def[0];
		}
	}

	private PhaseTransitionCutsceneState PhaseTransitionState { get; set; }

	private CutsceneState DyingState { get; set; }

	private SkeletonGhost SkeletonGhost => skeletonGhost ?? (skeletonGhost = base.SkeletonAnimation.GetComponent<SkeletonGhost>());

	private AreaBox CurrentArea
	{
		get
		{
			if (base.Phase != 1)
			{
				return MyArea.CastObject<FidielArea>().Phase2Area;
			}
			return MyArea.CastObject<FidielArea>().Phase1Area;
		}
	}

	private void LateUpdate()
	{
		AdjustXPosition();
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		PhaseTransitionState = new PhaseTransitionCutsceneState(this, 2, blockControl: false, hideCanvas: false);
		DyingState = new CutsceneState(this, 4);
		base.StateMachine.ChangeState(base.IdleState);
		TimelineBeginActions.Add("JumpToWall", JumpToWall);
		AddAction(ActionTiming.Begin, "JumpToPlayer", delegate(int duration, Parameter[] p)
		{
			JumpToPlayer(duration, p[0].Float);
		});
		TimelineBeginActions.Add("DropToFloor", DropToFloor);
		BeginParamActions.Add("MoveBehindPlayer", delegate(int duration, Parameter[] param)
		{
			MoveBehindPlayer(duration, param[0].Float);
		});
		AddAction(ActionTiming.Begin, "HoldPlayer", StartHoldPlayer);
		AddAction(ActionTiming.OnFrame, "HoldPlayer", HoldPlayer);
		BeginParamActions.Add("ThrowUpPlayer", delegate(int duration, Parameter[] param)
		{
			ThrowUpPlayer(duration, param[0].Float);
		});
		BeginParamActions.Add("JustThrowSpear", delegate(int _, Parameter[] param)
		{
			ThrowSpearAfterGrab(param[0].Float);
		});
		BeginParamActions.Add("ThrowSpearStrong", delegate(int _, Parameter[] param)
		{
			ThrowSpearStrong(param[0].Float);
		});
		TimelineBeginActions.Add("JumpToOutside", JumpToOutside);
		BeginParamActions.Add("StrongJumpSpearThrow", delegate(int _, Parameter[] param)
		{
			StrongJumpSpearThrow(param[0].Float);
		});
		BeginParamActions.Add("JumpToCenter", delegate(int duration, Parameter[] param)
		{
			JumpToCenter(duration, param[0].Float);
		});
		AddAction(ActionTiming.Begin, "CancelByGrab", delegate(Parameter[] p)
		{
			CancelByGrab(p[0].Int);
		});
		AddAction(ActionTiming.Begin, "CancelByThrow", delegate(Parameter[] p)
		{
			CancelByThrow(p[0].Int);
		});
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

	public override void OnEnterBounceState()
	{
		SkeletonGhost.ghostingEnabled = false;
	}

	public override void OnEnterExecution()
	{
		base.OnEnterExecution();
		SkeletonGhost.ghostingEnabled = false;
		DOTween.Kill(base.transform);
		ForceDrop();
		spear?.ReturnToPool();
	}

	protected override void AdjustXPosition()
	{
		Vector3 position = base.transform.position;
		float x = Mathf.Clamp(position.x, (float)CurrentArea.LowerLeft.x + Width / 2f, (float)CurrentArea.UpperRight.x - Width / 2f);
		base.transform.position = new Vector3(x, position.y);
	}

	protected override void OnEnterGroggyState()
	{
		base.OnEnterGroggyState();
		SkeletonGhost.ghostingEnabled = false;
		DOTween.Kill(base.transform);
		ForceDrop();
		spear?.ReturnToPool();
	}

	protected override void OnDie()
	{
		spear?.ReturnToPool();
		ClearPlayerStagger();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack1, attack2, attack3, aerialStrike, strongJump, highSpeedGrab };
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		return GetRandomSequence();
	}

	protected override CutsceneState GetTargetState(int index)
	{
		return index switch
		{
			-1 => DyingState, 
			2 => PhaseTransitionState, 
			4 => DyingState, 
			_ => null, 
		};
	}

	protected override void OnIncreasePhase(int before, int after)
	{
		base.OnIncreasePhase(before, after);
		if (after >= 2)
		{
			AddFatigue(phase2StartFatigue);
		}
	}

	public void StrongJumpSpearThrow(float speed)
	{
		Vector2 vector = strongJumpThrowTransform.position;
		spear = Enemy.PoolManager.GetObject<FidielSpear>("Fidiel_Spear.prefab", vector);
		spear.EnableStrongJumpEffect(value: true);
		Vector2 vector2 = new Vector2(areaZone.Center.x, areaZone.UpperRight.y + areaZone.Size.y / 2f);
		Vector2 normalized = (vector - VisualCenter).normalized;
		spear.transform.Flip(normalized.x.GetSign());
		spear.SetDirection(normalized);
		spear.transform.DOMove(vector2, speed).SetSpeedBased(isSpeedBased: true).SetEase(Ease.Linear)
			.OnComplete(delegate
			{
				spear.EnableStrongJumpEffect(value: false);
				spear.ReturnToPool();
			});
	}

	public void SetArea(Area area)
	{
		areaZone = new Zone(area.AreaBox.Center, area.AreaBox.Size);
	}

	public void SetGroundPosition()
	{
		base.SpawnPosition = VisualBottom;
	}

	public void RebindGroggyVCam(CinemachineVirtualCamera vcam)
	{
		base.VfxController.GetTimeline<GroggyTimelineEffect>("groggy_start").ReBindVCam(vcam);
	}

	private List<BossAttackSequence> GetRandomSequence()
	{
		List<int> list = new List<int>();
		list.AddRange(Enumerable.Repeat(0, attack1.GetPower(this)));
		list.AddRange(Enumerable.Repeat(1, attack2.GetPower(this)));
		list.AddRange(Enumerable.Repeat(2, attack3.GetPower(this)));
		list.AddRange(Enumerable.Repeat(4, strongJump.GetPower(this)));
		if (base.Phase < 2)
		{
			list.AddRange(Enumerable.Repeat(3, aerialStrike.GetPower(this)));
		}
		else
		{
			list.AddRange(Enumerable.Repeat(5, highSpeedGrab.GetPower(this)));
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
			3 => new List<BossAttackSequence> { aerialStrike, throwSpearWall }, 
			4 => new List<BossAttackSequence> { strongJump }, 
			5 => new List<BossAttackSequence> { highSpeedGrab }, 
			_ => new List<BossAttackSequence>(), 
		};
	}

	private void ThrowSpearAfterGrab(float speed)
	{
		Vector2 visualCenter = VisualCenter;
		Vector2 normalized = (playerThrowDestination - visualCenter).normalized;
		spearJumpDestination = Physics2D.Raycast(visualCenter, normalized, float.PositiveInfinity, spearLayer).point;
		spearJumpDestination.x = Mathf.Clamp(spearJumpDestination.x, CurrentArea.LowerLeft.x, CurrentArea.UpperRight.x);
		spearJumpDestination.y = Mathf.Clamp(spearJumpDestination.y, CurrentArea.LowerLeft.y, CurrentArea.UpperRight.y);
		spear = Enemy.PoolManager.GetObject<FidielSpear>("Fidiel_Spear.prefab", visualCenter);
		spear.transform.Flip(normalized.x.GetSign());
		spear.SetDirection(normalized);
		spear.EnableEffect(value: true);
		List<IDamageable> damageableList = new List<IDamageable>();
		spear.transform.DOMove(spearJumpDestination, speed).SetSpeedBased(isSpeedBased: true).SetEase(Ease.Linear)
			.OnUpdate(delegate
			{
				AttackUtility.AttackBox(spear.transform.position, spear.GetComponent<BoxCollider2D>().size, spear.Angle, new HitInfo(spear), LayerUtility.PlayerLayer, damageableList);
				if ((spear.Position - playerThrowDestination).normalized.x.GetSign() == FacingDirection.GetSignX())
				{
					Enemy.Player.transform.position = spear.transform.position;
				}
			})
			.OnComplete(delegate
			{
				base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { jumpToCenter }));
			});
	}

	private void ThrowSpearStrong(float speed)
	{
		Vector3 boneWorldPosition = GetBoneWorldPosition("Hand_L2");
		Vector2 direction = FacingDirection.ToVector2();
		RaycastHit2D raycastHit2D = Physics2D.Raycast(boneWorldPosition, direction, float.PositiveInfinity, spearLayer);
		spearJumpDestination = raycastHit2D.point;
		spear = Enemy.PoolManager.GetObject<FidielSpear>("Fidiel_Spear.prefab", boneWorldPosition);
		spear.transform.Flip(direction.x.GetSign());
		spear.SetDirection(direction);
		spear.EnableAerialStrikeEffect(value: true);
		List<IDamageable> damageableList = new List<IDamageable>();
		spear.transform.DOMove(raycastHit2D.point, speed).SetSpeedBased(isSpeedBased: true).SetEase(Ease.Linear)
			.OnUpdate(delegate
			{
				AttackUtility.AttackBox(spear.AerialStrikeCenter, spear.AerialStrikeSize, spear.Angle, new HitInfo(spear, "Strong"), LayerUtility.PlayerLayer, damageableList);
			})
			.OnComplete(spear.EmitAerialStrikeHitEffect);
	}

	private void JumpToWall(int duration)
	{
		Enemy.PoolManager.GetObject<PoolingParticle>("Fidiel_Throw_Spear_Wall_1.prefab", VisualBottom).Play();
		Enemy.PoolManager.GetObject<PoolingParticle>("Fidiel_Jump_To_Center_1.prefab", VisualBottom).Play();
		Vector2 vector = new Vector2(spearJumpDestination.x, spearJumpDestination.y - HitBox.size.y / 2f);
		float num = Vector2.Distance(VisualBottom, vector) / areaZone.Size.x;
		int value = Mathf.RoundToInt((float)duration * num);
		value = Mathf.Clamp(value, 0, duration);
		base.transform.DOJump(vector, 1f, 1, Util.FrameToSeconds(value)).SetEase(Ease.Linear).OnComplete(spear.ReturnToPool);
	}

	private void DropToFloor(int duration)
	{
		ShortcutExtensions.DOMove(endValue: new Vector2(VisualBottom.x, base.SpawnPosition.y), target: base.transform, duration: Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private void JumpToPlayer(int duration, float offsetX)
	{
		int sign = (VisualBottom.x - Enemy.Player.VisualBottom.x).GetSign();
		ShortcutExtensions.DOJump(endValue: new Vector2(Enemy.Player.VisualBottom.x + (float)sign * offsetX, base.SpawnPosition.y), target: base.transform, jumpPower: jumpPower, numJumps: 1, duration: Util.FrameToSeconds(duration)).SetEase(Ease.Linear).OnComplete(delegate
		{
			Enemy.PoolManager.GetObject<PoolingParticle>("Fidiel_Throw_Spear_Wall_2_1.prefab", VisualBottom).Play();
		});
	}

	private void MoveBehindPlayer(int duration, float distance)
	{
		float num = HitBox.size.x / 2f;
		float min = (float)CurrentArea.LowerLeft.x + num;
		float max = (float)CurrentArea.UpperRight.x - num;
		float endValue = Mathf.Clamp(Enemy.Player.VisualBottom.x + (float)FacingDirection.GetSignX() * distance, min, max);
		base.transform.DOMoveX(endValue, Util.FrameToSeconds(duration));
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (hitInfo.AttackName.Contains("TryGrab") && !Enemy.Player.PSM.StateOption.HasFlag(StateOption.Dodge) && Enemy.Player.CanGrab)
		{
			base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { highSpeedGrabThrow }));
		}
	}

	private void StartHoldPlayer()
	{
		Enemy.Player.SetGravityFree(active: true);
		Enemy.Player.PSM.ChangeState(new PlayerStaggerHitState(Enemy.Player.PSM)
		{
			Duration = highSpeedGrabThrow.Duration + jumpToCenter.Duration,
			NextState = Enemy.Player.PSM.DownState,
			TargetPosition = Enemy.Player.Position
		});
	}

	private void HoldPlayer()
	{
		Vector3 boneWorldPosition = GetBoneWorldPosition("Hand_R2");
		if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState playerStaggerHitState)
		{
			playerStaggerHitState.TargetPosition = boneWorldPosition;
		}
	}

	private void ThrowUpPlayer(int duration, float throwPower)
	{
		playerThrowDestination = throwDestination.position;
		playerThrowDestination.x = Mathf.Clamp(playerThrowDestination.x, CurrentArea.LowerLeft.x, CurrentArea.UpperRight.x);
		Enemy.Player.transform.DOJump(playerThrowDestination, throwPower, 1, Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private void JumpToOutside(int duration)
	{
		Vector3 endValue = new Vector3(strongJumpDirection.position.x, areaZone.UpperRight.y + HitBox.size.y / 2f);
		Enemy.PoolManager.GetObject<PoolingParticle>("Fidiel_Strong_Jump_Smoke.prefab", VisualBottom).Play();
		base.transform.DOMove(endValue, Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private void JumpToCenter(int duration, float power)
	{
		Enemy.Player.SetGravityFree(active: false);
		if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState)
		{
			Enemy.Player.PSM.ChangeState(Enemy.Player.PSM.DownState);
		}
		ShortcutExtensions.DOJump(endValue: new Vector2(areaZone.Center.x, base.SpawnPosition.y), target: base.transform, jumpPower: jumpPower, numJumps: 1, duration: Util.FrameToSeconds(duration)).SetEase(Ease.Linear).OnComplete(delegate
		{
			Enemy.PoolManager.GetObject<PoolingParticle>("Fidiel_Jump_To_Center_2_1.prefab", VisualBottom).Play();
		});
	}

	private void CancelByGrab(int chance)
	{
		if (base.Phase >= 2 && chance >= CryptoRandom.Range(0, 101))
		{
			base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { highSpeedGrab }));
			GhostingEnabled(grabGhosting).Forget();
		}
	}

	private void CancelByThrow(int chance)
	{
		if (base.Phase < 2 || chance < CryptoRandom.Range(0, 101))
		{
			return;
		}
		if (base.StateMachine.CurrentState is BossAttackState bossAttackState && bossAttackState.CurrentSequence == attack3)
		{
			base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { aerialStrikeAttack3, throwSpearWall }));
		}
		else
		{
			base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { aerialStrike, throwSpearWall }));
		}
		GhostingEnabled(throwSpearGhosting).Forget();
	}

	private async UniTaskVoid GhostingEnabled(int duration)
	{
		SkeletonGhost.ghostingEnabled = true;
		await UniTask.WaitForSeconds(Util.FrameToSeconds(duration));
		SkeletonGhost.ghostingEnabled = false;
	}
}
