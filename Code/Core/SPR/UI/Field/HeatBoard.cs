using System.Collections.Generic;
using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI.Field;

public class HeatBoard : MonoBehaviour
{
	[SerializeField]
	private List<HeatLevel> heatLevels;

	[Header("연출")]
	[SerializeField]
	private UIParticle openParticle;

	[SerializeField]
	private UIParticle skillParticle;

	[SerializeField]
	private UIParticle lastEmberParticle;

	private int unlockLevel;

	private float targetGauge;

	public void Initialize(int activeLevel, float gaugeValue)
	{
		UpdateData(activeLevel, gaugeValue);
		targetGauge = 0f;
		UpdateGauges(forced: true);
	}

	public void UpdateData(int activeLevel, float gaugeValue)
	{
		unlockLevel = activeLevel;
		if (gaugeValue < targetGauge)
		{
			skillParticle.Play();
		}
		targetGauge = gaugeValue;
		UpdateGauges();
	}

	private void UpdateGauges(bool forced = false)
	{
		int num = 0;
		for (int i = 0; i < heatLevels.Count; i++)
		{
			HeatLevel heatLevel = heatLevels[i];
			heatLevel.SetActive(i < unlockLevel);
			int maxGaugeOfLevel = PlayerHeat.GetMaxGaugeOfLevel(i + 1);
			float amount = Mathf.Clamp01((targetGauge - (float)num) / (float)maxGaugeOfLevel);
			heatLevel.UpdateHeatGauge(amount, forced);
			num += maxGaugeOfLevel;
		}
	}

	public async UniTask PlayLevelOpenSequenceAsync(int openLevel)
	{
		openParticle.Play();
		Sequence sequence = DOTween.Sequence();
		sequence.Append(heatLevels[PlayerStat.HeatMaxLevel].GetOpenSequence());
		for (int i = PlayerStat.HeatMaxLevel + 1; i < openLevel; i++)
		{
			sequence.Join(heatLevels[i].GetOpenSequence());
		}
		sequence.Play();
		await sequence.WaitAsync();
		PlayerStat playerStat = Game.Instance.Player.PlayerStat;
		playerStat.UnlockHeatLevel(openLevel);
		playerStat.ResetHeatGauge(isFulled: true);
	}

	public void PlayLastEmberParticle()
	{
		lastEmberParticle.Play();
	}

	public void PlayHeatLevelLastEmberSequence()
	{
		heatLevels.ForEach(delegate(HeatLevel level)
		{
			level.PlayLastEmberSequence();
		});
	}

	public void ResetLastEmberEffect()
	{
		heatLevels.ForEach(delegate(HeatLevel level)
		{
			level.ResetImages();
		});
	}
}
