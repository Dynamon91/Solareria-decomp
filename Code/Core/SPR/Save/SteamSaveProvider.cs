using System;
using System.IO;
using BayatGames.SaveGameFree;
using Steamworks;
using UnityEngine;

namespace SPR.Save;

public class SteamSaveProvider : ISaveProvider
{
	private string SteamSaveDirectory => Path.Combine(Application.persistentDataPath, SteamUser.GetSteamID().ToString());

	public string UniqueID => $"Steam_{SteamUser.GetSteamID()}";

	private string GetSavePath(string fileName)
	{
		return Path.Combine(SteamSaveDirectory, fileName);
	}

	static SteamSaveProvider()
	{
		SaveGame.Encode = true;
		SaveGame.EncodePassword = "8p1rit";
	}

	public void Save(string fileName, GameData data)
	{
		if (!SteamManager.Initialized)
		{
			return;
		}
		try
		{
			if (!Directory.Exists(SteamSaveDirectory))
			{
				Directory.CreateDirectory(SteamSaveDirectory);
			}
			string savePath = GetSavePath(fileName);
			SaveGame.Save(savePath, data);
			DebugLog.SaveDataLog("[SteamSave] 로컬 저장 완료: " + savePath);
		}
		catch (Exception ex)
		{
			DebugLog.SaveDataLogError("[SteamSave] 저장 실패: " + ex.Message);
		}
	}

	public GameData Load(string fileName)
	{
		if (!SteamManager.Initialized)
		{
			return null;
		}
		string savePath = GetSavePath(fileName);
		if (!File.Exists(savePath))
		{
			DebugLog.SaveDataLogWarning("[SteamSave] 파일을 찾을 수 없음: " + savePath);
			return TryMigrateFromLocal(fileName);
		}
		try
		{
			GameData result = LoadWithFallback(savePath);
			DebugLog.SaveDataLog("[SteamSave] 로드 완료: " + fileName);
			return result;
		}
		catch (Exception ex)
		{
			DebugLog.SaveDataLogError("[SteamSave] 로드 중 오류: " + ex.Message);
		}
		return null;
	}

	private GameData TryMigrateFromLocal(string fileName)
	{
		LocalSaveProvider localSaveProvider = new LocalSaveProvider();
		if (localSaveProvider.Exists(fileName))
		{
			DebugLog.SaveDataLog("[SteamSave] 기존 로컬 경로에서 파일 발견. Steam 경로로 이동합니다.");
			GameData gameData = localSaveProvider.Load(fileName);
			if (gameData != null)
			{
				Save(fileName, gameData);
				localSaveProvider.Delete(fileName);
				return gameData;
			}
		}
		return null;
	}

	public bool Exists(string fileName)
	{
		if (!SteamManager.Initialized)
		{
			return false;
		}
		return SteamRemoteStorage.FileExists(fileName);
	}

	public void Delete(string fileName)
	{
		if (SteamManager.Initialized)
		{
			SteamRemoteStorage.FileDelete(fileName);
		}
	}

	private GameData LoadWithFallback(string path)
	{
		GameData gameData = null;
		try
		{
			gameData = SaveGame.Load<GameData>(path);
		}
		catch (Exception)
		{
			try
			{
				gameData = SaveGame.Load<GameData>(path, !SaveGame.Encode, SaveGame.EncodePassword);
			}
			catch (Exception ex2)
			{
				DebugLog.SaveDataLogError("[SteamSave] 2차 로드 시도 실패: " + ex2.Message);
			}
		}
		if (gameData == null)
		{
			try
			{
				gameData = SaveGame.Load<GameData>(path, !SaveGame.Encode, SaveGame.EncodePassword);
			}
			catch
			{
			}
		}
		if (gameData == null)
		{
			DebugLog.SaveDataLogError("[SteamSave] 데이터 복구 실패. 파일이 손상되었거나 암호화 키가 다릅니다.");
		}
		return gameData;
	}
}
