using Cysharp.Threading.Tasks;
using SPR.Cutscene;

namespace SPR.Field;

public class DarkChasaElevatorCallback : ElevatorCallbackBase
{
	public override void Invoke()
	{
		if (Trigger.CheckTrigger("Trigger_DarkChasaShouldResonance") && !VeritaCutsceneProducer.IsVeritaInfected)
		{
			Game.Instance.UIManager.TalkCanvas.ShowSpecialTalk("DarkChasaClear_Resonance").Forget();
		}
	}
}
