namespace SPR.UI.Option;

public class SettingVSyncPicker : SettingPicker
{
	public override void Save()
	{
		SettingItem.SettingData.VSync = Current != 0;
	}

	protected override void OnSynchronize()
	{
		bool vSync = SettingItem.SettingData.VSync;
		Current = (vSync ? 1 : 0);
	}
}
