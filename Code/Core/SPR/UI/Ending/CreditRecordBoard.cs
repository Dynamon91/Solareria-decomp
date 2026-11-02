using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Achievement;
using SPR.L10N;
using SPR.UI.Map;
using TMPro;
using UnityEngine;

namespace SPR.UI.Ending;

public class CreditRecordBoard : MonoBehaviour
{
	[SerializeField]
	private LocaleText difficultyNameText;

	[SerializeField]
	private LocaleText difficultyContentText;

	[SerializeField]
	private LocaleText collectionNameText;

	[SerializeField]
	private TextMeshProUGUI collectionContentText;

	[SerializeField]
	private LocaleText playTimeNameText;

	[SerializeField]
	private TextMeshProUGUI playTimeContentText;

	[SerializeField]
	private float waitDuration = 1f;

	[SerializeField]
	private float fadeDuration = 0.75f;

	private List<CanvasGroup> canvasGroups;

	private bool hasAllCollection;

	public void SetData()
	{
		difficultyNameText.SetData("UI_MenuCategory_Difficulty");
		difficultyContentText.SetData(LocaleKey.DifficultyName(Game.Instance.SaveData.Difficulty));
		int total;
		int current;
		int globalCollection = MapCollection.GetGlobalCollection(Game.Instance.SaveData, out total, out current);
		collectionNameText.SetData("UI_GlobalCollection_Name");
		collectionContentText.text = $"{globalCollection}%";
		hasAllCollection = total == current;
		playTimeNameText.SetData("Credit_PlayTime_Name");
		TimeSpan timeSpan = TimeSpan.FromSeconds(Game.Instance.SaveData.GetCurrentPlayTime());
		playTimeContentText.text = $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
		canvasGroups = new List<CanvasGroup>(3)
		{
			difficultyNameText.GetComponent<CanvasGroup>(),
			collectionNameText.GetComponent<CanvasGroup>(),
			playTimeNameText.GetComponent<CanvasGroup>()
		};
		canvasGroups.ForEach(delegate(CanvasGroup group)
		{
			group.alpha = 0f;
		});
	}

	public async UniTask ActivateAsync()
	{
		for (int i = 0; i < canvasGroups.Count; i++)
		{
			await UniTask.WaitForSeconds(waitDuration);
			await canvasGroups[i].DOAlpha(1f, fadeDuration).WaitAsync();
		}
		if (hasAllCollection)
		{
			AchievementHelper.Unlock("AllClear");
		}
	}
}
