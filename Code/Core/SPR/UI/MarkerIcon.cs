using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI;

public class MarkerIcon : MonoBehaviour
{
	private RectTransform rect;

	private Sequence sequence;

	public bool IsTweenActive { get; private set; }

	private RectTransform RectTransform => rect ?? (rect = GetComponent<RectTransform>());

	public async UniTask ShakeVerticalAsync()
	{
		sequence.Kill(complete: true);
		IsTweenActive = true;
		sequence = RectTransform.DONodYOnce(-10f, 0.25f);
		await sequence.WaitAsync();
		IsTweenActive = false;
	}

	public void SetObjectActive(bool isActive)
	{
		if (isActive)
		{
			base.gameObject.SetActive(value: true);
			return;
		}
		base.gameObject.SetActive(value: false);
		sequence.Kill(complete: true);
		IsTweenActive = false;
	}
}
