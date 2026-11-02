namespace SPR.UI.Option;

public class SettingCombatCameraPicker : SettingPicker
{
	public override void Save()
	{
		bool combatCamera = Current != 0;
		SettingItem.SettingData.CombatCamera = combatCamera;
	}

	protected override void OnSynchronize()
	{
		bool combatCamera = SettingItem.SettingData.CombatCamera;
		Current = (combatCamera ? 1 : 0);
	}
}
