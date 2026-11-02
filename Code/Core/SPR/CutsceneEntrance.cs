using UnityEngine;

namespace SPR;

public class CutsceneEntrance : OtherEntrance
{
	[Header("Cutscene Producer")]
	[SerializeField]
	private CutsceneProducer cutscene;

	public override void Interact()
	{
		if (Game.Instance.CurrentFieldID.ToString() == "AshHills_26" && Game.Instance.SaveData.EnemyFightData.IsBossCleared(BossName.Imahen) && !Game.Instance.SaveData.WanderingData.IsWatchedCutscene("NpcRescueCutsceneProducer"))
		{
			Game.Instance.UIManager.FadeCanvas.WarningBoard.ShowWarning("UI_Library_Front_Warning");
		}
		else if (cutscene.CurrentState == 0)
		{
			cutscene.PlayTimelineAsync(0, blockControl: true).GetAwaiter().OnCompleted(delegate
			{
				base.Interact();
			});
		}
		else
		{
			base.Interact();
		}
	}
}
