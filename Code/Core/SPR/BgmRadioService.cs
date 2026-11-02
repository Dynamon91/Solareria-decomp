using SPR.Field;
using SPR.Save;

namespace SPR;

public class BgmRadioService : ServiceObject, IStateSavable
{
	public const int DefaultState = 0;

	public const int OffState = 1;

	private Area area;

	public string ID => Name;

	private Area Area => GetComponentInParent<Area>();

	public void LoadState(int state)
	{
	}

	public void ChangeState(int state)
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	public void ChangeBgm(bool play)
	{
		if (play)
		{
			ServiceObject.SoundManager.PlayBGM(Area.SoundKey);
			ChangeState(0);
		}
		else
		{
			ServiceObject.SoundManager.StopBGM();
			ChangeState(1);
		}
	}
}
