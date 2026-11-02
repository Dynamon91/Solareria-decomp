using System;
using Library;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SPR;

[Serializable]
public struct FieldID
{
	[SerializeField]
	private SectorName sectorName;

	[SerializeField]
	private int sceneIndex;

	public SectorName SectorName => sectorName;

	public int SceneIndex => sceneIndex;

	public string Address => $"{sectorName}_{sceneIndex:00}.unity";

	public static FieldID Empty => new FieldID(SectorName.None, 0);

	public FieldID(SectorName sectorName, int sceneIndex)
	{
		this.sectorName = sectorName;
		this.sceneIndex = sceneIndex;
	}

	public override string ToString()
	{
		return $"{SectorName}_{SceneIndex:00}";
	}

	public bool Equals(FieldID other)
	{
		if (sectorName == other.sectorName)
		{
			return sceneIndex == other.sceneIndex;
		}
		return false;
	}

	public bool Equals(SectorName name, int index)
	{
		if (sectorName == name)
		{
			return sceneIndex == index;
		}
		return false;
	}

	public RegionName GetRegionName()
	{
		return GetRegionName(sectorName);
	}

	public static RegionName GetRegionName(SectorName sector)
	{
		switch (sector)
		{
		case SectorName.None:
		case SectorName.Library:
		case SectorName.WorldsEnd:
			return RegionName.None;
		case SectorName.Earthford:
		case SectorName.TerradinGarden:
			return RegionName.Terradin;
		case SectorName.WatermirrorBasin:
		case SectorName.UndelCaverns:
			return RegionName.Ophilin;
		case SectorName.AshHills:
		case SectorName.FadedCastle:
			return RegionName.Pyron;
		case SectorName.WindshadeSpire:
			return RegionName.Zephiron;
		case SectorName.MucoraFortress:
		case SectorName.SolinLab:
			return RegionName.Mucora;
		case SectorName.SilentRuins:
		case SectorName.AbandonedTunnels:
			return RegionName.Neutral;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public FieldSubtitle GetSubtitle()
	{
		return GetSubtitle(this);
	}

	public static FieldSubtitle GetSubtitle(FieldID fieldID)
	{
		SectorName sectorName = fieldID.sectorName;
		int target = fieldID.sceneIndex;
		switch (sectorName)
		{
		case SectorName.None:
			return FieldSubtitle.None;
		case SectorName.SilentRuins:
			return FieldSubtitle.SilentRuins;
		case SectorName.Earthford:
			return FieldSubtitle.Earthford;
		case SectorName.TerradinGarden:
			if (!target.IsOneOf(30, 31, 32, 33, 34, 35))
			{
				if (!target.IsOneOf(36, 37))
				{
					return FieldSubtitle.TerradinGarden;
				}
				return FieldSubtitle.AshHills;
			}
			return FieldSubtitle.SeedKeeperBureau;
		case SectorName.Library:
			return FieldSubtitle.Library;
		case SectorName.AshHills:
			return FieldSubtitle.AshHills;
		case SectorName.WatermirrorBasin:
			return FieldSubtitle.WatermirrorBasin;
		case SectorName.FadedCastle:
			return FieldSubtitle.FadedCastle;
		case SectorName.WindshadeSpire:
			if (!target.IsOneOf(16, 17, 19, 21, 22, 23, 24))
			{
				return FieldSubtitle.WindshadeSpire;
			}
			return FieldSubtitle.MintBureau;
		case SectorName.MucoraFortress:
			return FieldSubtitle.MucoraFortress;
		case SectorName.UndelCaverns:
			return FieldSubtitle.UndelCaverns;
		case SectorName.SolinLab:
			return FieldSubtitle.SolinLab;
		case SectorName.AbandonedTunnels:
			return FieldSubtitle.AbandonedTunnels;
		case SectorName.WorldsEnd:
			return FieldSubtitle.WorldsEnd;
		default:
			DebugLog.GeneralLogError($"부제를 정의할 수 없는 지역({sectorName})입니다!");
			throw new ArgumentOutOfRangeException();
		}
	}

	public override bool Equals(object obj)
	{
		if (obj is FieldID other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine((int)sectorName, sceneIndex);
	}

	public static FieldID ConvertToFieldID(string address)
	{
		string[] array = address.Split('_', ' ', '.');
		SectorName result;
		SectorName sectorName = (Enum.TryParse<SectorName>(array[0], out result) ? result : SectorName.None);
		int result2;
		int num = (array[1].TryParseInt(out result2) ? result2 : (-1));
		return new FieldID(sectorName, num);
	}

	public static FieldID ConvertToFieldID(Scene scene)
	{
		string name = scene.name;
		if (!IsField(scene))
		{
			return new FieldID(SectorName.None, 0);
		}
		return ConvertToFieldID(name);
	}

	public static FieldID GetEditorFieldID()
	{
		return ConvertToFieldID(SceneManager.GetActiveScene());
	}

	public static FieldID NewGameStartSceneID()
	{
		return new FieldID(SectorName.None, 0);
	}

	public static bool operator ==(FieldID a, FieldID b)
	{
		if (a.SectorName == b.SectorName)
		{
			return a.SceneIndex == b.SceneIndex;
		}
		return false;
	}

	public static bool operator !=(FieldID a, FieldID b)
	{
		if (a.SectorName == b.SectorName)
		{
			return a.SceneIndex != b.SceneIndex;
		}
		return true;
	}

	public static bool IsField(Scene scene)
	{
		string name = scene.name;
		if (name == "SplashScene")
		{
			return false;
		}
		if (name == "Title")
		{
			return false;
		}
		if (name.Contains("Ending"))
		{
			return false;
		}
		if (name.Contains("Credit"))
		{
			return false;
		}
		return true;
	}

	public static SectorName GetSectorNameForPotteryRoad(FieldID fieldID)
	{
		if (fieldID.SectorName == SectorName.TerradinGarden)
		{
			int num = fieldID.SceneIndex;
			if (num == 36 || num == 37)
			{
				return SectorName.AshHills;
			}
		}
		return fieldID.SectorName;
	}
}
