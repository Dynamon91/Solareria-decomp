using Cysharp.Threading.Tasks;
using Library;
using Rewired;
using SPR.L10N;
using SPR.Library;
using SPR.UI.Tutorial;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SPR.UI.Option;

public class OptionTutorialBoard : MonoBehaviour, IControllerEventUpdate
{
	[SerializeField]
	private RawImage videoScreen;

	[SerializeField]
	private VideoPlayer videoPlayer;

	[SerializeField]
	private LocaleText nameText;

	[SerializeField]
	private LocaleText actionDescText;

	[SerializeField]
	private LocaleText actionKeyGuide;

	[SerializeField]
	private GameObject burstIcon;

	public ActionName CurrentActionName { get; private set; }

	public void Initialize()
	{
		nameText.gameObject.SetActive(value: false);
		actionDescText.gameObject.SetActive(value: false);
		videoPlayer.clip = null;
		actionKeyGuide.gameObject.SetActive(value: false);
		burstIcon.SetActive(value: false);
		videoScreen.color = Color.white;
		RewiredHelper.RegisterControllerChangeEvent(this);
	}

	public void SetData(ActionName actionName)
	{
		CurrentActionName = actionName;
		if (Game.Instance.SaveData.WanderingData.ShouldShowTutorial(actionName))
		{
			nameText.gameObject.SetActive(value: false);
			actionDescText.gameObject.SetActive(value: false);
			videoPlayer.clip = null;
			actionKeyGuide.gameObject.SetActive(value: false);
			burstIcon.SetActive(value: false);
			videoScreen.color = Color.black;
		}
		else
		{
			nameText.gameObject.SetActive(value: true);
			actionDescText.gameObject.SetActive(value: true);
			nameText.SetData(LocaleKey.TutorialName(actionName));
			actionDescText.SetData(LocaleKey.TutorialActionDesc(actionName));
			bool active = ActionNameUtil.ActionNameHasKeyGuide.Contains(actionName);
			actionKeyGuide.gameObject.SetActive(active);
			SetTutorialKeyGuide();
			burstIcon.SetActive(actionName == ActionName.Execution);
			SetTutorialGraphicAsync(actionName).Forget();
		}
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		SetTutorialKeyGuide();
	}

	private void SetTutorialKeyGuide()
	{
		if (ActionNameUtil.ActionNameHasKeyGuide.Contains(CurrentActionName))
		{
			ActionName actionName = ((CurrentActionName == ActionName.Parry) ? ActionName.Guard : CurrentActionName);
			actionKeyGuide.SetData($"Tutorial_{actionName}_ActionName");
		}
	}

	private async UniTask SetTutorialGraphicAsync(ActionName actionName)
	{
		videoScreen.texture = null;
		videoScreen.color = Color.white;
		videoScreen.SetAlpha(0f);
		if (ActionNameUtil.ActionNameShowsImage.Contains(actionName))
		{
			await SetTextureAsync(actionName);
		}
		else
		{
			await SetVideoAsync(actionName);
		}
	}

	private async UniTask SetVideoAsync(ActionName actionName)
	{
		AsyncOperationHandle<VideoClip> task = Addressables.LoadAssetAsync<VideoClip>(Address.TutorialExample(actionName));
		await task;
		if ((object)task.Result == null)
		{
			task = Addressables.LoadAssetAsync<VideoClip>(Address.TutorialExample(actionName, Language.English));
			await task;
		}
		videoPlayer.clip = task.Result;
		await UniTask.WaitUntil(() => videoPlayer.isActiveAndEnabled);
		videoPlayer.Prepare();
		await UniTask.WaitUntil(() => videoPlayer.isPrepared);
		videoScreen.texture = videoPlayer.texture;
		videoPlayer.Play();
		videoScreen.SetAlpha(1f);
	}

	private async UniTask SetTextureAsync(ActionName actionName)
	{
		AsyncOperationHandle<Sprite> task = Addressables.LoadAssetAsync<Sprite>(Address.TutorialExample(actionName));
		await task;
		if ((object)task.Result == null)
		{
			task = Addressables.LoadAssetAsync<Sprite>(Address.TutorialExample(actionName, Language.English));
			await task;
		}
		Sprite result = task.Result;
		videoScreen.texture = result.texture;
		videoScreen.SetAlpha(1f);
	}
}
