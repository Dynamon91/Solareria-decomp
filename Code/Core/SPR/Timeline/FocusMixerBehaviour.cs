using Library;
using SPR.Graphic;
using SPR.Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class FocusMixerBehaviour : PlayableBehaviour
{
	public bool UseSetting;

	private Material material;

	private readonly Color defaultColor = UnityEngine.Color.white;

	private readonly Color clearColor = UnityEngine.Color.clear;

	private static readonly int Color = Shader.PropertyToID("_Color");

	private static readonly int Alpha = Shader.PropertyToID("_Alpha");

	private static readonly int CentralPoint = Shader.PropertyToID("_Central_point");

	private static readonly int MaskSize = Shader.PropertyToID("_Mask_Size");

	private Material Material => material ?? (material = GraphicHelper.GetRenderFeatureMaterial(GraphicManager.RenderFeatureName.Focus));

	private bool ShouldNotPlay
	{
		get
		{
			if (UseSetting)
			{
				return !Game.Instance.SettingData.PyronActionVignette;
			}
			return false;
		}
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (ShouldNotPlay)
		{
			return;
		}
		Color b = clearColor;
		float num = 0f;
		Vector2 zero = Vector2.zero;
		float num2 = 0f;
		float num3 = 0f;
		int inputCount = playable.GetInputCount();
		for (int i = 0; i < inputCount; i++)
		{
			float inputWeight = playable.GetInputWeight(i);
			FocusBehaviour behaviour = ((ScriptPlayable<FocusBehaviour>)playable.GetInput(i)).GetBehaviour();
			b += behaviour.Color * inputWeight;
			num += behaviour.Alpha * inputWeight;
			if (BasicExtension.IsNull(behaviour.Target))
			{
				zero += behaviour.CentralPoint * inputWeight;
			}
			else
			{
				zero += GraphicHelper.GetNormalizedScreenPoint(behaviour.Target.position) * inputWeight;
			}
			num2 += behaviour.MaskSize * inputWeight;
			num3 += inputWeight;
		}
		Color value = UnityEngine.Color.Lerp(clearColor, b, num3);
		float value2 = Mathf.Lerp(0f, num, num3);
		Vector2 vector = Vector2.Lerp(Vector2.zero, zero, num3);
		float value3 = Mathf.Lerp(0f, num2, num3);
		Material.SetColor(Color, value);
		Material.SetFloat(Alpha, value2);
		Material.SetVector(CentralPoint, vector);
		Material.SetFloat(MaskSize, value3);
	}

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		if (!ShouldNotPlay)
		{
			GraphicHelper.SetRenderFeatureActive(GraphicManager.RenderFeatureName.Focus, value: true);
		}
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		if (!ShouldNotPlay)
		{
			GraphicHelper.SetRenderFeatureActive(GraphicManager.RenderFeatureName.Focus, value: false);
			Material.SetColor(Color, defaultColor);
			Material.SetFloat(Alpha, 0f);
		}
	}

	public override void OnPlayableDestroy(Playable playable)
	{
		if (!ShouldNotPlay)
		{
			GraphicHelper.SetRenderFeatureActive(GraphicManager.RenderFeatureName.Focus, value: false);
			Material.SetColor(Color, defaultColor);
			Material.SetFloat(Alpha, 0f);
		}
	}
}
