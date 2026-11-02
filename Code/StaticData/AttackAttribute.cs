using System;

[Flags]
public enum AttackAttribute
{
	None = 0,
	Default = 1,
	Down = 2,
	Stiff = 4,
	Stamina = 8,
	HeavyAttack = 0x10,
	IgnoreDodge = 0x20,
	Continuous = 0x40,
	IgnoreGuard = 0x80,
	Stagger = 0x100,
	BulletTime = 0x200,
	IgnoreSource = 0x400,
	Relocate = 0x800,
	Electricity = 0x1000,
	DestroyWall = 0x2000,
	HyperBurst = 0x4000,
	RedFear = 0x8020,
	FixedDamage = 0x10000,
	ParryGroggy = 0x20000,
	BlockStuck = 0x40000,
	Spore = 0x80000,
	Counter = 0x100000,
	Wet = 0x200000,
	Explosive = 0x10080
}
