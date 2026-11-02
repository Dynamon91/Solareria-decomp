using Cysharp.Threading.Tasks;
using Library;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SPR.UI.Ending;

public class EndingManager : Manager, IControllable
{
	[SerializeField]
	private TextMeshProUGUI thanksText;

	[SerializeField]
	private CanvasGroup keyGuideCanvasGroup;

	[SerializeField]
	private GameObject guideIcon;

	private static UIManager UIManager => Game.Instance.UIManager;

	private static InputManager InputManager => Game.Instance.InputManager;

	public bool IsValid => !this.IsNull();

	private void Awake()
	{
		Game.Instance.SetManager(this);
	}

	protected override void OnInitialize()
	{
	}

	public override UniTask OnSceneLoaded(Scene loadedScene)
	{
		if (loadedScene.name.Equals("EndingDemo"))
		{
			thanksText.color = new Color(1f, 1f, 1f, 0f);
			keyGuideCanvasGroup.alpha = 0f;
		}
		return default(UniTask);
	}

	public string ControllerName()
	{
		return "Ending Manager";
	}

	public async UniTask Activate()
	{
		UIManager.FieldCanvas.HideAllAsync(instant: true).Forget();
		await thanksText.DOAlpha(1f, 1f).WaitAsync();
		guideIcon.gameObject.SetActive(value: true);
		await keyGuideCanvasGroup.DOAlpha(1f, 2f).WaitAsync();
	}

	public async void Control()
	{
		bool confirmDown = InputManager.confirmDown;
		bool flag = InputManager.menuDown || InputManager.cancelDown;
		if (confirmDown || flag)
		{
			await Game.Instance.LoadTitleAsync();
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
		Game.Instance.RemoveManager(this);
		return default(UniTask);
	}
}
