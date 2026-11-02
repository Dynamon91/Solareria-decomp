using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using Library;
using UnityEngine;

namespace SPR.UI;

public class PausePanel : MonoBehaviour, IControllable
{
	[SerializeField]
	private LocaleText titleText;

	private CanvasGroup canvasGroup;

	private float originalTimeScale;

	private Bus masterBus;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private Bus MasterBus
	{
		get
		{
			if (!masterBus.isValid())
			{
				return RuntimeManager.GetBus("bus:/");
			}
			return masterBus;
		}
	}

	private static InputManager InputManager => Game.Instance.InputManager;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public bool IsValid => !base.gameObject.IsNull();

	public string ControllerName()
	{
		return "Pause Panel";
	}

	public UniTask Activate()
	{
		base.gameObject.SetActive(value: true);
		MasterBus.setPaused(paused: true);
		originalTimeScale = Time.timeScale;
		Time.timeScale = 0f;
		return UniTask.CompletedTask;
	}

	public void Control()
	{
		if (InputManager.pauseDown || InputManager.menuDown || InputManager.cancelDown)
		{
			InputManager.RemoveControlTarget(this).Forget();
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
		base.gameObject.SetActive(value: false);
		MasterBus.setPaused(paused: false);
		InputManager.pauseDown = false;
		Time.timeScale = originalTimeScale;
		return UniTask.CompletedTask;
	}
}
