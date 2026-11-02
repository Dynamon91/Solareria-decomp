using System;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI;
using UnityEngine;

namespace SPR.Cutscene;

public class InteractTrigger : MonoBehaviour, IInteractable
{
	[SerializeField]
	private int interactCanvasHeight;

	private FieldInteractCanvas interactCanvas;

	public string Name => base.name;

	public bool IsNull => this.IsNull();

	public int InteractableOrder => 0;

	public event Action OnInteract;

	public void AwakeObject(SaveData saveData)
	{
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, new Vector2(0f, interactCanvasHeight), delegate(FieldInteractCanvas canvas)
		{
			interactCanvas = canvas;
		}).Forget();
	}

	public void Interact()
	{
		interactCanvas.SetActive(value: false);
		this.OnInteract?.Invoke();
	}

	public void InteractEnter()
	{
		interactCanvas.SetActive(value: true);
	}

	public void InteractExit()
	{
		interactCanvas.SetActive(value: false);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.gray;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * interactCanvasHeight, Vector2.one);
		Vector3 position = base.transform.position;
		base.transform.position = position.SnapToHalf();
	}
}
