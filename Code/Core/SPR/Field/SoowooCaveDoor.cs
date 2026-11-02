using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Field;

public class SoowooCaveDoor : OtherEntrance, IService, IStateSavable
{
	[Header("Service Object")]
	[SerializeField]
	private ServiceInfo serviceInfo;

	[SerializeField]
	private float interactLocalPosX;

	private PlayableDirector playableDirector;

	private Animator animator;

	private FieldServiceCanvas serviceCanvas;

	private int currentState;

	private const int DefaultState = 0;

	private const int OpenState = 1;

	public string ID => Name;

	public override string Name => Util.GetID(Game.FieldID, "SoowooCaveDoor");

	public GameObject ServiceGameObject => base.gameObject;

	private PlayableDirector PlayableDirector => playableDirector ?? (playableDirector = GetComponent<PlayableDirector>());

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	private static InputManager InputManager => Game.Instance.InputManager;

	private static PlayerMover PlayerMover => Game.Instance.Player.mover;

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		InstantiateServiceCanvasAsync().Forget();
		this.CastObject<IStateSavable>().InitializeState();
	}

	public void OnServiceOpening()
	{
	}

	public void OnServiceClosing()
	{
	}

	public override void Interact()
	{
		if (currentState == 1)
		{
			CanInteract = false;
			InteractCanvas.SetActive(value: false);
			InputManager.BlockControlForTask(MoveSceneAsync(), "Player Enter Soowoo Cave").Forget();
		}
		else
		{
			OpenServiceCanvasAsync().Forget();
		}
	}

	private async UniTask MoveSceneAsync()
	{
		PlayerMover.SetAnimation("turn_back_2", loop: false);
		PlayerMover.AddAnimation("walk_turn_back");
		await UniTask.WaitForSeconds(1.5f);
		LoadField();
	}

	private async UniTaskVoid OpenServiceCanvasAsync()
	{
		serviceInfo.vcam.SetActive(value: true);
		await InputManager.BlockControlForTask(MovePlayerOnInteractAsync(), "Move Player For Interaction");
		PlayerMover.SetAnimation("cam_up");
		serviceCanvas.PlaceOnRight(playerIsOnLeft: true);
		InputManager.StackControlTarget(serviceCanvas).Forget();
	}

	public void OpenDoor()
	{
		PlayableDirector.Play();
		InputManager.BlockControlForTask(TurnBackAsync(), "Opening Cave Door").Forget();
	}

	private async UniTask TurnBackAsync()
	{
		CanInteract = false;
		InteractCanvas.SetActive(value: false);
		await UniTask.WaitForSeconds(0.5f);
		PlayerMover.SetAnimation("turn_back_2", loop: false);
		await UniTask.WaitForSeconds(2.5f);
		PlayerMover.SetAnimation("idle");
		ChangeState(1);
		CanInteract = true;
		InteractCanvas.SetActive(value: true);
		serviceInfo.vcam.SetActive(value: false);
	}

	private async UniTask MovePlayerOnInteractAsync()
	{
		await Game.Instance.Player.MoveXPositionAsync(base.transform.position.x + interactLocalPosX, isRunning: true);
		PlayerMover.Flip(Direction.Right);
		await UniTask.WaitUntil(() => PlayerMover.GetCurrentAnim(0).Name == "idle");
	}

	private async UniTask InstantiateServiceCanvasAsync()
	{
		serviceCanvas = await AddressableHelper.InstantiateAsync<FieldServiceCanvas>("Field Service Canvas.prefab", base.transform);
		serviceCanvas.Initialize(this, serviceInfo);
	}

	public void LoadState(int state)
	{
		currentState = state;
		Animator.Play((currentState == 0) ? "Soowoo_RockDoor_closed" : "Soowoo_RockDoor_open");
	}

	public void ChangeState(int state)
	{
		currentState = state;
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(SectorName.WatermirrorBasin, out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	public override async UniTask MovePlayerOnEnterAsync()
	{
		PlayerMover.SetAnimationInstantly("walk_front");
		await UniTask.WaitForSeconds(1f);
		PlayerMover.SetAnimation("turn_idle", loop: false);
		await UniTask.WaitUntil(() => PlayerMover.IsAnimComplete(0, "turn_idle"));
		PlayerMover.SetAnimation("idle");
	}
}
