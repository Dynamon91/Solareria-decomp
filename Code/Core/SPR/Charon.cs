using System;
using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using Library;
using SPR.Field;
using UnityEngine;

namespace SPR;

public class Charon : BossCharacter
{
	private enum Pattern
	{
		None,
		Sweep,
		Rock,
		Punch,
		Smash,
		Energy
	}

	[Header("패턴 디버깅")]
	[SerializeField]
	private Pattern patternDebug;

	[Header("패턴")]
	[SerializeField]
	private GenericBossAttackSequence<Charon> sweepAttack;

	[SerializeField]
	private GenericBossAttackSequence<Charon> rockAttack;

	[SerializeField]
	private GenericBossAttackSequence<Charon> energyAttack;

	[SerializeField]
	private GenericBossAttackSequence<Charon> punchAttack;

	[SerializeField]
	private GenericBossAttackSequence<Charon> smashAttack;

	[Header("SFX")]
	[SerializeField]
	private EventReference hitEvent;

	[Header("파라미터")]
	[SerializeField]
	private float executeFatigue;

	[Header("바위")]
	[SerializeField]
	private EventReference rockSignEvent;

	[SerializeField]
	private CharonRockParameter parameter;

	[SerializeField]
	private int rockAmount;

	[SerializeField]
	private int summonRadius;

	[SerializeField]
	private int rockWidth;

	[SerializeField]
	private float rockSpawnHeight;

	[SerializeField]
	private int penaltyPerRockAmount;

	private readonly List<CharonRock> charonRockList = new List<CharonRock>();

	private Area area;

	private CutsceneState DyingState { get; set; }

	public override string Name => "Charon";

	public override bool CanPullPlayer => false;

	private Area Area => area ?? (area = GetComponentInParent<Area>());

	protected override void Awake()
	{
		base.Awake();
		DyingState = new CutsceneState(this, 3);
		AddAction(ActionTiming.Begin, "PlayRockSound", (Action)delegate
		{
			Game.Instance.SoundManager.PlayOneShot(rockSignEvent);
		});
		AddAction(ActionTiming.Begin, "SummonRock", SummonRock);
	}

	public override void OnHitBurst()
	{
		AddFatigue(executeFatigue);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { smashAttack, rockAttack, energyAttack, punchAttack, sweepAttack };
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		string bounceAnim;
		if (patternDebug != Pattern.None)
		{
			switch (patternDebug)
			{
			case Pattern.Rock:
			case Pattern.Punch:
			case Pattern.Energy:
				bounceAnim = "parry";
				break;
			case Pattern.Sweep:
				bounceAnim = "parry_sweep";
				break;
			case Pattern.Smash:
				bounceAnim = "parry_smash";
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
			base.BounceAnim = bounceAnim;
			return patternDebug switch
			{
				Pattern.Sweep => new List<BossAttackSequence> { sweepAttack }, 
				Pattern.Rock => new List<BossAttackSequence> { rockAttack }, 
				Pattern.Punch => new List<BossAttackSequence> { punchAttack }, 
				Pattern.Smash => new List<BossAttackSequence> { smashAttack }, 
				Pattern.Energy => new List<BossAttackSequence> { energyAttack }, 
				_ => null, 
			};
		}
		if (base.StateMachine.CurrentState == base.GroggyState)
		{
			return new List<BossAttackSequence> { energyAttack };
		}
		List<int> list = new List<int>();
		list.AddRange(Enumerable.Repeat(0, smashAttack.GetPower(this)));
		list.AddRange(Enumerable.Repeat(1, rockAttack.GetPower(() => Mathf.Max(rockAttack.GetPower(this) - penaltyPerRockAmount * Enemy.PoolManager.GetActiveObjectCount("Charon Rock.prefab"), 0))));
		list.AddRange(Enumerable.Repeat(2, energyAttack.GetPower(this)));
		list.AddRange(Enumerable.Repeat(3, punchAttack.GetPower(this)));
		list.AddRange(Enumerable.Repeat(4, sweepAttack.GetPower(this)));
		int num = list[CryptoRandom.Range(0, list.Count)];
		for (int num2 = 0; num2 < base.AllAttackSequences.Count; num2++)
		{
			if (num2 != num)
			{
				base.AllAttackSequences[num2].CastObject<BossAttackSequence>().RisePower();
			}
		}
		switch (num)
		{
		case 1:
		case 2:
		case 3:
			bounceAnim = "parry";
			break;
		case 0:
			bounceAnim = "parry_smash";
			break;
		case 4:
			bounceAnim = "parry_sweep";
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		base.BounceAnim = bounceAnim;
		return num switch
		{
			0 => new List<BossAttackSequence> { smashAttack }, 
			1 => new List<BossAttackSequence> { rockAttack }, 
			2 => new List<BossAttackSequence> { energyAttack }, 
			3 => new List<BossAttackSequence> { punchAttack }, 
			4 => new List<BossAttackSequence> { sweepAttack }, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	protected override void OnEnterGroggyState()
	{
		base.OnEnterGroggyState();
		ResetSymbolVFX();
	}

	protected override void OnExitGroggyState()
	{
		base.OnExitGroggyState();
		DisableVFX("groggy_start");
	}

	protected override void OnDamaged(HitInfo hitInfo)
	{
		TryExplodeDashFlame();
		Enemy.SoundManager.PlayOneShotAttached(hitEvent, base.transform);
	}

	protected override void OnDie()
	{
		ResetSymbolVFX();
		ClearSpawnedRock();
	}

	protected override CutsceneState GetTargetState(int index)
	{
		return index switch
		{
			-1 => DyingState, 
			3 => DyingState, 
			_ => null, 
		};
	}

	protected override bool IsInExecutionRange()
	{
		bool num = (Enemy.Player.IsFacingRight ? (base.NearestEdgePoint.x - Enemy.Player.VisualBottom.x) : (Enemy.Player.VisualBottom.x - base.NearestEdgePoint.x)) <= (float)PlayerControlParameter.Instance.ExecutionEnabledDistance;
		bool flag = HitBox.bounds.Intersects(Enemy.Player.Bounds);
		return num || flag;
	}

	private void SummonRock()
	{
		AreaBox areaBox = Area.AreaBox;
		List<int> list = new List<int>();
		for (int i = areaBox.LowerLeft.x + rockWidth; i <= areaBox.UpperRight.x - rockWidth; i++)
		{
			list.Add(i);
		}
		for (int j = 0; j < rockAmount; j++)
		{
			foreach (CharonRock item in charonRockList.Where((CharonRock o) => o.CastObject<CharonRock>().gameObject.activeSelf).Cast<CharonRock>())
			{
				for (int num = Mathf.RoundToInt(item.VisualBottom.x) - rockWidth; num <= Mathf.RoundToInt(item.VisualBottom.x) + rockWidth; num++)
				{
					list.Remove(num);
				}
			}
			if (list.Count == 0)
			{
				break;
			}
			int index = CryptoRandom.Range(0, list.Count);
			int num2 = list[index];
			Vector2 vector = new Vector2(num2, VisualCenter.y + rockSpawnHeight);
			CharonRock charonRock = Enemy.PoolManager.GetObject<CharonRock>("Charon Rock.prefab", vector);
			charonRock.StartFalling(this, parameter, -j).Forget();
			AddRock(charonRock);
		}
	}

	private void ResetSymbolVFX()
	{
		DisableVFX("energy_charging");
	}

	private void ClearSpawnedRock()
	{
		foreach (CharonRock item in charonRockList.Where((CharonRock rock) => rock.gameObject.activeSelf && !rock.IsFalling))
		{
			item.TryTakeDamage(new HitInfo(Enemy.Player, "ForceKillAll"));
		}
		charonRockList.Clear();
	}

	private void AddRock(CharonRock spawnedRock)
	{
		if (!charonRockList.Contains(spawnedRock))
		{
			charonRockList.Add(spawnedRock);
		}
	}

	public override void OnEnterExecution()
	{
		base.OnEnterExecution();
		ResetSymbolVFX();
	}
}
