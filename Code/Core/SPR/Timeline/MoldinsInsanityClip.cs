using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class MoldinsInsanityClip : PlayableAsset
{
	public Color color = new Color(0.6784314f, 0.20392157f, 69f / 85f, 1f);

	public float vignettePower = 1.5f;

	public float vignetteIntensity = 3.5f;

	public Vector2 noiseTiling = new Vector2(1f, 1f);

	public Vector2 noiseSpeed = new Vector2(-0.12f, -0.12f);

	public float noiseScale = 10f;

	public float breathFrequency = 3f;

	public Vector2 distortionSpeed = new Vector2(0.12f, 0.12f);

	public float distortionScale = 3f;

	[Range(0f, 0.2f)]
	public float distortionAmount = 0.12f;

	public Vector2 dissolveSpeed = new Vector2(0.5f, 0.5f);

	public float dissolveScale = 3f;

	[Range(0f, 1f)]
	public float dissolveAmount = 1f;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<MoldinsInsanityBehaviour> scriptPlayable = ScriptPlayable<MoldinsInsanityBehaviour>.Create(graph);
		MoldinsInsanityBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.Color = color;
		behaviour.VignettePower = vignettePower;
		behaviour.VignetteIntensity = vignetteIntensity;
		behaviour.NoiseTiling = noiseTiling;
		behaviour.NoiseSpeed = noiseSpeed;
		behaviour.NoiseScale = noiseScale;
		behaviour.BreathFrequency = breathFrequency;
		behaviour.DistortionSpeed = distortionSpeed;
		behaviour.DistortionScale = distortionScale;
		behaviour.DistortionAmount = distortionAmount;
		behaviour.DissolveSpeed = dissolveSpeed;
		behaviour.DissolveScale = dissolveScale;
		behaviour.DissolveAmount = dissolveAmount;
		return scriptPlayable;
	}
}
