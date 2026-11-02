using System;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

[Serializable]
public class TimeDilationBehaviour : PlayableBehaviour
{
	[Tooltip("Time.timeScale replacement value.")]
	public float TimeScale = 1f;
}
