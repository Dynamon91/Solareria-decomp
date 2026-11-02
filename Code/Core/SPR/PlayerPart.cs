using System;
using System.Collections.Generic;
using SPR.Save;
using UnityEngine;

namespace SPR;

public class PlayerPart
{
	private readonly Dictionary<PartType, Part> equipParts = new Dictionary<PartType, Part>();

	private static int partNum = -1;

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static int PartNum
	{
		get
		{
			if (partNum < 0)
			{
				partNum = Util.GetEnumValues<PartType>().Length;
			}
			return partNum;
		}
	}

	private int EquipPartsNum => equipParts.Count;

	public bool IsEquippedFull => EquipPartsNum == PartNum;

	public void Initialize(PlayerData playerData)
	{
		List<string> list = new List<string>();
		foreach (PlayerData.PartData equippedPart in playerData.EquippedPartList)
		{
			Type type = Type.GetType("SPR." + equippedPart.PartID + "Part");
			if (type == null)
			{
				list.Add(equippedPart.PartID);
				DebugLog.SaveDataLogWarning("사용할 수 없는 장비를 장착 시도하고 있습니다. 해당 ID를 제거합니다. ID: " + equippedPart.PartID);
			}
			else if (Activator.CreateInstance(type) is Part part)
			{
				equipParts.Add(part.PartType, part);
				part.OnEquip();
			}
		}
		foreach (string item in list)
		{
			playerData.UnequipPart(item);
			playerData.RemoveAcquiredPart(item);
		}
	}

	public void AddPart(string id)
	{
		PartType partType = DataManager.Instance.GetPartData(id).PartType;
		Game.Instance.SaveData.WanderingData.AddUnwatchedPart(partType, id);
		PlayerData.AddAcquiredPart(partType, id);
	}

	public void SetLevel(string id, int level)
	{
		PartData partData = Game.DataManager.GetPartData(id);
		int maxLevel = partData.MaxLevel;
		level = Mathf.Clamp(level, 1, maxLevel);
		Game.Instance.SaveData.PlayerData.SetPartLevel(id, level);
		if (equipParts.TryGetValue(partData.PartType, out var value))
		{
			value.OnUnequip();
			value.OnEquip();
		}
	}

	public void Equip(string id)
	{
		Type type = Type.GetType("SPR." + id + "Part");
		if (type == null || !(Activator.CreateInstance(type) is Part part))
		{
			DebugLog.GeneralLogError(id + "에 해당하는 Part가 존재하지 않습니다.");
			return;
		}
		Unequip(part.PartType);
		PlayerData.EquipPart(part.ID);
		equipParts.Add(part.PartType, part);
		part.OnEquip();
		DebugLog.GeneralLog(id + " 파츠 장착");
	}

	public void Unequip(PartType partType)
	{
		if (equipParts.ContainsKey(partType))
		{
			Part part = equipParts[partType];
			equipParts.Remove(partType);
			part.OnUnequip();
			PlayerData.UnequipPart(part.ID);
		}
	}

	public void OnFixedUpdate()
	{
		foreach (KeyValuePair<PartType, Part> equipPart in equipParts)
		{
			equipPart.Value.OnFixedUpdate();
		}
	}

	public void OnHealthChanged()
	{
		foreach (KeyValuePair<PartType, Part> equipPart in equipParts)
		{
			if (equipPart.Value is IHealthChangeEvent healthChangeEvent)
			{
				healthChangeEvent.OnHealthChanged();
			}
		}
	}

	public void OnDamaged(IAttackSource source, int damage)
	{
		foreach (KeyValuePair<PartType, Part> equipPart in equipParts)
		{
			if (equipPart.Value is IDamagedEvent damagedEvent)
			{
				damagedEvent.OnDamaged(source, damage);
			}
		}
	}

	public void OnParry(Enemy enemy)
	{
		foreach (KeyValuePair<PartType, Part> equipPart in equipParts)
		{
			if (equipPart.Value is IParryEvent parryEvent)
			{
				parryEvent.OnParry(enemy);
			}
		}
	}

	public void OnKill()
	{
		foreach (KeyValuePair<PartType, Part> equipPart in equipParts)
		{
			if (equipPart.Value is IKillEvent killEvent)
			{
				killEvent.OnKill();
			}
		}
	}

	public void OnChargeStart()
	{
		foreach (KeyValuePair<PartType, Part> equipPart in equipParts)
		{
			if (equipPart.Value is ChargeSuperArmorPart chargeSuperArmorPart)
			{
				chargeSuperArmorPart.OnChargeStart();
				break;
			}
		}
	}

	public void OnChargeEndOrCancel()
	{
		foreach (KeyValuePair<PartType, Part> equipPart in equipParts)
		{
			if (equipPart.Value is ChargeSuperArmorPart chargeSuperArmorPart)
			{
				chargeSuperArmorPart.OnChargeEndOrCancel();
				break;
			}
		}
	}

	public void OnPyronActionAttack(List<IDamageable> damaged, bool isHeavyPyron)
	{
		foreach (KeyValuePair<PartType, Part> equipPart in equipParts)
		{
			if (equipPart.Value is IPyronEvent pyronEvent)
			{
				pyronEvent.OnPyronAttack(damaged, isHeavyPyron);
				break;
			}
		}
	}

	public bool TryGetChargeAttackMultiple(float staminaUsage, out float multiple)
	{
		multiple = 1f;
		foreach (KeyValuePair<PartType, Part> equipPart in equipParts)
		{
			if (equipPart.Value is IChargeAttackEvent chargeAttackEvent)
			{
				multiple += chargeAttackEvent.GetDamageAdditionalRate(staminaUsage);
			}
		}
		return multiple > 1f;
	}

	public bool CanDodgePaleblow()
	{
		if (equipParts.TryGetValue(PartType.Defense, out var value))
		{
			return value is DodgePaleblowPart;
		}
		return false;
	}

	public void Reset()
	{
		foreach (KeyValuePair<PartType, Part> equipPart in equipParts)
		{
			equipPart.Value.Reset();
		}
	}

	public void OnHeal()
	{
		if (equipParts.TryGetValue(PartType.Warmhorn, out var value) && value is WarmhornPart warmhornPart)
		{
			warmhornPart.OnHeal();
		}
	}

	public void OnStartHeal()
	{
		if (equipParts.TryGetValue(PartType.Warmhorn, out var value) && value is WarmhornPart warmhornPart)
		{
			warmhornPart.OnStartHeal();
		}
	}

	public void OnEndHeal()
	{
		if (equipParts.TryGetValue(PartType.Warmhorn, out var value) && value is WarmhornPart warmhornPart)
		{
			warmhornPart.OnEndHeal();
		}
	}

	public (int duration, int cycle) GetHealDurationCycle()
	{
		if (equipParts.TryGetValue(PartType.Warmhorn, out var value) && value is SlowHealPart slowHealPart)
		{
			return (duration: slowHealPart.HealDuration, cycle: slowHealPart.HealCycle);
		}
		return (duration: 0, cycle: 0);
	}

	public void OnShieldDestroyed(int blockedDamage)
	{
		foreach (KeyValuePair<PartType, Part> equipPart in equipParts)
		{
			if (equipPart.Value is IShieldDestroyedEvent shieldDestroyedEvent)
			{
				shieldDestroyedEvent.OnShieldDestroyed(blockedDamage);
			}
		}
	}

	public void OnHeatUnlocked(int maxLevel)
	{
		foreach (KeyValuePair<PartType, Part> equipPart in equipParts)
		{
			if (equipPart.Value is IHeatUnlockEvent heatUnlockEvent)
			{
				heatUnlockEvent.OnUnlockHeat(maxLevel);
			}
		}
	}

	public void OnResidualHeatChanged(int value)
	{
		foreach (KeyValuePair<PartType, Part> equipPart in equipParts)
		{
			if (equipPart.Value is IResidualHeatChangedEvent residualHeatChangedEvent)
			{
				residualHeatChangedEvent.OnResidualHeatChanged(value);
			}
		}
	}
}
