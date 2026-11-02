namespace SPR.Cutscene;

public class ChasaCutsceneTrigger : CutsceneTrigger
{
	private void OnEnable()
	{
		base.IsTriggered = false;
	}
}
