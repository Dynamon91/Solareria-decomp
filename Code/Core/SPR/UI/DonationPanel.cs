using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.Achievement;
using SPR.Field;
using SPR.Save;
using SPR.UI.Upgrade;
using TMPro;
using UnityEngine;

namespace SPR.UI;

public class DonationPanel : ServicePanel<Unit>
{
	private const int FadeDuration = 20;

	[SerializeField]
	private int unit = 100;

	[Header("References")]
	[SerializeField]
	private MarkerIcon markerIcon;

	[SerializeField]
	private EventReference donationSFX;

	[SerializeField]
	private CanvasGroup mainPanelCanvasGroup;

	[SerializeField]
	private CanvasGroup menuGroup;

	[SerializeField]
	private CanvasGroup thanksGroup;

	[SerializeField]
	private Animator leftArrow;

	[SerializeField]
	private Animator rightArrow;

	[SerializeField]
	private TextMeshProUGUI amountText;

	[SerializeField]
	private TextMeshProUGUI totalDonationText;

	[SerializeField]
	private TextMeshProUGUI thanksTotalDonationText;

	[SerializeField]
	private LocaleText thanksMessage;

	private int amount;

	private bool amountInputProgress;

	protected override bool UsingOhn => true;

	public override bool IsValid => !base.gameObject.IsNull();

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	private int TotalDonation => WanderingData.TotalDonation;

	public override string ControllerName()
	{
		return "Donation Panel";
	}

	public override void Initialize(IService serviceObject, GameObject vcam, Unit arg)
	{
		mainPanelCanvasGroup.alpha = 0f;
		thanksGroup.alpha = 0f;
		ServiceOwner = serviceObject;
		ServiceVcam = vcam;
	}

	public override bool HasStock()
	{
		return true;
	}

	protected override async UniTask OnActivate()
	{
		ServicePanel<Unit>.FieldCanvas.HideAllAsync(instant: false).Forget();
		base.gameObject.SetActive(value: true);
		ServiceVcam.SetActive(value: true);
		menuGroup.alpha = 1f;
		HasEnoughOhnHold();
		amount = 0;
		amountText.text = amount.ToString();
		totalDonationText.text = TotalDonation.ToString();
		thanksTotalDonationText.text = TotalDonation.ToString();
		await mainPanelCanvasGroup.DOAlpha(1f, Util.FrameToSeconds(20f)).SetEase(Ease.Linear).WaitAsync();
		await UniTask.CompletedTask;
	}

	public override void Control()
	{
		int num = ServicePanel<Unit>.InputManager.horizontalUI;
		bool confirmDown = ServicePanel<Unit>.InputManager.confirmDown;
		if (ServicePanel<Unit>.InputManager.menuDown || ServicePanel<Unit>.InputManager.cancelDown)
		{
			ServicePanel<Unit>.InputManager.RemoveControlTarget(this).Forget();
		}
		if (HasEnoughOhnHold())
		{
			if ((num < 0 && !leftArrow.gameObject.activeSelf) || (num > 0 && !rightArrow.gameObject.activeSelf))
			{
				num = 0;
			}
			if (num != 0)
			{
				AdjustCost(num).Forget();
			}
			if (confirmDown)
			{
				ServicePanel<Unit>.InputManager.BlockControlForTask(Donate(), "Donate").Forget();
			}
		}
	}

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
		base.gameObject.SetActive(value: false);
		ServiceVcam.SetActive(value: false);
		await ServicePanel<Unit>.FieldCanvas.ShowAllAsync(instant: false);
	}

	private async UniTaskVoid AdjustCost(int value)
	{
		if (amountInputProgress)
		{
			return;
		}
		float num = 0f;
		if (value >= 0)
		{
			if (value > 0)
			{
				rightArrow.Play("action");
				num = rightArrow.GetCurrentAnimatorStateInfo(0).length;
			}
		}
		else
		{
			leftArrow.Play("action");
			num = leftArrow.GetCurrentAnimatorStateInfo(0).length;
		}
		amountInputProgress = true;
		int max = Mathf.Min(ServicePanel<Unit>.PlayerStat.Money, ServiceOwner.CastObject<DonationBox>().DonationThreshold - TotalDonation);
		amount = Mathf.Clamp(amount + value * unit, 0, max);
		amountText.text = amount.ToString();
		await UniTask.WaitForSeconds(num / 4f);
		amountInputProgress = false;
	}

	private async UniTask Donate()
	{
		if (amount > 0)
		{
			ServicePanel<Unit>.PlayerStat.AddMoney(-amount);
			Game.Instance.SoundManager.PlayOneShot(donationSFX);
			await markerIcon.ShakeVerticalAsync();
			await ShowThanksMessage(Util.FrameToSeconds(20f));
			amount = 0;
			amountText.text = amount.ToString();
		}
	}

	private async UniTask ShowThanksMessage(float duration)
	{
		int prevTotalDonation = TotalDonation;
		WanderingData.AddTotalDonation(amount);
		string data = ((TotalDonation < ServiceOwner.CastObject<DonationBox>().DonationThreshold) ? "UI_DonationBox_Thanks" : "UI_DonationBox_IsFull");
		thanksMessage.SetData(data);
		await menuGroup.DOAlpha(0f, duration).SetEase(Ease.Linear).WaitAsync();
		await thanksGroup.DOAlpha(1f, duration).SetEase(Ease.Linear).WaitAsync();
		await thanksTotalDonationText.DOIntText(prevTotalDonation + amount, 1f).SetEase(Ease.Linear).WaitAsync();
		totalDonationText.text = TotalDonation.ToString();
		await UniTask.WaitForSeconds(duration);
		await thanksGroup.DOAlpha(0f, duration).SetEase(Ease.Linear).WaitAsync();
		await mainPanelCanvasGroup.DOAlpha(0f, duration).SetEase(Ease.Linear).WaitAsync();
		await ServicePanel<Unit>.InputManager.RemoveControlTarget(this);
		ServiceOwner.OnServiceClosing();
		Game.Instance.Player.mover.SetAnimation("idle");
		if (TotalDonation >= ServiceOwner.CastObject<DonationBox>().DonationThreshold)
		{
			ServiceOwner.CastObject<DonationBox>().DropTycheSeal();
			AchievementHelper.Unlock("DonateMax");
		}
	}

	private bool HasEnoughOhnHold()
	{
		leftArrow.gameObject.SetActive(ServicePanel<Unit>.PlayerData.Money >= unit && amount > 0);
		rightArrow.gameObject.SetActive(ServicePanel<Unit>.PlayerData.Money >= amount + unit);
		return ServicePanel<Unit>.PlayerData.Money >= unit;
	}
}
