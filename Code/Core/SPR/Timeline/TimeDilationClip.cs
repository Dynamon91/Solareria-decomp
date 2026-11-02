using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[Serializable]
public class TimeDilationClip : PlayableAsset, ITimelineClipAsset
{
	public float timeScale = 1f;

	public ClipCaps clipCaps => ClipCaps.Extrapolation | ClipCaps.Blending;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<TimeDilationBehaviour> scriptPlayable = ScriptPlayable<TimeDilationBehaviour>.Create(graph);
		scriptPlayable.GetBehaviour().TimeScale = timeScale;
		return scriptPlayable;
	}
}
