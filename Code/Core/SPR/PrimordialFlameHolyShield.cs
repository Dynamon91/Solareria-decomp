using Cysharp.Threading.Tasks;
using FMODUnity;
using UnityEngine;

namespace SPR;

public class PrimordialFlameHolyShield : MonoBehaviour
{
	[SerializeField]
	private ParticleSystem start;

	[SerializeField]
	private ParticleSystem loop;

	[SerializeField]
	private ParticleSystem end;

	[SerializeField]
	private float startLoopDelay;

	[SerializeField]
	private float endLoopDelay;

	[SerializeField]
	private EventReference spawnEvent;

	[SerializeField]
	private EventReference hitEvent;

	public bool IsActive { get; private set; }

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public async UniTaskVoid Activate()
	{
		start.Play();
		SoundManager.PlayOneShotAttached(spawnEvent, base.transform);
		await UniTask.WaitForSeconds(startLoopDelay);
		loop.Play();
		IsActive = true;
	}

	public async UniTaskVoid Deactivate(bool immediately = false)
	{
		IsActive = false;
		if (immediately)
		{
			loop.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			loop.gameObject.SetActive(value: false);
		}
		else
		{
			end.Play();
			await UniTask.WaitForSeconds(endLoopDelay);
			loop.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
		}
	}

	public void PlayHitEvent()
	{
		SoundManager.PlayOneShotAttached(hitEvent, base.transform);
	}
}
