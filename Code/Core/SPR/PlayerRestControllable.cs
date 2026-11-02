using Cysharp.Threading.Tasks;
using SPR.UI.Field;

namespace SPR;

public class PlayerRestControllable : IControllable
{
	public bool buttonDown;

	public bool IsValid => true;

	private static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	private static InputManager InputManager => Game.Instance.InputManager;

	public string ControllerName()
	{
		return "Player Idle Rest";
	}

	public UniTask Activate()
	{
		buttonDown = false;
		return UniTask.CompletedTask;
	}

	public void Control()
	{
		buttonDown = InputManager.horizontal != 0f || InputManager.vertical != 0 || InputManager.attackDown || InputManager.guardDown;
		PlayerController.OpenUI();
	}

	public UniTask Pause()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Restart()
	{
		FieldCanvas.ShowAllAsync(instant: false).Forget();
		return UniTask.CompletedTask;
	}

	public UniTask Deactivate()
	{
		return UniTask.CompletedTask;
	}
}
