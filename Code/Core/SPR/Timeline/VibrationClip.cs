using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

public class VibrationClip : PlayableAsset
{
	[Header("Vibration Intensity")]
	[Range(0f, 1f)]
	public float lowMotor;

	[Range(0f, 1f)]
	public float highMotor;

	public ClipCaps clipCaps => ClipCaps.None;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<VibrationBehaviour> scriptPlayable = ScriptPlayable<VibrationBehaviour>.Create(graph);
		VibrationBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.LowMotor = lowMotor;
		behaviour.HighMotor = highMotor;
		return scriptPlayable;
	}
}
