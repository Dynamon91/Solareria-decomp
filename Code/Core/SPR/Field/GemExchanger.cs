using DG.Tweening;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR.Field;

public class GemExchanger : ServiceObject
{
	[Header("Gem Exchanger")]
	[SerializeField]
	private SpriteRenderer gemLight;

	[SerializeField]
	private int lightOnDistance;

	private Sequence lightSequence;

	private bool isLightOn;

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		SetLightOn(isOn: false, force: true);
	}

	protected override void Update()
	{
		float num = Vector2.Distance(ServiceObject.PlayerMover.VisualCenter, VisualCenter);
		SetLightOn(num < (float)lightOnDistance);
	}

	private void SetLightOn(bool isOn, bool force = false)
	{
		if (isOn != isLightOn || force)
		{
			isLightOn = isOn;
			lightSequence?.Kill();
			lightSequence = DOTween.Sequence();
			float endValue = (isOn ? 20f : 8f) / 255f;
			float endValue2 = (isOn ? 6f : 3f);
			lightSequence.Append(gemLight.DOAlpha(endValue, 1f)).Join(gemLight.transform.DOScale(endValue2, 1f));
		}
	}
}
