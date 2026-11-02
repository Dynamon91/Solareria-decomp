namespace SPR.UI.Option;

public class SettingMasterSlider : SettingSlider
{
	public override void Synchronize()
	{
		base.BarValue_0_10 = SettingItem.SettingData.MasterVol;
		base.Synchronize();
	}

	public override void Save()
	{
		if (Changed)
		{
			SettingItem.SoundManager.SetMasterVolume(base.BarValue_0_10);
			Changed = false;
		}
	}
}
