using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackColor(0.11372549f, 0.61960787f, 20f / 51f)]
[TrackClipType(typeof(ParticleControlClip))]
public class ParticleControlTrack : TrackAsset
{
	public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
	{
		foreach (TimelineClip clip in GetClips())
		{
			ParticleControlClip particleControlClip = clip.asset as ParticleControlClip;
			if (particleControlClip != null)
			{
				particleControlClip.SetInfo(clip.start, clip.duration);
			}
		}
		return Playable.Create(graph, inputCount);
	}
}
