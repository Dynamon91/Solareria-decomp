using System;
using System.Linq;
using System.Text;
using Library;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class GameData
{
	[SerializeField]
	private string owningUser;

	[SerializeField]
	private string version;

	[SerializeField]
	private SaveData[] saveDataSlots;

	[SerializeField]
	private SettingData settingData;

	public const int SaveDataCount = 9;

	public SettingData SettingData => settingData;

	public string Version
	{
		get
		{
			return version;
		}
		set
		{
			version = value;
		}
	}

	public string OwningUser => owningUser;

	public GameData(string owningUser)
	{
		this.owningUser = owningUser;
		version = Application.version;
		saveDataSlots = new SaveData[9];
		settingData = new SettingData();
		for (int i = 0; i < 9; i++)
		{
			saveDataSlots[i] = new SaveData();
		}
	}

	public bool IsValid()
	{
		if (IsValidVersion())
		{
			return saveDataSlots.Where((SaveData slot) => slot != null).All((SaveData slot) => slot.IsValid());
		}
		return false;
	}

	public void Reset()
	{
		SaveData[] array = saveDataSlots;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Reset();
		}
	}

	public SaveData GetSaveData(int index)
	{
		if (index < 0 || index >= saveDataSlots.Length)
		{
			DebugLog.SaveDataLogError($"{index}번째 Save Data는 존재하지 않습니다.");
			return null;
		}
		return saveDataSlots[index];
	}

	public void SetSaveData(int index, SaveData saveData)
	{
		if (index < 0 || index >= saveDataSlots.Length)
		{
			DebugLog.SaveDataLogError($"{index}번째 Save Data는 존재하지 않습니다.");
			return;
		}
		saveData.RecordPlayTime();
		saveData.RecordDateTime();
		saveDataSlots[index] = saveData;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("owningUser:" + owningUser + ",");
		SaveData[] array = saveDataSlots;
		foreach (SaveData value in array)
		{
			stringBuilder.Append("{");
			stringBuilder.Append(value);
			stringBuilder.Append("},");
		}
		return stringBuilder.ToString();
	}

	public bool IsWatchedCutsceneInGlobal(string name, int state)
	{
		SaveData[] array = saveDataSlots;
		foreach (SaveData saveData in array)
		{
			if (saveData != null && saveData.WanderingData.IsWatchedCutscene(name, state))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsValidVersion()
	{
		if (string.IsNullOrEmpty(version))
		{
			DebugLog.SaveDataLogWarning("저장된 버전 정보가 없습니다.");
			return false;
		}
		(int, int, int) tuple = GetVersion(version);
		(int, int, int) tuple2 = GetVersion(Application.version);
		if (tuple.Item1 == tuple2.Item1 && tuple.Item2 == tuple2.Item2)
		{
			DebugLog.SaveDataLog("클라이언트 버전과 SaveData 버전이 호환됩니다. 현재 버전: " + Application.version + ", 저장된 버전: " + version);
			return true;
		}
		DebugLog.SaveDataLogWarning("현재 버전과 저장된 버전의 major/minor 버전이 다릅니다. 현재 버전: " + Application.version + ", 저장된 버전: " + version);
		return false;
		static (int major, int minor, int patch) GetVersion(string version)
		{
			string[] array = version.Split('.');
			return array.Length switch
			{
				4 => (major: array[1].ParseIntOrDefault(), minor: array[2].ParseIntOrDefault(), patch: array[3].ParseIntOrDefault()), 
				3 => (major: array[0].ParseIntOrDefault(), minor: array[1].ParseIntOrDefault(), patch: array[2].ParseIntOrDefault()), 
				_ => (major: 0, minor: 0, patch: 0), 
			};
		}
	}
}
