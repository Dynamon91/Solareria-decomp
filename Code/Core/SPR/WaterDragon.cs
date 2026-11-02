using SPR.Save;
using UnityEngine;

namespace SPR;

public class WaterDragon : Npc
{
	[SerializeField]
	private Transform underWaterCanvasPosition;

	[SerializeField]
	private ParticleSystem dive;

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		SetAnimation("dive_begin", loop: false);
	}

	public override void InteractEnter()
	{
		if (GetCurrentAnim(0).Name == "dive_begin")
		{
			ChangeInteractCanvasPosition(base.transform.InverseTransformPoint(underWaterCanvasPosition.position));
		}
		else
		{
			ChangeInteractCanvasPosition(interactCanvasPosition);
		}
		base.InteractEnter();
	}

	protected override void EnterIdleAnimation()
	{
	}
}
