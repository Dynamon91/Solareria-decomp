using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.Field;

public class EventElevatorLever : ElevatorLever
{
	[SerializeField]
	private GameObject lightObject;

	[SerializeField]
	private FieldID eventElevatorFieldID;

	private string EventElevatorID => "Elevator_" + eventElevatorFieldID.ToString();

	public override void LoadState(int state)
	{
		base.LoadState(state);
		if (ShouldCancel())
		{
			lightObject.gameObject.SetActive(value: false);
		}
	}

	protected override async UniTask OnActivateAsync()
	{
		if (ShouldCancel())
		{
			await CancelAsync();
		}
		else
		{
			await base.OnActivateAsync();
		}
	}

	private bool ShouldCancel()
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data) && data.TryGetObjectStateNum(EventElevatorID, out var state))
		{
			return state > 3;
		}
		return false;
	}
}
