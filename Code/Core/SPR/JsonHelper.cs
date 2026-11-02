using System.IO;
using UnityEngine;

namespace SPR;

public static class JsonHelper
{
	public static void SaveJson(object obj, string path, string fileName)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(path);
		if (!directoryInfo.Exists)
		{
			directoryInfo.Create();
		}
		string contents = JsonUtility.ToJson(obj, prettyPrint: true);
		File.WriteAllText(path + fileName, contents);
	}

	public static T LoadJson<T>(string path, string fileName) where T : new()
	{
		T val = new T();
		if (File.Exists(path + fileName))
		{
			return JsonUtility.FromJson<T>(File.ReadAllText(path + fileName));
		}
		SaveJson(val, path, fileName);
		return val;
	}
}
