namespace SPR.UI.Option;

public class SettingSFXSlider : SettingSlider
{
	public override void Synchronize()
	{
		base.BarValue_0_10 = SettingItem.SettingData.SoundVol;
		base.Synchronize();
	}

	public override void Save()
	{
		if (Changed)
		{
			SettingItem.SoundManager.SetSFXVolume(base.BarValue_0_10);
			SettingItem.SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem");
			Changed = false;
		}
	}
}
