using Cysharp.Threading.Tasks;
using Rewired;

namespace SPR.Field;

public class SproutStoneController : IControllable
{
	private readonly ShadowSproutStone stone;

	public bool IsValid => true;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static SkipBoard SkipBoard => Game.Instance.UIManager.FadeCanvas.SkipBoard;

	public string ControllerName()
	{
		return "Sprout Stone Controller";
	}

	public SproutStoneController(ShadowSproutStone stone)
	{
		this.stone = stone;
	}

	public UniTask Activate()
	{
		SkipBoard.Initialize(0.5f);
		stone.PlayInteractTimelineAsync().GetAwaiter().OnCompleted(delegate
		{
			InputManager.RemoveControlTarget(this).Forget();
		});
		return UniTask.CompletedTask;
	}

	private async UniTask ActivateAsync()
	{
		stone.PlayInteractTimelineAsync().GetAwaiter().OnCompleted(delegate
		{
			InputManager.RemoveControlTarget(this).Forget();
		});
	}

	public void Control()
	{
		bool menu = InputManager.menu;
		if (!menu && ReInput.controllers.GetAnyButtonDown())
		{
			SkipBoard.Activate();
		}
		if (SkipBoard.CheckInput(menu))
		{
			UniTask task = SkipTimelineAsync();
			InputManager.BlockControlForTask(task, "SkipTimeline").Forget();
		}
	}

	public UniTask Pause()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Restart()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Deactivate()
	{
		SkipBoard.Deactivate();
		stone.OnServiceClosing();
		return UniTask.CompletedTask;
	}

	private async UniTask SkipTimelineAsync()
	{
		await Game.Instance.UIManager.FadeCanvas.FadeOut(0.5f);
		stone.SkipTimeline();
		await Game.Instance.UIManager.FadeCanvas.FadeIn(0.5f);
	}
}
