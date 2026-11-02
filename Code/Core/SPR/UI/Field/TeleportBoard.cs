using Cysharp.Threading.Tasks;
using DG.Tweening;
using SPR.Save;
using SPR.UI.Map;
using UnityEngine;

namespace SPR.UI.Field;

public class TeleportBoard : MonoBehaviour, IControllable
{
	[SerializeField]
	private MapCursor libraryCursor;

	[SerializeField]
	private MapCursor altarCursor;

	[SerializeField]
	private LocaleText altarLocaleText;

	private Animator animator;

	private RectTransform cursorRect;

	private Sequence seq;

	private bool canControl;

	private static readonly int Open = Animator.StringToHash("Open");

	private static readonly int Close = Animator.StringToHash("Close");

	public bool IsValid => true;

	public static bool CanOpen => SaveData.WorldData.HasInteractedBonfire(LibraryID);

	public bool IsTeleporting { get; private set; }

	private static InputManager InputManager => Game.Instance.InputManager;

	private static PlayerMover Player => Game.Instance.Player.mover;

	private static SaveData SaveData => Game.Instance.SaveData;

	private static FieldID LibraryID => new FieldID(SectorName.Library, 3);

	private static FieldID LastFieldID => Game.Instance.SaveData.LastFieldID;

	public string ControllerName()
	{
		return "Teleport Board";
	}

	public void OnSceneLoaded()
	{
		if (InputManager.ContainsInControlStack(this))
		{
			InputManager.RemoveControlTarget(this).Forget();
		}
	}

	public async UniTask Activate()
	{
		if ((object)animator == null)
		{
			animator = GetComponent<Animator>();
		}
		altarLocaleText.SetData($"Subtitle_{LastFieldID.GetSubtitle()}");
		base.gameObject.SetActive(value: true);
		await PlayActiveAnimation(value: true);
		canControl = true;
		IsTeleporting = false;
	}

	public void Control()
	{
		if (!canControl)
		{
			return;
		}
		int verticalDownUI = InputManager.verticalDownUI;
		if (verticalDownUI > 0)
		{
			canControl = false;
			libraryCursor.SetFocus(value: true);
			IsTeleporting = true;
			Game.Instance.RuntimeData.ClearRuntimeDictionaries();
			Game.Instance.LoadFieldAsync(SceneDataModel.RespawnModel(LibraryID)).Forget();
			return;
		}
		if (verticalDownUI < 0)
		{
			canControl = false;
			altarCursor.SetFocus(value: true);
			IsTeleporting = true;
			Game.Instance.RuntimeData.ClearRuntimeDictionaries();
			Game.Instance.LoadFieldAsync(SceneDataModel.RespawnModel(Game.Instance.SaveData.LastFieldID)).Forget();
			return;
		}
		if (InputManager.dashDown)
		{
			Player.PSM.RollingInputItem.Execute();
		}
		if (InputManager.menuDown || InputManager.cancelDown)
		{
			InputManager.RemoveControlTarget(this).Forget();
		}
	}

	public UniTask Pause()
	{
		return default(UniTask);
	}

	public UniTask Restart()
	{
		return default(UniTask);
	}

	public UniTask Deactivate()
	{
		if (Game.IsInitialized)
		{
			PlayActiveAnimation(value: false).GetAwaiter().OnCompleted(delegate
			{
				base.gameObject.SetActive(value: false);
			});
		}
		else
		{
			base.gameObject.SetActive(value: false);
		}
		return default(UniTask);
	}

	private async UniTask PlayActiveAnimation(bool value)
	{
		int id = (value ? Close : Open);
		int trigger = (value ? Open : Close);
		animator.ResetTrigger(id);
		animator.SetTrigger(trigger);
		await UniTask.WaitForSeconds(1f / 3f);
	}

	public void TryRemoveControlOfThis()
	{
		if (InputManager.IsCurrentControlTarget(this))
		{
			InputManager.RemoveControlTarget(this).Forget();
		}
	}
}
