using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class FillPhaseClip : PlayableAsset
{
	public int vibrationAmount = 3;

	public float maxFillPhase = 0.4f;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<FillPhaseBehaviour> scriptPlayable = ScriptPlayable<FillPhaseBehaviour>.Create(graph);
		FillPhaseBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.VibrationAmount = vibrationAmount;
		behaviour.MaxFillPhase = maxFillPhase;
		return scriptPlayable;
	}
}
