using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using SPR.Library;
using SPR.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR;

public class TimelineEffect : MonoBehaviour
{
	[Tooltip("true라면 Parent의 Child로 따라다닙니다. false라면 Parent와 독립적으로 움직인 뒤 재생이 끝나면 다시 Parent의 Child로 들어갑니다.")]
	[SerializeField]
	private bool followSource = true;

	[Tooltip("true일 때, 해당 이펙트 재생 시 모든 이펙트를 중단합니다")]
	[SerializeField]
	private bool cancelOther;

	[Tooltip("다른 이펙트 재생으로 인해 중단되지 않습니다.")]
	[SerializeField]
	private bool ignoreCancel;

	[Tooltip("true일 때, 캐릭터가 방향을 전환했을 경우 타임라인을 정지합니다.")]
	[SerializeField]
	private bool cancelOnFlip;

	[Tooltip("이 프레임 이전에 다른 이펙트가 재생되면 타임라인 재생을 중단합니다. cancelOther로는 취소됩니다.")]
	[SerializeField]
	private int reallyActiveFrame;

	private PlayableDirector director;

	private Timer timer;

	private Transform originParent;

	private Vector3 originLocalPosition;

	private Vector3 originLocalScale;

	private int startDirection;

	private Character parentCharacter;

	private bool paused;

	protected PlayableDirector Director => director ?? (director = GetComponent<PlayableDirector>());

	private Transform OriginParent
	{
		get
		{
			if (BasicExtension.IsNull(originParent))
			{
				SetOriginParent();
			}
			return originParent;
		}
	}

	public float Duration => (float)Director.duration;

	public bool IsEnd { get; private set; }

	public bool Loop { get; private set; }

	public bool CancelOther => cancelOther;

	public bool IgnoreCancel => ignoreCancel;

	public bool IsReallyActive => Director.time >= (double)((float)reallyActiveFrame / 60f);

	public bool IsPaused => Director.state == PlayState.Paused;

	public float Speed { get; set; } = 1f;

	public double CurrentTime => Director.time;

	public event Action OnStop;

	public virtual void Initialize()
	{
		Director.playOnAwake = false;
	}

	private void SetOriginParent()
	{
		if (BasicExtension.IsNull(originParent))
		{
			originParent = base.transform.parent;
			originLocalPosition = base.transform.localPosition;
			originLocalScale = base.transform.localScale;
		}
	}

	public virtual void Play(bool loop = false, float time = 0f)
	{
		if (!BasicExtension.IsNull(Director))
		{
			if (timer == null)
			{
				timer = new Timer(Duration - time);
			}
			timer.Reset();
			if (Director.state == PlayState.Playing)
			{
				Director.Stop();
				SetActive(value: false);
			}
			SetActive(value: true);
			Loop = loop;
			if (loop)
			{
				Director.extrapolationMode = DirectorWrapMode.Loop;
			}
			if (Director.playableGraph.IsValid())
			{
				Director.playableGraph.GetRootPlayable(0).SetSpeed(Speed);
			}
			Director.time = time;
			Director.Play();
			if (cancelOnFlip)
			{
				parentCharacter = base.transform.GetComponentInParentWithDepth<Character>(3);
				startDirection = parentCharacter.transform.localScale.x.GetSign();
			}
		}
	}

	public void Stop()
	{
		if (!BasicExtension.IsNull(Director))
		{
			Director.Stop();
			timer?.Reset();
			SetActive(value: false);
		}
	}

	public void Pause()
	{
		Director.Pause();
		paused = true;
	}

	public void Resume()
	{
		Director.Resume();
		paused = false;
	}

	public List<EnemyAttackTrack> GetAllAttackTracks()
	{
		if ((object)Director == null)
		{
			return new List<EnemyAttackTrack>();
		}
		return (from t in Director.playableAsset.CastObject<TimelineAsset>().GetOutputTracks()
			where t is EnemyAttackTrack
			select t).Cast<EnemyAttackTrack>().ToList();
	}

	public List<T> GetAllTracks<T>() where T : TrackAsset
	{
		return (from t in Director.playableAsset.CastObject<TimelineAsset>().GetOutputTracks()
			where t is T
			select t).Cast<T>().ToList();
	}

	public void UpdateDirector(float deltaTime)
	{
		if (Director.playableGraph.IsValid() && !paused && Director.timeUpdateMode == DirectorUpdateMode.Manual)
		{
			Director.playableGraph.Evaluate(deltaTime * Speed);
		}
	}

	public T GetTrack<T>(string trackName = null) where T : TrackAsset
	{
		if (!string.IsNullOrEmpty(trackName))
		{
			return Director.GetTrack<T>(trackName);
		}
		return Director.GetTrack<T>();
	}

	private void Update()
	{
		if (IsEnd || !base.gameObject.activeSelf || timer == null || Loop || paused)
		{
			return;
		}
		if (timer.IsDone)
		{
			this.OnStop?.Invoke();
			SetActive(value: false);
			return;
		}
		timer.Tick(Time.deltaTime);
		if (cancelOnFlip && parentCharacter.transform.localScale.x.GetSign() != startDirection)
		{
			Stop();
		}
	}

	private void SetActive(bool value)
	{
		if (!followSource)
		{
			if (value)
			{
				SetOriginParent();
				Vector3 position = base.transform.position;
				base.transform.SetParent(null);
				base.transform.position = position;
			}
			else
			{
				base.transform.SetParent(OriginParent);
				base.transform.localPosition = originLocalPosition;
				base.transform.localScale = originLocalScale;
			}
		}
		if (!value)
		{
			Director.Stop();
			timer?.Reset();
		}
		IsEnd = !value;
	}
}
