using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Dreamteck.Splines;
using FMODUnity;
using Library;
using SPR.FSM.Base;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class Gaia : BossCharacter
{
	public const int OpeningStateIndex = 0;

	public const int RetryInFirstPhaseStateIndex = 1;

	public const int SecondPhaseStateIndex = 2;

	public const int RetryInSecondPhaseStateIndex = 3;

	public const int EndingStateIndex = 4;

	public const int AfterEndingStateIndex = 5;

	[Header("왕의 칼날")]
	[SerializeField]
	private int markAttackFrame = 70;

	[SerializeField]
	private int markDuration = 1200;

	[Header("Ease 그래프")]
	[SerializeField]
	private AnimationCurve groundSmashEase;

	[SerializeField]
	private Ease tumbleJumpEase;

	[SerializeField]
	private Ease tumbleLandingEase;

	[SerializeField]
	private Ease punishFlyEase;

	[SerializeField]
	private Ease punishLandingEase;

	[Header("공중 제비")]
	[SerializeField]
	private float tumbleP2YOffset;

	[Header("자비")]
	[SerializeField]
	private float mercyP2MinAngle;

	[SerializeField]
	private float mercyP2MaxAngle;

	[SerializeField]
	private float mercyPhase2FailRadius;

	[SerializeField]
	private float dampMultiplier;

	[Header("Sequences")]
	[SerializeField]
	private GenericBossAttackSequence<Gaia> groundSmash;

	[SerializeField]
	private GenericBossAttackSequence<Gaia> groundSmash2;

	[SerializeField]
	private GenericBossAttackSequence<Gaia> tumble;

	[SerializeField]
	private GenericBossAttackSequence<Gaia> charge;

	[SerializeField]
	private GenericBossAttackSequence<Gaia> punish;

	[SerializeField]
	private GenericBossAttackSequence<Gaia> punishPhase2;

	[SerializeField]
	private GenericBossAttackSequence<Gaia> mercyPhase1;

	[SerializeField]
	private GenericBossAttackSequence<Gaia> mercyPhase2;

	[SerializeField]
	private GenericBossAttackSequence<Gaia> mercyGrabPhase1;

	[SerializeField]
	private GenericBossAttackSequence<Gaia> mercyGrabPhase2;

	[SerializeField]
	private GenericBossAttackSequence<Gaia> mercyPhase1Fail;

	[SerializeField]
	private GenericBossAttackSequence<Gaia> mercyPhase2Fail;

	[SerializeField]
	private GenericBossAttackSequence<Gaia> bloom;

	[SerializeField]
	private GenericBossAttackSequence<Gaia> markAttack;

	[Header("Transform")]
	[SerializeField]
	private Transform groundSmashPhase2Transform;

	[SerializeField]
	private Transform chargePhase2Transform;

	[SerializeField]
	private Transform bloomShotTransform;

	[SerializeField]
	private Transform bladeRushReturnTransform;

	[SerializeField]
	private Transform mercyP2Transform;

	[SerializeField]
	private Transform chargeTransform;

	[SerializeField]
	private Transform tumbleTraceTransform;

	[Header("References")]
	[SerializeField]
	private SplineComputer tumbleJumpSpline;

	[SerializeField]
	private SplineComputer tumbleLandingSpline;

	[SerializeField]
	private SplineComputer chargeSpline;

	[SerializeField]
	private SplineComputer punishSpline;

	[SerializeField]
	private SplineComputer punishLandingSpline;

	[SerializeField]
	private SplineComputer punishPhase2Spline;

	[SerializeField]
	private GameObject phase2Trail;

	[SerializeField]
	private List<GameObject> phase2AttackTrails;

	[Header("Sfx")]
	[SerializeField]
	private EventReference tumbleBladeEvent;

	[SerializeField]
	private EventReference chargeBladeEvent;

	[SerializeField]
	private EventReference groundSmashBladeEvent;

	[SerializeField]
	private EventReference mercyWingEvent;

	private SplineFollower follower;

	private GaiaMark mark;

	private int def;

	private float punishXPos;

	private HitInfo prevHitInfo;

	public override string Name => "Gaia";

	private PhaseTransitionCutsceneState PhaseTransitionState { get; set; }

	private CutsceneState DyingState { get; set; }

	private SplineFollower Follower => follower ?? (follower = GetComponent<SplineFollower>());

	protected override int OriginDef => def;

	private GenericBossAttackSequence<Gaia> GroundSmash
	{
		get
		{
			if (base.Phase != 1)
			{
				return groundSmash2;
			}
			return groundSmash;
		}
	}

	private GenericBossAttackSequence<Gaia> MercyStart
	{
		get
		{
			if (base.Phase != 1)
			{
				return mercyPhase2;
			}
			return mercyPhase1;
		}
	}

	private void LateUpdate()
	{
		AdjustXPosition();
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		phase2Trail.SetActive(value: false);
		PhaseTransitionState = new PhaseTransitionCutsceneState(this, 2, blockControl: false, hideCanvas: false);
		DyingState = new CutsceneState(this, 4);
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AddressableHelper.PreloadAssetAsync("Gaia_Mark.prefab").Forget();
		base.StateMachine.ChangeState(null);
		tumbleJumpSpline.transform.SetParent(null);
		tumbleLandingSpline.transform.SetParent(null);
		punishSpline.transform.SetParent(null);
		punishLandingSpline.transform.SetParent(null);
		tumbleTraceTransform.SetParent(null);
		AddAction(ActionTiming.Begin, "TumbleJump", delegate(int duration, Parameter[] p)
		{
			TumbleJump(duration, p[0].Float);
		});
		AddAction(ActionTiming.Begin, "TumbleLanding", delegate(int duration)
		{
			SplineLanding(tumbleLandingSpline, duration, tumbleLandingEase);
		});
		AddAction(ActionTiming.End, "TumbleLanding", (Action)delegate
		{
			tumbleTraceTransform.position = new Vector3(Enemy.Player.VisualBottom.x, base.SpawnPosition.y + tumbleP2YOffset);
			tumbleTraceTransform.Flip((Enemy.Player.VisualBottom - VisualBottom).x.GetSign());
			if (base.Phase >= 2)
			{
				Enemy.PoolManager.GetObject<PoolingParticle>("vfx_Gaia_Tumble_FloorSign.prefab", new Vector3(tumbleTraceTransform.position.x, base.SpawnPosition.y)).Play();
			}
		});
		AddAction(ActionTiming.Begin, "SpurtBlade", delegate(Parameter[] p)
		{
			ShotBlade(p[0].Float, tumbleTraceTransform, tumbleBladeEvent);
		});
		AddAction(ActionTiming.Begin, "ChargeBlade", delegate(Parameter[] p)
		{
			ShotBlade(p[0].Float, chargePhase2Transform, chargeBladeEvent);
		});
		AddAction(ActionTiming.Begin, "PunishFly", PunishFly);
		AddAction(ActionTiming.Begin, "PunishLanding", delegate(int duration, Parameter[] p)
		{
			PunishLanding(punishLandingSpline, duration, p[0].Float, punishLandingEase);
		});
		AddAction(ActionTiming.Begin, "PunishPhase2Attack", delegate(Parameter[] p)
		{
			PunishPhase2BladeAttack(p[0].Int);
		});
		AddAction(ActionTiming.Begin, "MercyJump", delegate(int duration, Parameter[] p)
		{
			MercyJump(duration, p[0].Float);
		});
		AddAction(ActionTiming.Begin, "ThrowPlayer", delegate(Parameter[] p)
		{
			ThrowPlayer(p[0].Float, p[1].Float, p[2].Float).Forget();
		});
		AddAction(ActionTiming.Begin, "SpreadBlade", delegate(Parameter[] p)
		{
			SpreadBlade(p[0].Int, p[1].Int, p[2].Float).Forget();
		});
		AddAction(ActionTiming.Begin, "ShotBlade", delegate(Parameter[] p)
		{
			BloomShotBlade(p[0].String, p[1].Int, p[2].Int, bloomShotTransform);
		});
		AddAction(ActionTiming.Begin, "EmitPunishParticle", EmitPunishParticle);
		AddAction(ActionTiming.Begin, "EmitPunishDisappearParticle", EmitPunishDisappearParticle);
		mercyPhase1.OnStart += delegate
		{
			Enemy.SoundManager.PlayOneShotAttached(mercyWingEvent, base.transform);
		};
		mercyPhase2.OnStart += delegate
		{
			Enemy.SoundManager.PlayOneShotAttached(mercyWingEvent, base.transform);
		};
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

	public override void OnIdle()
	{
		if ((object)mark != null && mark.CanExecute)
		{
			mark.Execute().Forget();
			base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { markAttack }));
		}
		if (base.Phase != 2)
		{
			return;
		}
		foreach (GameObject phase2AttackTrail in phase2AttackTrails)
		{
			phase2AttackTrail.gameObject.SetActive(value: false);
		}
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (!hitInfo.AttackName.Contains("Charge") && hitInfo.AttackName == prevHitInfo.AttackName)
		{
			return;
		}
		if (hitInfo.AttackName.Contains("Grab"))
		{
			if (Enemy.Player.CanGrab && Enemy.Player.CanHit)
			{
				PlayerStaggerHitState newState = new PlayerStaggerHitState(Enemy.Player.PSM)
				{
					Duration = mercyGrabPhase1.Duration - 40,
					NextState = Enemy.Player.PSM.DownState,
					TargetPosition = base.Position + FacingDirection.ToVector2() * 3f
				};
				Enemy.Player.PSM.ChangeState(newState);
				base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { (base.Phase < 2) ? mercyGrabPhase1 : mercyGrabPhase2 }));
			}
		}
		else
		{
			if (CanMarkPlayer(Enemy.Player))
			{
				MarkToPlayer().Forget();
			}
			prevHitInfo = hitInfo;
		}
	}

	public override void OnHitBurst()
	{
		mark?.RemoveMark();
	}

	public override void OnEnterBounceState()
	{
		SetSkin((base.Phase == 1) ? "Gaia" : "Phase2");
		mark?.RemoveMark();
		ForceDrop();
	}

	public override void OnEnterExecution()
	{
		SetSkin((base.Phase == 1) ? "Gaia" : "Phase2");
		base.OnEnterExecution();
		ForceDrop();
	}

	public override void OnAttackEnd(IState nextState)
	{
		SetSkin((base.Phase == 1) ? "Gaia" : "Phase2");
	}

	protected override void Die()
	{
		EnterDie().Forget();
	}

	protected override void OnDie()
	{
		ClearMark();
		ClearPlayerStagger();
	}

	protected override void OnEnterGroggyState()
	{
		SetSkin((base.Phase == 1) ? "Gaia" : "Phase2");
		base.OnEnterGroggyState();
		ForceDrop();
	}

	protected override void OnIncreasePhase(int before, int after)
	{
		base.OnIncreasePhase(before, after);
		SetSkin("Phase2");
		phase2Trail.SetActive(value: true);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { groundSmash, tumble, charge, punish, MercyStart, bloom };
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		prevHitInfo = default(HitInfo);
		if (base.Phase > 1)
		{
			foreach (GameObject phase2AttackTrail in phase2AttackTrails)
			{
				phase2AttackTrail.gameObject.SetActive(value: true);
			}
		}
		return GetSequence();
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (base.StateMachine.CurrentState is PhaseTransitionCutsceneState && base.Def > 0 && GetCurrentAnim(0).Name.Equals("scene/phase_2_loop"))
		{
			DebugLog.GeneralLog("페이즈 전환 피격 모션 재생");
			base.SkeletonAnimation.SetAnimation("scene/phase_2_hit", 1, loop: false);
		}
		return base.TryTakeDamage(hitInfo);
	}

	public void ClearMark()
	{
		if ((object)mark != null)
		{
			mark.Clear();
			UnityEngine.Object.Destroy(mark);
			mark = null;
		}
	}

	protected override CutsceneState GetTargetState(int index)
	{
		SetSkin("Gaia");
		return index switch
		{
			-1 => DyingState, 
			2 => PhaseTransitionState, 
			4 => DyingState, 
			_ => null, 
		};
	}

	private List<BossAttackSequence> GetSequence()
	{
		List<BossAttackSequence> list = new List<BossAttackSequence>
		{
			GroundSmash,
			tumble,
			charge,
			(base.Phase < 2) ? punish : punishPhase2,
			MercyStart,
			bloom
		};
		List<int> list2 = new List<int>();
		list2.AddRange(Enumerable.Repeat(0, GroundSmash.GetPower(this)));
		list2.AddRange(Enumerable.Repeat(1, tumble.GetPower(this)));
		list2.AddRange(Enumerable.Repeat(2, charge.GetPower(this)));
		list2.AddRange(Enumerable.Repeat(3, (base.Phase < 2) ? punish.GetPower(this) : punishPhase2.GetPower(this)));
		list2.AddRange(Enumerable.Repeat(4, MercyStart.GetPower(this)));
		if (base.Phase >= 2)
		{
			list2.AddRange(Enumerable.Repeat(5, bloom.GetPower(this)));
		}
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
			0 => new List<BossAttackSequence> { GroundSmash }, 
			1 => new List<BossAttackSequence> { tumble }, 
			2 => new List<BossAttackSequence> { charge }, 
			3 => new List<BossAttackSequence> { (base.Phase < 2) ? punish : punishPhase2 }, 
			4 => new List<BossAttackSequence>
			{
				MercyStart,
				(base.Phase < 2) ? mercyPhase1Fail : mercyPhase2Fail
			}, 
			5 => new List<BossAttackSequence> { bloom }, 
			_ => new List<BossAttackSequence>(), 
		};
	}

	private void PunishPhase2BladeAttack(int duration)
	{
		GaiaBlade gaiaBlade = Enemy.PoolManager.GetObject<GaiaBlade>("Gaia_Blade.prefab", punishPhase2Spline.position);
		gaiaBlade.Gaia = this;
		gaiaBlade.SetSpline(punishPhase2Spline);
		gaiaBlade.Move(duration).SetEase(Ease.Linear).OnUpdate(gaiaBlade.HandleOverlap)
			.OnComplete(gaiaBlade.ReturnToPool);
	}

	private void TumbleJump(int duration, float xValue)
	{
		LookPlayer();
		tumbleJumpSpline.transform.position = VisualBottom;
		tumbleJumpSpline.transform.Flip(FacingDirection.GetSignX());
		Follower.spline = tumbleJumpSpline;
		SplinePoint point = tumbleJumpSpline.GetPoint(1);
		point.SetPositionX(Enemy.Player.VisualBottom.x + xValue * (float)(-FacingDirection.GetSignX()));
		tumbleJumpSpline.SetPoint(1, point);
		int facing = -FacingDirection.GetSignX();
		Follower.DOFollow(Util.FrameToSeconds(duration)).SetEase(tumbleJumpEase).OnUpdate(delegate
		{
			if ((Enemy.Player.VisualCenter - VisualCenter).normalized.x.GetSign() == FacingDirection.GetSignX())
			{
				point.SetPositionX(Enemy.Player.VisualBottom.x + xValue * (float)facing);
				tumbleJumpSpline.SetPoint(1, point);
			}
		})
			.OnComplete(delegate
			{
				Follower.spline = null;
			});
	}

	private void SplineLanding(SplineComputer spline, int duration, Ease ease)
	{
		spline.transform.position = new Vector3(VisualBottom.x, base.SpawnPosition.y);
		spline.transform.Flip(FacingDirection.GetSignX());
		Follower.spline = spline;
		Follower.DOFollow(Util.FrameToSeconds(duration)).SetEase(ease).OnComplete(delegate
		{
			Follower.spline = null;
			base.transform.position = new Vector3(VisualBottom.x, base.SpawnPosition.y);
		});
	}

	private void PunishLanding(SplineComputer spline, int duration, float xValue, Ease ease)
	{
		spline.transform.position = new Vector3(VisualBottom.x, base.SpawnPosition.y);
		spline.transform.Flip(FacingDirection.GetSignX());
		Follower.spline = spline;
		Vector2 direction = (Enemy.Player.VisualBottom - base.Position).normalized;
		SplinePoint point = spline.GetPoint(1);
		point.SetPositionX(Enemy.Player.VisualBottom.x + xValue * (float)(-direction.x.GetSign()));
		spline.SetPoint(1, point);
		Follower.DOFollow(Util.FrameToSeconds(duration)).SetEase(ease).OnUpdate(delegate
		{
			if (Mathf.Abs(Enemy.Player.Position.x - base.Position.x) > xValue)
			{
				point.SetPositionX(Enemy.Player.VisualBottom.x + xValue * (float)(-direction.x.GetSign()));
				spline.SetPoint(1, point);
				Vector3 pointTangent = spline.GetPointTangent(1);
				Vector3 pointTangent2 = spline.GetPointTangent2(1);
				spline.SetPointTangents(1, new Vector3(point.position.x, pointTangent.y), new Vector3(point.position.x, pointTangent2.y));
			}
		})
			.OnComplete(delegate
			{
				Follower.spline = null;
				base.transform.position = new Vector3(VisualBottom.x, base.SpawnPosition.y);
			});
	}

	private void PunishFly(int duration)
	{
		punishSpline.transform.position = VisualBottom;
		punishSpline.transform.Flip(FacingDirection.GetSignX());
		Follower.spline = punishSpline;
		Follower.DOFollow(Util.FrameToSeconds(duration)).SetEase(punishFlyEase).OnComplete(delegate
		{
			Follower.spline = null;
		});
	}

	private void MercyJump(int duration, float jumpPower)
	{
		ShortcutExtensions.DOJump(endValue: new Vector2(Enemy.Player.VisualBottom.x, base.SpawnPosition.y), target: base.transform, jumpPower: jumpPower, numJumps: 1, duration: Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private async UniTaskVoid ThrowPlayer(float xForce, float yForce, float gravityScale)
	{
		Enemy.Player.SetGravityFree(active: true);
		Enemy.Player.ResetLastHitDirection();
		Vector2 velocity = new Vector2(xForce * (float)(-FacingDirection.GetSignX()), yForce);
		Vector2 startVelocity = velocity;
		float leftBound = (float)MyArea.AreaBox.LowerLeft.x + Enemy.Player.ColliderBounds.size.x * 2f;
		float rightBound = (float)MyArea.AreaBox.UpperRight.x - Enemy.Player.ColliderBounds.size.x * 2f;
		Enemy.Player.Position += -FacingDirection.ToVector2() * 2f;
		Enemy.Player.Flip(FacingDirection);
		while (Enemy.Player.VisualBottom.x > leftBound && Enemy.Player.VisualBottom.x < rightBound && !(Enemy.Player.PSM.CurrentState is PlayerRollingState))
		{
			if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState playerStaggerHitState)
			{
				playerStaggerHitState.TargetVelocity = velocity;
			}
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
			velocity += Physics2D.gravity * (gravityScale * Time.fixedDeltaTime);
			if (Enemy.Player.IsOnGround)
			{
				startVelocity *= dampMultiplier;
				velocity = startVelocity;
				if (velocity == Vector2.zero)
				{
					Enemy.Player.SetGravityFree(active: false);
					return;
				}
			}
		}
		Enemy.Player.SetGravityFree(active: false);
	}

	private async UniTaskVoid SpreadBlade(int count, int interval, float speed)
	{
		float leftDelta = mercyP2MinAngle / ((float)count / 2f);
		float rightDelta = mercyP2MaxAngle / ((float)count / 2f);
		float leftAngle = 0f;
		float rightAngle = 0f;
		List<IDamageable> list = new List<IDamageable>();
		for (int i = 0; i < count / 2; i++)
		{
			Spread(leftAngle, speed, list);
			Spread(rightAngle, speed, list);
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(interval);
			leftAngle += leftDelta;
			rightAngle += rightDelta;
		}
	}

	private void Spread(float angle, float speed, List<IDamageable> list)
	{
		Vector3 vector = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
		Vector3 vector2 = mercyP2Transform.position + new Vector3(vector.x * (float)(-FacingDirection.GetSignX()), vector.y, vector.z);
		Vector3 normalized = (vector2 - mercyP2Transform.position).normalized;
		GaiaBlade gaiaBlade = Enemy.PoolManager.GetObject<GaiaBlade>("Gaia_Blade.prefab", mercyP2Transform.position + normalized * mercyPhase2FailRadius);
		gaiaBlade.Gaia = this;
		gaiaBlade.SetAlpha(1f);
		gaiaBlade.Rotate(vector2);
		gaiaBlade.Launch(normalized, speed, MyArea.AreaBox, list).Forget();
	}

	private void ShotBlade(float speed, Transform shotTransform, EventReference eventReference, bool followPlayer = false)
	{
		if (base.Phase < 2)
		{
			return;
		}
		List<IDamageable> list = new List<IDamageable>();
		if (followPlayer)
		{
			shotTransform.position = new Vector3(Enemy.Player.VisualBottom.x, base.SpawnPosition.y + tumbleP2YOffset);
			shotTransform.Flip((Enemy.Player.VisualBottom - VisualBottom).x.GetSign());
		}
		foreach (Transform item in shotTransform)
		{
			GaiaBlade gaiaBlade = Enemy.PoolManager.GetObject<GaiaBlade>("Gaia_Blade.prefab", shotTransform.position);
			gaiaBlade.Gaia = this;
			gaiaBlade.SetAlpha(1f);
			SplineComputer component = item.GetComponent<SplineComputer>();
			component.RebuildImmediate();
			Vector3 normalized = (component[component.sampleCount - 1].position - shotTransform.position).normalized;
			Physics2D.Raycast(shotTransform.position + new Vector3(0f, 0.1f), normalized, float.PositiveInfinity, LayerUtility.PlatformLayer);
			gaiaBlade.Rotate(shotTransform.position + normalized);
			gaiaBlade.Launch(normalized, speed, MyArea.AreaBox, list).Forget();
		}
		Enemy.SoundManager.PlayOneShotAttached(eventReference, base.transform);
	}

	private void ShotBlade(string address, float speed, Transform shotTransform, bool followPlayer = false)
	{
		if (base.Phase < 2)
		{
			return;
		}
		List<IDamageable> list = new List<IDamageable>();
		if (followPlayer)
		{
			shotTransform.position = new Vector3(Enemy.Player.VisualBottom.x, base.SpawnPosition.y + tumbleP2YOffset);
			shotTransform.Flip((Enemy.Player.VisualBottom - VisualBottom).x.GetSign());
		}
		foreach (Transform item in shotTransform)
		{
			GaiaBlade gaiaBlade = Enemy.PoolManager.GetObject<GaiaBlade>(address, shotTransform.position);
			gaiaBlade.Gaia = this;
			gaiaBlade.SetAlpha(1f);
			SplineComputer component = item.GetComponent<SplineComputer>();
			component.RebuildImmediate();
			Vector3 normalized = (component[component.sampleCount - 1].position - shotTransform.position).normalized;
			Physics2D.Raycast(shotTransform.position + new Vector3(0f, 0.1f), normalized, float.PositiveInfinity, LayerUtility.PlatformLayer);
			gaiaBlade.Rotate(shotTransform.position + normalized);
			gaiaBlade.Launch(normalized, speed, MyArea.AreaBox, list).Forget();
		}
	}

	private void BloomShotBlade(string address, int minSpeed, int maxSpeed, Transform shotTransform)
	{
		List<IDamageable> list = new List<IDamageable>();
		List<GaiaBlade> blades = new List<GaiaBlade>();
		int num = 0;
		foreach (Transform item in shotTransform)
		{
			num++;
			GaiaBlade blade = Enemy.PoolManager.GetObject<GaiaBlade>(address, shotTransform.position);
			blade.Gaia = this;
			blade.SetAlpha(1f);
			blades.Add(blade);
			SplineComputer component = item.GetComponent<SplineComputer>();
			component.RebuildImmediate();
			Vector3 normalized = (component[component.sampleCount - 1].position - shotTransform.position).normalized;
			blade.Rotate(shotTransform.position + normalized);
			if (num == shotTransform.childCount / 2)
			{
				float speed = (((float)maxSpeed <= 60f) ? 1f : ((float)maxSpeed * Time.fixedDeltaTime));
				blade.Launch(normalized, maxSpeed, MyArea.AreaBox, list).Forget();
				blade.OnFlying += delegate
				{
					blade.OverlapSize = new Vector2(blades[0].transform.position.y - base.SpawnPosition.y, blade.Box2D.size.y * speed);
				};
				blade.IsAttack = true;
			}
			else
			{
				blade.Launch(normalized, CryptoRandom.Range(minSpeed, maxSpeed), MyArea.AreaBox, list).Forget();
				blade.IsAttack = false;
			}
		}
	}

	private void EmitPunishParticle()
	{
		punishXPos = base.Position.x;
		Enemy.PoolManager.GetObject<PoolingParticle>("vfx_Gaia_Punish_ThrustAttack.prefab", new Vector2(punishXPos, base.SpawnPosition.y)).Play();
		Enemy.PoolManager.GetObject<PoolingParticle>("vfx_Gaia_Punish_Floor_Crack.prefab", new Vector2(punishXPos, base.SpawnPosition.y)).Play();
		Enemy.PoolManager.GetObject<PoolingParticle>("vfx_Gaia_Punish_PieceFloor.prefab", new Vector2(punishXPos, base.SpawnPosition.y)).Play();
		Enemy.PoolManager.GetObject<PoolingParticle>("vfx_Gaia_Punish_FloorAttack.prefab", new Vector2(punishXPos, base.SpawnPosition.y)).Play();
	}

	private void EmitPunishDisappearParticle()
	{
		Enemy.PoolManager.GetObject<PoolingParticle>("vfx_Gaia_Punish_Disappear.prefab", new Vector2(punishXPos, base.SpawnPosition.y)).Play();
	}

	public bool CanMarkPlayer(IDamageable damageable)
	{
		if (damageable is PlayerMover player)
		{
			return CanMarkPlayer(player);
		}
		return false;
	}

	private bool CanMarkPlayer(PlayerMover player)
	{
		if (!player.PSM.StateOption.HasFlag(StateOption.Guard) && !player.PSM.StateOption.HasFlag(StateOption.Parry))
		{
			return player.CanHit;
		}
		return false;
	}

	public async UniTaskVoid MarkToPlayer()
	{
		if ((object)mark == null)
		{
			mark = await AddressableHelper.InstantiateAsync<GaiaMark>("Gaia_Mark.prefab");
		}
		mark.SetAttackFrame(markAttackFrame);
		mark.AddCount(markDuration);
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Quaternion quaternion = Quaternion.Euler(0f, 0f, mercyP2MinAngle);
		Quaternion quaternion2 = Quaternion.Euler(0f, 0f, mercyP2MaxAngle);
		Vector3 vector = quaternion * Vector3.up * 10f;
		Vector3 vector2 = quaternion2 * Vector3.up * 10f;
		Vector3 vector3 = base.transform.position + mercyP2Transform.localPosition;
		Vector3 localScale = base.transform.localScale;
		Gizmos.color = Color.blue;
		Gizmos.DrawLine(vector3, vector3 + new Vector3(vector.x * (float)(-localScale.x.GetSign()), vector.y, vector.z));
		Gizmos.color = Color.red;
		Gizmos.DrawLine(vector3, vector3 + new Vector3(vector2.x * (float)(-localScale.x.GetSign()), vector2.y, vector2.z));
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(mercyP2Transform.position, mercyPhase2FailRadius);
	}

	public void DisablePhaseHitAnim()
	{
		base.SkeletonAnimation.AnimationState.SetEmptyAnimation(1, 0f);
	}

	public void SetDef(int value)
	{
		def = value;
	}
}
