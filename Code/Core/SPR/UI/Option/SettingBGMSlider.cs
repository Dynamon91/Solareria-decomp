namespace SPR.UI.Option;

public class SettingBGMSlider : SettingSlider
{
	public override void Synchronize()
	{
		base.BarValue_0_10 = SettingItem.SettingData.MusicVol;
		base.Synchronize();
	}

	public override void Save()
	{
		if (Changed)
		{
			SettingItem.SoundManager.SetBGMVolume(base.BarValue_0_10);
			Changed = false;
		}
	}
}
