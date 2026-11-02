using UnityEngine;

namespace SPR.UI.Option;

public abstract class SettingItem : MonoBehaviour
{
	private CanvasGroup canvasGroup;

	public CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	protected static SettingData SettingData => Game.Instance.SettingData;

	public abstract void OnFocus(bool value);

	public abstract void Control(int horizontal, int vertical);

	public abstract void Synchronize();

	public abstract void Save();

	public virtual void OnConfirm()
	{
	}
}
