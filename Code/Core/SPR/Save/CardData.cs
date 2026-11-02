using System;
using SPR.CoreStone;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class CardData
{
	[SerializeField]
	private int id;

	[SerializeField]
	private int level;

	[SerializeField]
	private int rank;

	private CoreStonePassive passive;

	private int part;

	public const int MinStatCardID = 1;

	public const int MaxStatCardID = 8;

	public const int MinLevel = 0;

	public const int MaxRank = 3;

	public const int DashAttackID = 1003;

	public const int DashPassWallID = 1007;

	public int ID => id;

	public int RawLevel => level;

	public int CurrentLevel => level + part;

	public int RawMaxLevel => TraitData.MaxLevel;

	public int Rank => rank;

	public bool IsStatCard => TraitData.MaxLevel > 0;

	private CoreStonePassive Passive
	{
		get
		{
			if (passive != null)
			{
				return passive;
			}
			Type type = Type.GetType("SPR.CoreStone." + TraitData.Name + "Passive");
			if (type == null)
			{
				DebugLog.GeneralLogError(TraitData.Name + "의 패시브에 대한 클래스를 구현하지 않았습니다.");
				return null;
			}
			passive = Activator.CreateInstance(type) as CoreStonePassive;
			return passive;
		}
	}

	private TraitData TraitData => Game.DataManager.GetTraitData(id);

	private PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	private PlayerMover PlayerMover => Game.Instance.Player.mover;

	public CardData(int id)
	{
		this.id = id;
		level = 0;
		rank = 1;
		part = 0;
	}

	public CardData(string name)
	{
		TraitData traitData = Game.DataManager.GetTraitData(name);
		if (traitData == null)
		{
			throw new ArgumentException("TraitData not found: " + name);
		}
		id = traitData.ID;
		level = 0;
		rank = 1;
	}

	public bool IsMaxLevel()
	{
		return level >= TraitData.MaxLevel;
	}

	public void SetLevel(int value)
	{
		level = value;
		PlayerStat.ApplyAllTraits();
	}

	public void SetPartLevel(int value)
	{
		part = value;
		PlayerStat.ApplyAllTraits();
	}

	public void SetRank(int value)
	{
		rank = value;
		PlayerStat.ApplyAllTraits();
	}

	public void ResetTraitEffect()
	{
		TraitData traitData = TraitData;
		if (traitData.MaxLevel != 0)
		{
			Passive.Reset();
		}
		else
		{
			ApplyCoreTrait(traitData, apply: false);
		}
	}

	public void ApplyTraitEffect()
	{
		TraitData traitData = TraitData;
		ApplyCoreTrait(traitData);
		if (traitData.MaxLevel == 0)
		{
			return;
		}
		foreach (var (statType, value) in StatUtility.GetTraitStatIncreasesByLevel(id, rank, CurrentLevel))
		{
			PlayerStat.GetStat(statType).AddTraitStat(value);
		}
		Passive.ApplyPassive(rank);
	}

	private void ApplyCoreTrait(TraitData traitData, bool apply = true)
	{
		if (traitData.MaxLevel <= 0)
		{
			switch (traitData.SkillName)
			{
			case "DownRolling":
				PlayerMover.SetSkillActive(typeof(DownRollingInputItem), apply);
				break;
			case "JumpDash":
				PlayerMover.SetSkillActive(typeof(JumpDashInputItem), apply);
				break;
			case "JumpDashAttack":
				PlayerMover.UpgradeState<PlayerJumpDashState>(apply);
				PlayerMover.UpgradeState<PlayerRollingState>(apply);
				break;
			case "SecondAirAttack":
				PlayerMover.SetSkillActive(typeof(SecondAirAttackInputItem), apply);
				break;
			case "FastDown":
				PlayerMover.SetSkillActive(typeof(FastDownInputItem), apply);
				break;
			case "FastDownAccel":
				PlayerMover.UpgradeState<PlayerFastDownState>(apply);
				break;
			case "DashPassWall":
				PlayerMover.UpgradeState<PlayerJumpDashState>(apply);
				PlayerMover.UpgradeState<PlayerRollingState>(apply);
				break;
			case "AshBreak":
				PlayerMover.SetSkillActive(typeof(AshBreakInputItem), apply);
				break;
			case "AshBlast":
				PlayerMover.SetSkillActive(typeof(AshBlastInputItem), apply);
				break;
			case "DoubleJump":
				PlayerMover.SetSkillActive(typeof(DoubleJumpUpInputItem), apply);
				break;
			case "ResidualHeat":
				PlayerStat.ResidualHeat.Activate();
				break;
			case "Counter":
				PlayerMover.UpgradeState<PlayerChargeAttackState>(apply);
				break;
			}
		}
	}
}
