using FMODUnity;
using UnityEngine;

namespace SPR;

public class IswLightning : LightningAttack
{
	[SerializeField]
	private int signDuration;

	[SerializeField]
	private int duration;

	[SerializeField]
	private EventReference lightningEvent;

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		Activate(signDuration, duration).Forget();
		Game.Instance.SoundManager.PlayOneShotAttached(lightningEvent, base.transform);
	}
}
