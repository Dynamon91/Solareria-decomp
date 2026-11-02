using SPR.Graphic;
using SPR.Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class MoldinsInsanityMixerBehaviour : PlayableBehaviour
{
	private Material material;

	private readonly Color defaultColor = UnityEngine.Color.white;

	private readonly Color clearColor = UnityEngine.Color.clear;

	private static readonly int Color = Shader.PropertyToID("_Color");

	private static readonly int VignettePower = Shader.PropertyToID("_VignettePower");

	private static readonly int VignetteIntensity = Shader.PropertyToID("_VignetteIntensity");

	private static readonly int NoiseTiling = Shader.PropertyToID("_NoiseTiling");

	private static readonly int NoiseSpeed = Shader.PropertyToID("_NoiseSpeed");

	private static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");

	private static readonly int BreathFrequency = Shader.PropertyToID("_BreathFrequency");

	private static readonly int DistortionSpeed = Shader.PropertyToID("_DistortionSpeed");

	private static readonly int DistortionScale = Shader.PropertyToID("_DistortionScale");

	private static readonly int DistortionAmount = Shader.PropertyToID("_DistortionAmount");

	private static readonly int DissolveSpeed = Shader.PropertyToID("_DissolveSpeed");

	private static readonly int DissolveScale = Shader.PropertyToID("_DissolveScale");

	private static readonly int DissolveAmount = Shader.PropertyToID("_DissolveAmount");

	private Material Material => material ?? (material = GraphicHelper.GetRenderFeatureMaterial(GraphicManager.RenderFeatureName.MoldinsInsanity));

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		Color b = clearColor;
		float num = 0f;
		float num2 = 0f;
		Vector2 zero = Vector2.zero;
		Vector2 zero2 = Vector2.zero;
		float num3 = 0f;
		float num4 = 0f;
		Vector2 zero3 = Vector2.zero;
		float num5 = 0f;
		float num6 = 0f;
		Vector2 zero4 = Vector2.zero;
		float num7 = 0f;
		float num8 = 0f;
		float num9 = 0f;
		int inputCount = playable.GetInputCount();
		for (int i = 0; i < inputCount; i++)
		{
			float inputWeight = playable.GetInputWeight(i);
			MoldinsInsanityBehaviour behaviour = ((ScriptPlayable<MoldinsInsanityBehaviour>)playable.GetInput(i)).GetBehaviour();
			b += behaviour.Color * inputWeight;
			num += behaviour.VignettePower * inputWeight;
			num2 += behaviour.VignetteIntensity * inputWeight;
			zero += behaviour.NoiseTiling * inputWeight;
			zero2 += behaviour.NoiseSpeed * inputWeight;
			num3 += behaviour.NoiseScale * inputWeight;
			num4 += behaviour.BreathFrequency * inputWeight;
			zero3 += behaviour.DistortionSpeed * inputWeight;
			num5 += behaviour.DistortionScale * inputWeight;
			num6 += behaviour.DistortionAmount * inputWeight;
			zero4 += behaviour.DissolveSpeed * inputWeight;
			num7 += behaviour.DissolveScale * inputWeight;
			num8 += behaviour.DissolveAmount * inputWeight;
			num9 += inputWeight;
		}
		Color color = UnityEngine.Color.Lerp(clearColor, b, num9);
		float vignettePower = Mathf.Lerp(0f, num, num9);
		float vignetteIntensity = Mathf.Lerp(0f, num2, num9);
		Vector2 noiseTiling = Vector2.Lerp(Vector2.zero, zero, num9);
		Vector2 noiseSpeed = Vector2.Lerp(Vector2.zero, zero2, num9);
		float noiseScale = Mathf.Lerp(0f, num3, num9);
		float breathFrequency = Mathf.Lerp(0f, num4, num9);
		Vector2 distortionSpeed = Vector2.Lerp(Vector2.zero, zero3, num9);
		float distortionScale = Mathf.Lerp(0f, num5, num9);
		float distortionAmount = Mathf.Lerp(0f, num6, num9);
		Vector2 dissolveSpeed = Vector2.Lerp(Vector2.zero, zero4, num9);
		float dissolveScale = Mathf.Lerp(0f, num7, num9);
		float dissolveAmount = Mathf.Lerp(0f, num8, num9);
		SetMaterial(color, vignettePower, vignetteIntensity, noiseTiling, noiseSpeed, noiseScale, breathFrequency, distortionSpeed, distortionScale, distortionAmount, dissolveSpeed, dissolveScale, dissolveAmount);
	}

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		GraphicHelper.SetRenderFeatureActive(GraphicManager.RenderFeatureName.MoldinsInsanity, value: true);
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		GraphicHelper.SetRenderFeatureActive(GraphicManager.RenderFeatureName.MoldinsInsanity, value: false);
		SetMaterialDefault();
	}

	public override void OnPlayableDestroy(Playable playable)
	{
		GraphicHelper.SetRenderFeatureActive(GraphicManager.RenderFeatureName.MoldinsInsanity, value: false);
		SetMaterialDefault();
	}

	private void SetMaterial(Color color, float vignettePower, float vignetteIntensity, Vector2 noiseTiling, Vector2 noiseSpeed, float noiseScale, float breathFrequency, Vector2 distortionSpeed, float distortionScale, float distortionAmount, Vector2 dissolveSpeed, float dissolveScale, float dissolveAmount)
	{
		Material.SetColor(Color, color);
		Material.SetFloat(VignettePower, vignettePower);
		Material.SetFloat(VignetteIntensity, vignetteIntensity);
		Material.SetVector(NoiseTiling, noiseTiling);
		Material.SetVector(NoiseSpeed, noiseSpeed);
		Material.SetFloat(NoiseScale, noiseScale);
		Material.SetFloat(BreathFrequency, breathFrequency);
		Material.SetVector(DistortionSpeed, distortionSpeed);
		Material.SetFloat(DistortionScale, distortionScale);
		Material.SetFloat(DistortionAmount, distortionAmount);
		Material.SetVector(DissolveSpeed, dissolveSpeed);
		Material.SetFloat(DissolveScale, dissolveScale);
		Material.SetFloat(DissolveAmount, dissolveAmount);
	}

	private void SetMaterialDefault()
	{
		SetMaterial(new Color(0.6784314f, 0.20392157f, 69f / 85f, 1f), 1.5f, 3.5f, new Vector2(1f, 1f), new Vector2(-0.12f, -0.12f), 10f, 3f, new Vector2(0.12f, 0.12f), 3f, 0.12f, new Vector2(0.5f, 0.5f), 3f, 1f);
	}
}
