using System.Collections.Generic;
using UnityEngine;

namespace SPR.Field;

public class VanguardArea : LegendaryArea
{
	[SerializeField]
	private Transform placeholderTransform;

	[SerializeField]
	private List<SpineDoor> doors;

	protected override void OnInitialize()
	{
		if (Game.Instance.SaveData.WorldData.IsEliminatedCreature(Game.Instance.CurrentFieldID, placeholderTransform.position))
		{
			SetWalls(value: false);
			TryDisableArea();
		}
		else
		{
			base.OnInitialize();
			base.LegendaryBounds.OnEnter += CloseDoor;
		}
	}

	private void CloseDoor()
	{
		SetWalls(value: true);
		foreach (SpineDoor door in doors)
		{
			door.SetDoorOpen(open: false);
		}
	}

	public void OpenDoor()
	{
		SetWalls(value: false);
		foreach (SpineDoor door in doors)
		{
			door.SetDoorOpen(open: true);
		}
	}
}
