namespace SPR.UI.Option;

public class SettingDamageEffectPicker : SettingPicker
{
	public override void Save()
	{
		bool damageEffect = Current != 0;
		SettingItem.SettingData.DamageEffect = damageEffect;
	}

	protected override void OnSynchronize()
	{
		bool damageEffect = SettingItem.SettingData.DamageEffect;
		Current = (damageEffect ? 1 : 0);
	}
}
