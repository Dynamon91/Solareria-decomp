using System.Linq;
using Library;
using URPGlitch.Runtime.AnalogGlitch;
using URPGlitch.Runtime.DigitalGlitch;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackClipType(typeof(AnalogGlitchClip))]
public class AnalogGlitchTrack : TrackAsset
{
	private Volume postProcessingVolume;

	private AnalogGlitchVolume analogVolume;

	private DigitalGlitchVolume digitalVolume;

	private Volume PPVolume
	{
		get
		{
			if (BasicExtension.IsNull(postProcessingVolume))
			{
				postProcessingVolume = Object.FindObjectsOfType<Volume>().FirstOrDefault((Volume volume) => volume.isGlobal);
			}
			return postProcessingVolume;
		}
	}

	private AnalogGlitchVolume AnalogVolume
	{
		get
		{
			if (BasicExtension.IsNull(analogVolume) && !(Application.isPlaying ? Game.Instance.GraphicManager.PPVolume : PPVolume).profile.TryGet<AnalogGlitchVolume>(out analogVolume))
			{
				DebugLog.GraphicLogError("Volume Profile에서 AnalogGlitchVolume을 찾을 수 없습니다. Volume Profile에 추가해주세요.");
			}
			return analogVolume;
		}
	}

	private DigitalGlitchVolume DigitalVolume
	{
		get
		{
			if (BasicExtension.IsNull(digitalVolume) && !(Application.isPlaying ? Game.Instance.GraphicManager.PPVolume : PPVolume).profile.TryGet<DigitalGlitchVolume>(out digitalVolume))
			{
				DebugLog.GraphicLogError("Volume Profile에서 AnalogGlitchVolume을 찾을 수 없습니다. Volume Profile에 추가해주세요.");
			}
			return digitalVolume;
		}
	}

	public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
	{
		ScriptPlayable<AnalogGlitchMixerBehaviour> scriptPlayable = ScriptPlayable<AnalogGlitchMixerBehaviour>.Create(graph, inputCount);
		AnalogGlitchMixerBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.AnalogVolume = AnalogVolume;
		behaviour.DigitalVolume = DigitalVolume;
		return scriptPlayable;
	}
}
