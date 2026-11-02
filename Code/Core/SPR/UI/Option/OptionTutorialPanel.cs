using System.Collections.Generic;
using System.Linq;
using SPR.UI.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Option;

public class OptionTutorialPanel : OptionPanel
{
	[SerializeField]
	private ScrollBoard scrollBoard;

	[SerializeField]
	private OptionTutorialBoard tutorialBoard;

	[SerializeField]
	private RectTransform slotParent;

	private List<TutorialDataCell> allCells;

	private int currentIndex;

	private float spacing;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public override string ControllerName()
	{
		return "Option Tutorial Panel";
	}

	public override void Initialize()
	{
		tutorialBoard.Initialize();
		spacing = slotParent.GetComponent<VerticalLayoutGroup>().spacing;
		base.Initialize();
	}

	protected override void OnActivate()
	{
		CreateCells();
		currentIndex = 0;
		FocusCell(currentIndex, instant: true);
		SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Confirm");
	}

	protected override void OnDeactivate()
	{
		ReturnCells();
		SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Confirm");
	}

	protected override void OnControl()
	{
		int verticalDownUI = OptionPanel.InputManager.verticalDownUI;
		int horizontalDownUI = OptionPanel.InputManager.horizontalDownUI;
		if (verticalDownUI != 0)
		{
			int num = allCells.Count - 1;
			int num2 = currentIndex - verticalDownUI;
			if (num2 < 0 || num2 > num)
			{
				return;
			}
			currentIndex = Mathf.Clamp(num2, 0, num);
			FocusCell(currentIndex);
		}
		if (horizontalDownUI == 0)
		{
			return;
		}
		ActionName currentActionName = allCells[currentIndex].ActionName;
		(ActionName, ActionName) tuple = ActionNameUtil.ActionNameHasPage.FirstOrDefault(((ActionName former, ActionName latter) tuple3) => currentActionName == tuple3.former);
		(ActionName, ActionName) tuple2 = tuple;
		if (tuple2.Item1 == ActionName.None && tuple2.Item2 == ActionName.None)
		{
			return;
		}
		int num3 = horizontalDownUI;
		if (num3 <= 0)
		{
			if (num3 < 0 && tutorialBoard.CurrentActionName != tuple.Item1)
			{
				tutorialBoard.SetData(tuple.Item1);
				ActivatePageKeyGuide(active: true, isFormer: true);
				SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Sub_Move");
			}
		}
		else if (tutorialBoard.CurrentActionName != tuple.Item2)
		{
			tutorialBoard.SetData(tuple.Item2);
			ActivatePageKeyGuide(active: true, isFormer: false);
			SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Sub_Move");
		}
	}

	private void FocusCell(int index, bool instant = false)
	{
		for (int i = 0; i < allCells.Count; i++)
		{
			allCells[i].Focus(i == index);
		}
		TutorialDataCell tutorialDataCell = allCells[index];
		scrollBoard.ScrollIfTargetIsOutOfViewport(tutorialDataCell.RectTransform, tutorialDataCell.RectTransform.sizeDelta.y + spacing, instant);
		tutorialBoard.SetData(tutorialDataCell.ActionName);
		ActionName currentActionName = allCells[currentIndex].ActionName;
		bool active = tutorialDataCell.IsActive && ActionNameUtil.ActionNameHasPage.Any(((ActionName former, ActionName latter) tuple) => currentActionName == tuple.former);
		ActivatePageKeyGuide(active, isFormer: true);
		if (!instant)
		{
			SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Sub_Move");
		}
	}

	private void CreateCells()
	{
		if (allCells == null)
		{
			allCells = new List<TutorialDataCell>();
		}
		foreach (ActionName actionName in ActionNameUtil.ActionNamesPopUp)
		{
			if (!ActionNameUtil.ActionNameHasPage.Any(((ActionName former, ActionName latter) pair) => pair.latter == actionName))
			{
				TutorialDataCell tutorialDataCell = Game.Instance.ObjectPoolManager.GetObject<TutorialDataCell>("Tutorial Data Cell.prefab", slotParent);
				tutorialDataCell.transform.localScale = Vector3.one;
				tutorialDataCell.SetData(actionName);
				allCells.Add(tutorialDataCell);
			}
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(slotParent);
	}

	private void ReturnCells()
	{
		allCells.ForEach(delegate(TutorialDataCell c)
		{
			c.ReturnToPool();
		});
		allCells.Clear();
	}

	private void ActivatePageKeyGuide(bool active, bool isFormer)
	{
		Transform child = base.transform.GetChild(base.transform.childCount - 1);
		if (!active)
		{
			child.GetChild(1).gameObject.SetActive(value: false);
			child.GetChild(2).gameObject.SetActive(value: false);
			return;
		}
		int index = ((!isFormer) ? 1 : 2);
		int index2 = (isFormer ? 1 : 2);
		child.GetChild(index).gameObject.SetActive(value: true);
		child.GetChild(index2).gameObject.SetActive(value: false);
	}
}
