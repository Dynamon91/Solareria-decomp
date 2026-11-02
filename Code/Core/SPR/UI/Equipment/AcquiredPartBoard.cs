using System.Collections.Generic;
using System.Linq;
using Library;
using SPR.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Equipment;

public class AcquiredPartBoard : MonoBehaviour
{
	[SerializeField]
	private int rowCount;

	[Header("Scroll")]
	[SerializeField]
	private RectTransform viewport;

	[SerializeField]
	private RectTransform contentGroup;

	[SerializeField]
	private int padding;

	[SerializeField]
	private float scrollDuration = 0.3f;

	private ScrollRect scrollRect;

	private IEnumerable<PlayerData.PartData> partDatas;

	private List<EquipmentPartCell> acquiredPartCells;

	private Dictionary<PartType, int> focusIndexDict;

	private PartType currentPartType;

	private int equippedCellIndex;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static ObjectPoolManager ObjectPoolManager => Game.Instance.ObjectPoolManager;

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private ScrollRect ScrollRect => scrollRect ?? (scrollRect = GetComponent<ScrollRect>());

	public int CurrentIndex => focusIndexDict[currentPartType];

	public void Initialize()
	{
		focusIndexDict = new Dictionary<PartType, int>
		{
			{
				PartType.Weapon,
				0
			},
			{
				PartType.Defense,
				0
			},
			{
				PartType.Heat,
				0
			},
			{
				PartType.Warmhorn,
				0
			}
		};
		if (acquiredPartCells != null)
		{
			acquiredPartCells.ForEach(delegate(EquipmentPartCell x)
			{
				ObjectPoolManager.Return(x);
			});
			acquiredPartCells.Clear();
		}
	}

	public void ResetFocus()
	{
		for (int i = 0; i < 4; i++)
		{
			focusIndexDict[(PartType)i] = 0;
		}
	}

	public void SetData(PartType partType, IEnumerable<PlayerData.PartData> partDatas, Dictionary<string, Sprite> sprites, bool shouldFocusCategory = false)
	{
		currentPartType = partType;
		equippedCellIndex = -1;
		if (acquiredPartCells == null)
		{
			acquiredPartCells = new List<EquipmentPartCell>();
		}
		else
		{
			acquiredPartCells.ForEach(delegate(EquipmentPartCell x)
			{
				ObjectPoolManager.Return(x);
			});
			acquiredPartCells.Clear();
		}
		if (!partDatas.IsNullOrEmpty())
		{
			foreach (PlayerData.PartData partData in partDatas)
			{
				EquipmentPartCell equipmentPartCell = ObjectPoolManager.GetObject<EquipmentPartCell>("Part Cell.prefab", contentGroup);
				equipmentPartCell.transform.localScale = Vector3.one;
				equipmentPartCell.SetData(partData.PartID, partData.PartType, sprites[partData.PartID]);
				equipmentPartCell.SetBackgroundActive(value: false);
				equipmentPartCell.SetNotiIconActive(Game.Instance.SaveData.WanderingData.HasUnwatchedPart(partType, partData.PartID));
				acquiredPartCells.Add(equipmentPartCell);
			}
			PlayerData.PartData equippedPartData = PlayerData.EquippedPartList.FirstOrDefault((PlayerData.PartData data) => data.PartType == partType);
			if (equippedPartData != null)
			{
				int index = (equippedCellIndex = acquiredPartCells.FindIndex((EquipmentPartCell cell) => cell.PartID == equippedPartData.PartID));
				acquiredPartCells[index].SetBackgroundActive(value: true);
			}
		}
		if (!acquiredPartCells.IsNullOrEmpty())
		{
			int num = (focusIndexDict[partType] = (shouldFocusCategory ? (-1) : ((equippedCellIndex != -1) ? equippedCellIndex : 0)));
			int index2 = num;
			acquiredPartCells.GetElementSafely(index2)?.Focus(value: true);
		}
	}

	public bool TryMoveCursor(int horizontal, int vertical, out string partID)
	{
		partID = "";
		if (acquiredPartCells.IsNullOrEmpty())
		{
			return false;
		}
		int num = focusIndexDict[currentPartType];
		bool flag = horizontal != 0 || vertical != 0;
		bool canExit = vertical == 1 || !flag;
		int num2 = acquiredPartCells.CalculateTargetCellIndex(num, rowCount, horizontal, vertical, canExit);
		if (flag && num == num2)
		{
			return false;
		}
		SetTargetCell(num2, out partID);
		return true;
	}

	private void SetTargetCell(int targetCellIndex, out string partID)
	{
		partID = "";
		acquiredPartCells.GetElementSafely(focusIndexDict[currentPartType])?.Focus(value: false);
		focusIndexDict[currentPartType] = targetCellIndex;
		EquipmentPartCell elementSafely = acquiredPartCells.GetElementSafely(targetCellIndex);
		if ((object)elementSafely != null)
		{
			elementSafely.Focus(value: true);
			partID = elementSafely.PartID;
		}
		DebugLog.GeneralLog($"SetTargetCell : {targetCellIndex}, {partID}");
	}

	public void ResetFocusIndex(out string id)
	{
		SetTargetCell(-1, out var _);
		id = acquiredPartCells.GetElementSafely(equippedCellIndex)?.PartID;
	}

	public bool TrySelect(out string partID)
	{
		partID = "";
		if (acquiredPartCells.IsNullOrEmpty())
		{
			return false;
		}
		int num = focusIndexDict[currentPartType];
		EquipmentPartCell elementSafely = acquiredPartCells.GetElementSafely(num);
		partID = (elementSafely ?? acquiredPartCells.GetElementSafely(equippedCellIndex))?.PartID;
		if (equippedCellIndex >= 0)
		{
			acquiredPartCells[equippedCellIndex].SetBackgroundActive(value: false);
		}
		if (equippedCellIndex != num && num != -1)
		{
			equippedCellIndex = num;
			elementSafely?.SetBackgroundActive(value: true);
		}
		return true;
	}

	public void OnUpgradeLevel()
	{
		if (!acquiredPartCells.IsNullOrEmpty())
		{
			int index = focusIndexDict[currentPartType];
			acquiredPartCells[index].UpdateLevel();
			acquiredPartCells[index].PlayParticle();
		}
	}

	public void ShakeCell()
	{
		int index = focusIndexDict[currentPartType];
		acquiredPartCells[index].ShakeImage();
	}

	public void OnUnequip()
	{
		int targetCellIndex = equippedCellIndex;
		equippedCellIndex = -1;
		SetTargetCell(targetCellIndex, out var _);
	}
}
