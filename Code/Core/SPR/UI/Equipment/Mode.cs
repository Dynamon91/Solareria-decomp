using System;

namespace SPR.UI.Equipment;

[Flags]
public enum Mode
{
	Cancel = 1,
	Unequip = 2,
	Equip = 4
}
