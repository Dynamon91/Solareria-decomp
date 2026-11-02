using SPR.Library;
using UnityEngine;

namespace SPR;

public class ImahenRepairMachine : ImahenMachineBase
{
	[SerializeField]
	private int repairCycleFrame;

	[SerializeField]
	private int repairAmount;

	[Header("VFX")]
	[SerializeField]
	private ParticleSystem particle;

	private FrameTimer timer;

	public override string Name => "Imahen_RepairMachine";

	protected override void OnTriggered()
	{
		base.OnTriggered();
		SetAnimation("operate/none", loop: false);
		timer = new FrameTimer(repairCycleFrame);
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if (IsTriggered)
		{
			if (timer.IsDone)
			{
				particle.Play();
				base.Area.ImahenCoreMachine.RegenHealth(repairAmount);
				timer.Reset();
			}
			timer.Tick();
		}
	}

	protected override void Die()
	{
		base.Die();
		particle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
	}
}
