using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class EvadeBarController : MonoBehaviour
{
	[Serializable]
	private class SingleEvadeBar
	{
		[SerializeField]
		private Image singleEvadeBarImage;

		public void Initialize()
		{
			singleEvadeBarImage.fillAmount = 0f;
		}

		public void UpdateEvadeBar(int currentEvadePoint, int maxEvadePoint)
		{
			singleEvadeBarImage.fillAmount = (float)currentEvadePoint / (float)maxEvadePoint;
		}
	}

	[SerializeField]
	private List<SingleEvadeBar> evadeBarList;

	public void Initialize()
	{
		evadeBarList.ForEach(delegate(SingleEvadeBar bar)
		{
			bar.Initialize();
		});
	}

	public void UpdateEvadeBar(int currentEvadePoint, int skillCost)
	{
		int num = currentEvadePoint / skillCost;
		int currentEvadePoint2 = currentEvadePoint % skillCost;
		for (int i = 0; i < evadeBarList.Count; i++)
		{
			SingleEvadeBar singleEvadeBar = evadeBarList[i];
			if (i < num)
			{
				singleEvadeBar.UpdateEvadeBar(skillCost, skillCost);
			}
			else if (i == num)
			{
				singleEvadeBar.UpdateEvadeBar(currentEvadePoint2, skillCost);
			}
			else
			{
				singleEvadeBar.UpdateEvadeBar(0, skillCost);
			}
		}
	}
}
