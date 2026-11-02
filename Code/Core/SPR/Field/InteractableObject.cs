using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using SPR.Save;
using SPR.UI;
using SPR.UI.Field;
using UnityEngine;

namespace SPR.Field;

[RequireComponent(typeof(BoxCollider2D))]
public class InteractableObject : MonoBehaviour, IInteractable
{
	[SerializeField]
	private string objectName;

	[SerializeField]
	private int interactableOrder;

	[SerializeField]
	private Vector2Int interactCanvasPosition;

	protected FieldInteractCanvas InteractCanvas;

	private BoxCollider2D coll;

	protected static InputManager InputManager => Game.Instance.InputManager;

	protected static WorldData WorldData => Game.Instance.SaveData.WorldData;

	protected static UIManager UIManager => Game.Instance.UIManager;

	protected static InteractionPanel InteractionPanel => UIManager.FieldCanvas.InteractionPanel;

	protected static PlayerMover Player => Game.Instance.Player.mover;

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	protected BoxCollider2D BoxCollider => coll ?? (coll = GetComponent<BoxCollider2D>());

	protected string ObjectID => Util.GetID(Game.FieldID, objectName);

	public string Name => objectName;

	public bool IsNull => this.IsNull();

	public virtual int InteractableOrder => interactableOrder;

	public virtual void AwakeObject(SaveData saveData)
	{
		base.gameObject.layer = LayerUtility.ObjectLayerIndex;
		BoxCollider.isTrigger = true;
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas result)
		{
			InteractCanvas = result;
		}).Forget();
	}

	public virtual void Interact()
	{
		InteractCanvas.SetActive(value: false);
		OnInteract();
	}

	protected virtual void OnInteract()
	{
		WorldData.AddInteractedFieldObject(ObjectID);
		UIManager.MenuCanvas.MemoryPanel.UpdateMemoryRecord(ObjectID);
		InteractionPanel.ShowDataImmediately(ItemType.None, ObjectID);
	}

	public virtual void InteractEnter()
	{
		InteractCanvas.SetActive(value: true);
	}

	public virtual void InteractExit()
	{
		InteractCanvas.SetActive(value: false);
	}

	protected virtual void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.gray;
		Gizmos.DrawWireCube(base.transform.position + interactCanvasPosition.ToVector3(), Vector2.one);
		Vector3 position = base.transform.position;
		base.transform.position = position.SnapToHalf();
		base.name = GetID(objectName);
	}

	public static string GetID(string objName)
	{
		return "Interactable Object - " + objName;
	}
}
