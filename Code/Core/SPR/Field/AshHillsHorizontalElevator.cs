namespace SPR.Field;

public class AshHillsHorizontalElevator : HorizontalElevator
{
	public void Deactivate()
	{
		ChangeState(2);
		base.gameObject.SetActive(value: false);
		leftFloorLever.gameObject.SetActive(value: false);
	}

	protected override void LoadState_Internal(int state)
	{
		base.LoadState_Internal(state);
		if (state == 2)
		{
			base.gameObject.SetActive(value: false);
			leftFloorLever.gameObject.SetActive(value: false);
		}
	}

	protected override void OnEnter()
	{
		base.gameObject.SetActive(value: true);
		leftFloorLever.gameObject.SetActive(value: true);
	}
}
