namespace SPR.UI.Option;

public class SettingPyronActionVignettePicker : SettingPicker
{
	public override void Save()
	{
		bool pyronActionVignette = Current != 0;
		SettingItem.SettingData.PyronActionVignette = pyronActionVignette;
	}

	protected override void OnSynchronize()
	{
		bool pyronActionVignette = SettingItem.SettingData.PyronActionVignette;
		Current = (pyronActionVignette ? 1 : 0);
	}
}
