using UnityEngine;

namespace SPR.Save;

public static class SaveHelper
{
	public const string IDENTIFIER = "SaveData.txt";

	private static readonly ISaveProvider CurrentProvider;

	private static string CurrentVersion => "v." + Application.version;

	static SaveHelper()
	{
		CurrentProvider = new SteamSaveProvider();
	}

	public static GameData NewSaveData()
	{
		string uniqueID = CurrentProvider.UniqueID;
		PlayerPrefs.DeleteAll();
		return new GameData(uniqueID);
	}

	public static void Save(GameData gameData)
	{
		gameData.Version = CurrentVersion;
		CurrentProvider.Save("SaveData.txt", gameData);
	}

	public static GameData Load()
	{
		GameData gameData = CurrentProvider.Load("SaveData.txt");
		if (gameData == null || !gameData.IsValid())
		{
			DebugLog.SaveDataLog("[SaveHelper] 유효한 데이터가 없어 새로 생성합니다.");
			gameData = NewSaveData();
			Save(gameData);
		}
		return gameData;
	}
}
