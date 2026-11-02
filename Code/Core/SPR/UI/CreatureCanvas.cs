using System;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI;

public class CreatureCanvas : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	private EffectSlider hpSlider;

	[SerializeField]
	private StunPointGroup stunPointGroup;

	private Sequence hideSequence;

	private RectTransform rectTransform;

	private const float ScaleFactor = 0.01f;

	private RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public void Initialize(float creatureHeight, int stunCount)
	{
		RectTransform.anchoredPosition = Vector2.up * (creatureHeight + 0.5f);
		SetActive(value: false);
		hpSlider.Initialize();
		stunPointGroup.SetCount(StunPointGroup.Type.Creature, stunCount).Forget();
	}

	public void Initialize(Vector2 position, float creatureHeight, int stunCount)
	{
		RectTransform.anchoredPosition = position + Vector2.up * (creatureHeight + 0.5f);
		SetActive(value: false);
		hpSlider.Initialize();
		stunPointGroup.SetCount(StunPointGroup.Type.Creature, stunCount).Forget();
	}

	public void UpdateHealthBar(int current, int max, int delta)
	{
		hideSequence?.Kill();
		hpSlider.gameObject.SetActive(current != max);
		hpSlider.UpdateEnemySlider(current, max, delta);
		SetActive(value: true);
		hideSequence = DOTween.Sequence().AppendInterval(8f).OnComplete(delegate
		{
			SetActive(value: false);
		});
	}

	public void SetActive(bool value)
	{
		hpSlider.gameObject.SetActive(value);
		stunPointGroup.gameObject.SetActive(value);
	}

	public void UpdateStunPoint(int value)
	{
		stunPointGroup.UpdateCount(value);
		SetActive(value: true);
	}

	public void ResetStunPoint()
	{
		stunPointGroup.ResetCount();
	}

	private void LateUpdate()
	{
		Vector3 localScale = base.transform.localScale;
		if (base.transform.lossyScale.x < 0f)
		{
			localScale.x = 0.01f * (float)(-localScale.x.GetSign());
		}
		base.transform.localScale = localScale;
	}

	public void PlayHealTween(bool loop, Type subscriberType)
	{
		hpSlider.PlayHealTween(loop, subscriberType);
	}

	public void StopHealTween(Type subscriberType)
	{
		hpSlider.StopHealTween(subscriberType);
	}
}
