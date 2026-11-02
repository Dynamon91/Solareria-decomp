using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI;

public class Cursor : MonoBehaviour
{
	[SerializeField]
	private float moveDuration = 0.2f;

	[Header("Shake")]
	[SerializeField]
	private float shakeDuration = 0.4f;

	[SerializeField]
	private float shakeStrength = 11f;

	[SerializeField]
	private int shakeVibrato = 12;

	private RectTransform rectTransform;

	private RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public Vector2 LocalPosition => RectTransform.localPosition;

	public void Initialize(bool active, Vector2 position)
	{
		RectTransform.localPosition = position;
		base.gameObject.SetActive(active);
	}

	public void Activate(RectTransform targetRect)
	{
		base.gameObject.SetActive(value: true);
		MoveAsync(targetRect, instant: true).Forget();
	}

	public void Deactivate()
	{
		base.gameObject.SetActive(value: false);
	}

	public async UniTask MoveAsync(Vector2 position)
	{
		if ((position - (Vector2)RectTransform.localPosition).magnitude < 0.125f)
		{
			await DOTweenModuleUI.DOShakeAnchorPos(strength: new Vector2(shakeStrength, 0f), target: RectTransform, duration: shakeDuration, vibrato: shakeVibrato, randomness: 0f).WaitAsync();
			return;
		}
		RectTransform.localPosition = position;
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(10);
	}

	public async UniTask MoveAsync(RectTransform targetRect, bool instant = false)
	{
		RectTransform.DOKill();
		Transform parent = base.transform.parent;
		Vector2 anchoredPosition = RectTransform.anchoredPosition;
		base.transform.SetParent(targetRect);
		RectTransform.anchoredPosition = Vector2.zero;
		base.transform.SetParent(parent);
		if (!instant)
		{
			Vector2 anchoredPosition2 = RectTransform.anchoredPosition;
			RectTransform.anchoredPosition = anchoredPosition;
			await RectTransform.DOAnchorPos(anchoredPosition2, moveDuration).WaitAsync();
		}
	}
}
