using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

public class NarrationSkipClip : PlayableAsset
{
	public float skipCheckTime;

	private TimelineClip clip;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<NarrationSkipBehaviour> scriptPlayable = ScriptPlayable<NarrationSkipBehaviour>.Create(graph);
		NarrationSkipBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.Director = owner.GetComponent<PlayableDirector>();
		behaviour.SkipCheckTime = skipCheckTime;
		behaviour.Clip = clip;
		return scriptPlayable;
	}

	public void SetClip(TimelineClip timelineClip)
	{
		clip = timelineClip;
	}
}
