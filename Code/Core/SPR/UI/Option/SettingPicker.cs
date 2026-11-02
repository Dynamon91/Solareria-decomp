using System.Collections.Generic;
using Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Option;

public abstract class SettingPicker : SettingItem
{
	[SerializeField]
	private List<string> optionLocaleKeys;

	[SerializeField]
	protected LocaleText localeText;

	[SerializeField]
	private List<Image> arrowImages;

	[SerializeField]
	private Image textIndicator;

	protected bool Changed;

	protected int Current;

	protected List<Animator> ArrowAnimators;

	private const float IndicatorActiveAlpha = 10f / 51f;

	protected const string ArrowAnimName = "action";

	protected const int AnimDurationFrame = 12;

	protected int FrameCount;

	protected virtual int IndexMax => optionLocaleKeys.Count - 1;

	protected bool CanControl => FrameCount <= 0;

	private void Awake()
	{
		ArrowAnimators = new List<Animator>(2);
		foreach (Image arrowImage in arrowImages)
		{
			ArrowAnimators.Add(arrowImage.gameObject.GetComponent<Animator>());
		}
	}

	private void FixedUpdate()
	{
		if (FrameCount > 0)
		{
			FrameCount--;
		}
	}

	public override void OnFocus(bool value)
	{
		float a = (value ? 1f : 0f);
		arrowImages.ForEach(delegate(Image image)
		{
			image.SetAlpha(a);
		});
		textIndicator.SetAlpha(value ? (10f / 51f) : 0f);
	}

	public override void Control(int horizontal, int vertical)
	{
		if (CanControl && horizontal != 0)
		{
			int current = Current;
			Current = Mathf.Clamp(Current + horizontal, 0, IndexMax);
			localeText.SetData(optionLocaleKeys[Current]);
			Changed = current != Current;
			if (Changed)
			{
				ArrowAnimators[(horizontal > 0) ? 1 : 0].Play("action");
				FrameCount = 12;
				Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
			}
			Save();
		}
	}

	public override void Synchronize()
	{
		OnSynchronize();
		localeText.SetData(optionLocaleKeys[Current]);
		FrameCount = 0;
	}

	protected abstract void OnSynchronize();
}
