using System;
using BayatGames.SaveGameFree;
using UnityEngine;

namespace SPR;

[Serializable]
public class SaveSetting
{
	[SerializeField]
	private string identifier;

	[SerializeField]
	private SaveGamePath savePath;

	public string Identifier => identifier;

	public SaveGamePath SavePath => savePath;

	public SaveSetting()
	{
		identifier = "SaveData.txt";
		savePath = SaveGamePath.PersistentDataPath;
	}

	public SaveSetting(string identifier, SaveGamePath savePath)
	{
		this.identifier = identifier;
		this.savePath = savePath;
	}
}
