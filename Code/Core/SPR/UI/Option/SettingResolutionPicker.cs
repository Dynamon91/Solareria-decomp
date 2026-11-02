using System.Collections.Generic;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR.UI.Option;

public class SettingResolutionPicker : SettingPicker
{
	protected override int IndexMax => GraphicHelper.Resolutions.Count - 1;

	public override void Control(int horizontal, int vertical)
	{
		if (base.CanControl && horizontal != 0)
		{
			int current = Current;
			Current = Mathf.Clamp(Current + horizontal, 0, IndexMax);
			localeText.SetRawText($"{GraphicHelper.Resolutions[Current].x} × {GraphicHelper.Resolutions[Current].y}");
			Changed = current != Current;
			if (Changed)
			{
				ArrowAnimators[(horizontal > 0) ? 1 : 0].Play("action");
				FrameCount = 12;
				Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
			}
			Save();
		}
	}

	public override void Synchronize()
	{
		OnSynchronize();
		IReadOnlyList<Vector2Int> resolutions = GraphicHelper.Resolutions;
		Current = Mathf.Clamp(Current, 0, resolutions.Count - 1);
		localeText.SetRawText($"{GraphicHelper.Resolutions[Current].x} × {GraphicHelper.Resolutions[Current].y}");
		FrameCount = 0;
	}

	protected override void OnSynchronize()
	{
		if (SettingItem.SettingData.Resolution.x == 0f)
		{
			SettingItem.SettingData.SetNativeResolution();
		}
		IReadOnlyList<Vector2Int> resolutions = GraphicHelper.Resolutions;
		Vector2Int target = SettingItem.SettingData.Resolution.ToVector2Int();
		int current = FindResolutionIndex(resolutions, target);
		Current = current;
		SetResolution(target.x, target.y);
	}

	public override void Save()
	{
		if (Changed)
		{
			int x = GraphicHelper.Resolutions[Current].x;
			int y = GraphicHelper.Resolutions[Current].y;
			SetResolution(x, y);
			SettingItem.SettingData.Resolution = new Vector2Int(x, y);
		}
	}

	private void SetResolution(int width, int height)
	{
		Resolution currentResolution = Screen.currentResolution;
		if (currentResolution.width != width || currentResolution.height != height)
		{
			Screen.SetResolution(width, height, SettingItem.SettingData.Fullscreen);
			PlayerPrefs.SetInt("ResolutionWidth", width);
			PlayerPrefs.SetInt("ResolutionHeight", height);
		}
	}

	private int FindResolutionIndex(IReadOnlyList<Vector2Int> list, Vector2Int target)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].x == target.x && list[i].y == target.y)
			{
				return i;
			}
		}
		int result = 0;
		long num = long.MaxValue;
		for (int j = 0; j < list.Count; j++)
		{
			long num2 = (long)list[j].x - (long)target.x;
			long num3 = (long)list[j].y - (long)target.y;
			long num4 = num2 * num2 + num3 * num3;
			if (num4 < num)
			{
				num = num4;
				result = j;
			}
		}
		return result;
	}
}
