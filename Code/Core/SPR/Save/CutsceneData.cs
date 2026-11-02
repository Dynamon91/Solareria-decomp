using System;
using System.Collections.Generic;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class CutsceneData
{
	[SerializeField]
	private string cutsceneName;

	[SerializeField]
	private List<int> watchedIndex;

	public string CutsceneName => cutsceneName;

	public bool Contains(int state)
	{
		return watchedIndex.Contains(state);
	}

	public CutsceneData(string name, int state)
	{
		cutsceneName = name;
		watchedIndex = new List<int> { state };
	}

	public void AddWatchedIndex(int state)
	{
		if (!watchedIndex.Contains(state))
		{
			watchedIndex.Add(state);
		}
	}
}
