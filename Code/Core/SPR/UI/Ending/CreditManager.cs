using Cysharp.Threading.Tasks;
using Library;
using SPR.Achievement;
using StaticData;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SPR.UI.Ending;

public class CreditManager : Manager, IControllable
{
	[SerializeField]
	private CreditTextController creditTextController;

	[SerializeField]
	private CanvasGroup keyGuideCanvasGroup;

	[SerializeField]
	private float speed;

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
		return default(UniTask);
	}

	private async UniTask PlayRollAsync()
	{
		await UniTask.WaitForSeconds(3f);
		creditTextController.SetData();
		await creditTextController.PlayText(speed);
		await UniTask.WaitForSeconds(2f);
		await keyGuideCanvasGroup.DOAlpha(1f, 0.5f).WaitAsync();
	}

	public string ControllerName()
	{
		return "Credit Manager";
	}

	public UniTask Activate()
	{
		if (Game.Instance.SaveData.EasiestDifficulty == Difficulty.Main)
		{
			AchievementHelper.Unlock("KingsReturn");
		}
		AchievementHelper.CheckTriggers();
		UIManager.FieldCanvas.HideAllAsync(instant: true).Forget();
		keyGuideCanvasGroup.alpha = 0f;
		PlayRollAsync().Forget();
		return UniTask.CompletedTask;
	}

	public async void Control()
	{
		if (!(keyGuideCanvasGroup.alpha < 1f) && InputManager.confirmDown)
		{
			await Game.Instance.UIManager.FadeCanvas.FadeOut(0.4f);
			await UniTask.WaitForSeconds(4f);
			Game.Instance.Save();
			await Game.Instance.LoadTitleAsync();
			await InputManager.RemoveControlTarget(this);
			await InputManager.RemoveControlTarget(Game.Instance.Player);
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
