using Library;
using SPR.Save;
using UnityEngine;

public class PositionDirectionSaver : MonoBehaviour, IStateSavable
{
	private const float DigitAccuracy = 100f;

	private const int SignDigit = 1000000;

	private int currentState;

	private int previousState;

	private SectorData sectorData;

	public string ID => $"{Game.Instance.CurrentFieldID}_{base.name}";

	private int CurrentState => ((base.transform.localScale.x >= 0f) ? 1 : (-1)) * 1000000 + (base.transform.position.x * 100f).RoundToInt();

	private void OnEnable()
	{
		if (currentState != 0)
		{
			ChangeState(CurrentState);
		}
		else
		{
			currentState = CurrentState;
		}
	}

	private void FixedUpdate()
	{
		if (previousState != CurrentState)
		{
			ChangeState(CurrentState);
		}
		previousState = CurrentState;
	}

	public void LoadState(int state)
	{
		if (state == 0)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		int num = ((state > 0) ? 1 : (-1));
		base.transform.localScale = new Vector3(num, base.transform.localScale.y, base.transform.localScale.z);
		int num2 = state - num * 1000000;
		base.transform.position = new Vector3((float)num2 / 100f, base.transform.position.y, base.transform.position.z);
		currentState = state;
		DebugLog.GeneralLog($"{base.name} Load : {state} => {base.transform.position.x}, {base.transform.localScale.x}");
	}

	public void ChangeState(int state)
	{
		if (sectorData != null || Game.Instance.SaveData.WorldData.TryGetSectorData(out sectorData))
		{
			sectorData.SaveObjectState(ID, state);
			DebugLog.GeneralLog($"{base.name} Save : {state} => {base.transform.position.x}, {base.transform.localScale.x}");
		}
	}

	public void Save()
	{
		ChangeState(CurrentState);
	}
}
