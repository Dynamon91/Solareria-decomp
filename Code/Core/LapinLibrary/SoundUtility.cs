using FMOD;
using FMOD.Studio;

namespace LapinLibrary;

public static class SoundUtility
{
	public static bool IsSameEvent(EventInstance e1, EventInstance e2)
	{
		return GetIDFromEvent(e1) == GetIDFromEvent(e2);
	}

	public static GUID GetIDFromEvent(EventInstance eventInstance)
	{
		if (eventInstance.getDescription(out var description) == RESULT.OK && description.getID(out var id) == RESULT.OK)
		{
			return id;
		}
		return default(GUID);
	}
}
