using Cysharp.Threading.Tasks;
using FMODUnity;
using UnityEngine;
using UnityEngine.VFX;

namespace SPR;

public class SanctuaryLightController : MonoBehaviour
{
	[SerializeField]
	private VisualEffect flame;

	[SerializeField]
	private GameObject sfxEmitter;

	[SerializeField]
	private EventReference activateEvent;

	private bool wasActive;

	private bool isSoundEmitting;

	private void Awake()
	{
		wasActive = false;
		isSoundEmitting = false;
		sfxEmitter.SetActive(value: false);
	}

	private void Update()
	{
		if (flame.aliveParticleCount > 0)
		{
			if (!wasActive)
			{
				Game.Instance.SoundManager.PlayOneShotAttached(activateEvent, base.transform);
				wasActive = true;
			}
			if (!sfxEmitter.activeInHierarchy && !isSoundEmitting)
			{
				isSoundEmitting = true;
				UniTask.WaitForSeconds(Random.value * 0.5f).GetAwaiter().OnCompleted(delegate
				{
					sfxEmitter.SetActive(value: true);
					sfxEmitter.GetComponent<StudioEventEmitter>().EventInstance.setPitch(Random.value * 2f * 0.05f + 1f - 0.05f);
				});
			}
		}
		else if (wasActive)
		{
			wasActive = false;
		}
	}
}
