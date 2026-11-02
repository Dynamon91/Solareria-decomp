using FMOD.Studio;
using FMODUnity;
using Library;
using UnityEngine;

namespace SPR.Field;

public class MovingPlatformSoundPlayer : MonoBehaviour
{
	[SerializeField]
	private EventReference operateEvent;

	[SerializeField]
	private EventReference stopEvent;

	[SerializeField]
	private EventReference disconnectEvent;

	[SerializeField]
	private EventReference connectEvent;

	private EventInstance operateInstance;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public void PlayOperate(Transform source = null)
	{
		operateInstance = (source.IsNull() ? SoundManager.PlayOneShot(operateEvent) : SoundManager.PlayOneShotAttached(operateEvent, source));
	}

	public void PlayStop(Transform source = null)
	{
		operateInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		operateInstance.release();
		if (source.IsNull())
		{
			SoundManager.PlayOneShot(stopEvent);
		}
		else
		{
			SoundManager.PlayOneShotAttached(stopEvent, source);
		}
	}

	public void PlayDisconnect(Transform source = null)
	{
		if (source.IsNull())
		{
			SoundManager.PlayOneShot(disconnectEvent);
		}
		else
		{
			SoundManager.PlayOneShotAttached(disconnectEvent, source);
		}
	}

	public void PlayConnect(Transform source = null)
	{
		if (source.IsNull())
		{
			SoundManager.PlayOneShot(connectEvent);
		}
		else
		{
			SoundManager.PlayOneShotAttached(connectEvent, source);
		}
	}
}
