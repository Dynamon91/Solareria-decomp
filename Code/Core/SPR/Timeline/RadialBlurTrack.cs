using Library;
using OccaSoftware.RadialBlur.Runtime;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackClipType(typeof(RadialBlurClip))]
public class RadialBlurTrack : TrackAsset
{
	private Volume postProcessingVolume;

	private RadialBlurPostProcess RadialBlur
	{
		get
		{
			if (BasicExtension.IsNull(postProcessingVolume))
			{
				postProcessingVolume = (Application.isPlaying ? Game.Instance.GraphicManager.PPVolume : GetPPVolumeInEditor());
			}
			if (BasicExtension.IsNull(postProcessingVolume) || !postProcessingVolume.profile.TryGet<RadialBlurPostProcess>(out var component))
			{
				DebugLog.GraphicLogWarning("Volume Profile에서 RadialBlurPostProcess를 찾을 수 없습니다. Volume Profile에 추가해주세요.");
				return null;
			}
			return component;
		}
	}

	public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
	{
		ScriptPlayable<RadialBlurMixerBehaviour> scriptPlayable = ScriptPlayable<RadialBlurMixerBehaviour>.Create(graph, inputCount);
		scriptPlayable.SetInputCount(inputCount);
		scriptPlayable.GetBehaviour().RadialBlur = RadialBlur;
		return scriptPlayable;
	}

	private Volume GetPPVolumeInEditor()
	{
		if (!BasicExtension.IsNull(postProcessingVolume))
		{
			return postProcessingVolume;
		}
		Volume[] array = Object.FindObjectsOfType<Volume>();
		int num = int.MinValue;
		Volume[] array2 = array;
		foreach (Volume volume in array2)
		{
			if (volume.priority > (float)num)
			{
				postProcessingVolume = volume;
				num = (int)volume.priority;
			}
		}
		return postProcessingVolume;
	}
}
