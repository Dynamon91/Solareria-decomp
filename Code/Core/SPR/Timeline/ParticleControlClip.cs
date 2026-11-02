using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

public class ParticleControlClip : PlayableAsset, IPropertyPreview, ITimelineClipAsset
{
	private const int k_MaxRandInt = 10000;

	private static readonly HashSet<ParticleSystem> s_SubEmitterCollector = new HashSet<ParticleSystem>();

	[SerializeField]
	public ExposedReference<GameObject> sourceGameObject;

	[SerializeField]
	public uint particleRandomSeed;

	[SerializeField]
	public PostPlaybackState postPlayback;

	public double clipIn;

	public bool detachOnEnd;

	private double m_Duration = PlayableBinding.DefaultDuration;

	private static HashSet<PlayableDirector> s_ProcessedDirectors = new HashSet<PlayableDirector>();

	private GameObject owner;

	private double clipStart;

	private double clipDuration;

	internal bool controllingDirectors { get; private set; }

	internal bool controllingParticles { get; private set; }

	public override double duration => m_Duration;

	public ClipCaps clipCaps => ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

	public void OnEnable()
	{
		if (particleRandomSeed == 0)
		{
			particleRandomSeed = (uint)UnityEngine.Random.Range(1, 10000);
		}
	}

	public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
	{
		Playable playable = Playable.Null;
		List<Playable> list = new List<Playable>();
		owner = go;
		GameObject gameObject = sourceGameObject.Resolve(graph.GetResolver());
		m_Duration = PlayableBinding.DefaultDuration;
		controllingParticles = false;
		controllingDirectors = false;
		if (gameObject != null)
		{
			IList<ParticleSystem> controllableParticleSystems = GetControllableParticleSystems(gameObject);
			UpdateDurationAndLoopFlag(controllableParticleSystems);
			_ = go.GetComponent<PlayableDirector>() != null;
			if (go == gameObject)
			{
				Debug.LogWarningFormat("Control Playable ({0}) is referencing the same PlayableDirector component than the one in which it is playing.", base.name);
			}
			SearchHierarchyAndConnectParticleSystem(controllableParticleSystems, graph, list);
			playable = ConnectPlayablesToMixer(graph, list);
		}
		if (!playable.IsValid())
		{
			playable = Playable.Create(graph);
		}
		return playable;
	}

	public void SetInfo(double start, double duration)
	{
		clipStart = start;
		clipDuration = duration;
	}

	private static Playable ConnectPlayablesToMixer(PlayableGraph graph, List<Playable> playables)
	{
		Playable playable = Playable.Create(graph, playables.Count);
		for (int i = 0; i != playables.Count; i++)
		{
			ConnectMixerAndPlayable(graph, playable, playables[i], i);
		}
		playable.SetPropagateSetTime(value: true);
		return playable;
	}

	public void UpdateDurationAndLoopFlag(IList<ParticleSystem> particleSystems)
	{
		if (particleSystems.Count == 0)
		{
			return;
		}
		double num = double.NegativeInfinity;
		bool flag = false;
		foreach (ParticleSystem particleSystem in particleSystems)
		{
			num = Math.Max(num, particleSystem.main.duration);
			flag = flag || particleSystem.main.loop;
		}
		m_Duration = (double.IsNegativeInfinity(num) ? PlayableBinding.DefaultDuration : num);
	}

	private IList<ParticleSystem> GetControllableParticleSystems(GameObject go)
	{
		List<ParticleSystem> list = new List<ParticleSystem>();
		if (go.GetComponent<ParticleSystem>() != null)
		{
			GetControllableParticleSystems(go.transform, list, s_SubEmitterCollector);
			s_SubEmitterCollector.Clear();
		}
		return list;
	}

	private void SearchHierarchyAndConnectParticleSystem(IEnumerable<ParticleSystem> particleSystems, PlayableGraph graph, List<Playable> outplayables)
	{
		foreach (ParticleSystem particleSystem in particleSystems)
		{
			if (particleSystem != null)
			{
				controllingParticles = true;
				ParticleControlClipInfo info = new ParticleControlClipInfo
				{
					ClipDuration = clipDuration,
					PlaybackState = postPlayback
				};
				ScriptPlayable<ParticleControlBehaviour> scriptPlayable = ParticleControlBehaviour.Create(graph, particleSystem, particleRandomSeed, info);
				scriptPlayable.GetBehaviour().ClipIn = clipIn;
				scriptPlayable.GetBehaviour().DetachOnEnd = detachOnEnd;
				scriptPlayable.GetBehaviour().Owner = owner.transform;
				outplayables.Add(scriptPlayable);
			}
		}
	}

	private static void GetControllableParticleSystems(Transform t, ICollection<ParticleSystem> roots, HashSet<ParticleSystem> subEmitters)
	{
		ParticleSystem component = t.GetComponent<ParticleSystem>();
		if (component != null && !subEmitters.Contains(component))
		{
			roots.Add(component);
			CacheSubEmitters(component, subEmitters);
		}
		for (int i = 0; i < t.childCount; i++)
		{
			GetControllableParticleSystems(t.GetChild(i), roots, subEmitters);
		}
	}

	private static void CacheSubEmitters(ParticleSystem ps, HashSet<ParticleSystem> subEmitters)
	{
		if (!(ps == null))
		{
			for (int i = 0; i < ps.subEmitters.subEmittersCount; i++)
			{
				subEmitters.Add(ps.subEmitters.GetSubEmitterSystem(i));
			}
		}
	}

	public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
	{
		if (!(director == null) && !s_ProcessedDirectors.Contains(director))
		{
			s_ProcessedDirectors.Add(director);
			GameObject gameObject = sourceGameObject.Resolve(director);
			if (gameObject != null)
			{
				PreviewParticles(driver, gameObject.GetComponentsInChildren<ParticleSystem>(includeInactive: true));
			}
			s_ProcessedDirectors.Remove(director);
		}
	}

	internal static void PreviewParticles(IPropertyCollector driver, IEnumerable<ParticleSystem> particles)
	{
		foreach (ParticleSystem particle in particles)
		{
			driver.AddFromName<ParticleSystem>(particle.gameObject, "randomSeed");
			driver.AddFromName<ParticleSystem>(particle.gameObject, "autoRandomSeed");
		}
	}

	private static void ConnectMixerAndPlayable(PlayableGraph graph, Playable mixer, Playable playable, int portIndex)
	{
		graph.Connect(playable, 0, mixer, portIndex);
		mixer.SetInputWeight(playable, 1f);
	}

	public IList<T> GetComponent<T>(GameObject gameObject)
	{
		List<T> list = new List<T>();
		if (gameObject != null)
		{
			gameObject.GetComponents(list);
		}
		return list;
	}
}
