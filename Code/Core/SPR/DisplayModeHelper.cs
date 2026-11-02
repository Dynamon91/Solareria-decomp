using UnityEngine;

namespace SPR;

public static class DisplayModeHelper
{
	public const string PlayerPrefsDisplayModeKey = "DisplayMode";

	public const string PlyaerPrefsResolutionWidthKey = "ResolutionWidth";

	public const string PlayerPrefsResolutionHeightKey = "ResolutionHeight";

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void ApplyOnBoot()
	{
		ApplyDisplayModeFromPrefs();
	}

	public static void ApplyDisplayModeFromPrefs()
	{
		int width = PlayerPrefs.GetInt("ResolutionWidth", Screen.currentResolution.width);
		int height = PlayerPrefs.GetInt("ResolutionHeight", Screen.currentResolution.height);
		FullScreenMode mode = (FullScreenMode)PlayerPrefs.GetInt("DisplayMode", 1);
		ApplyDisplayMode(width, height, mode);
	}

	public static void ApplyDisplayMode(int width, int height, FullScreenMode mode)
	{
		bool fullscreen = IsFullScreenMode(mode);
		FullScreenMode fullScreenMode = Screen.fullScreenMode;
		Resolution currentResolution = Screen.currentResolution;
		if (fullScreenMode != mode)
		{
			Screen.fullScreenMode = mode;
		}
		if (currentResolution.width != width || currentResolution.height != height)
		{
			Screen.SetResolution(width, height, fullscreen);
		}
	}

	public static bool IsFullScreenMode(FullScreenMode mode)
	{
		if (mode != FullScreenMode.FullScreenWindow)
		{
			return mode == FullScreenMode.ExclusiveFullScreen;
		}
		return true;
	}
}
