using System;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI;
using UnityEngine;

namespace SPR.Cutscene;

public class CharonInteractableHand : MonoBehaviour, IInteractable
{
	[SerializeField]
	private Transform interactCanvasTransform;

	private FieldInteractCanvas interactCanvas;

	private CharonCutsceneProducer charonCutsceneProducer;

	private BoxCollider2D box2D;

	public string Name => base.name;

	public bool IsNull => this.IsNull();

	public int InteractableOrder => 0;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public event Action OnInteract;

	public void AwakeObject(SaveData saveData)
	{
	}

	public void Enable()
	{
		Box2D.enabled = true;
		InstantiateInteractCanvasAsync().Forget();
		this.OnInteract = null;
	}

	public void Interact()
	{
		this.OnInteract?.Invoke();
		DisableAll();
	}

	public void InteractEnter()
	{
		interactCanvas?.SetActive(value: true);
	}

	public void InteractExit()
	{
		interactCanvas?.SetActive(value: false);
	}

	public void DisableAll()
	{
		interactCanvas?.SetActive(value: false);
		Box2D.enabled = false;
	}

	private async UniTaskVoid InstantiateInteractCanvasAsync()
	{
		interactCanvas = await AddressableHelper.InstantiateAsync<FieldInteractCanvas>("FieldInteractCanvas.prefab", base.transform);
		interactCanvas.transform.position = interactCanvasTransform.position;
		interactCanvas.SetActive(value: false);
	}
}
