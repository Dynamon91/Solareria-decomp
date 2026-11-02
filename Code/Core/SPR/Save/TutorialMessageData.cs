using System;
using SPR.UI.Tutorial;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class TutorialMessageData
{
	[SerializeField]
	private ActionName action;

	[SerializeField]
	private TutorialState state;

	[SerializeField]
	private int clearNum;

	public ActionName Action => action;

	public TutorialState State => state;

	public int ClearNum => clearNum;

	public TutorialMessageData(ActionName actionName, TutorialState tutorialState)
	{
		action = actionName;
		state = tutorialState;
		clearNum = 0;
	}

	public void SetState(TutorialState tutorialState)
	{
		state = tutorialState;
		if (state == TutorialState.Done)
		{
			clearNum++;
			if (clearNum < ActionNameUtil.GetClearNum(action))
			{
				state = TutorialState.None;
			}
		}
	}
}
