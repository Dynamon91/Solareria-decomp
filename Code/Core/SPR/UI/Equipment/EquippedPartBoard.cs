using System.Collections.Generic;
using System.Linq;
using SPR.L10N;
using SPR.Save;
using UnityEngine;

namespace SPR.UI.Equipment;

public class EquippedPartBoard : MonoBehaviour
{
	[SerializeField]
	private List<EquipmentPartCell> cells;

	[SerializeField]
	private GameObject basicWristband;

	[SerializeField]
	private GameObject enhancedWristband;

	[SerializeField]
	private LocaleText wristbandLocaleText;

	private int focusIndex;

	public string FocusPartID => cells[focusIndex].PartID;

	public PartType FocusPartType => cells[focusIndex].PartType;

	public void Initialize()
	{
		cells.ForEach(delegate(EquipmentPartCell c)
		{
			c.Initialize();
		});
		focusIndex = 0;
	}

	public void ResetFocus()
	{
		focusIndex = 0;
	}

	public void OnActivate()
	{
		bool flag = Game.Instance.SaveData.PlayerData.GetAcquiredItemCount("EnhancedWristband") > 0;
		basicWristband.SetActive(!flag);
		enhancedWristband.SetActive(flag);
		wristbandLocaleText.SetData(LocaleKey.ItemName(ItemType.Wristband, flag ? "EnhancedWristband" : "PyronWristband"));
		for (int i = 0; i < cells.Count; i++)
		{
			cells[i].Focus(i == focusIndex);
		}
	}

	public void SetData(IEnumerable<PlayerData.PartData> equippedPartDatas, Dictionary<string, Sprite> sprites)
	{
		foreach (EquipmentPartCell cell in cells)
		{
			PlayerData.PartData partData = equippedPartDatas.FirstOrDefault((PlayerData.PartData data) => data.PartType == cell.PartType);
			if (partData != null)
			{
				cell.SetData(partData.PartID, partData.PartType, sprites[partData.PartID]);
			}
			else
			{
				cell.Clear();
			}
		}
	}

	public void MoveCursor(int horizontal)
	{
		if (horizontal == 0)
		{
			return;
		}
		int index = focusIndex;
		if (horizontal < 0)
		{
			focusIndex--;
			if (focusIndex < 0)
			{
				focusIndex = cells.Count - 1;
			}
		}
		else
		{
			focusIndex++;
			if (focusIndex > cells.Count - 1)
			{
				focusIndex = 0;
			}
		}
		cells[index].Focus(value: false);
		cells[focusIndex].Focus(value: true);
	}

	public void PlayEquipParticle(int index)
	{
		cells[index].PlayParticle();
	}
}
