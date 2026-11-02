using SPR.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Title;

public class TitleImage : MonoBehaviour
{
	[SerializeField]
	private TitleSpriteAsset spriteAsset;

	private Image image;

	private Animator animator;

	private Image Image => image ?? (image = GetComponent<Image>());

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	public void SetData()
	{
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < 9; i++)
		{
			SaveData saveData = Game.Instance.GetSaveData(i);
			flag2 = flag2 || saveData.TalkData.GetTalkCount("EndingCutscene_Thank_Real") > 0;
			flag = flag || saveData.TalkData.GetTalkCount("EndingCutscene_Thank_Normal") > 0;
		}
		if (flag2 || flag)
		{
			Image.sprite = (flag2 ? spriteAsset.trueEndingSprite : spriteAsset.normalEndingSprite);
			Image.color = Color.white;
			Animator.enabled = false;
			for (int j = 0; j < base.transform.childCount - 1; j++)
			{
				base.transform.GetChild(j).gameObject.SetActive(value: false);
			}
		}
	}
}
