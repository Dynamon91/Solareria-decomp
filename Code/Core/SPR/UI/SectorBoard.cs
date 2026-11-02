using System;
using System.Collections.Generic;
using DG.Tweening;
using Library;
using SPR.L10N;
using SPR.Library;
using SPR.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class SectorBoard : MonoBehaviour
{
	private enum State
	{
		None,
		Checking,
		CheckingSpecial
	}

	[SerializeField]
	private LocaleText titleText;

	[SerializeField]
	private LocaleText descriptionText;

	[SerializeField]
	private float fadeInDuration;

	[SerializeField]
	private float sustainDuration;

	[SerializeField]
	private float fadeOutDuration;

	private RectTransform titleLayout;

	private CanvasGroup canvasGroup;

	private Sequence seq;

	private State state;

	private SectorName sectorName;

	private Func<Vector2, bool> currentCondition;

	private Timer delayTimer;

	private static readonly List<FieldID> SpecialSectors = new List<FieldID>
	{
		new FieldID(SectorName.TerradinGarden, 30),
		new FieldID(SectorName.WindshadeSpire, 16)
	};

	private static PlayerMover Player => Game.Instance.Player.mover;

	private static FieldID CurrentFieldID => Game.Instance.CurrentFieldID;

	private static WorldData WorldData => Game.Instance.SaveData.WorldData;

	public void Initialize()
	{
		if ((object)titleLayout == null)
		{
			titleLayout = titleText.transform.parent.GetComponent<RectTransform>();
		}
		if ((object)canvasGroup == null)
		{
			canvasGroup = GetComponent<CanvasGroup>();
		}
		canvasGroup.alpha = 0f;
		base.gameObject.SetActive(value: true);
	}

	public void OnSceneLoaded()
	{
		ResetCheck();
	}

	public void TryAppear(SectorName sector, Func<Vector2, bool> condition, bool isForced = false)
	{
		if (TrySpecialSector(condition))
		{
			return;
		}
		if (!isForced)
		{
			if (WorldData.HasShownSector(sector))
			{
				return;
			}
			if (CurrentFieldID.Equals(SectorName.SilentRuins, 0))
			{
				WorldData.AddShownSector(sector);
				return;
			}
		}
		sectorName = sector;
		currentCondition = condition;
		state = State.Checking;
	}

	private void Update()
	{
		if (state == State.None)
		{
			return;
		}
		if (Game.Instance.UIManager.TalkCanvas.IsNarrating)
		{
			if (delayTimer == null)
			{
				delayTimer = new Timer(1f);
			}
			return;
		}
		if (delayTimer != null && !delayTimer.IsDone)
		{
			delayTimer.Tick(Time.deltaTime);
			return;
		}
		delayTimer = null;
		if (currentCondition(Player.VisualBottom))
		{
			switch (state)
			{
			case State.Checking:
				WorldData.AddShownSector(CurrentFieldID.SectorName);
				break;
			case State.CheckingSpecial:
				WorldData.AddShownSpecialSector(CurrentFieldID);
				break;
			}
			SetData();
			ResetCheck();
		}
	}

	private void ResetCheck()
	{
		state = State.None;
		currentCondition = null;
	}

	private void SetData()
	{
		(string, string) tuple = state switch
		{
			State.Checking => (LocaleKey.SectorName(sectorName), LocaleKey.SectorName(sectorName, desc: true)), 
			State.CheckingSpecial => ($"Sector_Special_{CurrentFieldID}", $"Sector_Special_{CurrentFieldID}_Desc"), 
			_ => ("", ""), 
		};
		titleText.SetData(tuple.Item1);
		if (DataManager.Instance.IsLocaleExist(tuple.Item2))
		{
			descriptionText.SetData(tuple.Item2);
		}
		else
		{
			descriptionText.ClearText();
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(titleLayout);
		seq?.Kill();
		seq = DOTween.Sequence();
		seq.Append(canvasGroup.DOAlpha(1f, fadeInDuration)).AppendInterval(sustainDuration).Append(canvasGroup.DOAlpha(0f, fadeOutDuration));
	}

	private bool TrySpecialSector(Func<Vector2, bool> condition)
	{
		if (!SpecialSectors.Contains(CurrentFieldID))
		{
			return false;
		}
		if (WorldData.HasShownSpecialSector(CurrentFieldID))
		{
			return false;
		}
		state = State.CheckingSpecial;
		currentCondition = condition;
		return true;
	}
}
