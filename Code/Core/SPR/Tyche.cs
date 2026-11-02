using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.FSM.Base;
using SPR.Field;
using SPR.Library;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class Tyche : BossCharacter
{
	public const int OpeningStateIndex = 0;

	public const int RetryInFirstPhaseStateIndex = 1;

	public const int SecondPhaseStateIndex = 2;

	public const int RetryInSecondPhaseStateIndex = 3;

	public const int DefeatStateIndex = 4;

	public const int AfterEndingStateIndex = 5;

	[Header("강한 박치기")]
	[SerializeField]
	private Ease strongButtEase;

	[Header("패시브 - 실체화")]
	[SerializeField]
	private int swapChance;

	[Header("패시브 - 바람화")]
	[SerializeField]
	private int breathSwapChance;

	[SerializeField]
	private float swapFatigue;

	[SerializeField]
	private int swapMoveRange;

	[Header("직선 브레스")]
	[SerializeField]
	private float straightBreathRadius = 10f;

	[SerializeField]
	private float straightBreathStart;

	[SerializeField]
	private float straightBreathEnd;

	[SerializeField]
	private Vector3 breathAimOriginPosition;

	[Header("Sequence")]
	[SerializeField]
	private GenericBossAttackSequence<Tyche> cloneButt;

	[SerializeField]
	private GenericBossAttackSequence<Tyche> strongButt;

	[SerializeField]
	private GenericBossAttackSequence<Tyche> compressionBreath;

	[SerializeField]
	private GenericBossAttackSequence<Tyche> biteDig;

	[SerializeField]
	private GenericBossAttackSequence<Tyche> biteOut;

	[SerializeField]
	private GenericBossAttackSequence<Tyche> bite;

	[SerializeField]
	private GenericBossAttackSequence<Tyche> breathSwapMove;

	[SerializeField]
	private GenericBossAttackSequence<Tyche> straightBreathMove;

	[SerializeField]
	private GenericBossAttackSequence<Tyche> straightBreath;

	[Header("Reference")]
	[SerializeField]
	private DynamicHitBox dynamicHitBox;

	[SerializeField]
	private Transform straightBreathAim;

	[SerializeField]
	private CinemachineImpulseSource hitWallImpulse;

	[SerializeField]
	private Transform swapEffectTransform;

	private TycheClone clone;

	private bool isBreathCanceled;

	private bool stopBreathPattern;

	private Vector3 straightBreathPosition;

	private bool swapActive;

	public override string Name => "Tyche";

	public override string GroggyLoopAnim => "groggy";

	protected override BoxCollider2D HitBox => dynamicHitBox.Box2D;

	private PhaseTransitionCutsceneState PhaseTransitionState { get; set; }

	private CutsceneState DefeatState { get; set; }

	private TycheClone Clone
	{
		get
		{
			return clone;
		}
		set
		{
			clone = value;
			clone.SetMainBody(this);
		}
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		PhaseTransitionState = new PhaseTransitionCutsceneState(this, 2, blockControl: false, hideCanvas: false);
		DefeatState = new CutsceneState(this, 4);
		base.StateMachine.ChangeState(base.IdleState);
		dynamicHitBox.SetEnemy(this);
		AddAction(ActionTiming.Begin, "SpawnClone", SummonClone);
		AddAction(ActionTiming.Begin, "ShootAirCannon", delegate(int _, Parameter[] param)
		{
			ShootAirCannon(param[0].Float, param[1].Float, param[2].Int, param[3].Int);
		});
		AddAction(ActionTiming.Begin, "Dig", Dig);
		AddAction(ActionTiming.Begin, "Out", Out);
		AddAction(ActionTiming.Begin, "HoldPlayer", HoldPlayerBegin);
		AddAction(ActionTiming.OnFrame, "HoldPlayer", HoldPlayer);
		AddAction(ActionTiming.Begin, "ReleasePlayer", delegate(int duration, Parameter[] p)
		{
			ReleasePlayer(duration, p[0].Float, p[1].Float, p[2].Ease, p[3].String);
		});
		AddAction(ActionTiming.Begin, "SwapWithClone", EmitSwapEffect);
		AddAction(ActionTiming.End, "SwapWithClone", SwapWithClone);
		AddAction(ActionTiming.Begin, "BreathSwapWithClone", BreathSwapWithClone);
		AddAction(ActionTiming.Begin, "SwapRandomMove", SwapRandomMove);
		AddAction(ActionTiming.OnFrame, "AimPlayer", AimPlayer);
		AddAction(ActionTiming.Begin, "StraightBreath", delegate(int duration, Parameter[] p)
		{
			StraightBreath(duration, p[0].Int).Forget();
		});
		AddAction(ActionTiming.OnFrame, "CheckStraightBreathPosition", (Action<int, Parameter[]>)delegate
		{
			CheckStraightBreathPosition();
		});
		AddAction(ActionTiming.Begin, "MoveStraightBreathPosition", (Action<int, Parameter[]>)delegate
		{
			MoveStraightBreathPosition();
		});
		strongButt.OnStart += ResetSaturation;
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

	public override void OnAttackEnd(IState nextState)
	{
		if (stopBreathPattern)
		{
			stopBreathPattern = false;
		}
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (hitInfo.AttackName.Contains("BiteOut"))
		{
			if (Enemy.Player.CanParry(hitInfo))
			{
				base.BounceAnim = "bite_parry";
				base.StateMachine.ChangeState(new EnemyBounceState(this, Util.SecondsToFrame(GetAnimDuration("bite_parry")), useVFX: true, resetStun: false));
			}
			else
			{
				BossAttackState bossAttackState = base.AttackState.CastObject<BossAttackState>();
				bossAttackState.AddAttackSequence(new List<BossAttackSequence> { bite });
				bossAttackState.ShiftNextSequence();
			}
		}
		if (hitInfo.AttackName.Contains("StrongButt") && Enemy.Player.PSM.StateOption.HasFlag(StateOption.Parry) && base.CurrentStun > 0)
		{
			base.BounceAnim = "parry";
			base.StateMachine.ChangeState(new EnemyBounceState(this, Util.SecondsToFrame(GetAnimDuration(base.BounceAnim)), useVFX: true, resetStun: false));
			DropMoney();
			HitInfo hitInfo2 = new HitInfo(this, "StrongButtFail");
			TryTakeDamage(new HitInfo(Enemy.Player, hitInfo2.Damage, 0f, 0f, AttackDirection.Facing, hitInfo2.AttackAttribute));
		}
	}

	public override void OnPlatformHit(HitInfo hitInfo)
	{
		if (!(base.StateMachine.CurrentState is EnemyBounceState))
		{
			float num = base.Position.x + (float)FacingDirection.GetSignX() * strongButt.GetSightZone().Size.x;
			if (num <= (float)MyArea.AreaBox.LowerLeft.x || num >= (float)MyArea.AreaBox.UpperRight.x)
			{
				base.BounceAnim = "strong_butt_fail";
				base.StateMachine.ChangeState(new EnemyBounceState(this, Util.SecondsToFrame(GetAnimDuration(base.BounceAnim)), useVFX: true, resetStun: false));
				DropMoney();
				HitInfo hitInfo2 = new HitInfo(this, "StrongButtFail");
				TryTakeDamage(new HitInfo(Enemy.Player, hitInfo2.Damage, 0f, 0f, AttackDirection.Facing, hitInfo2.AttackAttribute));
				hitWallImpulse.GenerateImpulse();
			}
		}
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { cloneButt, strongButt, compressionBreath, biteDig };
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		return GetRandomSequence();
	}

	protected override CutsceneState GetTargetState(int index)
	{
		return index switch
		{
			-1 => DefeatState, 
			2 => PhaseTransitionState, 
			4 => DefeatState, 
			_ => null, 
		};
	}

	protected override void OnDie()
	{
		Clone?.ReturnToPool();
		ClearPlayerStagger();
	}

	private List<BossAttackSequence> GetRandomSequence()
	{
		base.BounceAnim = "parry";
		if (isBreathCanceled)
		{
			stopBreathPattern = true;
			isBreathCanceled = false;
		}
		List<int> list = new List<int>();
		list.AddRange(Enumerable.Repeat(0, cloneButt.GetPower(this)));
		list.AddRange(Enumerable.Repeat(1, strongButt.GetPower(this)));
		if (!stopBreathPattern)
		{
			list.AddRange(Enumerable.Repeat(2, compressionBreath.GetPower(this)));
		}
		list.AddRange(Enumerable.Repeat(3, biteDig.GetPower(this)));
		list.AddRange(Enumerable.Repeat(4, straightBreathMove.GetPower(this)));
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
			0 => new List<BossAttackSequence> { cloneButt }, 
			1 => new List<BossAttackSequence> { strongButt }, 
			2 => new List<BossAttackSequence> { compressionBreath }, 
			3 => new List<BossAttackSequence> { biteDig, biteOut }, 
			4 => new List<BossAttackSequence> { straightBreathMove }, 
			_ => new List<BossAttackSequence>(), 
		};
	}

	private void SummonClone()
	{
		Clone = Enemy.PoolManager.GetObject<TycheClone>("TycheClone.prefab", base.transform.position);
		Clone.Area = MyArea;
		Clone.Butt();
	}

	private void ShootAirCannon(float speed, float maxHeight, int cycle, int duration)
	{
		Vector2 vector = GetBoneWorldPosition("mouth");
		Enemy.PoolManager.GetObject<TycheAirCannon>("TycheAirCannon.prefab", vector).Shoot(speed, maxHeight, new Vector2(Enemy.Player.VisualBottom.x, base.SpawnPosition.y), cycle, duration);
	}

	private void EmitSwapEffect()
	{
		swapActive = false;
		if (base.Phase >= 2 && CryptoRandom.Range(1, 101) <= swapChance)
		{
			swapActive = true;
			Enemy.PoolManager.GetObject<PoolableTimeLimitObject>("Tyche_Cancel.prefab", Clone.SwapEffectTransform.position).Initialize("Tyche_Cancel.prefab", 1f);
		}
	}

	private void SwapWithClone()
	{
		if (swapActive && (object)Clone != null && Clone.gameObject.activeSelf)
		{
			Transform obj = base.transform;
			Transform transform = Clone.transform;
			Vector3 position = Clone.transform.position;
			Vector3 position2 = base.transform.position;
			Vector3 vector = (obj.position = position);
			vector = (transform.position = position2);
			Enemy.PoolManager.GetObject<PoolableTimeLimitObject>("Tyche_Cancel.prefab", swapEffectTransform.position).Initialize("Tyche_Cancel.prefab", 1f);
			base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { strongButt }));
			Clone.RemoveAnimation();
			Clone.Butt();
		}
	}

	private void BreathSwapWithClone()
	{
		if (base.Phase >= 2 && CryptoRandom.Range(1, 101) <= breathSwapChance)
		{
			isBreathCanceled = true;
			Clone = Enemy.PoolManager.GetObject<TycheClone>("TycheClone.prefab", VisualBottom);
			Clone.SortingOrder = Enemy.PoolManager.GetActiveObjectCount("TycheClone.prefab");
			if (base.AttackState.CurrentSequence == compressionBreath)
			{
				Clone.SetCompressionBreath();
			}
			base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { breathSwapMove }));
			base.Fatigue = swapFatigue;
		}
	}

	private void SwapRandomMove(int duration)
	{
		float endValue = Mathf.Clamp(VisualBottom.x + (float)CryptoRandom.Range(-swapMoveRange, swapMoveRange), MyArea.AreaBox.LowerLeft.x, MyArea.AreaBox.UpperRight.x);
		base.transform.DOMoveX(endValue, Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private void AimPlayer()
	{
		Bone bone = base.SkeletonAnimation.Skeleton.FindBone("target");
		Bone bone2 = base.SkeletonAnimation.Skeleton.FindBone("mouth");
		Vector3 vector = Enemy.Player.VisualCenter;
		Vector3 worldPosition = bone2.GetWorldPosition(base.transform);
		Vector3 vector2 = (vector - worldPosition).normalized * 10f;
		vector2 = new Vector3(Mathf.Abs(vector2.x), vector2.y + 1f);
		Vector3 vector3 = bone2.GetWorldPosition(base.transform) - base.transform.position;
		bone.SetPositionSkeletonSpace(vector3 + vector2);
	}

	private async UniTaskVoid StraightBreath(int duration, int waitDuration)
	{
		Vector3 boneWorldPosition = GetBoneWorldPosition("mouth");
		PoolingParticle breath = Enemy.PoolManager.GetObject<PoolingParticle>("Tyche_StraightBreath.prefab", boneWorldPosition);
		List<IDamageable> list = new List<IDamageable>();
		int frame = 0;
		float arcAngle = straightBreathStart;
		float angleDelta = (straightBreathEnd - straightBreathStart) / (float)(duration - waitDuration);
		straightBreathAim.position = GetArcPosition(arcAngle);
		for (; frame < duration; frame++)
		{
			if (frame >= waitDuration - 1)
			{
				straightBreathAim.position = GetArcPosition(arcAngle);
				arcAngle += angleDelta;
			}
			boneWorldPosition = GetBoneWorldPosition("mouth");
			breath.transform.position = boneWorldPosition;
			Vector3 normalized = (straightBreathAim.position - boneWorldPosition).normalized;
			float num = Vector2.SignedAngle(Vector2.up, normalized);
			Quaternion quaternion = Quaternion.Euler(0f, 0f, num);
			breath.transform.rotation = quaternion;
			breath.Play();
			BoxCollider2D component = breath.GetComponent<BoxCollider2D>();
			AttackUtility.AttackBox(breath.transform.position + quaternion * component.offset, component.size, num, new HitInfo(this, "StraightBreath"), LayerUtility.PlayerLayer, list);
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
		breath.Stop(withChildren: true);
		straightBreathAim.localPosition = breathAimOriginPosition;
	}

	private void ReleasePlayer(int duration, float distance, float jumpPower, Ease ease, string playerAnimation)
	{
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/Player/Combat/SFX_Player_Parrying");
		Enemy.Player.SetAnimation(playerAnimation, loop: false, 1f, 0, 0f, vfxEnable: false);
		int signX = FacingDirection.GetSignX();
		float x = Mathf.Clamp(Enemy.Player.Position.x + (float)signX * distance, MyArea.AreaBox.LowerLeft.x, MyArea.AreaBox.UpperRight.x);
		float y = base.SpawnPosition.y + 1f;
		ShortcutExtensions.DOJump(endValue: new Vector3(x, y), target: Enemy.Player.transform, jumpPower: jumpPower, numJumps: 1, duration: Util.FrameToSeconds(duration)).SetUpdate(UpdateType.Fixed).SetEase(ease)
			.OnComplete(delegate
			{
				Enemy.Player.SetGravityFree(active: false);
				Enemy.Player.PSM.ChangeState(Enemy.Player.PSM.IdleState);
			});
	}

	private void CheckStraightBreathPosition()
	{
		float num = (float)MyArea.AreaBox.LowerLeft.x + HitBox.size.x;
		float num2 = (float)MyArea.AreaBox.UpperRight.x - HitBox.size.x;
		float num3 = Mathf.Abs(num - VisualBottom.x);
		float num4 = Mathf.Abs(num2 - VisualBottom.x);
		LookAt(((num3 > num4) ? MyArea.AreaBox.UpperRight : MyArea.AreaBox.LowerLeft).ToVector3());
		straightBreathPosition = new Vector3((num3 > num4) ? num2 : num, VisualBottom.y);
		if (VisualBottom.x < num || VisualBottom.x > num2)
		{
			BossAttackState bossAttackState = base.AttackState.CastObject<BossAttackState>();
			bossAttackState.AddAttackSequence(new List<BossAttackSequence> { straightBreath });
			bossAttackState.ShiftNextSequence();
		}
	}

	private void MoveStraightBreathPosition()
	{
		base.transform.position = straightBreathPosition;
	}

	private Vector3 GetArcPosition(float angle)
	{
		Vector3 boneWorldPosition = GetBoneWorldPosition("body_line_2_4");
		Vector3 vector = Quaternion.Euler(0f, 0f, angle) * Vector3.down * straightBreathRadius;
		return boneWorldPosition + new Vector3((float)FacingDirection.GetSignX() * vector.x, vector.y, vector.z);
	}

	private void ResetSaturation()
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(Shader.PropertyToID("_HsvSaturation"), 1f);
		MeshRenderer.SetPropertyBlock(materialPropertyBlock);
	}

	private void DropMoney()
	{
		int minInclusive = Mathf.RoundToInt(7f);
		int num = Mathf.RoundToInt(26f);
		foreach (int randomAmount in SPR.Field.Money.GetRandomAmounts(CryptoRandom.Range(minInclusive, num + 1)))
		{
			Money money = Game.Instance.ObjectPoolManager.GetObject<Money>("Money.prefab", VisualCenter);
			money.Initialize(randomAmount);
			money.gameObject.SetActive(value: true);
		}
	}

	private void Dig(int duration)
	{
		base.transform.DOMoveY(base.transform.position.y - HitBox.size.y, Util.FrameToSeconds(duration)).SetEase(Ease.Linear).OnComplete(delegate
		{
			base.IsImmortal = true;
		});
	}

	private void Out(int duration)
	{
		base.IsImmortal = false;
		base.transform.position = new Vector3(Enemy.Player.VisualBottom.x, base.transform.position.y);
		base.transform.DOMoveY(base.SpawnPosition.y, Util.FrameToSeconds(duration));
	}

	private void HoldPlayerBegin(int duration)
	{
		Enemy.Player.SetGravityFree(active: true);
		PlayerStaggerHitState newState = new PlayerStaggerHitState(Enemy.Player.PSM)
		{
			Duration = duration,
			NextState = Enemy.Player.PSM.IdleState
		};
		Enemy.Player.PSM.ChangeState(newState);
	}

	private void HoldPlayer()
	{
		if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState playerStaggerHitState)
		{
			playerStaggerHitState.TargetPosition = GetBoneWorldPosition("mouth");
		}
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Vector3 boneWorldPosition = GetBoneWorldPosition("body_line_2_4");
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(boneWorldPosition, straightBreathRadius);
		Gizmos.color = Color.blue;
		Gizmos.DrawLine(boneWorldPosition, GetArcPosition(straightBreathStart));
		Gizmos.color = Color.red;
		Gizmos.DrawLine(boneWorldPosition, GetArcPosition(straightBreathEnd));
	}
}
