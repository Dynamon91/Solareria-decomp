using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI;
using SPR.UI.Field;
using UnityEngine;

namespace SPR.Field;

public class HeatWallTrigger : MonoBehaviour, IInteractable, IStateSavable
{
	[SerializeField]
	private Vector2Int interactCanvasPosition = new Vector2Int(0, -11);

	private const int AcquiredState = 1;

	private BoxCollider2D boxCollider;

	private FieldInteractCanvas interactCanvas;

	private BoxCollider2D BoxCollider => boxCollider ?? (boxCollider = GetComponent<BoxCollider2D>());

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	private static InputManager InputManager => Game.Instance.InputManager;

	public string ID => "Heat_Wall_Trigger";

	public string Name => ID;

	public bool IsNull => this.IsNull();

	public int InteractableOrder => 1;

	public virtual void AwakeObject(SaveData saveData)
	{
		DebugLog.GeneralLog("Item name : " + base.name);
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas result)
		{
			interactCanvas = result;
		}).Forget();
	}

	public void Interact()
	{
		BoxCollider.enabled = false;
		interactCanvas.SetActive(value: false);
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem");
		ChangeState(1);
		InteractAsync().Forget();
	}

	private UniTask InteractAsync()
	{
		Game.Instance.SaveData.WorldData.AddInteractedFieldObject("HeatCoreLV0");
		Game.Instance.UIManager.MenuCanvas.MemoryPanel.UpdateMemoryRecord("HeatCoreLV0");
		FieldCanvas.InteractionPanel.ShowDataImmediately(ItemType.Heat, string.Empty);
		base.gameObject.SetActive(value: false);
		return default(UniTask);
	}

	public void InteractEnter()
	{
		interactCanvas.SetActive(value: true);
	}

	public void InteractExit()
	{
		interactCanvas.SetActive(value: false);
	}

	public void LoadState(int state)
	{
		base.gameObject.SetActive(state == 0);
	}

	public void ChangeState(int state)
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}
}
