using System;
using BayatGames.SaveGameFree;

namespace SPR.Save;

public class LocalSaveProvider : ISaveProvider
{
	public string UniqueID => $"Dev_{DateTime.Now:yyMMddHHmmss}";

	static LocalSaveProvider()
	{
		SaveGame.Encode = true;
		SaveGame.EncodePassword = "8p1rit";
	}

	public void Save(string fileName, GameData data)
	{
		SaveGame.Save(fileName, data);
	}

	public GameData Load(string fileName)
	{
		GameData gameData = null;
		try
		{
			gameData = SaveGame.Load<GameData>(fileName);
		}
		catch
		{
		}
		if (gameData == null)
		{
			DebugLog.SaveDataLog("기본 로드 실패(null). 암호화 옵션을 반전하여 재시도합니다.");
			try
			{
				gameData = SaveGame.Load<GameData>(fileName, !SaveGame.Encode, SaveGame.EncodePassword);
				SaveGame.Save(fileName, gameData);
			}
			catch (Exception ex)
			{
				DebugLog.SaveDataLogError("[LocalSaveProvider] 재시도 로드 실패: " + ex.Message);
			}
		}
		return gameData;
	}

	public bool Exists(string fileName)
	{
		return SaveGame.Exists(fileName);
	}

	public void Delete(string fileName)
	{
		SaveGame.Delete(fileName);
	}
}
