using Cysharp.Threading.Tasks;
using Library;
using SPR.UI.Store;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.UI.Upgrade;

public abstract class UpgradePanel : ServicePanel<Unit>
{
	[Header("General")]
	[SerializeField]
	protected UpgradeBoardBase upgradeBoard;

	[SerializeField]
	private PlayableAsset openTimeline;

	[SerializeField]
	protected PlayableAsset successTimeline;

	private PlayableDirector director;

	protected virtual bool CanDeactivate => true;

	protected PlayableDirector Director => director ?? (director = GetComponent<PlayableDirector>());

	protected bool IsDirecting => Director.state == PlayState.Playing;

	private static PlayerMover PlayerMover => Game.Instance.Player.mover;

	public override bool IsValid => !BasicExtension.IsNull(base.gameObject);

	protected abstract string ConfirmType { get; }

	public override void Initialize(IService serviceObject, GameObject vcam, Unit arg)
	{
		ServiceOwner = serviceObject;
		ServiceVcam = vcam;
	}

	public override bool HasStock()
	{
		return true;
	}

	protected override async UniTask OnActivate()
	{
		await OnActivateAsync();
		PlayerMover.SetAnimation("cam_up");
		base.gameObject.SetActive(value: true);
		ServiceOwner.OnServiceOpening();
		ServiceVcam.SetActive(value: true);
		if (openTimeline != null)
		{
			Director.playableAsset = openTimeline;
			Director.Play();
		}
		await UniTask.WaitWhile(() => Director.state == PlayState.Playing);
	}

	protected abstract UniTask OnActivateAsync();

	public override void Control()
	{
		int horizontalDownUI = ServicePanel<Unit>.InputManager.horizontalDownUI;
		int verticalDownUI = ServicePanel<Unit>.InputManager.verticalDownUI;
		bool confirmDown = ServicePanel<Unit>.InputManager.confirmDown;
		bool flag = ServicePanel<Unit>.InputManager.menuDown || ServicePanel<Unit>.InputManager.cancelDown;
		if (horizontalDownUI != 0 || verticalDownUI != 0)
		{
			OnControlInput(horizontalDownUI, verticalDownUI);
		}
		else if (confirmDown)
		{
			if (IsDirecting)
			{
				OnDirectingInput();
				return;
			}
			UpgradeCheckResult upgradeCheckResult = upgradeBoard.CanProceedUpgrade();
			if (upgradeCheckResult == UpgradeCheckResult.Success)
			{
				OnUpgradeSuccess();
			}
			else
			{
				OnUpgradeFailed(upgradeCheckResult);
			}
		}
		else if (flag && CanDeactivate)
		{
			if (IsDirecting)
			{
				OnDirectingInput();
			}
			else
			{
				ServicePanel<Unit>.InputManager.RemoveControlTarget(this).Forget();
			}
		}
	}

	protected abstract void OnControlInput(int horizontal, int vertical);

	protected virtual void OnDirectingInput()
	{
	}

	protected virtual void OnUpgradeSuccess()
	{
		ServicePanel<Unit>.ConfirmPopup.SetModel(new ConfirmModel(ConfirmType, OnConfirmInput));
		ServicePanel<Unit>.InputManager.StackControlTarget(ServicePanel<Unit>.ConfirmPopup).Forget();
	}

	protected virtual void OnUpgradeFailed(UpgradeCheckResult result)
	{
		upgradeBoard.OnBlockUpgrade(result);
	}

	protected abstract void OnConfirmInput();

	public override UniTask Pause()
	{
		return UniTask.CompletedTask;
	}

	public override UniTask Restart()
	{
		return UniTask.CompletedTask;
	}

	protected override async UniTask OnDeactivate()
	{
		await OnDeactivateAsync();
		base.gameObject.SetActive(value: false);
		ServiceOwner.OnServiceClosing();
		ServiceVcam.SetActive(value: false);
		PlayerMover.SetAnimation("idle");
	}

	protected virtual UniTask OnDeactivateAsync()
	{
		return UniTask.CompletedTask;
	}

	protected abstract void UpdateControlGuide();

	protected async UniTask PlaySuccessTimelineAsync()
	{
		Director.playableAsset = successTimeline;
		Director.Play();
		await UniTask.WaitWhile(() => Director.state == PlayState.Playing);
	}

	protected void CheckSoldOutScenario()
	{
		if (!HasStock())
		{
			if (ServiceOwner is ServiceNPC<Unit> serviceNPC)
			{
				serviceNPC.PlaySoldOutScenario();
			}
			ServicePanel<Unit>.InputManager.RemoveControlTarget(this).Forget();
			if (ServiceOwner is ServiceNPC<StoreType> serviceNPC2)
			{
				serviceNPC2.PlaySoldOutScenario();
			}
		}
	}

	public abstract void UpdateData();
}
