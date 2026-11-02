using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RewiredData : IUserData
{
	[Serializable]
	public class StringPair
	{
		[SerializeField]
		private string key;

		[SerializeField]
		private string value;

		public string Key => key;

		public string Value
		{
			get
			{
				return value;
			}
			set
			{
				this.value = value;
			}
		}

		public StringPair(string key, string value)
		{
			this.key = key;
			this.value = value;
		}
	}

	[SerializeField]
	private List<StringPair> rewiredDataList;

	private List<StringPair> RewiredDataList => rewiredDataList ?? (rewiredDataList = new List<StringPair>());

	public void RefineDataOnInitialize()
	{
	}

	public bool IsValid()
	{
		return true;
	}

	public bool HasKey(string key)
	{
		foreach (StringPair rewiredData in RewiredDataList)
		{
			if (rewiredData.Key == key)
			{
				return true;
			}
		}
		return false;
	}

	public string GetString(string key)
	{
		foreach (StringPair rewiredData in RewiredDataList)
		{
			if (rewiredData.Key == key)
			{
				return rewiredData.Value;
			}
		}
		return null;
	}

	public void SetString(string key, string value)
	{
		foreach (StringPair rewiredData in RewiredDataList)
		{
			if (rewiredData.Key == key)
			{
				rewiredData.Value = value;
				return;
			}
		}
		RewiredDataList.Add(new StringPair(key, value));
	}
}
