using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class RadialBlurBehaviour : PlayableBehaviour
{
	public float Intensity;

	public Vector2 Center;

	public Transform Target;

	public float Delay;

	public int NumberOfSamples;
}
