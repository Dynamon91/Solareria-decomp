using System.Threading;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI;
using SPR.UI.Map;
using UnityEngine;

namespace SPR.Field;

[RequireComponent(typeof(BoxCollider2D))]
public class ItemObject : MonoBehaviour, IInteractable, IStateSavable, IMappable
{
	[SerializeField]
	private string itemName;

	[SerializeField]
	private bool useFixedPosition;

	[SerializeField]
	private Vector2Int fixedPosition;

	protected const int AcquiredState = 1;

	private readonly Vector2Int interactCanvasPosition = new Vector2Int(0, 4);

	private BoxCollider2D boxCollider;

	private Animator animator;

	private FieldInteractCanvas interactCanvas;

	private PureItem pureItem;

	private static readonly int Acquire = Animator.StringToHash("Acquire");

	private BoxCollider2D BoxCollider => boxCollider ?? (boxCollider = GetComponent<BoxCollider2D>());

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	private string ItemName
	{
		get
		{
			if (pureItem == null)
			{
				if (!itemName.IsNullOrEmpty())
				{
					return itemName;
				}
				return "UnknownIngredient";
			}
			return pureItem.ItemName;
		}
	}

	protected Vector2Int Position
	{
		get
		{
			if (!useFixedPosition)
			{
				return base.transform.position.ToVector2Int();
			}
			return fixedPosition;
		}
	}

	private static FieldController FieldController => Game.Instance.FieldController;

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static WorldData WorldData => Game.Instance.SaveData.WorldData;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public string ID => Util.GetID(Game.FieldID, ItemName, Position);

	public string Name => ItemName;

	public bool IsNull => this.IsNull();

	public int InteractableOrder => 10;

	public bool IsDropItem { get; private set; }

	public void AwakeObject(SaveData saveData)
	{
		pureItem = new PureItem(ItemName);
		IsDropItem = false;
		InstantiateInteractCanvasAsync().Forget();
	}

	private void Update()
	{
		Util.CheckShownInCamera(ID, base.transform.position);
	}

	public virtual void Interact()
	{
		AcquireItemAsync().Forget();
		pureItem.AddToPlayerData();
	}

	public void InteractEnter()
	{
		interactCanvas.SetActive(value: true);
	}

	public void InteractExit()
	{
		interactCanvas.SetActive(value: false);
	}

	public virtual void LoadState(int state)
	{
		base.gameObject.SetActive(state == 0);
	}

	public void ChangeState(int state)
	{
		if (WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	private void InitializeAndDrop(string itemName, Vector2Int dropPos)
	{
		InitializeForDropItem(itemName, dropPos);
		base.transform.position = dropPos.ToVector3();
		BoxCollider.enabled = true;
	}

	private async UniTask InitializeAndDropAsync(string itemName, Vector2 startPos, Vector2Int endPos, CancellationToken token = default(CancellationToken))
	{
		InitializeForDropItem(itemName, endPos);
		base.transform.position = startPos;
		BoxCollider.enabled = false;
		SoundManager.PlayOneShotAttached("event:/SFX/Object/ItemBox/SFX_itemBox_Item_Appear", base.transform);
		await base.transform.DODrop(startPos, endPos).WaitAsync(token);
		BoxCollider.enabled = true;
	}

	private void InitializeForDropItem(string itemName, Vector2Int finalDropPos)
	{
		this.itemName = itemName;
		pureItem = new PureItem(itemName);
		IsDropItem = true;
		InstantiateInteractCanvasAsync().Forget();
		string iD = Util.GetID(Game.FieldID, ItemName, finalDropPos);
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.AddDropItemID(iD);
		}
		Area area;
		if (FieldController.CurrentArea.IsNull())
		{
			if (!FieldController.TryFindAreaByPosition(finalDropPos, out area))
			{
				area = FieldController.MainArea;
			}
		}
		else
		{
			area = FieldController.CurrentArea;
		}
		base.transform.SetParent(area.transform);
		base.name = GetType().Name + "_" + ItemName;
	}

	private UniTask InstantiateInteractCanvasAsync()
	{
		return FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas result)
		{
			interactCanvas = result;
		});
	}

	private async UniTask AcquireItemAsync()
	{
		BoxCollider.enabled = false;
		interactCanvas.SetActive(value: false);
		SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem");
		if (IsDropItem)
		{
			if (WorldData.TryGetSectorData(out var data))
			{
				data.RemoveDropItemID(ID);
			}
		}
		else
		{
			ChangeState(1);
		}
		Animator.SetTrigger(Acquire);
		await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f);
		base.gameObject.SetActive(value: false);
	}

	public MapObjectInfo GetMapObjectInfo(Vector2 center)
	{
		Vector2 position = Position + Vector2.down - center;
		return new MapObjectInfo(ItemName.Contains("Relic") ? MapObjectType.Relics : MapObjectType.FieldItem, position, ID);
	}

	public static async UniTask SpawnItemAsync(string itemName, Vector2Int spawnPos)
	{
		(await AddressableHelper.InstantiateAsync<ItemObject>("ItemObject.prefab")).InitializeAndDrop(itemName, spawnPos);
	}

	public static async UniTask DropItemAsync(string itemName, Vector2 startPos, Vector2Int endPos, CancellationToken token = default(CancellationToken))
	{
		await (await AddressableHelper.InstantiateAsync<ItemObject>("ItemObject.prefab")).InitializeAndDropAsync(itemName, startPos, endPos, token);
	}

	private void OnDrawGizmosSelected()
	{
		base.transform.position = base.transform.position.RoundToInt();
	}

	private void OnValidate()
	{
		string text = itemName;
		if (!string.IsNullOrEmpty(text))
		{
			base.name = GetType().Name + "_" + text;
		}
	}
}
