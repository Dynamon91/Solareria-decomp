using System;

[Flags]
public enum StateOption
{
	None = 0,
	Controllable = 1,
	Immortal = 2,
	Dodge = 4,
	GravityFree = 8,
	BlockStaminaRegain = 0x10,
	BlockHeavyCharge = 0x20,
	Guard = 0x40,
	Parry = 0x80,
	BlockStuck = 0x100,
	SuperArmor = 0x200,
	IgnoreDamage = 0x400
}
