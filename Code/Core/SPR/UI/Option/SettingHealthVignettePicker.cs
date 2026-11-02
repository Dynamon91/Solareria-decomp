namespace SPR.UI.Option;

public class SettingHealthVignettePicker : SettingPicker
{
	public override void Save()
	{
		bool healthVignette = Current != 0;
		SettingItem.SettingData.HealthVignette = healthVignette;
	}

	protected override void OnSynchronize()
	{
		bool healthVignette = SettingItem.SettingData.HealthVignette;
		Current = (healthVignette ? 1 : 0);
	}
}
