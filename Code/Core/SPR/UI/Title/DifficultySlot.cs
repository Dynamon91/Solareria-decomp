using SPR.L10N;
using StaticData;
using UnityEngine;

namespace SPR.UI.Title;

public class DifficultySlot : MonoBehaviour
{
	[SerializeField]
	private Difficulty difficulty;

	[SerializeField]
	private GameObject cursor;

	[SerializeField]
	private LocaleText nameLocaleText;

	[SerializeField]
	private LocaleText descLocaleText;

	private CanvasGroup canvasGroup;

	public Difficulty Difficulty => difficulty;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public void SetData()
	{
		nameLocaleText.SetData(LocaleKey.DifficultyName(difficulty));
		descLocaleText.SetData(LocaleKey.DifficultyDesc(difficulty));
	}

	public void Focus(bool value)
	{
		CanvasGroup.alpha = (value ? 1f : 0.5f);
		cursor.SetActive(value);
	}
}
