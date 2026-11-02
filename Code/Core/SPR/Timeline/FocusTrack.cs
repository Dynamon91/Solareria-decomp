using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackClipType(typeof(FocusClip))]
public class FocusTrack : TrackAsset
{
	[SerializeField]
	private bool useSetting;

	public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
	{
		ScriptPlayable<FocusMixerBehaviour> scriptPlayable = ScriptPlayable<FocusMixerBehaviour>.Create(graph, inputCount);
		scriptPlayable.GetBehaviour().UseSetting = useSetting;
		return scriptPlayable;
	}
}
