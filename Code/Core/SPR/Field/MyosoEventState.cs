using System;

namespace SPR.Field;

[Flags]
public enum MyosoEventState
{
	None = 0,
	PlayerHealthLow = 1,
	PlayerBounced = 2,
	PlayerGrabbed = 4
}
