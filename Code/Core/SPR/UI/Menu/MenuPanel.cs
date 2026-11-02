using System.Threading;
using Cysharp.Threading.Tasks;
using LapinLibrary;
using Library;
using SPR.UI.Rest;
using UnityEngine;

namespace SPR.UI.Menu;

public abstract class MenuPanel : MonoBehaviour, IControllable
{
	private MenuCanvas menuCanvas;

	private CancellationTokenSource pauseTokenSource;

	protected MenuCanvas MenuCanvas => menuCanvas ?? (menuCanvas = GetComponentInParent<MenuCanvas>(includeInactive: true));

	protected static InputManager InputManager => Game.Instance.InputManager;

	protected static SettingData SettingData => Game.Instance.SettingData;

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	protected static FadeCanvas FadeCanvas => Game.Instance.UIManager.FadeCanvas;

	protected static RestCanvas RestCanvas => Game.Instance.UIManager.RestCanvas;

	protected static bool IsRest => InputManager.ContainsInControlStack(RestCanvas);

	public abstract MenuCategory Category { get; }

	protected virtual bool DeactivateInput => false;

	protected virtual bool CanDeactivate => true;

	protected virtual bool CanChangeCategory
	{
		get
		{
			if (CanDeactivate)
			{
				return !IsRest;
			}
			return false;
		}
	}

	public virtual bool IsValid => !base.gameObject.IsNull();

	public string ControllerName()
	{
		return base.gameObject.name;
	}

	public async UniTask Activate()
	{
		base.gameObject.SetActive(value: true);
		MenuCanvas.GuideGroup.Change(Category);
		OnActivate();
		await UniTask.Yield();
	}

	protected abstract void OnActivate();

	public void Control()
	{
		bool flag = InputManager.menuDown || InputManager.cancelDown || DeactivateInput;
		int categoryUI = InputManager.categoryUI;
		if (categoryUI != 0 && CanChangeCategory)
		{
			MenuCategory category = Util.AddToEnumCircular(Category, categoryUI);
			MenuCanvas.FocusCategory(category);
		}
		else if (flag && CanDeactivate)
		{
			DeactivateMenuAsync().Forget();
		}
		else
		{
			OnControl();
		}
	}

	protected abstract void OnControl();

	public UniTask Pause()
	{
		CancellationToken cancellationToken = TaskUtil.RefreshToken(ref pauseTokenSource);
		UniTask task = UniTask.WaitUntil(() => Game.Instance.UIManager.FadeCanvas.IsBlack, PlayerLoopTiming.Update, cancellationToken);
		task.GetAwaiter().OnCompleted(delegate
		{
			if (task.Status == UniTaskStatus.Succeeded)
			{
				DeactivateMenuAsync().Forget();
			}
		});
		return default(UniTask);
	}

	public UniTask Restart()
	{
		DebugLog.GeneralLog($"cancel token : ({pauseTokenSource != null})");
		pauseTokenSource?.Cancel();
		OnRestart();
		return default(UniTask);
	}

	public async UniTask Deactivate()
	{
		pauseTokenSource?.Cancel();
		base.gameObject.SetActive(value: false);
		OnDeactivate();
		await UniTask.Yield();
	}

	protected abstract void OnDeactivate();

	protected virtual void OnRestart()
	{
	}

	public virtual void Initialize()
	{
	}

	protected async UniTask DeactivateMenuAsync()
	{
		await InputManager.RemoveControlTarget(this);
		await InputManager.RemoveControlTarget(MenuCanvas);
	}

	private void Update()
	{
	}
}
