using System.Collections.Generic;
using System.Linq;
using Library;
using SPR.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace SPR;

public class VFXController : MonoBehaviour
{
	private Dictionary<string, TimelineEffect> vfxDictionary;

	public TimelineEffect CurrentTimeline { get; private set; }

	private Dictionary<string, TimelineEffect> VFXDictionary
	{
		get
		{
			if (vfxDictionary == null)
			{
				InitializeVFXDictionary();
			}
			return vfxDictionary;
		}
	}

	private void Awake()
	{
	}

	public void InitializeVFXDictionary()
	{
		TimelineEffect[] componentsInChildren = GetComponentsInChildren<TimelineEffect>(includeInactive: true);
		vfxDictionary = new Dictionary<string, TimelineEffect>();
		TimelineEffect[] array = componentsInChildren;
		foreach (TimelineEffect timelineEffect in array)
		{
			timelineEffect.Initialize();
			vfxDictionary.Add(timelineEffect.name, timelineEffect);
		}
	}

	public void Play(string animationName, bool loop = false, float time = 0f)
	{
		bool flag = false;
		if (VFXDictionary.ContainsKey(animationName))
		{
			CurrentTimeline = VFXDictionary[animationName];
			flag = CurrentTimeline.CancelOther;
			CurrentTimeline.Play(loop, time);
		}
		foreach (KeyValuePair<string, TimelineEffect> item in VFXDictionary)
		{
			if (!(item.Key == animationName) && !item.Value.IgnoreCancel)
			{
				if (flag)
				{
					item.Value.Stop();
				}
				else if (item.Value.Loop || !item.Value.IsReallyActive)
				{
					item.Value.Stop();
				}
			}
		}
	}

	public void Stop(string animationName)
	{
		if (!VFXDictionary.ContainsKey(animationName))
		{
			DebugLog.GraphicLogWarning(base.transform.parent.name + "에게는 " + animationName + "에 대한 vfx가 없습니다.");
		}
		else
		{
			VFXDictionary[animationName].Stop();
		}
	}

	public void StopAll()
	{
		foreach (KeyValuePair<string, TimelineEffect> item in VFXDictionary)
		{
			item.Value.Stop();
		}
		CurrentTimeline = null;
	}

	public void Pause()
	{
		CurrentTimeline?.Pause();
	}

	public void Resume()
	{
		CurrentTimeline?.Resume();
	}

	public int GetDuration(string key)
	{
		return Util.SecondsToFrame(VFXDictionary[key].Duration);
	}

	public List<Zone> GetAllAttackZones()
	{
		List<EnemyAttackTrack> list = new List<EnemyAttackTrack>();
		foreach (TimelineEffect value in VFXDictionary.Values)
		{
			list.AddRange(value.GetAllAttackTracks());
		}
		List<Zone> list2 = new List<Zone>();
		IEnumerable<TimelineClip> source = list.SelectMany((EnemyAttackTrack t) => t.GetClips());
		IEnumerable<EnemyAttackClip> source2 = from c in source
			where c.asset is EnemyAttackClip enemyAttackClip && enemyAttackClip.showInGizmo
			select c.asset.CastObject<EnemyAttackClip>();
		IEnumerable<EnemyDynamicAttackClip> source3 = from c in source
			where c.asset is EnemyDynamicAttackClip enemyDynamicAttackClip && enemyDynamicAttackClip.showInGizmo
			select c.asset.CastObject<EnemyDynamicAttackClip>();
		list2.AddRange(source2.SelectMany((EnemyAttackClip clip) => clip.zones));
		list2.AddRange(source3.Select((EnemyDynamicAttackClip clip) => clip.zone));
		return list2;
	}

	public bool IsTimelineEnd(string timelineName)
	{
		if (VFXDictionary.ContainsKey(timelineName))
		{
			return VFXDictionary[timelineName].IsEnd;
		}
		return true;
	}

	public void UpdateTimeline(float deltaTime)
	{
		if ((object)CurrentTimeline == null)
		{
			return;
		}
		CurrentTimeline.UpdateDirector(deltaTime);
		foreach (var (_, timelineEffect2) in VFXDictionary)
		{
			if (timelineEffect2.gameObject.activeSelf && timelineEffect2 != CurrentTimeline)
			{
				timelineEffect2.UpdateDirector(deltaTime);
			}
		}
	}

	public void SetSpeedAll(float speed)
	{
		VFXDictionary.Values.ForEach((TimelineEffect x) => x.Speed = speed);
	}

	public T GetTimeline<T>(string timelineName) where T : TimelineEffect
	{
		if (VFXDictionary.ContainsKey(timelineName))
		{
			return VFXDictionary[timelineName].CastObject<T>();
		}
		return null;
	}
}
