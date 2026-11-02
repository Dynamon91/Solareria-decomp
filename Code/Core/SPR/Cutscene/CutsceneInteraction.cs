using System;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI;
using UnityEngine;

namespace SPR.Cutscene;

public class CutsceneInteraction : MonoBehaviour, IInteractable
{
	[SerializeField]
	private int interactCanvasHeight;

	private FieldInteractCanvas interactCanvas;

	private bool isInteracted;

	private bool isAwoken;

	private Action onInteract;

	public string Name => "Cutscene Interaction";

	public bool IsNull => this.IsNull();

	public int InteractableOrder => 0;

	public void SetInteractCallBack(Action onInteract)
	{
		this.onInteract = onInteract;
	}

	public bool IsInteractionCompleted()
	{
		if (!isInteracted)
		{
			return false;
		}
		isInteracted = false;
		return true;
	}

	public void AwakeObject(SaveData saveData)
	{
		if (!isAwoken)
		{
			FieldInteractCanvas.InstantiateCanvasAsync(base.transform, new Vector2(0f, interactCanvasHeight), delegate(FieldInteractCanvas canvas)
			{
				interactCanvas = canvas;
			}).Forget();
			isInteracted = false;
			base.gameObject.SetActive(value: false);
			isAwoken = true;
		}
	}

	public void Interact()
	{
		isInteracted = true;
		base.gameObject.SetActive(value: false);
		onInteract?.Invoke();
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
