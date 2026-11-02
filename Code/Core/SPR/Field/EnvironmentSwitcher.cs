using SPR.Save;
using UnityEngine;

namespace SPR.Field;

public class EnvironmentSwitcher : MonoBehaviour, IStateSavable
{
	public const int Default = 0;

	public const int Switched = 1;

	[SerializeField]
	private GameObject from;

	[SerializeField]
	private GameObject to;

	public string ID => base.name;

	public void LoadState(int state)
	{
		switch (state)
		{
		case 0:
			from.SetActive(value: true);
			to.SetActive(value: false);
			break;
		case 1:
			from.SetActive(value: false);
			to.SetActive(value: true);
			break;
		}
	}

	public void ChangeState(int state)
	{
		LoadState(state);
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}
}
