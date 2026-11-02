using System;
using System.Collections.Generic;
using Library;
using Spine.Unity.Playables;
using UnityEngine;
using UnityEngine.Timeline;

namespace SPR;

[Serializable]
public abstract class AttackSequence
{
	[Serializable]
	public struct AnimationInfo
	{
		[SerializeField]
		public string name;

		[SerializeField]
		public int duration;

		[SerializeField]
		public bool loop;

		public AnimationInfo(string name, int duration, bool loop)
		{
			this.name = name;
			this.duration = duration;
			this.loop = loop;
		}
	}

	[SerializeField]
	protected List<AnimationInfo> animationList;

	[SerializeField]
	private bool showInGizmos;

	[SerializeField]
	private Zone sightArea;

	[SerializeField]
	protected TimelineEffect timelineEffect;

	[SerializeField]
	private bool checkPlatform;

	[SerializeField]
	private int chaseFrame;

	private List<AttackBehaviour> behaviourList;

	private List<IDamageable> attackedList = new List<IDamageable>();

	private int currentFrame;

	protected static ObjectPoolManager PoolManager => Game.Instance.ObjectPoolManager;

	public bool ShowInGizmos => showInGizmos;

	public bool IsDone => currentFrame >= behaviourList.Count;

	public bool CheckPlatform => checkPlatform;

	public int ChaseFrame => chaseFrame;

	public bool IsFirstFrame => currentFrame == 0;

	public void ResetFrame(Enemy enemy)
	{
		currentFrame = 0;
	}

	protected abstract List<AttackBehaviour> GenerateFrameBehaviour(Enemy enemy);

	public void Start(Enemy enemy)
	{
		OnStart_ForBoss(enemy);
		behaviourList = GenerateFrameBehaviour(enemy);
		attackedList.Clear();
	}

	protected virtual void OnStart_ForBoss(Enemy enemy)
	{
	}

	public void FixedUpdate(Enemy enemy, float fdt)
	{
		if (currentFrame < behaviourList.Count)
		{
			behaviourList[currentFrame].Invoke(fdt, attackedList);
			currentFrame++;
		}
	}

	public Zone GetSightZone()
	{
		return sightArea;
	}

	public Zone GetSightZone(Enemy enemy)
	{
		return sightArea.GetZoneOf(enemy);
	}

	public void SetAnimationList()
	{
		SpineAnimationStateTrack track = timelineEffect.GetTrack<SpineAnimationStateTrack>();
		animationList = new List<AnimationInfo>();
		foreach (TimelineClip clip in track.GetClips())
		{
			SpineAnimationStateClip spineAnimationStateClip = clip.asset.CastObject<SpineAnimationStateClip>();
			AnimationInfo item = new AnimationInfo(spineAnimationStateClip.template.animationReference.Animation.Name, Util.SecondsToFrame(clip.duration), spineAnimationStateClip.template.loop);
			animationList.Add(item);
		}
	}

	public bool IsSameTimelineEffect(TimelineEffect timelineEffect)
	{
		return this.timelineEffect == timelineEffect;
	}

	public void ForceFinish()
	{
		currentFrame = behaviourList.Count;
	}

	protected void ResetAttackList()
	{
		attackedList.Clear();
	}
}
