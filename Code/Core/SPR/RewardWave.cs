using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using UnityEngine;

namespace SPR;

public class RewardWave : EnemyWave
{
	[Space]
	[SerializeField]
	private int moneyAmount;

	private readonly List<GameObject> rewards = new List<GameObject>();

	private void OnDrawGizmosSelected()
	{
		Vector2 vector = base.transform.position;
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(vector, Vector3.one);
	}

	private void OnValidate()
	{
		base.gameObject.name = "Reward Wave";
	}

	protected override void OnInitialize()
	{
		rewards.Clear();
		for (int i = 0; i < base.transform.childCount; i++)
		{
			rewards.Add(base.transform.GetChild(i).gameObject);
		}
		UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate).GetAwaiter().OnCompleted(delegate
		{
			rewards.ForEach(delegate(GameObject item)
			{
				item.SetActive(value: false);
			});
		});
	}

	protected override void OnActivate()
	{
		rewards.ForEach(delegate(GameObject item)
		{
			item.SetActive(value: true);
		});
		SpreadMoney();
	}

	private void SpreadMoney()
	{
		if (moneyAmount == 0)
		{
			return;
		}
		int minInclusive = Mathf.RoundToInt((float)moneyAmount * 0.7f);
		int num = Mathf.RoundToInt((float)moneyAmount * 1.3f);
		List<int> randomAmounts = Money.GetRandomAmounts(CryptoRandom.Range(minInclusive, num + 1));
		Vector2 vector = base.transform.position;
		foreach (int item in randomAmounts)
		{
			Money money = Game.Instance.ObjectPoolManager.GetObject<Money>("Money.prefab", vector);
			money.Initialize(item);
			money.gameObject.SetActive(value: true);
		}
	}
}
