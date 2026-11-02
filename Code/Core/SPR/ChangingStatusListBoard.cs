using System.Collections.Generic;
using UnityEngine;

namespace SPR;

public class ChangingStatusListBoard : MonoBehaviour
{
	private List<ChangingStatusInfoBoard> statusInfoBoards;

	public void SetData(List<ChangingStatusInfoBoard.StatusChangeInfo> changeInfos)
	{
		if (statusInfoBoards == null)
		{
			statusInfoBoards = new List<ChangingStatusInfoBoard>(base.transform.childCount);
			statusInfoBoards.AddRange(GetComponentsInChildren<ChangingStatusInfoBoard>());
		}
		for (int i = 0; i < changeInfos.Count; i++)
		{
			ChangingStatusInfoBoard.StatusChangeInfo data = changeInfos[i];
			ChangingStatusInfoBoard changingStatusInfoBoard = statusInfoBoards[i];
			changingStatusInfoBoard.SetData(data);
			changingStatusInfoBoard.gameObject.SetActive(value: true);
		}
		for (int j = changeInfos.Count; j < statusInfoBoards.Count && statusInfoBoards.Count >= j - 1; j++)
		{
			statusInfoBoards[j].gameObject.SetActive(value: false);
		}
	}
}
