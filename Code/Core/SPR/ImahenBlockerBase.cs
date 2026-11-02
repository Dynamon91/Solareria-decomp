using SPR.Save;
using UnityEngine;

namespace SPR;

public abstract class ImahenBlockerBase : SpineObject, IStateSavable
{
	protected enum State
	{
		Closed,
		Opened
	}

	[SerializeField]
	protected float adjustSpeed;

	public string ID => $"{Game.Instance.CurrentFieldID.ToString()}_{base.transform.position}";

	public abstract void OnTriggered();

	public abstract void LoadState(int state);

	public void ChangeState(int state)
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}
}
