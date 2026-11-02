using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using Library;
using SPR.Save;
using SPR.UI;
using SPR.UI.Field;
using SPR.UI.Map;
using UnityEngine;

namespace SPR.Field;

public class RelicWall : MonoBehaviour, IStateSavable, IInteractable, IMappable
{
	private enum State
	{
		None,
		Interacted
	}

	[SerializeField]
	private RegionName regionName;

	[Header("Reference")]
	[SerializeField]
	private Vector2Int interactCanvasPosition;

	[SerializeField]
	private EventReference activateEvent;

	private State currentState;

	private FieldInteractCanvas interactCanvas;

	private Animator animator;

	private static readonly int LightOn = Animator.StringToHash("LightOn");

	private EventInstance activateInstance;

	public string ID => Util.GetID(Game.FieldID, Name);

	public string Name => RelicItemName + "Wall";

	public bool IsNull => base.gameObject.IsNull();

	public int InteractableOrder => 0;

	private string RelicItemName => $"{regionName}Relic";

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	private static PlayerMover Player => Game.Instance.Player.mover;

	private static UIManager UIManager => Game.Instance.UIManager;

	private static InteractionPanel InteractionPanel => UIManager.FieldCanvas.InteractionPanel;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public void AwakeObject(SaveData saveData)
	{
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas result)
		{
			interactCanvas = result;
		}).Forget();
	}

	public void LoadState(int state)
	{
		currentState = (State)state;
		base.enabled = currentState == State.None;
	}

	public void ChangeState(int state)
	{
		currentState = (State)state;
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	private void Update()
	{
		Util.CheckShownInCamera(ID, base.transform.position);
	}

	public void Interact()
	{
		if (currentState != State.Interacted)
		{
			interactCanvas.SetActive(value: false);
			ChangeState(1);
			InputManager.BlockControlForTask(InteractAsync(), "Interact Relic Wall").Forget();
		}
	}

	public void InteractEnter()
	{
		if (currentState != State.Interacted)
		{
			interactCanvas.SetActive(value: true);
			Animator.SetBool(LightOn, value: true);
			activateInstance = SoundManager.PlayOneShotAttached(activateEvent, base.transform);
		}
	}

	public void InteractExit()
	{
		if (currentState != State.Interacted)
		{
			interactCanvas.SetActive(value: false);
			Animator.SetBool(LightOn, value: false);
			if (activateInstance.isValid())
			{
				activateInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
				activateInstance.release();
			}
		}
	}

	private async UniTask InteractAsync()
	{
		await PlayerTurningAnimationAsync(turnBack: true);
		new PureItem(RelicItemName).AddToPlayerData(ItemAcquireDisplayOrder.ShowFullPanelImmediately);
		await UniTask.WaitUntil(() => InputManager.IsCurrentControlTarget(InteractionPanel));
		await UniTask.WaitUntil(() => !InputManager.IsCurrentControlTarget(InteractionPanel));
		Animator.SetBool(LightOn, value: false);
		await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
		await PlayerTurningAnimationAsync(turnBack: false);
	}

	private async UniTask PlayerTurningAnimationAsync(bool turnBack)
	{
		if (turnBack)
		{
			PlayerStateMachine psm = Player.PSM;
			await UniTask.WaitUntil(() => psm.CurrentState == psm.IdleState);
			await Player.SetAnimationAsync("turn_back");
			Player.SetAnimation("idle_back");
		}
		else
		{
			await Player.SetAnimationAsync("turn_front");
			Player.SetAnimation("idle");
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.gray;
		Gizmos.DrawWireCube(base.transform.position + interactCanvasPosition.ToVector3(), Vector2.one);
		Vector3 position = base.transform.position;
		base.transform.position = position.SnapToHalf();
		base.name = Name;
	}

	public MapObjectInfo GetMapObjectInfo(Vector2 center)
	{
		float y = 9f;
		Vector2 position = base.transform.position.ToVector2() - new Vector2(0f, y) - center;
		return new MapObjectInfo(MapObjectType.Relics, position, ID);
	}
}
