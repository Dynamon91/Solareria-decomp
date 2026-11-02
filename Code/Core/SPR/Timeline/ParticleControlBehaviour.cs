using System;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class ParticleControlBehaviour : PlayableBehaviour
{
	private enum State
	{
		None,
		Started,
		Decaying,
		Finished
	}

	private const float kUnsetTime = float.MaxValue;

	private float m_LastPlayableTime = float.MaxValue;

	private float m_LastParticleTime = float.MaxValue;

	private uint m_RandomSeed = 1u;

	public Transform Owner;

	public double ClipIn;

	public bool DetachOnEnd;

	private State _state;

	private double clipDuration;

	private float particleLifetime;

	private PostPlaybackState postPlaybackState;

	private GameObject rootGameObject;

	private float originSpeed;

	private State state
	{
		get
		{
			return _state;
		}
		set
		{
			_state = value;
		}
	}

	public ParticleSystem particleSystem { get; private set; }

	private bool IsRootParticle
	{
		get
		{
			if (!BasicExtension.IsNull(particleSystem))
			{
				return (object)particleSystem.gameObject == rootGameObject;
			}
			return false;
		}
	}

	public static ScriptPlayable<ParticleControlBehaviour> Create(PlayableGraph graph, ParticleSystem component, uint randomSeed, ParticleControlClipInfo info)
	{
		if (component == null)
		{
			return ScriptPlayable<ParticleControlBehaviour>.Null;
		}
		ScriptPlayable<ParticleControlBehaviour> result = ScriptPlayable<ParticleControlBehaviour>.Create(graph);
		result.GetBehaviour().Initialize(component, randomSeed, info);
		return result;
	}

	public void Initialize(ParticleSystem ps, uint randomSeed, ParticleControlClipInfo info)
	{
		m_RandomSeed = Math.Max(1u, randomSeed);
		particleSystem = ps;
		rootGameObject = GetRootGameObject(particleSystem);
		state = State.None;
		SetRandomSeed(particleSystem, m_RandomSeed);
		ApplyInfo(info);
		originSpeed = particleSystem.main.simulationSpeed;
	}

	private void ApplyInfo(ParticleControlClipInfo info)
	{
		clipDuration = info.ClipDuration;
		postPlaybackState = info.PlaybackState;
		particleLifetime = Util.GetParticlesMaxLifeTime(particleSystem);
	}

	private static void SetRandomSeed(ParticleSystem particleSystem, uint randomSeed)
	{
		if (!(particleSystem == null))
		{
			particleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			if (particleSystem.useAutoRandomSeed)
			{
				particleSystem.useAutoRandomSeed = false;
				particleSystem.randomSeed = randomSeed;
			}
			for (int i = 0; i < particleSystem.subEmitters.subEmittersCount; i++)
			{
				SetRandomSeed(particleSystem.subEmitters.GetSubEmitterSystem(i), ++randomSeed);
			}
		}
	}

	public override void PrepareFrame(Playable playable, FrameData data)
	{
		float num = (float)playable.GetTime();
		if (Application.isPlaying)
		{
			if (!IsRootParticle)
			{
				return;
			}
			State state = this.state;
			if (state == State.None || state == State.Finished)
			{
				return;
			}
			rootGameObject.SetActive(value: true);
			if (postPlaybackState != PostPlaybackState.Remain)
			{
				if (clipDuration - (double)num < (double)particleLifetime && this.state == State.Started)
				{
					this.state = State.Decaying;
					particleSystem.Stop();
				}
				else if (clipDuration - (double)num >= (double)particleLifetime && this.state == State.Decaying)
				{
					this.state = State.Started;
					particleSystem.time = (float)ClipIn;
					SetParticleSimulateSpeed(data.effectiveSpeed);
					particleSystem.Play();
				}
			}
		}
		else if (particleSystem == null || !particleSystem.gameObject.activeInHierarchy)
		{
			m_LastPlayableTime = float.MaxValue;
		}
		else
		{
			float time = particleSystem.time;
			if (m_LastPlayableTime > num || !Mathf.Approximately(time, m_LastParticleTime))
			{
				Simulate(num, restart: true);
			}
			else if (m_LastPlayableTime < num)
			{
				Simulate(num - m_LastPlayableTime, restart: false);
			}
			m_LastPlayableTime = num;
			m_LastParticleTime = particleSystem.time;
		}
	}

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		particleSystem.time = (float)ClipIn;
		SetParticleSimulateSpeed(info.effectiveSpeed);
		if (Application.isPlaying && state == State.Finished)
		{
			return;
		}
		m_LastPlayableTime = float.MaxValue;
		if (IsRootParticle)
		{
			rootGameObject.SetActive(value: true);
		}
		if (Application.isPlaying && state == State.None)
		{
			if (DetachOnEnd)
			{
				rootGameObject.transform.SetParent(Owner.transform);
				rootGameObject.transform.localScale = Vector3.one;
				rootGameObject.transform.localPosition = Vector3.zero;
			}
			particleSystem.Play();
			state = State.Started;
		}
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		SetParticleSimulateSpeed(1f);
		if (Application.isPlaying && DetachOnEnd)
		{
			rootGameObject.transform.SetParent(null);
		}
		if (!IsRootParticle)
		{
			return;
		}
		float num = (float)playable.GetTime();
		if (postPlaybackState == PostPlaybackState.Remain)
		{
			if (Application.isPlaying || (double)num >= clipDuration)
			{
				return;
			}
		}
		else
		{
			if (Application.isPlaying && state == State.Started && (double)num >= clipDuration)
			{
				TurnOff();
			}
			if (Application.isPlaying && state != State.Decaying)
			{
				return;
			}
		}
		if (Application.isPlaying)
		{
			UniTask.WaitWhile(() => particleSystem.IsAlive()).GetAwaiter().OnCompleted(TurnOff);
		}
		else
		{
			TurnOff();
		}
		void TurnOff()
		{
			rootGameObject.SetActive(value: false);
			m_LastPlayableTime = float.MaxValue;
			if (state != State.None)
			{
				state = State.Finished;
			}
		}
	}

	private void Simulate(float time, bool restart)
	{
		float maximumDeltaTime = Time.maximumDeltaTime;
		if (restart)
		{
			particleSystem.Simulate(0f, withChildren: false, restart: true, fixedTimeStep: false);
		}
		while (time > maximumDeltaTime)
		{
			particleSystem.Simulate(maximumDeltaTime, withChildren: false, restart: false, fixedTimeStep: false);
			time -= maximumDeltaTime;
		}
		if (time > 0f)
		{
			particleSystem.Simulate(time, withChildren: false, restart: false, fixedTimeStep: false);
		}
	}

	internal static GameObject GetRootGameObject(UnityEngine.Object componentOrGameObject)
	{
		GameObject gameObject = GetGameObject(componentOrGameObject);
		if (!(gameObject == null))
		{
			return GetRootParticleObject(gameObject);
		}
		return null;
	}

	private static GameObject GetRootParticleObject(GameObject go)
	{
		while (true)
		{
			Transform parent = go.transform.parent;
			if ((object)parent == null)
			{
				return go;
			}
			if (!parent.TryGetComponent<ParticleSystem>(out var _))
			{
				break;
			}
			go = parent.gameObject;
		}
		return go;
	}

	private static GameObject GetGameObject(UnityEngine.Object componentOrGameObject)
	{
		GameObject gameObject = componentOrGameObject as GameObject;
		if (gameObject != null)
		{
			return gameObject;
		}
		Component component = componentOrGameObject as Component;
		if (!(component != null))
		{
			return null;
		}
		return component.gameObject;
	}

	private void SetParticleSimulateSpeed(float speed)
	{
		if (!BasicExtension.IsNull(particleSystem))
		{
			ParticleSystem.MainModule main = particleSystem.main;
			main.simulationSpeed = originSpeed * speed;
		}
	}
}
