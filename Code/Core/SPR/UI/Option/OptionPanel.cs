using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.UI.Option;

public abstract class OptionPanel : MonoBehaviour, IControllable
{
	protected static InputManager InputManager => Game.Instance.InputManager;

	public bool IsValid => true;

	public abstract string ControllerName();

	public virtual void Initialize()
	{
		base.gameObject.SetActive(value: false);
	}

	public UniTask Activate()
	{
		base.gameObject.SetActive(value: true);
		OnActivate();
		return UniTask.CompletedTask;
	}

	protected abstract void OnActivate();

	public void Control()
	{
		bool num = InputManager.menuDown || InputManager.cancelDown;
		OnControl();
		if (num)
		{
			DeactivateOption().Forget();
		}
	}

	protected abstract void OnControl();

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
		base.gameObject.SetActive(value: false);
		OnDeactivate();
		return UniTask.CompletedTask;
	}

	protected virtual void OnDeactivate()
	{
	}

	protected virtual async UniTask DeactivateOption()
	{
		await InputManager.RemoveControlTarget(this);
		await InputManager.RemoveControlTarget(Game.Instance.UIManager.OptionCanvas);
	}
}
