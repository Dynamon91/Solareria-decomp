using SPR.Field;
using UnityEngine;

namespace SPR;

public interface IRideable : IGround
{
	Vector2 Velocity { get; }

	void OnStartRide();

	void OnEndRide();
}
