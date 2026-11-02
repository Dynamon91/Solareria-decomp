using Cysharp.Threading.Tasks;
using Library;
using Rewired;
using SPR.L10N;
using SPR.Library;
using SPR.UI;
using UnityEngine;

namespace SPR.Cutscene;

public abstract class ButtonActionBase : MonoBehaviour, IControllable, IControllerEventUpdate
{
	[Header("Ingame")]
	[SerializeField]
	[Range(0f, 1f)]
	private float initialGauge = 0.5f;

	[SerializeField]
	private int minNumPerSecond;

	[SerializeField]
	private float duration;

	[Header("References")]
	[SerializeField]
	private ButtonActionCanvas buttonActionShort;

	[SerializeField]
	private ButtonActionCanvas buttonActionLong;

	private const float NegativeCf = -1f;

	protected float Gauge;

	private bool wasPulling;

	private bool isKeyLong;

	protected float Timer = float.MaxValue;

	private float lastHitTimer = float.MaxValue;

	private CutsceneProducer cutsceneProducer;

	public bool IsValid => !this.IsNull();

	private static InputManager InputManager => Game.Instance.InputManager;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	protected float Duration => duration;

	private float GaugePerClick => 2f / ((float)minNumPerSecond * duration) / 2f;

	private float NegativeGaugePerSec => -1f / duration / 2f;

	private float MeanClickPeriod
	{
		get
		{
			if (minNumPerSecond != 0)
			{
				return 1f / (float)minNumPerSecond;
			}
			return 0f;
		}
	}

	private bool IsComplete => Gauge >= 1f;

	private ButtonActionCanvas CurrentButtonAction
	{
		get
		{
			if (!isKeyLong)
			{
				return buttonActionShort;
			}
			return buttonActionLong;
		}
	}

	protected CutsceneProducer CutsceneProducer => cutsceneProducer ?? (cutsceneProducer = base.transform.parent.GetComponent<CutsceneProducer>());

	private void Awake()
	{
		RewiredHelper.RegisterControllerChangeEvent(this);
	}

	private void OnDestroy()
	{
		RewiredHelper.RemoveControllerChangeEvent(this);
	}

	public string ControllerName()
	{
		return GetType().Name;
	}

	public async UniTask Activate()
	{
		await UniTask.Yield();
		base.gameObject.SetActive(value: true);
		Gauge = initialGauge;
		Timer = 0f;
		lastHitTimer = float.MaxValue;
		UpdateButtonActionCanvas();
		SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_ButtonAction_Appear");
	}

	public void Control()
	{
		if (InputManager.confirmDown)
		{
			Gauge += GaugePerClick;
			lastHitTimer = 0f;
			CurrentButtonAction.OnPress();
			SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_ButtonAction_Input");
		}
		if (IsComplete)
		{
			InputManager.RemoveControlTarget(this).GetAwaiter().OnCompleted(OnSuccess);
			SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_ButtonAction_Clear");
		}
		if (Timer >= duration)
		{
			InputManager.RemoveControlTarget(this).GetAwaiter().OnCompleted(OnFail);
		}
		OnControl();
		Timer += Time.deltaTime;
		lastHitTimer += Time.deltaTime;
		Gauge += NegativeGaugePerSec * Time.deltaTime;
		Show();
	}

	protected abstract void OnSuccess();

	protected abstract void OnFail();

	protected virtual void OnControl()
	{
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
		await CurrentButtonAction.OnFinishAsync(IsComplete);
		base.gameObject.SetActive(value: false);
	}

	private void Show()
	{
		CurrentButtonAction.SetFill(Gauge);
		bool flag = lastHitTimer < MeanClickPeriod;
		if (!wasPulling && flag)
		{
			OnPull();
		}
		else if (wasPulling && !flag)
		{
			OnNotPull();
		}
		wasPulling = flag;
	}

	protected abstract void OnPull();

	protected abstract void OnNotPull();

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		if (base.gameObject.activeInHierarchy)
		{
			UpdateButtonActionCanvas();
		}
	}

	private void UpdateButtonActionCanvas()
	{
		isKeyLong = ButtonActionHelper.IsKeyLong();
		buttonActionShort.SetActive(!isKeyLong, Gauge);
		buttonActionLong.SetActive(isKeyLong, Gauge);
		Locale.TryParseIcon("<icon=input_confirmui>", out var output);
		CurrentButtonAction.SetText(output);
		CurrentButtonAction.SetPosition(base.transform.position);
	}
}
