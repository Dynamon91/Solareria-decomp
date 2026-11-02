using Library;
using UnityEngine;

namespace SPR;

public class WorldParticle : MonoBehaviour, IEventListener
{
	[SerializeField]
	private WorldParticleEvent.WorldParticle particle;

	[SerializeField]
	private ParticleSystem sign;

	private void Awake()
	{
		LazySingleton<MessageSystem>.Instance.Subscribe<WorldParticleEvent>(this);
	}

	private void OnDestroy()
	{
		LazySingleton<MessageSystem>.Instance.UnSubscribe<WorldParticleEvent>(this);
	}

	public void OnEvent(global::Library.Event e)
	{
		if (e is WorldParticleEvent worldParticleEvent && worldParticleEvent.Particle == particle)
		{
			sign.Play(withChildren: true);
		}
	}
}
