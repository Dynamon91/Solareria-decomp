using System;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class MemoryNotiData
{
	[SerializeField]
	private string id;

	[SerializeField]
	private WatchedState state;

	public string Id => id;

	public WatchedState State => state;

	public bool IsUnwatched => state == WatchedState.Unwatched;

	public MemoryNotiData(string id)
	{
		this.id = id;
		state = WatchedState.Unwatched;
	}

	public void SetWatched()
	{
		state = WatchedState.Watched;
	}

	public void SetUnwatched()
	{
		state = WatchedState.Unwatched;
	}
}
