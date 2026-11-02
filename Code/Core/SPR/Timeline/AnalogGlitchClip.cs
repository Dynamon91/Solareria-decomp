using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class AnalogGlitchClip : PlayableAsset
{
	[Header("Analog")]
	public float scanLineJitter;

	public float verticalJump;

	public float horizontalShake;

	public float colorDrift;

	[Header("Digital")]
	public float digitalIntensity;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<AnalogGlitchBehaviour> scriptPlayable = ScriptPlayable<AnalogGlitchBehaviour>.Create(graph);
		AnalogGlitchBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.ScanLineJitter = scanLineJitter;
		behaviour.VerticalJump = verticalJump;
		behaviour.HorizontalShake = horizontalShake;
		behaviour.ColorDrift = colorDrift;
		behaviour.DigitalIntensity = digitalIntensity;
		return scriptPlayable;
	}
}
