using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

public static class DebugLog
{
	public static bool IsDebugBuild => UnityEngine.Debug.isDebugBuild;

	public static void GeneralLog(string str)
	{
		UnityEngine.Debug.Log($"{Time.time}\t[General Log] {str}");
	}

	public static void GeneralLogWarning(string str)
	{
		UnityEngine.Debug.LogWarning($"{Time.time}\t[General LogWarning] {str}");
	}

	public static void GeneralLogError(string str)
	{
		UnityEngine.Debug.LogError($"{Time.time}\t[General LogError] {str}");
	}

	public static void AddressableLog(string str)
	{
		UnityEngine.Debug.Log($"{Time.time}\t[Addressable Log] {str}");
	}

	public static void AddressableLogWarning(string str)
	{
		UnityEngine.Debug.LogWarning($"{Time.time}\t[Addressable LogWarning] {str}");
	}

	public static void AddressableLogError(string str)
	{
		UnityEngine.Debug.LogError($"{Time.time}\t[Addressable LogError] {str}");
	}

	public static void SaveDataLog(string str)
	{
		UnityEngine.Debug.Log($"{Time.time}\t[SaveData Log] {str}");
	}

	public static void SaveDataLogWarning(string str)
	{
		UnityEngine.Debug.Log($"{Time.time}\t[SaveData LogWarning] {str}");
	}

	public static void SaveDataLogError(string str)
	{
		UnityEngine.Debug.LogError($"{Time.time}\t[SaveData LogError] {str}");
	}

	public static void ControlStackLog(string str)
	{
		UnityEngine.Debug.Log($"{Time.time}\t[Control Stack Log] {str}");
	}

	public static void ControlStackLogWarning(string str)
	{
		UnityEngine.Debug.LogWarning($"{Time.time}\t[Control Stack LogWarning] {str}");
	}

	public static void ControlStackLogError(string str)
	{
		UnityEngine.Debug.LogError($"{Time.time}\t[Control Stack LogError] {str}");
	}

	public static void DataManagerLog(string str)
	{
		UnityEngine.Debug.Log($"{Time.time}\t[DataManager Log] {str}");
	}

	public static void DataManagerLogWarning(string str)
	{
		UnityEngine.Debug.LogWarning($"{Time.time}\t[DataManager LogWarning] {str}");
	}

	public static void DataManagerLogError(string str)
	{
		UnityEngine.Debug.LogError($"{Time.time}\t[DataManager LogError] {str}");
	}

	[Conditional("UNITY_EDITOR")]
	public static void ScenarioLog(string str)
	{
		UnityEngine.Debug.Log($"{Time.time}\t[Scenario Log] {str}");
	}

	public static void ScenarioLogWarning(string str)
	{
		UnityEngine.Debug.LogWarning($"{Time.time}\t[Scenario LogWarning] {str}");
	}

	public static void ScenarioLogError(string str)
	{
		UnityEngine.Debug.LogError($"{Time.time}\t[Scenario LogError] {str}");
	}

	[Conditional("UNITY_EDITOR")]
	public static void SoundLog(string str)
	{
		UnityEngine.Debug.Log($"{Time.time}\t[Sound Log] {str}");
	}

	public static void SoundLogWarning(string str)
	{
		UnityEngine.Debug.LogWarning($"{Time.time}\t[Sound LogWarning] {str}");
	}

	public static void SoundLogError(string str)
	{
		UnityEngine.Debug.LogError($"{Time.time}\t[Sound LogError] {str}");
	}

	public static void GraphicLog(string str)
	{
		UnityEngine.Debug.Log($"{Time.time}\t[Graphic Log] {str}");
	}

	public static void GraphicLogWarning(string str)
	{
		UnityEngine.Debug.LogWarning($"{Time.time}\t[Graphic Log] {str}");
	}

	public static void GraphicLogError(string str)
	{
		UnityEngine.Debug.LogError($"{Time.time}\t[Graphic LogError] {str}");
	}

	public static void ControllerLog(string str)
	{
		UnityEngine.Debug.Log($"{Time.time}\t[Controller Log] {str}");
	}

	public static void ControllerLogWarning(string str)
	{
		UnityEngine.Debug.LogWarning($"{Time.time}\t[Controller LogWarning] {str}");
	}

	public static void ControllerLogError(string str)
	{
		UnityEngine.Debug.LogError($"{Time.time}\t[Controller LogError] {str}");
	}

	public static void AchievementLog(string str)
	{
		UnityEngine.Debug.Log($"{Time.time}\t[Achievement Log] {str}");
	}

	public static string ToStringLog(this IEnumerable<int> list)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (int item in list)
		{
			stringBuilder.Append($"{item}, ");
		}
		return stringBuilder.ToString();
	}
}
