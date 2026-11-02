using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using SPR.Save;
using SPR.UI;
using UnityEngine;

namespace SPR.Field;

[RequireComponent(typeof(BoxCollider2D))]
public class TalkableObject : MonoBehaviour, IInteractable
{
	[SerializeField]
	private string objectName;

	[SerializeField]
	private int interactableOrder;

	[SerializeField]
	private Vector2Int interactCanvasPosition;

	private FieldInteractCanvas interactCanvas;

	private BoxCollider2D coll;

	protected BoxCollider2D Coll => coll ?? (coll = GetComponent<BoxCollider2D>());

	private static UIManager UIManager => Game.Instance.UIManager;

	private string ScenarioID => $"{Game.Instance.CurrentFieldID}_{objectName}";

	public string Name => objectName;

	public bool IsNull => this.IsNull();

	public virtual int InteractableOrder => interactableOrder;

	public virtual void AwakeObject(SaveData saveData)
	{
		base.gameObject.layer = LayerUtility.ObjectLayerIndex;
		Coll.isTrigger = true;
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas result)
		{
			interactCanvas = result;
		}).Forget();
	}

	public void Interact()
	{
		interactCanvas.SetActive(value: false);
		OnInteract();
	}

	protected virtual void OnInteract()
	{
		Scenario scenario = ScenarioHelper.GetScenario(ScenarioID);
		UIManager.TalkCanvas.StartTalk(scenario, OnInteractOver);
	}

	public virtual void InteractEnter()
	{
		interactCanvas.SetActive(value: true);
	}

	public virtual void InteractExit()
	{
		interactCanvas.SetActive(value: false);
	}

	protected virtual void OnInteractOver()
	{
	}
}
