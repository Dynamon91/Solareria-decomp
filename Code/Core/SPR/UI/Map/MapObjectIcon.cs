using SPR.Save;
using UnityEngine;

namespace SPR.UI.Map;

public class MapObjectIcon : MapIcon
{
	private const float Ratio = 1.1169451f;

	[Header("References")]
	[SerializeField]
	private GameObject recent;

	[SerializeField]
	private GameObject highlight;

	[Header("Effect Prefabs")]
	[SerializeField]
	private GameObject relicsPrefab;

	[SerializeField]
	private GameObject fiveWarriorsPrefab;

	[SerializeField]
	private GameObject informationPrefab;

	[Header("Serialized Values")]
	[SerializeField]
	private FieldID fieldID;

	[SerializeField]
	private MapObjectType mapObjectType;

	[SerializeField]
	private int intValue;

	[SerializeField]
	private string stringValue;

	[SerializeField]
	private Sprite enabledSprite;

	[SerializeField]
	private Sprite disabledSprite;

	private RectTransform iconRect;

	private Vector2 enabledSize;

	private Vector2 disabledSize;

	private RectTransform IconRect => iconRect ?? (iconRect = iconImage.GetComponent<RectTransform>());

	public MapObjectType Type => mapObjectType;

	public FieldID FieldID => fieldID;

	public int IntValue => intValue;

	public string StringValue => stringValue;

	public bool IsDisabled { get; private set; }

	private bool IsInLibrary => FieldID.GetSubtitle() == FieldSubtitle.Library;

	protected override bool ShouldBeActive
	{
		get
		{
			bool flag = CheckInfoStone();
			if (!flag)
			{
				flag = mapObjectType switch
				{
					MapObjectType.Altar => FieldID != FieldID.NewGameStartSceneID() && MapIcon.SaveData.WorldData.HasInteractedBonfire(FieldID), 
					MapObjectType.CorestoneStatue => IsVisitedField() && MapIcon.SaveData.WorldData.TryGetObjectState(FieldID.SectorName, stringValue, out var state) && state == 0, 
					MapObjectType.FieldItem => IsVisitedField() && (!TryGetObjectStateData(out var data) || (data.state == 0 && data.watched)) && (intValue == 0 || MapIcon.SaveData.WorldData.MapData.IsItemSpawnedByOther(stringValue)), 
					MapObjectType.Information => false, 
					MapObjectType.Portal => IsVisitedField(), 
					MapObjectType.Npc => IsVisitedField() && !Npc.CheckTriggerDisable(stringValue) && TryGetObjectStateData(out var data2) && data2.watched, 
					MapObjectType.SproutStone => IsVisitedField() && TryGetObjectStateData(out var data3) && data3.watched, 
					MapObjectType.Boss => MapIcon.SaveData.EnemyFightData.GetBossTryCount((BossName)intValue) > 0 && !MapIcon.SaveData.EnemyFightData.IsBossCleared((BossName)intValue), 
					MapObjectType.FiveWarriors => (MapIcon.SaveData.EnemyFightData.GetBossTryCount((BossName)intValue) > 0 || ObtainedFiveRelics()) && !MapIcon.SaveData.EnemyFightData.IsBossCleared((BossName)intValue), 
					MapObjectType.ItemBox => IsVisitedField() && TryGetObjectStateData(out var data4) && data4.state == 0 && data4.watched, 
					MapObjectType.Sprout => CheckSprout(), 
					MapObjectType.Relics => ObtainedFiveRelics() && (!TryGetObjectStateData(out var data5) || data5.state == 0), 
					_ => false, 
				};
			}
			return flag;
		}
	}

	private string InfoID
	{
		get
		{
			_ = mapObjectType;
			return stringValue;
		}
	}

	private bool CheckInfoStone()
	{
		if (MapIcon.SaveData.WanderingData.HasKnownInfoStone(InfoID))
		{
			return !Trigger.CheckTrigger("Trigger_" + InfoID);
		}
		return false;
	}

	private bool TryGetObjectStateData(out SectorData.ObjectState data)
	{
		return MapIcon.SaveData.WorldData.TryGetObjectStateData(FieldID.SectorName, stringValue, out data);
	}

	private bool IsVisitedField()
	{
		return MapIcon.SaveData.WorldData.IsVisitedField(FieldID);
	}

	private bool ObtainedFiveRelics()
	{
		int current;
		return MapIcon.SaveData.PlayerData.HasEverObtainedItem("AshHillsFiveRelics", out current);
	}

	private bool ObtainedSproutPendant()
	{
		int current;
		return MapIcon.SaveData.PlayerData.HasEverObtainedItem("PlagueSproutPendant", out current);
	}

	private bool CheckSprout()
	{
		SectorData.ObjectState data;
		bool flag = TryGetObjectStateData(out data);
		bool flag2 = !flag || data.state < intValue;
		if (!(IsVisitedField() && flag && data.watched && flag2))
		{
			return ObtainedSproutPendant() && flag2;
		}
		return true;
	}

	public void SetHighlight(bool isHighlight)
	{
		highlight.SetActive(isHighlight);
	}

	public void SetRecent(bool isRecent)
	{
		recent.SetActive(isRecent);
	}

	public void SetDisabled(bool isLibraryDestroyed, out bool isActive)
	{
		isActive = true;
		switch (mapObjectType)
		{
		case MapObjectType.Altar:
			IsDisabled = isLibraryDestroyed && !FieldID.Equals(SectorName.Library, 3) && !FieldID.Equals(SectorName.SolinLab, 99);
			SetIconImage();
			break;
		case MapObjectType.Npc:
			IsDisabled = isLibraryDestroyed && IsInLibrary;
			if (IsDisabled)
			{
				isActive = false;
			}
			break;
		case MapObjectType.Portal:
			IsDisabled = isLibraryDestroyed;
			SetIconImage();
			break;
		}
	}

	private void SetIconImage()
	{
		if (enabledSize.Equals(default(Vector2)))
		{
			enabledSize = IconRect.sizeDelta;
			disabledSize = enabledSize * 1.1169451f;
		}
		IconRect.sizeDelta = (IsDisabled ? disabledSize : enabledSize);
		iconImage.sprite = (IsDisabled ? disabledSprite : enabledSprite);
	}
}
