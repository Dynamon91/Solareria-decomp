using System.Collections.Generic;
using Library;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Option;

public abstract class SettingSlider : SettingItem
{
	[SerializeField]
	private RectTransform barRectTransform;

	[SerializeField]
	private TextMeshProUGUI percentText;

	[SerializeField]
	private List<Image> arrowImages;

	private const string ArrowAnimName = "action";

	private const int AnimDurationFrame = 12;

	private float totalWidth;

	private int frameCount;

	protected bool Changed;

	private Image barEndImage;

	private List<Animator> arrowAnimators;

	protected float BarValue_0_10
	{
		get
		{
			return barRectTransform.rect.width / TotalWidth * 10f;
		}
		set
		{
			Vector2 sizeDelta = barRectTransform.sizeDelta;
			sizeDelta.x = value / 10f * TotalWidth;
			barRectTransform.sizeDelta = sizeDelta;
		}
	}

	private float TotalWidth
	{
		get
		{
			if (totalWidth != 0f)
			{
				return totalWidth;
			}
			return barRectTransform.parent.GetComponent<RectTransform>().sizeDelta.x;
		}
	}

	private bool CanControl => frameCount <= 0;

	private void Awake()
	{
		barEndImage = barRectTransform.GetChild(0).GetComponent<Image>();
		arrowAnimators = new List<Animator>(2);
		foreach (Image arrowImage in arrowImages)
		{
			arrowAnimators.Add(arrowImage.gameObject.GetComponent<Animator>());
		}
	}

	private void FixedUpdate()
	{
		if (frameCount > 0)
		{
			frameCount--;
		}
	}

	public override void OnFocus(bool value)
	{
		float a = (value ? 1f : 0f);
		arrowImages.ForEach(delegate(Image image)
		{
			image.SetAlpha(a);
		});
		barEndImage.SetAlpha(a);
	}

	public override void Control(int horizontal, int vertical)
	{
		if (CanControl && horizontal != 0)
		{
			if (horizontal < 0 && BarValue_0_10 > 0f)
			{
				Changed = true;
				BarValue_0_10 -= 1f;
			}
			else if (horizontal > 0 && BarValue_0_10 < 10f)
			{
				Changed = true;
				BarValue_0_10 += 1f;
			}
			if (Changed)
			{
				arrowAnimators[(horizontal > 0) ? 1 : 0].Play("action");
				frameCount = 12;
			}
			SetPercentText();
			Save();
		}
	}

	public override void Synchronize()
	{
		SetPercentText();
		frameCount = 0;
	}

	private void SetPercentText()
	{
		percentText.text = $"{Mathf.RoundToInt(BarValue_0_10 * 10f)}%";
	}
}
