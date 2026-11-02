using System.Linq;
using Rewired;
using SPR.L10N;
using SPR.Library;

namespace SPR.Cutscene;

public static class ButtonActionHelper
{
	private static readonly string[] LongKeys = new string[9] { "space", "ctrl", "shift", "return", "alt", "tab", "esc", "Shoulder", "Touchpad" };

	public static bool IsKeyLong()
	{
		string[] array = Locale.ParseInputIcon("confirmui", RewiredHelper.Controller.type == ControllerType.Joystick).Split('_', '-');
		if (array.Length >= 2)
		{
			return LongKeys.Contains(array[1]);
		}
		return false;
	}
}
