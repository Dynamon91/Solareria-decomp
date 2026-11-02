using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.FSM.Base;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class SeedKeeper : BossCharacter
{
	[Header("2페이즈")]
	[SerializeField]
	private float phase2StartFatigue = 3f;

	[Header("나무 방패")]
	[SerializeField]
	private int defaultDefense;

	[SerializeField]
	private int woodShieldDefense;

	[SerializeField]
	private int woodShieldRegenFrame;

	[Header("무게 추 휘두르기")]
	[SerializeField]
	private float macePlayerDistance;

	[SerializeField]
	private float maceJumpHeightPerFrame;

	[SerializeField]
	private int maceJumpFallFrame;

	[Header("주먹 휘두르기")]
	[SerializeField]
	private float fistPlayerDistance;

	[Header("잡기")]
	[SerializeField]
	private Zone grabZone;

	[SerializeField]
	private float grabCalibration;

	[SerializeField]
	private float grabJumpPower;

	[SerializeField]
	private int grabJumpDuration;

	[SerializeField]
	private float maxDistance;

	[Header("Sequence")]
	[SerializeField]
	private GenericBossAttackSequence<SeedKeeper> macePhase1;

	[SerializeField]
	private GenericBossAttackSequence<SeedKeeper> macePhase2;

	[SerializeField]
	private GenericBossAttackSequence<SeedKeeper> fist;

	[SerializeField]
	private GenericBossAttackSequence<SeedKeeper> thrust;

	[SerializeField]
	private GenericBossAttackSequence<SeedKeeper> leapAttack;

	[SerializeField]
	private GenericBossAttackSequence<SeedKeeper> grab;

	[SerializeField]
	private GenericBossAttackSequence<SeedKeeper> grabLoop;

	[SerializeField]
	private GenericBossAttackSequence<SeedKeeper> tackle;

	[Header("Sound")]
	[SerializeField]
	private EventReference hitEvent;

	private Vector2 start;

	public const int OpeningStateIndex = 0;

	public const int RetryStateIndex = 1;

	public const int PhaseTransitionStateIndex = 2;

	public const int DyingStateIndex = 3;

	public const int ClearState = 4;

	private SeedKeeperWoodShield woodShield;

	private SeedKeeperVine vine;

	public override string Name => "SeedKeeper";

	private PhaseTransitionCutsceneState PhaseTransitionState { get; set; }

	private CutsceneState DyingState { get; set; }

	public Vector2 End { get; private set; }

	public float FloorY => base.SpawnPosition.y;

	private GenericBossAttackSequence<SeedKeeper> Mace
	{
		get
		{
			if (base.Phase != 1)
			{
				return macePhase2;
			}
			return macePhase1;
		}
	}

	protected override int OriginDef
	{
		get
		{
			if (base.Phase == 1)
			{
				return 0;
			}
			if (!woodShield.IsActive)
			{
				return base.EnemyStatData.Def[0];
			}
			return base.EnemyStatData.Def[1] + base.EnemyStatData.Def[0];
		}
	}

	public override bool CanMakeStuck => woodShield?.IsActive ?? false;

	protected override void Awake()
	{
		PhaseTransitionState = new PhaseTransitionCutsceneState(this, 2, blockControl: true);
		DyingState = new CutsceneState(this, 3);
		base.Awake();
		base.StateMachine.ChangeState(base.IdleState);
		TimelineActions.Add("MaceJump", delegate
		{
			base.transform.position += new Vector3(0f, maceJumpHeightPerFrame);
		});
		TimelineBeginActions.Add("MaceJumpFalling", delegate
		{
			base.transform.DOMoveY(base.SpawnPosition.y, Util.FrameToSeconds(maceJumpFallFrame));
		});
		AddAction(ActionTiming.Begin, "Leap", delegate(int duration, Parameter[] p)
		{
			Leap(duration, p[0].Float);
		});
		AddAction(ActionTiming.Begin, "Rotate", delegate(int duration, Parameter[] p)
		{
			DoRotate(duration, p[0].Float);
		});
		AddAction(ActionTiming.Begin, "DropToPlayer", DropToPlayer);
		AddAction(ActionTiming.Begin, "AdjustAfterDrop", AdjustAfterDrop);
		AddAction(ActionTiming.Begin, "Grab", delegate(Parameter[] p)
		{
			Grab(p[0].Int, p[1].Int).Forget();
		});
		AddAction(ActionTiming.Begin, "GrabHeal", delegate(Parameter[] p)
		{
			GrabHeal(p[0].Int, p[1].Int);
		});
		AddAction(ActionTiming.Begin, "Shield", GenerateShield);
	}

	private void LateUpdate()
	{
		AdjustXPosition();
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

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { Mace, fist, thrust, leapAttack, leapAttack, grab };
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		return SelectAttackPattern();
	}

	public override void OnEnterExecution()
	{
		base.OnEnterExecution();
		base.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
		ForceDrop();
	}

	protected override void OnEnterGroggyState()
	{
		base.OnEnterGroggyState();
		base.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
		ForceDrop();
	}

	protected override void OnDamaged(HitInfo hitInfo)
	{
		if (!CanMakeStuck)
		{
			base.OnDamaged(hitInfo);
			Game.Instance.SoundManager.PlayOneShotAttached("event:/Creature/SFX_Creature_Hit", base.transform).setParameterByName("Creature_Material", 4f);
		}
		else
		{
			TryExplodeDashFlame();
			Enemy.SoundManager.PlayOneShotAttached(hitEvent, base.transform);
		}
	}

	protected override void OnDie()
	{
		ClearPlayerStagger();
		vine?.ReturnToPool();
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.HeavyAttack))
		{
			woodShield?.Break();
		}
		return base.TryTakeDamage(hitInfo);
	}

	protected override void OnIncreasePhase(int before, int after)
	{
		SetSkin("Phase2");
		woodShield = GetComponentInChildren<SeedKeeperWoodShield>();
		woodShield.Initialize();
		base.Fatigue = phase2StartFatigue;
	}

	protected override CutsceneState GetTargetState(int index)
	{
		return index switch
		{
			-1 => DyingState, 
			2 => PhaseTransitionState, 
			3 => DyingState, 
			_ => null, 
		};
	}

	public override void AddStunCount(int value)
	{
		base.AddStunCount(value);
		if (base.CurrentStun == 0)
		{
			base.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
			ForceDrop();
		}
	}

	private Vector2 GetLerpDelta(int duration)
	{
		return (End - start) / duration;
	}

	private void DoRotate(int duration, float angleAdjustment)
	{
		DOTween.Kill(base.transform);
		Vector2 normalized = (Enemy.Player.VisualCenter - VisualCenter).normalized;
		float num = Vector2.SignedAngle(Vector2.up, normalized);
		num += (float)(-num.GetSign()) * angleAdjustment;
		base.transform.DORotate(new Vector3(0f, 0f, num), Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private void Rotate()
	{
		Vector2 normalized = (Enemy.Player.VisualCenter - VisualCenter).normalized;
		float z = Vector2.SignedAngle(Vector2.up, normalized);
		base.transform.rotation = Quaternion.Euler(0f, 0f, z);
	}

	public void ResetRotate()
	{
		base.transform.rotation = Quaternion.identity;
	}

	private void SetShieldPosition(Vector2 position)
	{
		if ((object)woodShield != null)
		{
			woodShield.transform.position = position;
		}
	}

	private void Leap(int duration, float height)
	{
		base.transform.DOMove(VisualCenter + new Vector2(0f, height), Util.FrameToSeconds(duration)).SetEase(Ease.Linear).OnUpdate(delegate
		{
			SetShieldPosition(VisualBottom);
		});
	}

	private void DropToPlayer(int duration)
	{
		DOTween.Kill(base.transform);
		Rotate();
		start = VisualBottom;
		End = new Vector2(Enemy.Player.VisualBottom.x, FloorY);
		base.transform.DOMove(End, Util.FrameToSeconds(duration)).SetEase(Ease.Linear).OnUpdate(delegate
		{
			SetShieldPosition(VisualBottom);
		})
			.OnComplete(delegate
			{
				SetShieldPosition(VisualCenter);
			});
	}

	private void AdjustAfterDrop(int duration)
	{
		Vector2 lerpDelta = GetLerpDelta(duration);
		base.transform.position = End - new Vector2(lerpDelta.x, 0f);
		ResetRotate();
		SetShieldPosition(VisualCenter);
	}

	private async UniTaskVoid Grab(int duration, int grabTiming)
	{
		vine = Enemy.PoolManager.GetObject<SeedKeeperVine>("SeedKeeper_Vine.prefab", new Vector3(Enemy.Player.VisualBottom.x, base.SpawnPosition.y));
		Enemy.PoolManager.GetObject<PoolingParticle>("vfx_SeedKeeper_Floor_Smoke_03.prefab", base.Position).Play();
		vine.EnableVFX("grab");
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(grabTiming);
		BoxCollider2D component = vine.GetComponent<BoxCollider2D>();
		Collider2D[] array = AttackUtility.OverlapBoxAll((Vector2)vine.transform.position + component.offset, component.size, LayerUtility.PlayerLayer);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].TryGetComponent<PlayerMover>(out var _) && Enemy.Player.CanHit)
			{
				Enemy.Player.Position = vine.Position;
				Enemy.Player.PSM.ChangeState(new PlayerStaggerHitState(Enemy.Player.PSM)
				{
					Duration = duration,
					NextState = Enemy.Player.PSM.IdleState,
					Animation = "seedkeeper_grab"
				});
				base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { grabLoop }));
				vine.EnableVFX("drain");
				Type type = GetType();
				BossCharacter.BossBoard.PlayHealTween(loop: true, type);
				await UniTaskExtension.WaitForFixedUpdatesOrCancel(vine.Duration - 60);
				BossCharacter.BossBoard.StopHealTween(type);
				await UniTaskExtension.WaitForFixedUpdatesOrCancel(60);
				vine.ReturnToPool();
				break;
			}
		}
	}

	private void GrabHeal(int stunCount, int hp)
	{
		BoxCollider2D component = vine.GetComponent<BoxCollider2D>();
		AttackUtility.AttackBox((Vector2)vine.transform.position + component.offset, component.size, new HitInfo(this, "Grab"), LayerUtility.PlayerLayer);
		AddStunCount(stunCount);
		health += hp;
		OnChangeHealth(hp);
	}

	private void GenerateShield()
	{
		woodShield?.Generate();
	}

	private List<BossAttackSequence> SelectAttackPattern()
	{
		List<int> list = new List<int>();
		list.AddRange(Enumerable.Repeat(0, thrust.GetPower(this)));
		list.AddRange(Enumerable.Repeat(1, Mace.GetPower(this)));
		list.AddRange(Enumerable.Repeat(2, fist.GetPower(this)));
		list.AddRange(Enumerable.Repeat(3, grab.GetPower(this)));
		int num = list[CryptoRandom.Range(0, list.Count)];
		List<BossAttackSequence> list2 = new List<BossAttackSequence> { thrust, Mace, fist, grab, leapAttack };
		for (int i = 0; i < list2.Count; i++)
		{
			if (i != num && (list2[i] != grab || (base.AttackState.LastSequence != grab && base.AttackState.LastSequence != grabLoop)) && (list2[i] != leapAttack || base.AttackState.LastSequence != leapAttack))
			{
				list2[i].CastObject<BossAttackSequence>().RisePower();
			}
		}
		switch (num)
		{
		case 0:
		{
			List<int> list3 = new List<int>();
			list3.AddRange(Enumerable.Repeat(0, leapAttack.GetPower(this)));
			list3.AddRange(Enumerable.Repeat(1, grab.GetPower(this)));
			int index = CryptoRandom.Range(0, list3.Count);
			switch (list3[index])
			{
			case 0:
				return GetLeap();
			case 1:
				return GetGrab();
			}
			break;
		}
		case 1:
			return new List<BossAttackSequence> { Mace };
		case 2:
			return new List<BossAttackSequence> { fist };
		case 3:
			return new List<BossAttackSequence> { grab };
		}
		return new List<BossAttackSequence>();
	}

	private List<BossAttackSequence> GetLeap()
	{
		if (base.Phase != 1)
		{
			return new List<BossAttackSequence> { thrust, thrust, leapAttack };
		}
		return new List<BossAttackSequence> { thrust, leapAttack };
	}

	private List<BossAttackSequence> GetGrab()
	{
		if (base.Phase != 1)
		{
			return new List<BossAttackSequence> { thrust, thrust, grab };
		}
		return new List<BossAttackSequence> { thrust, grab };
	}
}
