using System;

[Flags]
public enum HitType
{
	None = 0,
	Player = 1,
	Creature = 2,
	Boss = 4,
	Object = 8,
	Platform = 0x10,
	Environment = 0x20
}
