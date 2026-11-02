using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI;
using UnityEngine;

namespace SPR;

public class OtherEntrance : Entrance, IInteractable
{
	[SerializeField]
	private SectorName sectorName;

	[SerializeField]
	private string address;

	[SerializeField]
	private Vector2 playerSpawnPosition;

	[Header("Interact Canvas")]
	[SerializeField]
	private Vector2Int interactCanvasPosition;

	protected FieldInteractCanvas InteractCanvas;

	protected bool CanInteract;

	private static readonly Vector2 SpawnMoveHalf = new Vector2(0.5f, 0f);

	public override bool IsVertical => true;

	public virtual string Name => base.gameObject.name;

	public bool IsNull => (object)this == null;

	public int InteractableOrder => 1;

	public string SpecialAddress => address;

	protected override Vector2 GetEntranceWorldPosition()
	{
		return playerSpawnPosition - Vector2.up;
	}

	protected override void Start()
	{
		Initialize();
	}

	protected override SceneDataModel GetSceneDataModel()
	{
		return SceneDataModel.MoveSpecialModel((sectorName == SectorName.None) ? Game.Instance.CurrentFieldID.SectorName : sectorName, address, base.name);
	}

	public virtual void AwakeObject(SaveData saveData)
	{
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas result)
		{
			InteractCanvas = result;
		}).Forget();
		CanInteract = true;
	}

	public virtual void Interact()
	{
		CanInteract = false;
		InteractCanvas.SetActive(value: false);
		LoadField();
	}

	public void InteractEnter()
	{
		if (CanInteract)
		{
			InteractCanvas.SetActive(value: true);
		}
	}

	public void InteractExit()
	{
		if (CanInteract)
		{
			InteractCanvas.SetActive(value: false);
		}
	}

	protected override void FixedUpdate()
	{
	}

	public override Vector2 GetPlayerSpawnPosition()
	{
		return playerSpawnPosition + SpawnMoveHalf;
	}

	public override Vector2 GetPlayerEnterEndPosition()
	{
		return playerSpawnPosition - SpawnMoveHalf;
	}

	protected override void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(GetPlayerSpawnPosition(), 0.5f);
		Gizmos.DrawWireCube(GetPlayerEnterEndPosition(), Vector3.one);
		Gizmos.color = Color.gray;
		Gizmos.DrawWireCube(base.transform.position + interactCanvasPosition.ToVector3(), Vector2.one);
	}
}
