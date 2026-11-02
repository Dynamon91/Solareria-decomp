using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackClipType(typeof(FillPhaseClip))]
[TrackBindingType(typeof(Renderer))]
public class FillPhaseTrack : TrackAsset
{
	protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
	{
		ScriptPlayable<FillPhaseBehaviour> scriptPlayable = ScriptPlayable<FillPhaseBehaviour>.Create(graph);
		FillPhaseBehaviour behaviour = scriptPlayable.GetBehaviour();
		FillPhaseClip fillPhaseClip = (FillPhaseClip)clip.asset;
		behaviour.VibrationAmount = fillPhaseClip.vibrationAmount;
		behaviour.MaxFillPhase = fillPhaseClip.maxFillPhase;
		return scriptPlayable;
	}
}
