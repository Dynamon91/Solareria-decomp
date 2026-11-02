using System.Collections.Generic;
using System.Linq;
using SPR.Graphic;
using SPR.Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public abstract class ShockWaveMixerBehaviourBase : PlayableBehaviour
{
	protected enum ShockWaveType
	{
		First,
		Second,
		Third
	}

	private Material material;

	private readonly int ringPosition;

	private readonly int distance;

	private readonly int size;

	private readonly int power;

	private readonly ShockWaveType type;

	private static readonly Dictionary<ShockWaveType, bool> ActiveWaves = new Dictionary<ShockWaveType, bool>();

	private Material Material => material ?? (material = GraphicHelper.GetRenderFeatureMaterial(GraphicManager.RenderFeatureName.ShockWave));

	protected ShockWaveMixerBehaviourBase(ShockWaveType type)
	{
		this.type = type;
		string text = type switch
		{
			ShockWaveType.Third => "3", 
			ShockWaveType.Second => "2", 
			_ => "", 
		};
		ringPosition = Shader.PropertyToID("_RingSpawnPosition" + text);
		distance = Shader.PropertyToID("_WaveDistanceFromCenter" + text);
		size = Shader.PropertyToID("_ShockwaveSize" + text);
		power = Shader.PropertyToID("_DistortionPower" + text);
	}

	private static void SetThisShockwaveActive(ShockWaveType type, bool active)
	{
		if (!ActiveWaves.TryAdd(type, active))
		{
			ActiveWaves[type] = active;
		}
		bool value = ActiveWaves.Values.Any((bool v) => v);
		GraphicHelper.SetRenderFeatureActive(GraphicManager.RenderFeatureName.ShockWave, value);
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (playerData is Transform transform)
		{
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			int inputCount = playable.GetInputCount();
			for (int i = 0; i < inputCount; i++)
			{
				float inputWeight = playable.GetInputWeight(i);
				ShockWaveBehaviour behaviour = ((ScriptPlayable<ShockWaveBehaviour>)playable.GetInput(i)).GetBehaviour();
				num += behaviour.Size * inputWeight;
				num2 += behaviour.Distance * inputWeight;
				num3 += behaviour.Power * inputWeight;
				num4 += inputWeight;
			}
			float value = Mathf.Lerp(0f, num, num4);
			float value2 = Mathf.Lerp(0f, num2, num4);
			float num5 = Mathf.Lerp(0f, num3, num4);
			if (num5 <= 0f)
			{
				DeactivateWave();
				return;
			}
			SetThisShockwaveActive(type, active: true);
			Material.SetVector(ringPosition, GraphicHelper.GetNormalizedScreenPoint(transform.position));
			Material.SetFloat(size, value);
			Material.SetFloat(distance, value2);
			Material.SetFloat(power, num5);
		}
	}

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		SetThisShockwaveActive(type, active: true);
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		DeactivateWave();
	}

	public override void OnPlayableDestroy(Playable playable)
	{
		DeactivateWave();
	}

	private void DeactivateWave()
	{
		Material.SetFloat(size, 0f);
		Material.SetFloat(distance, 0f);
		Material.SetFloat(power, 0f);
		SetThisShockwaveActive(type, active: false);
	}
}
