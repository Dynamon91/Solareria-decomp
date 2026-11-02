using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR.UI.Trait;

public class TraitStatusDetailBoard : MonoBehaviour
{
	private List<TraitStatusDetailElement> stats;

	private readonly List<CardData> currentTraits = new List<CardData>();

	private readonly Dictionary<StatType, float> currentStatsCache = new Dictionary<StatType, float>();

	private int index;

	private Sequence seq;

	private CanvasGroup canvasGroup;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	public void Initialize()
	{
		stats = GetComponentsInChildren<TraitStatusDetailElement>().ToList();
		stats.ForEach(delegate(TraitStatusDetailElement stat)
		{
			stat.Initialize();
		});
	}

	public void OnActivate()
	{
		stats.ForEach(delegate(TraitStatusDetailElement stat)
		{
			stat.ResetData();
			stat.SetCursor(value: false, instant: true);
		});
	}

	public void SetActive(bool value, bool instant = false)
	{
		int num = (value ? 1 : 0);
		if (instant)
		{
			CanvasGroup.alpha = num;
			return;
		}
		seq?.Kill();
		seq = DOTween.Sequence().Join(CanvasGroup.DOAlpha(num, 1f / 6f).SetEase(Ease.Linear));
	}

	public void ShowCurrentDiffs(IReadOnlyDictionary<int, int> diffs = null)
	{
		if (diffs == null)
		{
			DebugLog.GeneralLogError("Trait 변화치를 전달받지 못했습니다!");
			return;
		}
		currentStatsCache.Clear();
		GetCurrentTraits();
		foreach (CardData item in currentTraits.Where((CardData card) => !diffs.ContainsKey(card.ID)))
		{
			foreach (var (key, num) in StatUtility.GetTraitStatIncreasesByLevel(item.ID, item.Rank, item.CurrentLevel))
			{
				if (!currentStatsCache.TryAdd(key, num))
				{
					currentStatsCache[key] += num;
				}
			}
		}
		List<StatType> list = new List<StatType>();
		foreach (CardData item2 in currentTraits.Where((CardData card) => diffs.ContainsKey(card.ID)))
		{
			int iD = item2.ID;
			int valueOrDefault = diffs.GetValueOrDefault(iD, 0);
			foreach (var (statType, num2) in StatUtility.GetTraitStatIncreasesByLevel(iD, item2.Rank, item2.CurrentLevel + valueOrDefault))
			{
				if (!list.Contains(statType))
				{
					list.Add(statType);
				}
				if (!currentStatsCache.TryAdd(statType, num2))
				{
					currentStatsCache[statType] += num2;
				}
			}
		}
		foreach (TraitStatusDetailElement stat in stats)
		{
			StatType type = stat.Type;
			if (currentStatsCache.TryGetValue(type, out var value))
			{
				stat.SetChange(list.Contains(type), value);
			}
			else
			{
				stat.SetChange(changed: false);
			}
		}
	}

	private void GetCurrentTraits()
	{
		currentTraits.Clear();
		for (int i = 1; i <= 8; i++)
		{
			if (PlayerData.TryGetCardData(i, out var cardData))
			{
				currentTraits.Add(cardData);
			}
		}
	}

	public void ShowTargetStats(int id, bool instant = false)
	{
		StatType[] args = (from tuple in StatUtility.GetTraitStatIncreasesByLevel(id, 1, 1)
			select tuple.statType).ToArray();
		foreach (TraitStatusDetailElement stat in stats)
		{
			stat.SetFocus(stat.Type.IsOneOf(args), instant);
		}
	}

	public void HideTargetStats(bool instant = false)
	{
		foreach (TraitStatusDetailElement stat in stats)
		{
			stat.SetFocus(focus: false, instant);
		}
	}

	public StatType SetCursorActive(bool value, bool instant = false)
	{
		if (!value)
		{
			stats[index].SetCursor(value: false, instant);
			return StatType.None;
		}
		stats[index].SetCursor(value: true, instant);
		return MoveCursor(0);
	}

	public StatType MoveCursor(int input)
	{
		int num = index;
		index = Mathf.Clamp(index + input, 0, stats.Count - 1);
		TraitStatusDetailElement traitStatusDetailElement = stats[index];
		if (num != index)
		{
			stats[num].SetCursor(value: false);
			traitStatusDetailElement.SetCursor(value: true);
		}
		return traitStatusDetailElement.Type;
	}
}
