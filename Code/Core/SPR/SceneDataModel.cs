using UnityEngine.SceneManagement;

namespace SPR;

public readonly struct SceneDataModel
{
	public bool IsNull => SceneType == SceneType.None;

	public SceneType SceneType { get; }

	public SceneLoadType SceneLoadType { get; }

	public string PlayerSpawnEntranceName
	{
		get
		{
			if (SceneLoadType == SceneLoadType.ComeFromOther)
			{
				return EntranceName;
			}
			return string.Empty;
		}
	}

	public string SceneAddress { get; }

	public string EntranceName { get; }

	public string SpecialKey { get; }

	public static SceneDataModel CharonCarryingModel => new SceneDataModel(SceneType.Field, SceneLoadType.ComeFromOther, new FieldID(SectorName.SilentRuins, 5).Address, string.Empty, "CharonCarrying");

	private SceneDataModel(SceneType sceneType, SceneLoadType sceneLoadType, string sceneAddress, string entranceName, string specialKey)
	{
		SceneType = sceneType;
		SceneLoadType = sceneLoadType;
		SceneAddress = sceneAddress;
		EntranceName = entranceName;
		SpecialKey = specialKey;
	}

	public SceneDataModel(Scene scene)
	{
		SceneType = SceneType.Field;
		SceneLoadType = SceneLoadType.ComeFromOther;
		SceneAddress = scene.name + ".unity";
		EntranceName = string.Empty;
		SpecialKey = string.Empty;
	}

	public static SceneDataModel GetOutGameModel(string sceneName)
	{
		if (sceneName == "Title")
		{
			return TitleModel();
		}
		if (sceneName == "SplashScene")
		{
			return SplashModel();
		}
		if (sceneName.Contains("Credit"))
		{
			return CreditModel();
		}
		if (sceneName.Contains("Ending"))
		{
			return EndingModel();
		}
		DebugLog.GeneralLogError("OutGameModel을 생성할 수 없습니다. 씬 이름이 잘못되었습니다.");
		return default(SceneDataModel);
	}

	public static SceneDataModel SplashModel()
	{
		return new SceneDataModel(SceneType.Splash, SceneLoadType.None, string.Empty, string.Empty, string.Empty);
	}

	public static SceneDataModel TitleModel()
	{
		return new SceneDataModel(SceneType.Title, SceneLoadType.None, "Title.unity", string.Empty, string.Empty);
	}

	public static SceneDataModel EndingModel()
	{
		return new SceneDataModel(SceneType.Ending, SceneLoadType.None, "Ending.unity", string.Empty, string.Empty);
	}

	public static SceneDataModel CreditModel()
	{
		return new SceneDataModel(SceneType.Credit, SceneLoadType.None, "Credit.unity", string.Empty, string.Empty);
	}

	public static SceneDataModel NewGameSceneDataModel()
	{
		return new SceneDataModel(SceneType.Field, SceneLoadType.StartFromHere, new FieldID(SectorName.SilentRuins, 0).Address, string.Empty, "PlayerBorn");
	}

	public static SceneDataModel MoveModel(FieldID fieldID, string entranceName)
	{
		return new SceneDataModel(SceneType.Field, SceneLoadType.ComeFromOther, fieldID.Address, entranceName, string.Empty);
	}

	public static SceneDataModel RespawnModel(FieldID fieldID)
	{
		return new SceneDataModel(SceneType.Field, SceneLoadType.StartFromHere, fieldID.Address, string.Empty, string.Empty);
	}

	public static SceneDataModel SaveModel(FieldID fieldID)
	{
		return new SceneDataModel(SceneType.Field, SceneLoadType.SaveFromHere, fieldID.Address, string.Empty, string.Empty);
	}

	public static SceneDataModel MoveSpecialModel(SectorName sectorName, string address, string entranceName)
	{
		return new SceneDataModel(SceneType.Field, SceneLoadType.ComeFromOther, $"{sectorName}_{address}.unity", entranceName, string.Empty);
	}

	public static SceneDataModel MoveByOtherModel(FieldID fieldID, string entranceName, bool isVertical)
	{
		string specialKey = (isVertical ? "vertical" : "horizontal");
		return new SceneDataModel(SceneType.Field, SceneLoadType.MoveByOther, fieldID.Address, entranceName, specialKey);
	}

	public static SceneDataModel MoveCutsceneModel(FieldID fieldID, string targetCutscene)
	{
		return new SceneDataModel(SceneType.Field, SceneLoadType.None, fieldID.Address, targetCutscene, "Cutscene");
	}
}
