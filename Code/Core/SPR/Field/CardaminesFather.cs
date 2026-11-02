using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI;
using UnityEngine;

namespace SPR.Field;

public class CardaminesFather : MonoBehaviour, IInteractable, IStateSavable
{
	private enum CorpseState
	{
		Lootable,
		Looted,
		Buried
	}

	[SerializeField]
	private Vector2Int interactCanvasPosition;

	[SerializeField]
	private GameObject fatherObject;

	[SerializeField]
	private ParticleSystem effectObject;

	[SerializeField]
	private GameObject cardamineObject;

	private FieldInteractCanvas interactCanvas;

	private BoxCollider2D col;

	private CorpseState currentState;

	private const string CardamineDisappearedTriggerID = "Trigger_NPC_Earthford_13_Cardamine";

	public string Name => GetType().Name;

	public bool IsNull => this.IsNull();

	public int InteractableOrder => 0;

	public string ID => $"{Game.Instance.CurrentFieldID}_{GetType().Name}";

	private BoxCollider2D Col => col ?? (col = GetComponent<BoxCollider2D>());

	private static WorldData WorldData => Game.Instance.SaveData.WorldData;

	public void AwakeObject(SaveData saveData)
	{
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas canvas)
		{
			interactCanvas = canvas;
		}).Forget();
		if (!Trigger.CheckTrigger("Trigger_NPC_Earthford_13_Cardamine"))
		{
			ChangeState(2);
		}
	}

	public void Interact()
	{
		interactCanvas.SetActive(value: false);
		new PureItem("RoughFlowerPendant").AddToPlayerData(ItemAcquireDisplayOrder.ShowFullPanel);
		ChangeState(1);
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
		currentState = (CorpseState)state;
		SetVisualState(currentState);
	}

	public void ChangeState(int state)
	{
		currentState = (CorpseState)state;
		SetVisualState(currentState, instant: false);
		if (WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	private void SetVisualState(CorpseState state, bool instant = true)
	{
		switch (state)
		{
		case CorpseState.Lootable:
			fatherObject.SetActive(value: true);
			effectObject.gameObject.SetActive(value: true);
			Col.enabled = true;
			cardamineObject.SetActive(value: false);
			break;
		case CorpseState.Looted:
			fatherObject.SetActive(value: true);
			if (instant)
			{
				effectObject.gameObject.SetActive(value: false);
			}
			else
			{
				effectObject.Stop();
				UniTask.WaitUntil(() => effectObject.particleCount == 0).GetAwaiter().OnCompleted(delegate
				{
					effectObject.gameObject.SetActive(value: false);
				});
			}
			Col.enabled = false;
			cardamineObject.SetActive(value: false);
			break;
		case CorpseState.Buried:
			fatherObject.SetActive(value: false);
			Col.enabled = false;
			cardamineObject.SetActive(value: true);
			break;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.gray;
		Gizmos.DrawWireCube(base.transform.position + interactCanvasPosition.ToVector3(), Vector2.one);
		Vector3 position = base.transform.position;
		base.transform.position = position.SnapToHalf();
	}
}
