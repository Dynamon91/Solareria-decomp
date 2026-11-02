using SPR.Graphic;
using SPR.Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class ColorVignetteMixerBehaviour : PlayableBehaviour
{
	private Material material;

	private readonly Color defaultColor = UnityEngine.Color.white;

	private readonly Color clearColor = UnityEngine.Color.clear;

	private static readonly int Color = Shader.PropertyToID("_Color");

	private static readonly int Radius = Shader.PropertyToID("_VignetteRadius");

	private Material Material => material ?? (material = GraphicHelper.GetRenderFeatureMaterial(GraphicManager.RenderFeatureName.ColorVignette));

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		Color b = clearColor;
		float num = 0f;
		float num2 = 0f;
		int inputCount = playable.GetInputCount();
		for (int i = 0; i < inputCount; i++)
		{
			float inputWeight = playable.GetInputWeight(i);
			ColorVignetteBehaviour behaviour = ((ScriptPlayable<ColorVignetteBehaviour>)playable.GetInput(i)).GetBehaviour();
			b += behaviour.Color * inputWeight;
			num += behaviour.Radius * inputWeight;
			num2 += inputWeight;
		}
		Color value = UnityEngine.Color.Lerp(clearColor, b, num2);
		float value2 = Mathf.Lerp(0f, num, num2);
		Material.SetColor(Color, value);
		Material.SetFloat(Radius, value2);
	}

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		GraphicHelper.SetRenderFeatureActive(GraphicManager.RenderFeatureName.ColorVignette, value: true);
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		GraphicHelper.SetRenderFeatureActive(GraphicManager.RenderFeatureName.ColorVignette, value: false);
		Material.SetColor(Color, defaultColor);
		Material.SetFloat(Radius, 1f);
	}

	public override void OnPlayableDestroy(Playable playable)
	{
		GraphicHelper.SetRenderFeatureActive(GraphicManager.RenderFeatureName.ColorVignette, value: false);
		Material.SetColor(Color, defaultColor);
		Material.SetFloat(Radius, 1f);
	}
}
