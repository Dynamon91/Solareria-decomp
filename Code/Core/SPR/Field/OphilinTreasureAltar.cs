using Cysharp.Threading.Tasks;
using Library;
using SPR.Cutscene;
using SPR.Save;
using SPR.UI;
using UnityEngine;

namespace SPR.Field;

public class OphilinTreasureAltar : MonoBehaviour, IInteractable
{
	private const string AccessApprovedKey = "UndelCaverns_15_Approved";

	private const string AccessDenyKey = "UndelCaverns_15_Deny";

	[Header("References")]
	[SerializeField]
	private OphilinTreasureCutsceneProducer cutsceneProducer;

	[SerializeField]
	private NarrationBoard narrationBoard;

	[Header("Values")]
	[SerializeField]
	private Vector2Int interactCanvasPosition;

	[SerializeField]
	private string messageKey;

	[SerializeField]
	private float messageDuration;

	private Collider2D col;

	private FieldInteractCanvas interactCanvas;

	public string Name => "Ophilin Treasure Altar";

	public bool IsNull => this.IsNull();

	public int InteractableOrder => 1;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static ConfirmPopup ConfirmPopup => Game.Instance.UIManager.FieldCanvas.ConfirmPopup;

	public void AwakeObject(SaveData saveData)
	{
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas result)
		{
			interactCanvas = result;
		}).Forget();
		col = GetComponent<Collider2D>();
	}

	public void Interact()
	{
		interactCanvas.SetActive(value: false);
		if (Game.Instance.SaveData.PlayerData.GetAcquiredItemCount("OphilinTreasureKey") > 0)
		{
			ConfirmPopup.SetModel(new ConfirmModel("OphilinTreasureCutscene", OnConfirmInput));
			InputManager.StackControlTarget(ConfirmPopup).Forget();
			return;
		}
		Game.Instance.UIManager.FadeCanvas.WarningBoard.ShowWarning("UndelCaverns_15_Deny");
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_NegativePopup");
		col.enabled = false;
		UniTask.WaitForSeconds(messageDuration).GetAwaiter().OnCompleted(delegate
		{
			col.enabled = true;
		});
	}

	public void InteractEnter()
	{
		interactCanvas.SetActive(value: true);
	}

	public void InteractExit()
	{
		interactCanvas.SetActive(value: false);
	}

	private void OnConfirmInput()
	{
		cutsceneProducer.PlayTimelineAsync(0, blockControl: true).Forget();
		col.enabled = false;
		narrationBoard.SetLocale("UndelCaverns_15_Approved");
		narrationBoard.SetActiveDuration(messageDuration, messageDuration / 4f);
		Game.Instance.SaveData.PlayerData.TryRemoveAcquiredItem("OphilinTreasureKey");
	}
}
