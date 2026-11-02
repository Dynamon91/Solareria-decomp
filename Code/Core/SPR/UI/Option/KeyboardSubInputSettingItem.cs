using Cysharp.Threading.Tasks;

namespace SPR.UI.Option;

public class KeyboardSubInputSettingItem : KeyboardInputSettingItem
{
	public override void OnFocus(bool value)
	{
	}

	public override void Control(int horizontal, int vertical)
	{
	}

	public override void Save()
	{
	}

	public override void OnConfirm()
	{
	}

	public override UniTask StartMapping()
	{
		return default(UniTask);
	}

	protected override void DeleteKey()
	{
	}
}
