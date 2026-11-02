using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SPR.UI.Menu.Status;

public class HeatSkillDetailInfoBoard : MonoBehaviour
{
	[SerializeField]
	private LocaleText nameText;

	[SerializeField]
	private LocaleText summaryText;

	[SerializeField]
	private LocaleText descText;

	[SerializeField]
	private VideoPlayer videoPlayer;

	[SerializeField]
	private RawImage videoScreen;

	private CanvasGroup canvasGroup;

	private Dictionary<HeatSkillType, VideoClip> clips;

	public bool IsActive => CanvasGroup.alpha >= 1f;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public void Initialize()
	{
		CanvasGroup.alpha = 0f;
		base.gameObject.SetActive(value: false);
		CacheVideoClipsAsync().Forget();
	}

	private async UniTask CacheVideoClipsAsync()
	{
		clips = new Dictionary<HeatSkillType, VideoClip>();
		for (int i = 1; i < 5; i++)
		{
			HeatSkillType type = (HeatSkillType)i;
			AsyncOperationHandle<VideoClip> handle = Addressables.LoadAssetAsync<VideoClip>(Address.HeatSkillVideoClip(type));
			await handle.Task;
			clips.Add(type, handle.Result);
		}
	}

	public void SetData(HeatSkillType skillType)
	{
		nameText.SetData($"Heat_{skillType}_Name");
		summaryText.SetData($"Heat_{skillType}_Guide");
		descText.SetData($"Heat_{skillType}_Desc");
		SetVideoAsync(skillType).Forget();
	}

	private async UniTask SetVideoAsync(HeatSkillType skillType)
	{
		videoPlayer.clip = clips[skillType];
		videoPlayer.Prepare();
		await UniTask.WaitUntil(() => videoPlayer.isPrepared);
		videoScreen.texture = videoPlayer.texture;
		videoPlayer.Play();
	}

	public void SetActive(bool value)
	{
		if (value == base.gameObject.activeSelf)
		{
			return;
		}
		if (value)
		{
			CanvasGroup.alpha = 0f;
			base.gameObject.SetActive(value: true);
		}
		Tween t = CanvasGroup.DOAlpha(value ? 1 : 0, 1f / 6f);
		if (!value)
		{
			t.OnComplete(delegate
			{
				base.gameObject.SetActive(value: false);
			});
		}
	}
}
