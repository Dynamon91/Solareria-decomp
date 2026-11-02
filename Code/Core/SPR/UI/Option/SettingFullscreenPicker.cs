using UnityEngine;
using UnityEngine.Device;

namespace SPR.UI.Option;

public class SettingFullscreenPicker : SettingPicker
{
	protected override void OnSynchronize()
	{
		Current = 0;
		Current = (int)SettingItem.SettingData.Fullscreen;
		if (Current > 2)
		{
			Current = 2;
		}
		SetFullScreenMode(Current < 2, SettingItem.SettingData.Fullscreen);
	}

	public override void Save()
	{
		if (Changed)
		{
			int num = Current;
			if (num == 2)
			{
				num = 3;
			}
			FullScreenMode fullScreenMode = (FullScreenMode)num;
			SetFullScreenMode(num < 2, fullScreenMode);
			SettingItem.SettingData.Fullscreen = fullScreenMode;
		}
	}

	private void SetFullScreenMode(bool isFullScreen, FullScreenMode mode)
	{
		UnityEngine.Device.Screen.fullScreen = isFullScreen;
		UnityEngine.Device.Screen.fullScreenMode = mode;
		PlayerPrefs.SetInt("DisplayMode", (int)mode);
	}
}
