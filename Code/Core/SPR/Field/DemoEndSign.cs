using Cysharp.Threading.Tasks;
using SPR.Save;
using SPR.UI;

namespace SPR.Field;

public class DemoEndSign : InteractableObject
{
	private static ConfirmPopup ConfirmPopup => InteractableObject.UIManager.FieldCanvas.ConfirmPopup;

	private static SaveData SaveData => Game.Instance.SaveData;

	protected override void OnInteract()
	{
		InteractableObject.InteractionPanel.OnDeactivate += ShowReturnToAltarConfirmPopup;
		base.OnInteract();
	}

	private void ShowReturnToAltarConfirmPopup()
	{
		ConfirmModel model = new ConfirmModel("ReturnToRecentAltar", delegate
		{
			Game.Instance.LoadFieldAsync(SceneDataModel.RespawnModel(SaveData.LastFieldID)).GetAwaiter().OnCompleted(delegate
			{
				Game.Instance.SaveField();
			});
		});
		ConfirmPopup.SetModel(model);
		InteractableObject.InputManager.StackControlTarget(ConfirmPopup).Forget();
	}
}
