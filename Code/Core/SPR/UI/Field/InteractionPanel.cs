using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Cutscene;
using SPR.L10N;
using SPR.Library;
using SPR.Save;
using SPR.UI.Talk;
using SPR.UI.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace SPR.UI.Field;

public class InteractionPanel : MonoBehaviour, IControllable
{
	[Serializable]
	private class DescriptionLayout
	{
		[SerializeField]
		private Image image;

		[SerializeField]
		private LocaleText nameText;

		[SerializeField]
		private Image divideLineImage;

		[SerializeField]
		private Image divideDiamondImage;

		[SerializeField]
		private LocaleText keyText;

		[SerializeField]
		private LocaleText descText;

		[Header("파츠/코어스톤")]
		[SerializeField]
		private Image outlineImage;

		[SerializeField]
		private Image surfaceImage;

		[SerializeField]
		private Sprite partsOutlineSprite;

		[SerializeField]
		private Sprite partsSurfaceSprite;

		[SerializeField]
		private Sprite statCoreStoneOutlineSprite;

		[SerializeField]
		private Sprite statCoreStoneSurfaceSprite;

		[SerializeField]
		private Sprite skillCoreStoneOutlineSprite;

		[SerializeField]
		private Sprite skillCoreStoneSurfaceSprite;

		[SerializeField]
		private Sprite warmhornOutlineSprite;

		[SerializeField]
		private Sprite warmhornSurfaceSprite;

		[Header("성물")]
		[SerializeField]
		private Image relicSurfaceImage;

		[Header("다섯 전사들 혼불")]
		[Header("아이템의 위치와 맞춰야하는 오브젝트 목록")]
		[SerializeField]
		private List<RectTransform> effectRootRects;

		private Sequence fadeSeq;

		private RectTransform layoutGroupRect;

		private RectTransform LayoutGroupRect => layoutGroupRect ?? (layoutGroupRect = image.transform.parent.parent.GetComponent<RectTransform>());

		private TextMeshProUGUI NameTextComp => nameText.GetComponent<TextMeshProUGUI>();

		private bool IsSpecialItem(ItemType itemType, string id)
		{
			if (itemType != ItemType.Wristband || !(id == "EnhancedWristband"))
			{
				if (itemType != ItemType.CoreStone && itemType != ItemType.Part && itemType != ItemType.Relic && itemType != ItemType.SpiritFlame)
				{
					return itemType == ItemType.Warmhorn;
				}
				return true;
			}
			return true;
		}

		public void SetActive(bool value)
		{
			LayoutGroupRect.gameObject.SetActive(value);
		}

		public void SetData(ItemType itemType, string id)
		{
			SetImage(itemType, id).Forget();
			SetEffect(itemType, id);
			nameText.SetData(LocaleKey.ItemName(itemType, id));
			descText.SetData(LocaleKey.ItemDesc(itemType, id));
			if (itemType == ItemType.CoreStone && LocaleKey.TryGetSkillKey(id, out var localeKey))
			{
				keyText.gameObject.SetActive(value: true);
				keyText.SetData(localeKey);
			}
			else
			{
				keyText.gameObject.SetActive(value: false);
			}
			UIHelper.RebuildLayoutAfterFramesAsync(LayoutGroupRect, 1, OnCompleteRebuildLayout).Forget();
		}

		public void SetVisible(bool value)
		{
			image.SetAlpha(value ? 1 : 0);
			NameTextComp.alpha = (value ? 1 : 0);
			keyText.SetAlpha(value ? 1 : 0);
			descText.SetAlpha(value ? 1 : 0);
			divideLineImage.SetAlpha(value ? 1 : 0);
			divideDiamondImage.SetAlpha(value ? 1 : 0);
		}

		private async UniTask SetImage(ItemType itemType, string id)
		{
			string text = ((itemType != ItemType.None) ? Address.ItemSprite(id) : ("Image/" + id));
			string key = text;
			if (!AddressableHelper.HasKey(key))
			{
				image.transform.parent.gameObject.SetActive(value: false);
				return;
			}
			if (itemType == ItemType.Note)
			{
				image.transform.parent.gameObject.SetActive(value: false);
				return;
			}
			fadeSeq?.Kill();
			image.SetAlpha(0f);
			image.transform.parent.gameObject.SetActive(value: true);
			AsyncOperationHandle<Sprite> task = Addressables.LoadAssetAsync<Sprite>(key);
			await task;
			image.sprite = task.Result;
			relicSurfaceImage.sprite = task.Result;
			if (!IsSpecialItem(itemType, id))
			{
				fadeSeq = DOTween.Sequence();
				fadeSeq.Append(image.DOAlpha(1f, 0.4f));
			}
		}

		private void SetEffect(ItemType itemType, string id)
		{
			if (IsSpecialItem(itemType, id))
			{
				SetVisible(value: false);
				switch (itemType)
				{
				case ItemType.Part:
					outlineImage.sprite = partsOutlineSprite;
					surfaceImage.sprite = partsSurfaceSprite;
					break;
				case ItemType.CoreStone:
				{
					bool flag = Util.IsSkillCoreStone(id);
					outlineImage.sprite = (flag ? skillCoreStoneOutlineSprite : statCoreStoneOutlineSprite);
					surfaceImage.sprite = (flag ? skillCoreStoneSurfaceSprite : statCoreStoneSurfaceSprite);
					break;
				}
				case ItemType.Warmhorn:
					outlineImage.sprite = warmhornOutlineSprite;
					surfaceImage.sprite = warmhornSurfaceSprite;
					break;
				}
			}
		}

		private void OnCompleteRebuildLayout()
		{
			foreach (RectTransform effectRootRect in effectRootRects)
			{
				effectRootRect.transform.position = image.transform.position;
			}
		}
	}

	[Serializable]
	private class InteractionData
	{
		[SerializeField]
		private ItemType itemType;

		[SerializeField]
		private string id;

		public ItemType ItemType => itemType;

		public string ID => id;

		public InteractionData(ItemType itemType, string id)
		{
			this.itemType = itemType;
			this.id = id;
		}
	}

	[SerializeField]
	private DescriptionLayout descriptionLayout;

	[SerializeField]
	private GameObject heatDescription;

	[SerializeField]
	private GameObject keyGuideTextObject;

	[Header("Timeline Assets")]
	[SerializeField]
	private PlayableAsset partsCoreStoneTimeline;

	[SerializeField]
	private PlayableAsset relicTimeline;

	[SerializeField]
	private PlayableAsset baranSpiritTimeline;

	[SerializeField]
	private PlayableAsset darkChasaSpiritTimeline;

	[SerializeField]
	private PlayableAsset gaiaSpiritTimeline;

	[SerializeField]
	private PlayableAsset moldinSpiritTimeline;

	[SerializeField]
	private PlayableAsset soowooSpiritTimeline;

	private CanvasGroup canvasGroup;

	private readonly Queue<InteractionData> dataQueue = new Queue<InteractionData>();

	private PlayableDirector director;

	private Dictionary<BossName, PlayableAsset> fiveWarriorsSpiritTimelineDict;

	private ItemType itemType;

	private string itemID;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private PlayableDirector Director => director ?? (director = GetComponent<PlayableDirector>());

	private Dictionary<BossName, PlayableAsset> FiveWarriorsSpiritTimelineDict
	{
		get
		{
			Dictionary<BossName, PlayableAsset> dictionary = fiveWarriorsSpiritTimelineDict;
			if (dictionary == null)
			{
				Dictionary<BossName, PlayableAsset> obj = new Dictionary<BossName, PlayableAsset>
				{
					{
						BossName.Baran,
						baranSpiritTimeline
					},
					{
						BossName.DarkChasa,
						darkChasaSpiritTimeline
					},
					{
						BossName.Gaia,
						gaiaSpiritTimeline
					},
					{
						BossName.Moldin,
						moldinSpiritTimeline
					},
					{
						BossName.Soowoo,
						soowooSpiritTimeline
					}
				};
				Dictionary<BossName, PlayableAsset> dictionary2 = obj;
				fiveWarriorsSpiritTimelineDict = obj;
				dictionary = dictionary2;
			}
			return dictionary;
		}
	}

	public bool IsValid => true;

	public bool IsDisplaying
	{
		get
		{
			if (!base.gameObject.activeSelf)
			{
				return dataQueue.Count > 0;
			}
			return true;
		}
	}

	private static InputManager InputManager => Game.Instance.InputManager;

	private static PlayerController Player => Game.Instance.Player;

	private static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	private static TutorialCanvas TutorialCanvas => Game.Instance.UIManager.TutorialCanvas;

	public event Action OnActivate;

	public event Action OnDeactivate;

	public string ControllerName()
	{
		return "Interaction Panel";
	}

	public void Initialize()
	{
		LocaleText[] componentsInChildren = heatDescription.GetComponentsInChildren<LocaleText>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].SetData();
		}
		dataQueue.Clear();
		base.gameObject.SetActive(value: false);
	}

	public async UniTask Activate()
	{
		this.OnActivate?.Invoke();
		this.OnActivate = null;
		base.gameObject.SetActive(value: true);
		keyGuideTextObject.SetActive(value: false);
		SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem_Show");
		if (this.itemType != ItemType.None)
		{
			SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetCard");
		}
		FieldCanvas.HideAllAsync(instant: false).Forget();
		await FadeInOutPanelAsync(value: true);
		ItemType itemType = this.itemType;
		if (itemType == ItemType.Part || itemType == ItemType.CoreStone || itemType == ItemType.Warmhorn)
		{
			Director.extrapolationMode = DirectorWrapMode.None;
			Director.playableAsset = partsCoreStoneTimeline;
			Director.Play();
			await UniTask.WaitUntil(() => Director.state != PlayState.Playing);
		}
		else if (this.itemType == ItemType.Relic || (this.itemType == ItemType.Wristband && itemID == "EnhancedWristband"))
		{
			Director.extrapolationMode = DirectorWrapMode.Hold;
			Director.playableAsset = relicTimeline;
			Director.Play();
			await UniTask.WaitUntil(() => Director.time >= Director.duration - 0.009999999776482582);
		}
		else if (this.itemType == ItemType.SpiritFlame)
		{
			Director.extrapolationMode = DirectorWrapMode.None;
			Director.playableAsset = GetSpiritFlameTimeline(itemID);
			Director.Play();
			await UniTask.WaitUntil(() => Director.state != PlayState.Playing);
		}
		keyGuideTextObject.SetActive(value: true);
	}

	public void Control()
	{
		if (InputManager.confirmDown || InputManager.menuDown || InputManager.cancelDown)
		{
			InputManager.RemoveControlTarget(this).Forget();
		}
	}

	public UniTask Pause()
	{
		if (Game.Instance.Player.isDead)
		{
			InputManager.RemoveControlTarget(this).Forget();
		}
		return default(UniTask);
	}

	public UniTask Restart()
	{
		return default(UniTask);
	}

	public async UniTask Deactivate()
	{
		SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem_Hide");
		if (itemType == ItemType.Heat && PlayerStat.HeatMaxLevel <= 0)
		{
			FirstHeatCutsceneProducer.GetProducer().TurnOnBackgrounds();
		}
		await FadeInOutPanelAsync(value: false);
		if (itemType == ItemType.CoreStone)
		{
			if (WanderingData.ShouldShowTutorial(ActionName.Card))
			{
				TutorialCanvas.SetData(ActionName.Card);
				InputManager.StackControlTarget(TutorialCanvas).Forget();
			}
		}
		else if (itemType == ItemType.Heat && PlayerStat.HeatMaxLevel <= 0)
		{
			FirstHeatCutsceneProducer.GetProducer().PlayTimelineAsync(0, blockControl: true).Forget();
		}
		if (!InputManager.ContainControlTarget(typeof(CutsceneProducer)) && !InputManager.ContainControlTarget(typeof(TalkCanvas)) && Game.Instance.FieldController.CurrentArea.TryGetCutsceneProducer(out var producer) && !producer.IsProducing)
		{
			FieldCanvas.ShowAllAsync(instant: false).Forget();
		}
		base.gameObject.SetActive(value: false);
		if (dataQueue.Count > 0)
		{
			StartBusyWaiting().Forget();
		}
		this.OnDeactivate?.Invoke();
		this.OnDeactivate = null;
	}

	public void ShowDataImmediately(ItemType itemType, string itemID)
	{
		SetData(itemType, itemID);
		InputManager.StackControlTarget(this).Forget();
	}

	public void EnqueueItemData(string itemID)
	{
		ItemData itemData = Game.DataManager.GetItemData(itemID);
		dataQueue.Enqueue(new InteractionData(itemData.ItemType, itemID));
		StartBusyWaiting().Forget();
	}

	private void SetData(ItemType itemType, string id)
	{
		this.itemType = itemType;
		itemID = id;
		descriptionLayout.SetActive(value: false);
		heatDescription.SetActive(value: false);
		if (itemType == ItemType.Heat)
		{
			heatDescription.SetActive(value: true);
			return;
		}
		descriptionLayout.SetActive(value: true);
		descriptionLayout.SetData(itemType, id);
	}

	private async UniTask StartBusyWaiting()
	{
		await UniTask.WaitUntil(() => InputManager.IsCurrentControlTarget(Player));
		if (dataQueue.Count != 0)
		{
			InteractionData interactionData = dataQueue.Dequeue();
			SetData(interactionData.ItemType, interactionData.ID);
			InputManager.StackControlTarget(this).Forget();
		}
	}

	private async UniTask FadeInOutPanelAsync(bool value)
	{
		float duration = 0.4f;
		float endValue = (value ? 1f : 0f);
		float alpha = (value ? 0f : 1f);
		CanvasGroup.alpha = alpha;
		await CanvasGroup.DOAlpha(endValue, duration).WaitAsync();
	}

	private PlayableAsset GetSpiritFlameTimeline(string id)
	{
		foreach (BossName key in FiveWarriorsSpiritTimelineDict.Keys)
		{
			if (id.Contains(key.ToString()))
			{
				return FiveWarriorsSpiritTimelineDict[key];
			}
		}
		DebugLog.GeneralLogError(id + " 아이템은 혼불이 아닙니다. 아이템 타입이나 아이템 이름을 확인해주세요.");
		return null;
	}
}
