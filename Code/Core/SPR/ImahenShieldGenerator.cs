using Spine;
using UnityEngine;

namespace SPR;

public class ImahenShieldGenerator : ImahenMachineBase
{
	[Header("VFX")]
	[SerializeField]
	private ParticleSystem particle;

	public override string Name => "Imahen_ShieldGenerator";

	protected override void OnTriggered()
	{
		base.OnTriggered();
		SetAnimation("operate/left_right_top", loop: false);
	}

	protected override void OnAnimationComplete(TrackEntry trackEntry)
	{
		base.OnAnimationComplete(trackEntry);
		if (trackEntry.Animation.Name == "operate/left_right_top")
		{
			particle.Play();
			base.Area.ImahenCoreMachine.EnableShield();
		}
	}

	protected override void Die()
	{
		base.Die();
		particle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		base.Area.ImahenCoreMachine.TryDisableShield();
	}
}
