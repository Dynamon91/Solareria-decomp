using DG.Tweening;
using UnityEngine;

namespace SPR;

public class WaterDragonFlood : MonoBehaviour
{
	public void SetHeight(float height, int duration, bool isActive)
	{
		if (isActive)
		{
			base.gameObject.SetActive(value: true);
		}
		DOTween.Sequence().Append(base.transform.DOScaleY(height, Util.FrameToSeconds(duration)).SetEase(Ease.Linear)).Join(base.transform.DOLocalMoveY(height / 2f, Util.FrameToSeconds(duration)).SetEase(Ease.Linear))
			.OnComplete(delegate
			{
				if (!isActive)
				{
					base.gameObject.SetActive(value: false);
				}
			});
	}
}
