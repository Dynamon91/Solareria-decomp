using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Library;

public static class TimelineExtension
{
	public static void TrackBinding<T>(this PlayableDirector playableDirector, string trackName, Object value) where T : TrackAsset
	{
		T track = playableDirector.GetTrack<T>(trackName);
		playableDirector.SetGenericBinding(track, value);
	}

	public static T GetTrack<T>(this PlayableDirector playableDirector) where T : TrackAsset
	{
		return playableDirector.playableAsset.CastObject<TimelineAsset>().GetOutputTracks().First((TrackAsset t) => t is T)
			.CastObject<T>();
	}

	public static T GetTrack<T>(this PlayableDirector playableDirector, string trackName) where T : TrackAsset
	{
		return playableDirector.playableAsset.CastObject<TimelineAsset>().GetTrack<T>(trackName);
	}

	public static T GetTrack<T>(this PlayableAsset playableAsset, string trackName) where T : TrackAsset
	{
		return playableAsset.CastObject<TimelineAsset>().GetTrack<T>(trackName);
	}

	public static T GetTrack<T>(this TimelineAsset timeline, string trackName) where T : TrackAsset
	{
		return timeline.GetOutputTracks().First((TrackAsset t) => t is T && t.name.Equals(trackName)).CastObject<T>();
	}

	public static void EvaluateAtFirstFrame(this PlayableDirector playableDirector)
	{
		playableDirector.time = 0.0;
		playableDirector.Evaluate();
	}

	public static void EvaluateAtLastFrame(this PlayableDirector playableDirector)
	{
		PlayableAsset playableAsset = playableDirector.playableAsset;
		playableDirector.time = playableAsset.duration;
		playableDirector.Evaluate();
	}
}
