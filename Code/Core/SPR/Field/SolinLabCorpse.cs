using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI;
using SPR.UI.Field;
using UnityEngine;

namespace SPR.Field;

public class SolinLabCorpse : MonoBehaviour, IInteractable, IStateSavable
{
	private const int InitialState = 0;

	private const int DroppedState = 1;

	[SerializeField]
	private ParticleSystem effectObject;

	[SerializeField]
	private Vector2Int interactCanvasPosition;

	private FieldInteractCanvas interactCanvas;

	private BoxCollider2D col;

	private int currentState;

	public string Name => GetType().Name;

	public bool IsNull => this.IsNull();

	public int InteractableOrder => 0;

	public string ID => $"{Game.Instance.CurrentFieldID}_{GetType().Name}";

	private BoxCollider2D Col => col ?? (col = GetComponent<BoxCollider2D>());

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static InteractionPanel InteractionPanel => Game.Instance.UIManager.FieldCanvas.InteractionPanel;

	public void AwakeObject(SaveData saveData)
	{
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas canvas)
		{
			interactCanvas = canvas;
		}).Forget();
	}

	public void Interact()
	{
		interactCanvas.SetActive(value: false);
		InteractionPanel.EnqueueItemData("SolinLabEntranceKey");
		PlayerData.AddAcquiredItem("SolinLabEntranceKey");
		ChangeState(1);
		SetActive(value: false);
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
		currentState = state;
		SetActive(currentState == 0, instant: true);
	}

	public void ChangeState(int state)
	{
		currentState = state;
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	private void SetActive(bool value, bool instant = false)
	{
		Col.enabled = value;
		if (value)
		{
			return;
		}
		if (instant)
		{
			effectObject.gameObject.SetActive(value: false);
			return;
		}
		effectObject.Stop();
		UniTask.WaitUntil(() => effectObject.particleCount == 0).GetAwaiter().OnCompleted(delegate
		{
			effectObject.gameObject.SetActive(value: false);
		});
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.gray;
		Gizmos.DrawWireCube(base.transform.position + interactCanvasPosition.ToVector3(), Vector2.one);
		Vector3 position = base.transform.position;
		base.transform.position = position.SnapToHalf();
	}
}
