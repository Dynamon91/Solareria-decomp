using System;
using Spine;

namespace SPR.Library;

public class SpineAnimEvent
{
	public readonly string EventName;

	private readonly Action func;

	public SpineAnimEvent(string eventName, Action func)
	{
		EventName = eventName;
		this.func = func;
	}

	public void HandleSpineAnimEvent(TrackEntry trackEntry, Event e)
	{
		if (e.Data.Name == EventName)
		{
			func();
		}
	}

	public bool Equals(string eventName, Action func)
	{
		if (string.Equals(EventName, eventName))
		{
			return (object)this.func == func;
		}
		return false;
	}
}
