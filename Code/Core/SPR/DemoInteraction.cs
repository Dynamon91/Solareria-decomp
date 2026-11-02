using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI;
using UnityEngine;

namespace SPR;

public class DemoInteraction : MonoBehaviour, IInteractable
{
	[SerializeField]
	private float interactCanvasHeight;

	private FieldInteractCanvas interactCanvas;

	public string Name => "Demo Interaction";

	public bool IsNull => base.gameObject.IsNull();

	public int InteractableOrder => 0;

	public void AwakeObject(SaveData saveData)
	{
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, new Vector2(0f, interactCanvasHeight), delegate(FieldInteractCanvas result)
		{
			interactCanvas = result;
		}).Forget();
	}

	public void Interact()
	{
		interactCanvas.SetActive(value: false);
		Game.Instance.UIManager.FieldCanvas.InteractionPanel.ShowDataImmediately(ItemType.None, "Demo_Lever");
	}

	public void InteractEnter()
	{
		interactCanvas.SetActive(value: true);
	}

	public void InteractExit()
	{
		interactCanvas.SetActive(value: false);
	}
}
