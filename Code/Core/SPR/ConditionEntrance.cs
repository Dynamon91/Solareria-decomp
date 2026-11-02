using UnityEngine;

namespace SPR;

public class ConditionEntrance : Entrance
{
	[Header("If Condition Is True...")]
	[SerializeField]
	private string conditionTrigger;

	[SerializeField]
	private FieldID conditionSceneID;

	[SerializeField]
	private string conditionEntranceName;

	protected override bool CanConnectMap => true;

	protected override SceneDataModel GetSceneDataModel()
	{
		bool num = Trigger.CheckTrigger(conditionTrigger);
		FieldID fieldID = (num ? conditionSceneID : TargetSceneID);
		string entranceName = (base.name = (num ? conditionEntranceName : base.name));
		return SceneDataModel.MoveModel(fieldID, entranceName);
	}
}
