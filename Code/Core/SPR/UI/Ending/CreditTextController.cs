using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Ending;

public class CreditTextController : MonoBehaviour
{
	[SerializeField]
	private RectTransform layoutGroup;

	[SerializeField]
	private RectTransform parentGroup;

	[SerializeField]
	private CreditText textPrefab;

	[SerializeField]
	private CreditRecordBoard creditRecordBoard;

	public void SetData()
	{
		for (int i = 0; i < Game.DataManager.CreditDataList.Count; i++)
		{
			CreditData creditData = Game.DataManager.CreditDataList[i];
			bool flag = !string.IsNullOrEmpty(creditData.Content1);
			if (!flag && string.IsNullOrEmpty(creditData.Content2))
			{
				CreditText creditText = Object.Instantiate(textPrefab, parentGroup);
				creditText.gameObject.SetActive(value: true);
				creditText.SetData(string.Empty, string.Empty);
			}
			else if (flag)
			{
				CreditText creditText2 = Object.Instantiate(textPrefab, parentGroup);
				creditText2.gameObject.SetActive(value: true);
				creditText2.SetData(creditData.Content1, creditData.Content2);
			}
		}
		creditRecordBoard.SetData();
		LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup);
	}

	public async UniTask PlayText(float speed)
	{
		await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
		Game.Instance.SoundManager.PlayBGM("Credit");
		float y = layoutGroup.sizeDelta.y;
		await layoutGroup.DOAnchorPosY(y, speed).SetSpeedBased().SetEase(Ease.Linear)
			.WaitAsync();
		await creditRecordBoard.ActivateAsync();
	}
}
