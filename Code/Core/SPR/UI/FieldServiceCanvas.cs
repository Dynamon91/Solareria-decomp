using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Field;
using SPR.L10N;
using SPR.UI.Choice;
using SPR.UI.Store;
using SPR.UI.Upgrade;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class FieldServiceCanvas : MonoBehaviour, IControllable
{
	[SerializeField]
	private float inputDelayDuration;

	[Header("References")]
	[SerializeField]
	private LocaleText serviceNameLocaleText;

	[SerializeField]
	private List<ChoiceOption> choiceOptions;

	[SerializeField]
	private OptionSelectMarker selectMarker;

	[SerializeField]
	private MarkerIcon markerIcon;

	[SerializeField]
	private RectTransform messageBoxRectTransform;

	[SerializeField]
	private Image lineImage;

	private IService serviceOwner;

	private RectTransform rectTransform;

	private GameObject serviceVcam;

	private List<ChoiceData> choices;

	private int targetIndex;

	private RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	private static InputManager InputManager => Game.Instance.InputManager;

	private static UIManager UIManager => Game.Instance.UIManager;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public bool IsValid => true;

	public void Initialize(IService owner, ServiceInfo info)
	{
		serviceOwner = owner;
		serviceNameLocaleText.SetData(LocaleKey.ServiceName(info.serviceName));
		RectTransform.anchoredPosition = info.canvasPivot;
		serviceVcam = info.vcam;
		choices = new List<ChoiceData>();
		foreach (FieldServiceType serviceType in GetServiceTypes(info.serviceName))
		{
			AddChoice(serviceType);
		}
		SetActive(value: false);
	}

	public void PlaceOnRight(bool playerIsOnLeft)
	{
		Vector2 anchoredPosition = RectTransform.anchoredPosition;
		anchoredPosition.x = (float)(playerIsOnLeft ? 1 : (-1)) * Mathf.Abs(anchoredPosition.x);
		RectTransform.anchoredPosition = anchoredPosition;
	}

	public void SetActive(bool value)
	{
		base.gameObject.SetActive(value);
	}

	private void AddChoice(FieldServiceType serviceType)
	{
		string localeString = Locale.GetLocaleString(LocaleKey.ServiceTypeName(serviceType));
		Action onSelectChoice = serviceType switch
		{
			FieldServiceType.Exit => ExitAction, 
			FieldServiceType.Exchange => delegate
			{
				OpenOrWarnServicePanel(FieldServiceType.Exchange, UIManager.FieldCanvas.ExchangePanel);
			}, 
			FieldServiceType.Donation => delegate
			{
				OpenServicePanel(UIManager.FieldCanvas.DonationPanel);
			}, 
			FieldServiceType.Cook => CookAction, 
			FieldServiceType.LevelUp => delegate
			{
				OpenServicePanel(UIManager.FieldCanvas.LevelUpPanel);
			}, 
			FieldServiceType.Teleport => delegate
			{
				UsePortal().Forget();
			}, 
			FieldServiceType.Talk => StartTalk, 
			FieldServiceType.SproutReward => delegate
			{
				DropRewardOrWarnAsync().Forget();
			}, 
			FieldServiceType.OpenHonorDoor => OpenHonorDoor, 
			FieldServiceType.Parts => delegate
			{
				OpenServicePanel(UIManager.FieldCanvas.PartUpgradePanel);
			}, 
			FieldServiceType.WarmhornCount => delegate
			{
				OpenServicePanel(UIManager.FieldCanvas.UpgradeWarmhornCountPanel);
			}, 
			FieldServiceType.CoreStone => delegate
			{
				OpenServicePanel(UIManager.FieldCanvas.CardEngravingPanel);
			}, 
			FieldServiceType.GiantElevator => delegate
			{
				MoveGiantElevatorAsync().Forget();
			}, 
			FieldServiceType.SoowooCave => delegate
			{
				TryOpenSoowooCaveAsync().Forget();
			}, 
			FieldServiceType.MapMarker => delegate
			{
				OpenOrWarnStorePanel(FieldServiceType.MapMarker, StoreType.Painter);
			}, 
			FieldServiceType.OpenMucoraDoor => OpenMucoraDoor, 
			FieldServiceType.Dev_Test => delegate
			{
				OpenServicePanel(UIManager.FieldCanvas.CardEngravingPanel);
			}, 
			FieldServiceType.PlayOrgel => delegate
			{
				ChangeOrgel(play: true);
			}, 
			FieldServiceType.StopOrgel => delegate
			{
				ChangeOrgel(play: false);
			}, 
			FieldServiceType.OpenSolinLabEntranceDoor => delegate
			{
				OpenSolinLabEntranceDoorAsync().Forget();
			}, 
			_ => throw new ArgumentOutOfRangeException("serviceType", serviceType, null), 
		};
		choices.Add(new ChoiceData(localeString, showNotiIcon: false, onSelectChoice));
	}

	private async UniTask SelectChoiceAsync()
	{
		SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Interaction");
		UniTask task = markerIcon.ShakeVerticalAsync();
		await InputManager.BlockControlForTask(task, "Select Marker Icon Shake");
		choices[targetIndex].OnSelectChoice();
	}

	private int GetNextTargetIndex(int vertical)
	{
		int num = targetIndex - vertical;
		if (num < 0)
		{
			num = 0;
		}
		else if (num >= choices.Count)
		{
			num = choices.Count - 1;
		}
		return num;
	}

	private void ChangeTargetTo(int index, bool immediately = false)
	{
		if (targetIndex != index || immediately)
		{
			targetIndex = index;
			for (int i = 0; i < choiceOptions.Count; i++)
			{
				choiceOptions[i].Focus(i == targetIndex);
			}
			selectMarker.MoveToAsync(choiceOptions[targetIndex].Rect, immediately).Forget();
			if (!immediately)
			{
				SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Move");
			}
		}
	}

	public string ControllerName()
	{
		return "Service Choice Board";
	}

	public async UniTask Activate()
	{
		for (int i = 0; i < choiceOptions.Count; i++)
		{
			if (i < choices.Count)
			{
				choiceOptions[i].SetData(choices[i].ChoiceText);
				choiceOptions[i].Show(choices[i].ShowNotiIcon);
			}
			else
			{
				choiceOptions[i].Hide();
			}
		}
		RectTransform.sizeDelta = new Vector2(RectTransform.sizeDelta.x, 120f * (float)choices.Count + 260f);
		selectMarker.SetActive(value: false);
		ChangeTargetTo(0, immediately: true);
		Vector2 anchoredPosition = messageBoxRectTransform.anchoredPosition;
		anchoredPosition.x = -800f;
		messageBoxRectTransform.anchoredPosition = anchoredPosition;
		lineImage.DOAlpha(0f, 0f);
		SetActive(value: true);
		await UniTask.WaitForSeconds(inputDelayDuration);
		Sequence sequence = DOTween.Sequence();
		sequence.Append(lineImage.DOAlpha(0.3f, Time.fixedDeltaTime * 20f));
		sequence.Insert(Time.fixedDeltaTime * 5f, messageBoxRectTransform.DOAnchorPosX(0f, Time.fixedDeltaTime * 15f));
		SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_ServicePanel");
		await sequence.WaitAsync();
		selectMarker.SetActive(value: true);
		SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Move");
	}

	public void Control()
	{
		bool confirmDown = InputManager.confirmDown;
		int verticalDownUI = InputManager.verticalDownUI;
		bool flag = InputManager.menuDown || InputManager.cancelDown;
		if (confirmDown)
		{
			SelectChoiceAsync().Forget();
		}
		if (verticalDownUI != 0)
		{
			int nextTargetIndex = GetNextTargetIndex(verticalDownUI);
			ChangeTargetTo(nextTargetIndex);
		}
		if (flag)
		{
			ExitAction();
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

	public async UniTask Deactivate()
	{
		Sequence sequence = DOTween.Sequence();
		sequence.Append(lineImage.DOAlpha(0f, Time.fixedDeltaTime * 15f));
		sequence.Join(messageBoxRectTransform.DOAnchorPosX(-800f, Time.fixedDeltaTime * 10f));
		await sequence.WaitAsync();
		ChangeTargetTo(0, immediately: true);
		SetActive(value: false);
	}

	private void ExitAction()
	{
		serviceVcam.gameObject.SetActive(value: false);
		Game.Instance.Player.mover.SetAnimation("idle");
		InputManager.RemoveControlTarget(this).Forget();
	}

	private void DonationAction()
	{
		InputManager.RemoveControlTarget(this).GetAwaiter().OnCompleted(delegate
		{
			InputManager.StackControlTarget(UIManager.FieldCanvas.DonationPanel).Forget();
		});
	}

	private void CookAction()
	{
		StorePanel storePanel = UIManager.FieldCanvas.StorePanel;
		string text = serviceOwner.Name;
		StoreType storeType;
		if (!(text == "TouristPot"))
		{
			if (!(text == "CookMachine"))
			{
				throw new ArgumentOutOfRangeException();
			}
			storeType = StoreType.Restaurant;
		}
		else
		{
			storeType = StoreType.TouristPot;
		}
		StoreType storeType2 = storeType;
		if (storeType2 == StoreType.TouristPot)
		{
			OpenOrWarnStorePanel(FieldServiceType.Cook, storeType2);
		}
		else
		{
			OpenServicePanel(storePanel, storeType2);
		}
	}

	private void OpenServicePanel<T>(ServicePanel<T> panel, T tArg = default(T))
	{
		panel.Initialize(serviceOwner, serviceVcam, tArg);
		InputManager.SwitchControlTarget(this, panel).Forget();
	}

	private void OpenOrWarnServicePanel<T>(FieldServiceType serviceType, ServicePanel<T> panel, T tArg = default(T))
	{
		if (panel.HasStock())
		{
			OpenServicePanel(panel, tArg);
			return;
		}
		UIManager.FadeCanvas.WarningBoard.ShowWarning(LocaleKey.ServiceWarning(serviceType, UpgradeCheckResult.NotEnoughMaterial));
		ExitAction();
	}

	private void OpenOrWarnStorePanel(FieldServiceType serviceType, StoreType type)
	{
		StorePanel storePanel = UIManager.FieldCanvas.StorePanel;
		storePanel.Initialize(serviceOwner, serviceVcam, type);
		if (storePanel.HasStock())
		{
			InputManager.SwitchControlTarget(this, storePanel).Forget();
			return;
		}
		UIManager.FadeCanvas.WarningBoard.ShowWarning(LocaleKey.ServiceWarning(serviceType, UpgradeCheckResult.NotEnoughMaterial));
		ExitAction();
	}

	private async UniTaskVoid DropRewardOrWarnAsync()
	{
		if (serviceOwner.CastObject<ShadowSproutStone>().HasReward())
		{
			await InputManager.RemoveControlTarget(this);
			await InputManager.StackControlTarget(serviceOwner.CastObject<ShadowSproutStone>().Controller);
		}
		else
		{
			UIManager.FadeCanvas.WarningBoard.ShowWarning(LocaleKey.SproutWarning(Game.FieldID.GetRegionName()));
			ExitAction();
		}
	}

	private async UniTaskVoid UsePortal()
	{
		await InputManager.RemoveControlTarget(this);
		await InputManager.BlockControlForTask(serviceOwner.CastObject<Portal>().MoveSceneAsync(), "Use Portal");
	}

	private void StartTalk()
	{
		InputManager.RemoveControlTarget(this).Forget();
		Scenario scenario = ScenarioHelper.GetScenario(serviceOwner.Name + "_Talk");
		UIManager.TalkCanvas.StartTalk(scenario);
	}

	private async UniTaskVoid MoveGiantElevatorAsync()
	{
		await InputManager.RemoveControlTarget(this);
		await InputManager.BlockControlForTask(serviceOwner.CastObject<GiantElevator>().MoveSceneAsync(serviceVcam), "Move Giant Elevator");
	}

	private void OpenHonorDoor()
	{
		serviceOwner.CastObject<HonorDoor>().TryOpenDoor();
		InputManager.RemoveControlTarget(this).Forget();
	}

	private void OpenMucoraDoor()
	{
		InputManager.RemoveControlTarget(this).Forget();
		InputManager.BlockControlForTask(serviceOwner.CastObject<MucoraPortalDoor>().OpenDoor(), "Open Mucora Portal Door").Forget();
	}

	private async UniTaskVoid TryOpenSoowooCaveAsync()
	{
		if (Game.Instance.SaveData.PlayerData.TryRemoveAcquiredItem("SoowooCavePass"))
		{
			InputManager.RemoveControlTarget(this).Forget();
			serviceOwner.CastObject<SoowooCaveDoor>().OpenDoor();
			UIManager.FadeCanvas.WarningBoard.ShowWarning(LocaleKey.ServiceWarning(FieldServiceType.SoowooCave, UpgradeCheckResult.Success));
		}
		else
		{
			ExitAction();
			UIManager.FadeCanvas.WarningBoard.ShowWarning(LocaleKey.ServiceWarning(FieldServiceType.SoowooCave, UpgradeCheckResult.NotEnoughMaterial));
		}
	}

	private void ChangeOrgel(bool play)
	{
		((BgmRadioService)serviceOwner).ChangeBgm(play);
		ExitAction();
	}

	private async UniTask OpenSolinLabEntranceDoorAsync()
	{
		if (Game.Instance.SaveData.PlayerData.HasEverObtainedItem("SolinLabEntranceKey", out var _))
		{
			await InputManager.RemoveControlTarget(this);
			await InputManager.BlockControlForTask(serviceOwner.CastObject<SolinLabEntranceDoor>().OpenDoorAsync(), "Open SolinLab Entrance Door");
		}
		else
		{
			Game.Instance.UIManager.FadeCanvas.WarningBoard.ShowWarning("UI_SolinLabEntranceDoor_Warning");
		}
	}

	private static List<FieldServiceType> GetServiceTypes(ServiceName serviceName)
	{
		return serviceName switch
		{
			ServiceName.OhnExchanger => new List<FieldServiceType>
			{
				FieldServiceType.Exchange,
				FieldServiceType.Exit
			}, 
			ServiceName.DonationBox => new List<FieldServiceType>
			{
				FieldServiceType.Donation,
				FieldServiceType.Exit
			}, 
			ServiceName.TouristPot => new List<FieldServiceType>
			{
				FieldServiceType.Cook,
				FieldServiceType.Exit
			}, 
			ServiceName.VeritaHologram => new List<FieldServiceType>
			{
				FieldServiceType.LevelUp,
				FieldServiceType.Exit
			}, 
			ServiceName.Portal => new List<FieldServiceType>
			{
				FieldServiceType.Teleport,
				FieldServiceType.Exit
			}, 
			ServiceName.SproutStone => new List<FieldServiceType>
			{
				FieldServiceType.SproutReward,
				FieldServiceType.Exit
			}, 
			ServiceName.HonorDoor => new List<FieldServiceType>
			{
				FieldServiceType.OpenHonorDoor,
				FieldServiceType.Exit
			}, 
			ServiceName.PartsMachine => new List<FieldServiceType>
			{
				FieldServiceType.Parts,
				FieldServiceType.Exit
			}, 
			ServiceName.WarmhornMachine => new List<FieldServiceType>
			{
				FieldServiceType.WarmhornCount,
				FieldServiceType.Exit
			}, 
			ServiceName.CoreStoneMachine => new List<FieldServiceType>
			{
				FieldServiceType.CoreStone,
				FieldServiceType.Exit
			}, 
			ServiceName.CookMachine => new List<FieldServiceType>
			{
				FieldServiceType.Cook,
				FieldServiceType.Exit
			}, 
			ServiceName.GiantElevator => new List<FieldServiceType>
			{
				FieldServiceType.GiantElevator,
				FieldServiceType.Exit
			}, 
			ServiceName.SoowooCave => new List<FieldServiceType>
			{
				FieldServiceType.SoowooCave,
				FieldServiceType.Exit
			}, 
			ServiceName.MuhaHouseJar => new List<FieldServiceType>
			{
				FieldServiceType.MapMarker,
				FieldServiceType.Exit
			}, 
			ServiceName.MucoraPortalDoor => new List<FieldServiceType>
			{
				FieldServiceType.OpenMucoraDoor,
				FieldServiceType.Exit
			}, 
			ServiceName.SeedBureauOrgel => new List<FieldServiceType>
			{
				FieldServiceType.PlayOrgel,
				FieldServiceType.StopOrgel
			}, 
			ServiceName.SolinLabEntranceDoor => new List<FieldServiceType>
			{
				FieldServiceType.OpenSolinLabEntranceDoor,
				FieldServiceType.Exit
			}, 
			ServiceName.Dev_Test => new List<FieldServiceType>
			{
				FieldServiceType.Dev_Test,
				FieldServiceType.Exit
			}, 
			ServiceName.TycheTreasureRadio => new List<FieldServiceType>
			{
				FieldServiceType.PlayOrgel,
				FieldServiceType.StopOrgel
			}, 
			ServiceName.HotelRadio => new List<FieldServiceType>
			{
				FieldServiceType.PlayOrgel,
				FieldServiceType.StopOrgel
			}, 
			_ => throw new ArgumentOutOfRangeException("serviceName", serviceName, null), 
		};
	}
}
