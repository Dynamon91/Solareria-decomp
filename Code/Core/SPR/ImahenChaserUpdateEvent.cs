using Library;
using UnityEngine;

namespace SPR;

public class ImahenChaserUpdateEvent : global::Library.Event
{
	public readonly ImahenChaser.Mode SwitchMode;

	public readonly Vector3 NewReturnPosition;

	public ImahenChaserUpdateEvent(ImahenChaser.Mode switchMode, Vector3 newReturnPosition = default(Vector3))
	{
		SwitchMode = switchMode;
		NewReturnPosition = newReturnPosition;
	}
}
