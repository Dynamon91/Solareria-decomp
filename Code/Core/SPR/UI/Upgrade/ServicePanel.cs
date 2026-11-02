using System;
using Cysharp.Threading.Tasks;
using SPR.Save;
using SPR.UI.Field;
using UnityEngine;

namespace SPR.UI.Upgrade;

public abstract class ServicePanel<TArg> : MonoBehaviour, IControllable
{
	protected IService ServiceOwner;

	protected GameObject ServiceVcam;

	protected abstract bool UsingOhn { get; }

	protected static InputManager InputManager => Game.Instance.InputManager;

	protected static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	protected static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	protected static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	protected static ConfirmPopup ConfirmPopup => FieldCanvas.ConfirmPopup;

	public abstract bool IsValid { get; }

	protected event Action OnDeactiveteEvent;

	public abstract void Initialize(IService serviceObject, GameObject vcam, TArg arg);

	public abstract bool HasStock();

	public abstract string ControllerName();

	public virtual async UniTask Activate()
	{
		await OnActivate();
		if (UsingOhn)
		{
			Game.Instance.UIManager.FieldCanvas.MoneyBoard.ActivateContinuous(value: true);
		}
	}

	public virtual async UniTask Deactivate()
	{
		if (UsingOhn)
		{
			Game.Instance.UIManager.FieldCanvas.MoneyBoard.ActivateContinuous(value: false);
		}
		await OnDeactivate();
		this.OnDeactiveteEvent?.Invoke();
		this.OnDeactiveteEvent = null;
	}

	protected abstract UniTask OnActivate();

	protected abstract UniTask OnDeactivate();

	public abstract void Control();

	public abstract UniTask Pause();

	public abstract UniTask Restart();
}
