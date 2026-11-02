using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI;
using SPR.UI.Field;
using UnityEngine;

namespace SPR.Field;

public class ServiceObject : SpineObject, IInteractable, IService
{
	[Header("Service Object")]
	[SerializeField]
	private ServiceName serviceName;

	[SerializeField]
	private Vector2Int interactCanvasPosition;

	[SerializeField]
	private float interactLocalPosX;

	[SerializeField]
	private bool shouldFaceRightOnInteract = true;

	[SerializeField]
	private Vector2Int servicePivot;

	[SerializeField]
	private GameObject serviceVcam;

	private FieldInteractCanvas interactCanvas;

	private FieldServiceCanvas serviceCanvas;

	protected static PlayerMover PlayerMover => Game.Instance.Player.mover;

	protected static InputManager InputManager => Game.Instance.InputManager;

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	protected static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	public override string Name => serviceName.ToString();

	public GameObject ServiceGameObject => base.gameObject;

	public bool IsNull => this.IsNull();

	public int InteractableOrder => 1;

	public ServiceName ServiceName => serviceName;

	public Vector2Int CanvasPivotPos => servicePivot;

	public GameObject ServiceVcam => serviceVcam;

	public virtual void AwakeObject(SaveData saveData)
	{
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas result)
		{
			interactCanvas = result;
		}).Forget();
		InstantiateServiceCanvasAsync().Forget();
	}

	public virtual void Interact()
	{
		interactCanvas.SetActive(value: false);
		InteractAsync().Forget();
	}

	public virtual void InteractEnter()
	{
		interactCanvas.SetActive(value: true);
	}

	public virtual void InteractExit()
	{
		interactCanvas.SetActive(value: false);
	}

	private async UniTask InstantiateServiceCanvasAsync()
	{
		serviceCanvas = await AddressableHelper.InstantiateAsync<FieldServiceCanvas>("Field Service Canvas.prefab", base.transform);
		ServiceInfo info = new ServiceInfo
		{
			serviceName = serviceName,
			canvasPivot = CanvasPivotPos,
			vcam = serviceVcam
		};
		serviceCanvas.Initialize(this, info);
	}

	private async UniTask InteractAsync()
	{
		if (Game.Instance.Player.StatusEffectController.Contain<WetStatusEffect>())
		{
			Game.Instance.UIManager.FadeCanvas.WarningBoard.ShowWarning("UI_Wet_Warning");
			return;
		}
		await InputManager.BlockControlForTask(MovePlayerOnInteractAsync(), "Move Player For Interaction");
		PlayerMover.SetAnimation("cam_up");
		serviceCanvas.PlaceOnRight(playerIsOnLeft: true);
		InputManager.StackControlTarget(serviceCanvas).Forget();
	}

	private async UniTask MovePlayerOnInteractAsync()
	{
		await Game.Instance.Player.MoveXPositionAsync(VisualBottom.x + interactLocalPosX, isRunning: true);
		PlayerMover.Flip(shouldFaceRightOnInteract ? Direction.Right : Direction.Left);
		await UniTask.WaitUntil(() => PlayerMover.GetCurrentAnim(0).Name == "idle");
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Gizmos.color = Color.gray;
		Gizmos.DrawWireCube(base.transform.position + interactCanvasPosition.ToVector3(), Vector2.one);
		Vector2 vector = new Vector2(6f, 6.2f);
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(VisualBottom + servicePivot - new Vector2(0f, vector.y / 2f), vector);
		Vector3 position = base.transform.position;
		base.transform.position = position.SnapToHalf();
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(VisualBottom + new Vector2(interactLocalPosX, 2f), new Vector2(2f, 4f));
	}

	public virtual void OnServiceOpening()
	{
	}

	public virtual void OnServiceClosing()
	{
	}

	public void OnServiceSuccess()
	{
	}
}
