using DG.Tweening;
using SPR.UI.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Option;

public class OptionSoundPanel : MenuPanel
{
	[SerializeField]
	private Slider masterSlider;

	[SerializeField]
	private Slider bgmSlider;

	[SerializeField]
	private Slider sfxSlider;

	[SerializeField]
	private RectTransform cursorTransform;

	private Slider[] sliders;

	private int currentIndex;

	private const float FadeDuration = 0.6f;

	private Slider CurrentSlider => sliders[currentIndex];

	public override MenuCategory Category => MenuCategory.System;

	private void Awake()
	{
		sliders = new Slider[3] { masterSlider, bgmSlider, sfxSlider };
		masterSlider.onValueChanged.AddListener(MenuPanel.SoundManager.SetMasterVolume);
		bgmSlider.onValueChanged.AddListener(MenuPanel.SoundManager.SetBGMVolume);
		sfxSlider.onValueChanged.AddListener(MenuPanel.SoundManager.SetSFXVolume);
	}

	protected override void OnActivate()
	{
		masterSlider.value = MenuPanel.SettingData.MasterVol;
		bgmSlider.value = MenuPanel.SettingData.MusicVol;
		sfxSlider.value = MenuPanel.SettingData.SoundVol;
		Slider[] array = sliders;
		foreach (Slider target in array)
		{
			FadeHighlight(isOn: false, isInstant: true, target);
		}
		currentIndex = 0;
		FadeHighlight(isOn: true, isInstant: true);
		UpdateCursor(isInstant: true);
	}

	protected override void OnControl()
	{
		if (MenuPanel.InputManager.verticalDownUI != 0)
		{
			FadeHighlight(isOn: false, isInstant: false);
			currentIndex = Mathf.Clamp(currentIndex - MenuPanel.InputManager.verticalDownUI, 0, sliders.Length - 1);
			FadeHighlight(isOn: true, isInstant: false);
			UpdateCursor(isInstant: false);
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
		}
		if (MenuPanel.InputManager.horizontalDownUI != 0)
		{
			CurrentSlider.value += MenuPanel.InputManager.horizontalDownUI;
			if ((object)CurrentSlider == sfxSlider)
			{
				MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem");
			}
		}
	}

	protected override void OnDeactivate()
	{
	}

	private void UpdateCursor(bool isInstant)
	{
		Vector2 anchoredPosition = CurrentSlider.transform.parent.GetComponent<RectTransform>().anchoredPosition;
		cursorTransform.DOKill();
		cursorTransform.DOAnchorPos(anchoredPosition, isInstant ? 0f : 0.6f);
	}

	private void FadeHighlight(bool isOn, bool isInstant, Slider target = null)
	{
		if ((object)target == null)
		{
			target = CurrentSlider;
		}
		Image component = target.transform.parent.GetChild(0).GetComponent<Image>();
		component.DOKill();
		component.DOFade(isOn ? 1 : 0, isInstant ? 0f : 0.6f);
	}
}
