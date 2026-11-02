using System;
using System.Collections.Generic;
using SPR.Save;
using SPR.UI.Field;
using StaticData;
using UnityEngine;

namespace SPR;

public class PlayerStat
{
	private List<Stat> stats;

	private ClampedInt experience;

	private float defaultMultiplier;

	private static PlayerHeat playerHeat;

	private readonly PlayerMover playerMover;

	public const float DefaultHealth = 40f;

	public const float DefaultStamina = 20f;

	public const float DefaultStaminaRegain = 13f;

	public const float DefaultDamageMultiplier = 1f;

	private const float DefaultGuardDamageReduceRate = 50f;

	private const float DefaultGuardStaminaReduceRate = 100f;

	private const int DefaultGroggyAttack = 2;

	private const string FlameDefaultID = "heat_flame_default";

	public const string DefaultSkinName = "player";

	public int Level { get; private set; }

	public int Money { get; private set; }

	public int CurrentExp => experience.Value;

	public int MaxExp => experience.Max;

	public bool IsMaxLevel => Level >= 120;

	public static int HeatLevel => playerHeat.Level.Value;

	public static int HeatMaxLevel => playerHeat.Level.Max;

	public static float HeatGauge => playerHeat.Gauge.Value;

	public static float MaxHeatGauge => playerHeat.Gauge.Max;

	public ResidualHeat ResidualHeat { get; private set; }

	private static DataManager DataManager => Game.DataManager;

	private static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	public PlayerStat(PlayerMover mover, PlayerPart part)
	{
		playerMover = mover;
		playerHeat = new PlayerHeat();
		InitializeStat(PlayerData);
		InitializeEquipment();
	}

	private void InitializeStat(PlayerData data)
	{
		Level = data.Level;
		Difficulty difficulty = Game.Instance.SaveData.Difficulty;
		stats = new List<Stat>
		{
			new Health(40f, difficulty),
			new Stamina(20f, difficulty),
			new StaminaRegain(13f, difficulty),
			new Attack(10f, difficulty),
			new Defense(0f, difficulty),
			new CollectingPower(100f, difficulty),
			new CollectingRange(100f, difficulty),
			new FireAttack(10f, difficulty),
			new CounterAttack(20f, difficulty),
			new GroggyAttack(2f, difficulty),
			new GuardDamageReduceRate(50f, difficulty),
			new GuardStaminaReduceRate(100f, difficulty),
			new ShieldMaximum(0f, difficulty),
			new ResidualRate(50f, difficulty),
			new ResidualHeal(2f, difficulty),
			new ParryGroggy(1f, difficulty),
			new GoodsProtection(0f, difficulty),
			new WarmhornHealAmount(20f, difficulty),
			new WarmhornCount(data.PotionInitialCount, difficulty)
		};
		experience = new ClampedInt(data.CurrentExp, 0, GetMaxExperienceOf(Level));
		Money = data.Money;
		ResidualHeat = new ResidualHeat();
		defaultMultiplier = 1f;
		playerHeat.Initialize(HeatLevel, data.HeatLevel);
	}

	public void OnFixedUpdate()
	{
		if (!playerMover.PSM.StateOption.HasFlag(StateOption.BlockStaminaRegain))
		{
			GetStat<Stamina>().RecoverStaminaPerFrame(Time.fixedDeltaTime);
		}
		playerHeat.OnFixedUpdate();
	}

	public void UpdateFieldCanvas()
	{
		GetStat<Health>().UpdateHealthBar();
		GetStat<Stamina>().UpdateStaminaBar();
		GetStat<WarmhornCount>().UpdatePotionBoard();
		FieldCanvas.LevelBoard.UpdateLevelBoard();
		playerMover.PlayerShield.UpdateUI();
	}

	public void RecoverStat(bool isHeatFulled = false)
	{
		if (PlayerHeat.OnLastEmber)
		{
			PlayerHeat.OnLastEmber = false;
		}
		GetStat<Health>().Darkness = 0f;
		Game.Instance.Player.PlayerPart.Reset();
		GetStat<WarmhornCount>().ResetPotion();
		GetStat<Health>().AddMax();
		GetStat<Stamina>().ResetStamina();
		ResetHeatGauge(isHeatFulled);
		Game.Instance.Player.StatusEffectController.Clear();
		playerMover.PlayerShield.Reset();
	}

	public void SetLevel(int value)
	{
		value = Mathf.Clamp(value, 1, 120);
		int num = value - Level;
		Level = value;
		PlayerData.SetLevel(Level);
		PlayerData.AddTotalTraitPoint(num);
		if (num > 0)
		{
			FieldCanvas.LevelBoard.StartLevelUp(num);
		}
		else
		{
			FieldCanvas.LevelBoard.UpdateLevelBoard();
		}
	}

	public static void AddHeatGauge(float amount)
	{
		playerHeat.AddGauge(amount);
		DebugLog.GeneralLog($"열기 변화량 : {amount}");
	}

	public void ResetHeatGauge(bool isFulled)
	{
		playerHeat.ResetGauge(isFulled);
	}

	public void AddStatAppliedExperience(int amount)
	{
		amount = Mathf.CeilToInt((float)amount * GetStat<CollectingPower>().Rate);
		AddExperience(amount);
	}

	public void AddExperience(int amount)
	{
		if (!IsMaxLevel)
		{
			var (num, current, max) = GetLevelAndExpOf(GetAbsoluteExperience().Value + amount);
			if (num >= 120)
			{
				num = 120;
				current = 0;
				max = 0;
			}
			SetLevel(num);
			experience = new ClampedInt(current, 0, max);
			FieldCanvas.LevelBoard.UpdateExpBar();
			PlayerData.SetCurrentExp(experience.Value);
		}
	}

	public static int GetAbsoluteExperienceAtLevel(int level)
	{
		int num = 0;
		for (int i = 1; i < level; i++)
		{
			num += GetMaxExperienceOf(i);
		}
		return num;
	}

	public static (int level, int exp, int max) GetLevelAndExpOf(int absExp)
	{
		int i;
		for (i = 1; absExp >= GetMaxExperienceOf(i); i++)
		{
			absExp -= GetMaxExperienceOf(i);
		}
		int maxExperienceOf = GetMaxExperienceOf(i);
		return (level: i, exp: absExp, max: maxExperienceOf);
	}

	public ClampedInt GetAbsoluteExperience()
	{
		int absoluteExperienceAtLevel = GetAbsoluteExperienceAtLevel(Level);
		return new ClampedInt(max: absoluteExperienceAtLevel + GetMaxExperienceOf(Level), current: absoluteExperienceAtLevel + CurrentExp, min: 0);
	}

	public void AddMoney(int amount)
	{
		FieldCanvas.MoneyBoard.UpdateMoneyBoard(Money, amount);
		Money += amount;
		PlayerData.SetMoney(Money);
	}

	public void SetDefaultDamageMultiplier(float value)
	{
		defaultMultiplier = value;
	}

	public void MakeEnemyBurn(Enemy enemy, string skillDataID, int dataIndex = 0)
	{
		(float, float) damageCoefficient = GetDamageCoefficient(skillDataID, dataIndex);
		PlayerBurnData playerBurnData = Game.DataManager.GetPlayerBurnData(skillDataID);
		enemy.StartBurning(playerBurnData.Duration, playerBurnData.CycleFrame, damageCoefficient.Item1, damageCoefficient.Item2);
	}

	public void MakeEnemiesBurn(IEnumerable<IDamageable> damageables, string skillDataID, int dataIndex)
	{
		PlayerBurnData playerBurnData = Game.DataManager.GetPlayerBurnData(skillDataID);
		(float, float) damageCoefficient = GetDamageCoefficient(skillDataID, dataIndex);
		foreach (IDamageable damageable in damageables)
		{
			if (damageable is Enemy enemy)
			{
				enemy.StartBurning(playerBurnData.Duration, playerBurnData.CycleFrame, damageCoefficient.Item1, damageCoefficient.Item2);
			}
		}
	}

	public (int attack, int fire) GetMultipliedDamageResult(string skillDataID, int dataIndex, float multiplier)
	{
		(float, float) damageCoefficient = GetDamageCoefficient(skillDataID, dataIndex);
		return GetDamageInteger(random: true, damageCoefficient.Item1 * multiplier, damageCoefficient.Item2 * multiplier);
	}

	public (int attack, int fire) GetDamageResult(string skillDataID, int dataIndex)
	{
		return GetMultipliedDamageResult(skillDataID, dataIndex, defaultMultiplier);
	}

	public (int attack, int fire) GetDamageResultWithoutRandom(string skillDataID, int dataIndex)
	{
		(float, float) damageCoefficient = GetDamageCoefficient(skillDataID, dataIndex);
		return GetDamageInteger(random: false, damageCoefficient.Item1, damageCoefficient.Item2);
	}

	public (float attack, float fire) GetDamageCoefficient(string skillDataID, int dataIndex)
	{
		SkillData skillData = DataManager.GetSkillData(skillDataID);
		float item = skillData.Coefficients[dataIndex] * GetStat<Attack>().Current;
		float item2 = skillData.FireCoefficients[dataIndex] * GetStat<FireAttack>().Current;
		return (attack: item, fire: item2);
	}

	public (int attack, int fire) GetDamageInteger(bool random, float damage, float fireDamage)
	{
		if (random)
		{
			float num = damage * (float)pcp.damageRandomRate / 100f;
			damage = UnityEngine.Random.Range(damage - num, damage + num);
			float num2 = fireDamage * (float)pcp.damageRandomRate / 100f;
			fireDamage = UnityEngine.Random.Range(fireDamage - num2, fireDamage + num2);
		}
		return (attack: Mathf.CeilToInt(damage), fire: Mathf.CeilToInt(fireDamage));
	}

	public float GetMultipliedGroggyDamage(string skillDataID, int dataIndex, float multiplier)
	{
		return GetCurrentGroggyDamage(skillDataID, dataIndex) * multiplier;
	}

	public float GetCurrentGroggyDamage(string skillDataID, int dataIndex)
	{
		SkillData skillData = DataManager.GetSkillData(skillDataID);
		return GetCurrentGroggyDamage(skillData.GroggyCoefficients[dataIndex]);
	}

	public float GetCurrentGroggyDamage(float coefficient)
	{
		return coefficient * GetStat<GroggyAttack>().Current;
	}

	public void UnlockHeatLevel(int maxLevel)
	{
		if (HeatMaxLevel == 0)
		{
			playerMover.SetSkillActive(typeof(ChargeAttackInputItem), active: true);
		}
		playerHeat.SetLevel(HeatLevel, maxLevel);
		Game.Instance.SaveData.PlayerData.SetHeatLevel(maxLevel);
		Game.Instance.Player.PlayerPart.OnHeatUnlocked(maxLevel);
		GetStat<Health>().AddMax();
	}

	public void ResetAllTraits()
	{
		stats.ForEach(delegate(Stat s)
		{
			s.ResetTrait();
		});
		foreach (CardData cardData in PlayerData.CardDataList)
		{
			cardData.ResetTraitEffect();
		}
	}

	public void ApplyAllTraits()
	{
		stats.ForEach(delegate(Stat s)
		{
			s.ResetTrait();
		});
		foreach (CardData cardData in PlayerData.CardDataList)
		{
			cardData.ResetTraitEffect();
			cardData.ApplyTraitEffect();
		}
	}

	public void ApplyAllFoods()
	{
		stats.ForEach(delegate(Stat s)
		{
			s.ResetFood();
		});
		PlayerData.ApplyAllFoodItems();
	}

	public void ApplyDifficulty(Difficulty difficulty)
	{
		if (difficulty != Difficulty.None)
		{
			stats.ForEach(delegate(Stat s)
			{
				s.ChangeDifficulty(difficulty);
			});
		}
	}

	public static int GetMaxExperienceOf(int level)
	{
		return 100 + 20 * (level - 1) + Sum(level);
		static int Sum(int num)
		{
			if (num <= 2)
			{
				return 0;
			}
			return Sum(num - 1) + num - 2;
		}
	}

	public Stat GetStat(StatType statType)
	{
		Stat stat = stats.Find((Stat stat2) => stat2.StatType == statType);
		if (stat != null)
		{
			return stat;
		}
		DebugLog.GeneralLogError($"{statType} 스탯을 찾을 수 없습니다.");
		return null;
	}

	public T GetStat<T>() where T : Stat
	{
		Stat stat = stats.Find((Stat stat2) => stat2.GetType() == typeof(T));
		if (stat != null)
		{
			return (T)stat;
		}
		DebugLog.GeneralLogError($"{typeof(T)} 스탯을 찾을 수 없습니다.");
		return null;
	}

	private void InitializeEquipment()
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(SectorName.SilentRuins, out var data))
		{
			int state;
			bool value = data.TryGetObjectStateNum("PlayerWeaponCutsceneProducer", out state) && state == 1;
			ActivateBasicAttack(value);
		}
	}

	public void ActivateBasicAttack(bool value)
	{
		playerMover.SetSkillActive(typeof(BasicAttackInputItem), value);
		playerMover.SetSkillActive(typeof(FirstAirAttackInputItem), value);
		playerMover.SetSkillActive(typeof(GuardInputItem), value);
		playerMover.SetSkillActive(typeof(ParryInputItem), value);
		playerMover.SetSkillActive(typeof(BurstInputItem), value);
		string skin = (value ? "player" : "intro");
		playerMover.SetSkin(skin);
		if (!value)
		{
			return;
		}
		string[] names = Enum.GetNames(typeof(PartType));
		for (int i = 0; i < names.Length; i++)
		{
			string id = names[i] + "Default";
			if (PlayerData.GetPartLevel(id) < 0)
			{
				Game.Instance.Player.PlayerPart.AddPart(id);
			}
		}
	}
}
