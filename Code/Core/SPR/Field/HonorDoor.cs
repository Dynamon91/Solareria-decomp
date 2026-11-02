using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.Save;
using SPR.UI;
using UnityEngine;

namespace SPR.Field;

public class HonorDoor : OtherEntrance, IService
{
	[Header("Service Object")]
	[SerializeField]
	private Vector2 visualPivot;

	[SerializeField]
	private ServiceName serviceName;

	[SerializeField]
	private float interactLocalPosX;

	[SerializeField]
	private Vector2Int servicePivot;

	[SerializeField]
	private GameObject serviceVcam;

	[Header("References")]
	[SerializeField]
	private BoxCollider2D highlightBox;

	[SerializeField]
	private SpriteRenderer[] spriteRenderers;

	[SerializeField]
	private ParticleSystem[] particles;

	[SerializeField]
	private EventReference openEvent;

	private bool isTurnOn;

	private FieldServiceCanvas serviceCanvas;

	private Animator animator;

	public override string Name => "HonorDoor";

	public GameObject ServiceGameObject => base.gameObject;

	private Vector2 VisualBottom => base.transform.position + (Vector3)visualPivot;

	private static PlayerMover PlayerMover => Game.Instance.Player.mover;

	private static InputManager InputManager => Game.Instance.InputManager;

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		InstantiateServiceCanvasAsync().Forget();
	}

	public override void Interact()
	{
		InteractAsync().Forget();
	}

	public void OnServiceOpening()
	{
	}

	public void OnServiceClosing()
	{
	}

	public void OnServiceSuccess()
	{
	}

	public void TryOpenDoor()
	{
		if (spriteRenderers.Any((SpriteRenderer r) => r.color.a <= 0f))
		{
			Game.Instance.UIManager.FadeCanvas.WarningBoard.ShowWarning("UI_HonorDoor_Warning");
			PlayerMover.SetAnimation("idle");
		}
		else
		{
			CanInteract = false;
			InteractCanvas.SetActive(value: false);
			EnterRoom().Forget();
		}
	}

	protected override void FixedUpdate()
	{
		Highlight();
	}

	private void Highlight()
	{
		isTurnOn = Game.Instance.Player.mover.ColliderBounds.Intersects(highlightBox.bounds);
		SpriteRenderer[] array;
		if (isTurnOn)
		{
			array = spriteRenderers;
			foreach (SpriteRenderer spriteRenderer in array)
			{
				if (!(spriteRenderer.color.a >= 1f) && !DOTween.IsTweening(spriteRenderer) && Game.Instance.SaveData.PlayerData.HasEverObtainedItem(ItemDataKey.SealItemKey(spriteRenderer.name), out var _))
				{
					spriteRenderer.DOAlpha(1f, 1f).SetEase(Ease.Linear);
				}
			}
			return;
		}
		array = spriteRenderers;
		foreach (SpriteRenderer spriteRenderer2 in array)
		{
			if (!(spriteRenderer2.color.a <= 0f) && !DOTween.IsTweening(spriteRenderer2))
			{
				spriteRenderer2.DOAlpha(0f, 1f).SetEase(Ease.Linear);
			}
		}
	}

	private async UniTaskVoid EnterRoom()
	{
		SpriteRenderer[] array = spriteRenderers;
		foreach (SpriteRenderer spriteRenderer in array)
		{
			Game.Instance.SaveData.PlayerData.TryRemoveAcquiredItem(ItemDataKey.SealItemKey(spriteRenderer.name));
		}
		PlayerMover player = Game.Instance.Player.mover;
		NullControllable nullControllable = new NullControllable("HonorHall");
		await Game.Instance.InputManager.StackControlTarget(nullControllable);
		Animator.Play("AshHill Library Honor Door_open");
		player.SetAnimation("turn_back", loop: false);
		await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f);
		player.SetAnimation("walk_turn_back");
		await Game.Instance.UIManager.FadeCanvas.FadeOut(2f);
		LoadField();
		await Game.Instance.InputManager.RemoveControlTarget(nullControllable);
	}

	private async UniTask InstantiateServiceCanvasAsync()
	{
		serviceCanvas = await AddressableHelper.InstantiateAsync<FieldServiceCanvas>("Field Service Canvas.prefab", base.transform);
		ServiceInfo info = new ServiceInfo
		{
			serviceName = ServiceName.HonorDoor,
			canvasPivot = servicePivot,
			vcam = serviceVcam
		};
		serviceCanvas.Initialize(this, info);
	}

	private async UniTask InteractAsync()
	{
		await InputManager.BlockControlForTask(MovePlayerOnInteractAsync(), "Move Player For Interaction");
		PlayerMover.SetAnimation("cam_up");
		serviceCanvas.PlaceOnRight(playerIsOnLeft: true);
		InputManager.StackControlTarget(serviceCanvas).Forget();
	}

	private async UniTask MovePlayerOnInteractAsync()
	{
		await Game.Instance.Player.MoveXPositionAsync(VisualBottom.x, isRunning: true);
		PlayerMover.Flip(Direction.Right);
		await UniTask.WaitUntil(() => PlayerMover.GetCurrentAnim(0).Name == "idle");
	}

	public void PlayParticle()
	{
		particles.ForEach(delegate(ParticleSystem x)
		{
			x.Play();
		});
		Game.Instance.SoundManager.PlayOneShotAttached(openEvent, base.transform);
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Vector2 vector = new Vector2(6f, 6.2f);
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(VisualBottom + servicePivot - new Vector2(0f, vector.y / 2f), vector);
		Vector3 position = base.transform.position;
		base.transform.position = position.RoundToInt();
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(VisualBottom + new Vector2(interactLocalPosX, 2f), new Vector2(2f, 4f));
	}
}
