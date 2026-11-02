using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class FocusBehaviour : PlayableBehaviour
{
	public Color Color;

	public float Alpha;

	public Vector2 CentralPoint;

	public Transform Target;

	public float MaskSize;
}
