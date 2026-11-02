using Cysharp.Threading.Tasks;
using DG.Tweening;
using Rewired;
using SPR.L10N;
using SPR.Library;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SPR.UI.Trait;

public class TraitSkillInfoBoard : TraitInfoBoard, IControllerEventUpdate
{
	[SerializeField]
	private RawImage videoScreen;

	[SerializeField]
	private VideoPlayer videoPlayer;

	[SerializeField]
	private Image unacquiredImage;

	[SerializeField]
	private LocaleText nameText;

	[SerializeField]
	private LocaleText keyText;

	[SerializeField]
	private LocaleText descText;

	private Vector2 initialPos;

	private readonly Vector2 posDelta = new Vector2(-20f, 0f);

	private Texture defaultTexture;

	private string inputGuideKey;

	public override void Initialize()
	{
		initialPos = base.RectTransform.anchoredPosition;
		base.RectTransform.anchoredPosition = initialPos + posDelta;
		defaultTexture = videoScreen.texture;
		RewiredHelper.RegisterControllerChangeEvent(this);
	}

	public override void SetInfo(TraitCardElement card, bool isRest, int point)
	{
		string text = Game.DataManager.GetTraitData(card.ID).Name;
		if (((TraitSkillCardElement)card).Acquired)
		{
			videoScreen.gameObject.SetActive(value: true);
			SetVideoAsync(text).Forget();
			inputGuideKey = LocaleKey.SkillKey(text);
			SetKeyText();
			unacquiredImage.gameObject.SetActive(value: false);
		}
		else
		{
			videoScreen.gameObject.SetActive(value: false);
			keyText.ClearText();
			unacquiredImage.sprite = AddressableHelper.GetAsset<Sprite>("CoreStone", text);
			unacquiredImage.gameObject.SetActive(value: true);
		}
		nameText.SetData(LocaleKey.ItemName(text));
		descText.SetData(LocaleKey.ItemDesc(text));
	}

	protected override void OnSetActive(bool value, bool instant)
	{
		base.OnSetActive(value, instant);
		Vector2 vector = (value ? initialPos : (initialPos + posDelta));
		if (instant)
		{
			base.RectTransform.anchoredPosition = vector;
		}
		else
		{
			FadeSeq.Join(base.RectTransform.DOAnchorPos(vector, 1f / 6f).SetEase(Ease.Linear));
		}
	}

	private async UniTask SetVideoAsync(string skillName)
	{
		videoScreen.texture = defaultTexture;
		AsyncOperationHandle<VideoClip> task = Addressables.LoadAssetAsync<VideoClip>(Address.TraitSprite(skillName));
		await task;
		videoPlayer.clip = task.Result;
		videoPlayer.Prepare();
		await UniTask.WaitUntil(() => videoPlayer.isPrepared);
		videoScreen.texture = videoPlayer.texture;
		videoPlayer.Play();
	}

	private void SetKeyText()
	{
		if (Game.DataManager.IsLocaleExist(inputGuideKey))
		{
			keyText.SetData(inputGuideKey);
		}
		else
		{
			keyText.ClearText();
		}
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		SetKeyText();
	}
}
