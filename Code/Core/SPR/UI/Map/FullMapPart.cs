using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Map;

public class FullMapPart : MonoBehaviour
{
	[SerializeField]
	[HideInInspector]
	private FieldSubtitle type;

	[SerializeField]
	private Image fillImage;

	[SerializeField]
	private Image borderImage;

	[SerializeField]
	private float duration = 1f / 6f;

	[SerializeField]
	[HideInInspector]
	private Color fillColor;

	[SerializeField]
	[HideInInspector]
	private Color borderColor;

	private RectTransform rect;

	private static readonly Color FillHighlightColor = Color.white;

	private static readonly Color BorderHighlightColor = Color.white;

	private const float BorderHighlightAlpha = 1f;

	private Sequence seq;

	public FieldSubtitle Type => type;

	public Vector2 WorldConstant => RectTransform.anchoredPosition;

	public Vector2 ConstantLowerLeft => RectTransform.anchoredPosition - RectTransform.sizeDelta / 2f;

	public Vector2 ConstantUpperRight => RectTransform.anchoredPosition + RectTransform.sizeDelta / 2f;

	public RectTransform RectTransform => rect ?? (rect = GetComponent<RectTransform>());

	public void SetActive(bool value)
	{
		base.gameObject.SetActive(value);
	}

	public void SetHighlight(bool value)
	{
		seq?.Kill();
		seq = DOTween.Sequence();
		seq.Join(fillImage.DOColor(value ? FillHighlightColor : fillColor, duration)).Join(borderImage.DOColor(value ? BorderHighlightColor : borderColor, duration));
	}
}
