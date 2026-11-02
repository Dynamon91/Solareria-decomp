namespace SPR.Save;

public interface IStateSavable
{
	string ID { get; }

	void InitializeState()
	{
		int state = 0;
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			if (data.TryGetObjectStateNum(ID, out var state2))
			{
				state = state2;
			}
			else
			{
				data.SaveObjectState(ID, state);
			}
		}
		LoadState(state);
	}

	void LoadState(int state);

	void ChangeState(int state);
}
