using System.Collections.Generic;
using System.Linq;
using Library;
using SPR.Field;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR;

public class VeritaDownKickFlame : PoolingObject
{
	[SerializeField]
	private GameObject[] fieldEffects;

	[SerializeField]
	private GameObject[] endEffects;

	private PlayableDirector director;

	private TimelineEffect timelineEffect;

	public Area Area { get; set; }

	private PlayableDirector Director => director ?? (director = GetComponent<PlayableDirector>());

	private TimelineEffect TimelineEffect => timelineEffect ?? (timelineEffect = GetComponent<TimelineEffect>());

	public void SetMute()
	{
		TimelineEffect.OnStop -= ReturnToPool;
		IEnumerable<TrackAsset> outputTracks = Director.playableAsset.CastObject<TimelineAsset>().GetOutputTracks();
		TrackAsset[] array = (outputTracks as TrackAsset[]) ?? outputTracks.ToArray();
		TrackAsset[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].muted = false;
		}
		GameObject[] array3 = fieldEffects;
		foreach (GameObject fieldEffect in array3)
		{
			if (!Area.IsInArea(fieldEffect.transform.position))
			{
				TrackAsset trackAsset = array.FirstOrDefault((TrackAsset t) => t.name == fieldEffect.name);
				if ((object)trackAsset != null)
				{
					trackAsset.muted = true;
				}
			}
		}
		array3 = endEffects;
		foreach (GameObject endEffect in array3)
		{
			if (!Area.IsInArea(endEffect.transform.position))
			{
				TrackAsset trackAsset2 = array.FirstOrDefault((TrackAsset t) => t.name == endEffect.name);
				if ((object)trackAsset2 != null)
				{
					trackAsset2.muted = true;
				}
			}
		}
		Director.RebuildGraph();
	}

	public void PlayEffect()
	{
		TimelineEffect.Play();
		TimelineEffect.OnStop += ReturnToPool;
	}
}
