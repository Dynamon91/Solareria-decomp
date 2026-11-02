using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Map;

public class MapMarkerBoardIcon : MonoBehaviour
{
	[SerializeField]
	private int markerType;

	[SerializeField]
	private Image iconImage;

	[SerializeField]
	private TextMeshProUGUI numText;

	[SerializeField]
	private Sprite lockedSprite;

	private Sprite defaultSprite;

	private static readonly Color LockedColor = new Color(1f, 1f, 1f, 0.4f);

	private static readonly Color RanOutColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);

	private static readonly Color DefaultColor = Color.white;

	private const float FadeDuration = 1f / 12f;

	public int MarkerType => markerType;

	public void SetData(bool isLocked, bool isRunOut, int num)
	{
		if ((object)defaultSprite == null)
		{
			defaultSprite = iconImage.sprite;
		}
		iconImage.sprite = (isLocked ? lockedSprite : defaultSprite);
		iconImage.color = (isLocked ? LockedColor : (isRunOut ? RanOutColor : DefaultColor));
		SetNum(num);
	}

	public void SetColor(bool isRanOut)
	{
		iconImage.DOColor(isRanOut ? RanOutColor : DefaultColor, 1f / 12f);
	}

	public void SetNum(int num)
	{
		numText.text = ((num < 0) ? string.Empty : num.ToString());
	}
}
