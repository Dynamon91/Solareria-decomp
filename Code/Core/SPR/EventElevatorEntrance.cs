using UnityEngine;

namespace SPR;

public class EventElevatorEntrance : ElevatorEntrance
{
	[Header("이벤트용 씬 정보")]
	[SerializeField]
	private FieldID eventSceneID;

	[SerializeField]
	private string eventEntranceName;

	protected override FieldID TargetSceneID
	{
		get
		{
			if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
			{
				if (data.TryGetObjectStateNum(ElevatorID, out var state))
				{
					if (state != 0)
					{
						return targetSceneID;
					}
					return eventSceneID;
				}
				data.SaveObjectState(ElevatorID, 0);
				return eventSceneID;
			}
			return targetSceneID;
		}
	}

	private string ElevatorID => "Elevator_" + eventSceneID.ToString();

	protected override void Start()
	{
		base.Start();
		if (TargetSceneID == eventSceneID)
		{
			base.name = eventEntranceName;
		}
	}
}
