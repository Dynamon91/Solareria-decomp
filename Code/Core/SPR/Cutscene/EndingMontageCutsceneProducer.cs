namespace SPR.Cutscene;

public class EndingMontageCutsceneProducer : SceneMontageCutsceneProducer
{
	protected override (FieldID fieldID, string cutsceneName) Target
	{
		get
		{
			if (Game.FieldID.Equals(SectorName.AshHills, 12))
			{
				return (fieldID: Trigger.CheckTrigger("Trigger_IsTrueEnding") ? new FieldID(SectorName.AbandonedTunnels, 1) : base.Target.fieldID, cutsceneName: base.Target.cutsceneName);
			}
			return base.Target;
		}
	}

	protected override SceneDataModel SceneDataModel
	{
		get
		{
			if (!Game.FieldID.Equals(SectorName.Library, 1))
			{
				return base.SceneDataModel;
			}
			return SceneDataModel.CreditModel();
		}
	}
}
