using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.Field;

public class DoorLever : InteractableLever
{
	[SerializeField]
	private Door door;

	public override string ID => Util.GetID(Game.FieldID, "Door Lever");

	public override void LoadState(int state)
	{
		switch (state)
		{
		case 0:
			door.SetDoorOpen(open: false);
			break;
		case 1:
			door.SetDoorOpen(open: true);
			break;
		}
		base.LoadState(state);
	}

	protected override async UniTask OnActivateAsync()
	{
		ChangeState(1);
		door.SetDoorOpen(open: true);
		await UniTask.CompletedTask;
	}
}
