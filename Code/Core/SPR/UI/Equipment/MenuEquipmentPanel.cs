using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using SPR.L10N;
using SPR.Save;
using SPR.UI.Menu;
using SPR.UI.Tutorial;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SPR.UI.Equipment;

public class MenuEquipmentPanel : MenuPanel
{
	[SerializeField]
	private EquippedPartBoard equippedPartBoard;

	[SerializeField]
	private AcquiredPartBoard acquiredPartBoard;

	[SerializeField]
	private PartInfoBoard equippedPartInfoBoard;

	[SerializeField]
	private PartInfoBoard focusedPartInfoBoard;

	[SerializeField]
	private CategoryList categoryList;

	[SerializeField]
	private GameObject panelName;

	[SerializeField]
	private GameObject scrollKeyGuide;

	private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

	private Dictionary<PartType, List<PlayerData.PartData>> partDataDict;

	private bool shouldReset;

	private bool canEquip;

	private PartType currentPartType;

	public override MenuCategory Category => MenuCategory.Equipment;

	public override bool IsValid
	{
		get
		{
			if (!base.gameObject.IsNull())
			{
				return Game.Instance.SaveData.PlayerData.HasEverObtainedItemType(ItemType.Wristband);
			}
			return false;
		}
	}

	protected override bool CanChangeCategory => !MenuPanel.IsRest;

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static PlayerPart PlayerPart => Game.Instance.Player.PlayerPart;

	private bool IsFocusingCategory => acquiredPartBoard.CurrentIndex == -1;

	public override void Initialize()
	{
		equippedPartBoard.Initialize();
		acquiredPartBoard.Initialize();
		equippedPartInfoBoard.Initialize();
		focusedPartInfoBoard.Initialize();
		categoryList.Initialize();
		currentPartType = PartType.Weapon;
	}

	protected override void OnActivate()
	{
		if (partDataDict == null)
		{
			partDataDict = new Dictionary<PartType, List<PlayerData.PartData>>();
		}
		currentPartType = PartType.Weapon;
		equippedPartBoard.ResetFocus();
		acquiredPartBoard.ResetFocus();
		partDataDict.Clear();
		for (int i = 0; i < 4; i++)
		{
			PartType partType = (PartType)i;
			List<PlayerData.PartData> list = new List<PlayerData.PartData>();
			PlayerData.PartData equippedPartData = PlayerData.EquippedPartList.FirstOrDefault((PlayerData.PartData data) => data.PartType == partType);
			if (equippedPartData != null)
			{
				list.Add(equippedPartData);
				list.AddRange(PlayerData.AcquiredPartList.Where((PlayerData.PartData data) => data.PartType == partType && data.PartID != equippedPartData.PartID));
			}
			else
			{
				list.AddRange(PlayerData.AcquiredPartList.Where((PlayerData.PartData data) => data.PartType == partType));
			}
			partDataDict.Add(partType, list);
		}
		panelName.SetActive(MenuPanel.IsRest);
		categoryList.OnActivate(MenuPanel.IsRest);
		equippedPartBoard.OnActivate();
		base.MenuCanvas.GuideGroup.ActivateEquipment(GetKeyGuideMode(""));
		ActivateAsync().Forget();
		if (Game.Instance.SaveData.WanderingData.ShouldShowTutorial(ActionName.Part))
		{
			TutorialCanvas tutorialCanvas = Game.Instance.UIManager.TutorialCanvas;
			tutorialCanvas.SetData(ActionName.Part);
			MenuPanel.InputManager.StackControlTarget(tutorialCanvas).Forget();
		}
	}

	protected override void OnControl()
	{
		int rawSubCategory = (MenuPanel.IsRest ? MenuPanel.InputManager.categoryUI : MenuPanel.InputManager.subCategory);
		int horizontalDownUI = MenuPanel.InputManager.horizontalDownUI;
		int verticalDownUI = MenuPanel.InputManager.verticalDownUI;
		bool confirmDown = MenuPanel.InputManager.confirmDown;
		bool flag = MenuPanel.InputManager.menuDown || MenuPanel.InputManager.cancelDown;
		int num = (IsFocusingCategory ? Util.GetPrioritizedControl(rawSubCategory, horizontalDownUI) : Util.GetNotOverlappedControl(rawSubCategory, horizontalDownUI));
		if (num != 0)
		{
			equippedPartBoard.MoveCursor(num);
			currentPartType = equippedPartBoard.FocusPartType;
			string focusPartID = equippedPartBoard.FocusPartID;
			Sprite currentPartTypeIconSafe = GetCurrentPartTypeIconSafe();
			Sprite imageSprite = (string.IsNullOrEmpty(focusPartID) ? currentPartTypeIconSafe : GetSpriteSafe(focusPartID));
			focusedPartInfoBoard.SetData(null, currentPartType, currentPartTypeIconSafe);
			equippedPartInfoBoard.SetData(focusPartID, currentPartType, imageSprite);
			FocusCategoryAsync(equippedPartBoard.FocusPartType, rawSubCategory == 0).Forget();
			scrollKeyGuide.SetActive(equippedPartInfoBoard.IsScrollAvailable() || focusedPartInfoBoard.IsScrollAvailable());
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Sub_Move");
			return;
		}
		if (horizontalDownUI != 0 || verticalDownUI != 0)
		{
			MoveCursor(horizontalDownUI, verticalDownUI);
		}
		if (confirmDown)
		{
			string partID;
			if (!MenuPanel.IsRest)
			{
				if (!IsFocusingCategory)
				{
					acquiredPartBoard.ShakeCell();
					MenuPanel.FadeCanvas.WarningBoard.ShowWarning(LocaleKey.PartWarning("Equip"));
				}
			}
			else if (acquiredPartBoard.TrySelect(out partID))
			{
				if (partID.IsNullOrEmpty())
				{
					MoveCursor(0, -1);
				}
				else
				{
					EquipPart(partID);
				}
			}
		}
		if (flag && !IsFocusingCategory)
		{
			acquiredPartBoard.ResetFocusIndex(out var id);
			SelectPart(id);
		}
		if (MenuPanel.InputManager.scroll != 0f)
		{
			equippedPartInfoBoard.Scroll(MenuPanel.InputManager.scroll.GetSign());
			focusedPartInfoBoard.Scroll(MenuPanel.InputManager.scroll.GetSign());
		}
		void MoveCursor(int h, int v)
		{
			if (acquiredPartBoard.TryMoveCursor(h, v, out var partID2))
			{
				SelectPart(partID2);
			}
		}
		void SelectPart(string text)
		{
			OnWatchedPartCell(currentPartType, text);
			UpdateFocusedInfoBoard(text, currentPartType);
			UpdateCategoryCursor(rawSubCategory == 0);
			base.MenuCanvas.GuideGroup.ActivateEquipment(GetKeyGuideMode(text));
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
		}
	}

	protected override void OnDeactivate()
	{
	}

	private async UniTask ActivateAsync()
	{
		List<UniTask> list = new List<UniTask>(4);
		for (int i = 0; i < 4; i++)
		{
			PartType partType = (PartType)i;
			string id = Address.IconSprite(MenuCategory.Equipment, partType.ToString());
			list.Add(CacheSpriteAsync(id));
		}
		await UniTask.WhenAll(list);
		await RefreshEquippedItemBoardAsync();
		await FocusCategoryAsync(currentPartType, shouldFocusCategory: false, force: true);
	}

	private async UniTask RefreshEquippedItemBoardAsync()
	{
		IReadOnlyList<PlayerData.PartData> equippedPartDatas = PlayerData.EquippedPartList;
		await UniTask.WhenAll(Enumerable.Select(equippedPartDatas, (PlayerData.PartData partData2) => CacheItemSpriteAsync(partData2.PartID)));
		equippedPartBoard.SetData(equippedPartDatas, spriteCache);
		PlayerData.PartData partData = equippedPartDatas.FirstOrDefault((PlayerData.PartData data) => data.PartType == currentPartType);
		if (partData == null)
		{
			Sprite currentPartTypeIconSafe = GetCurrentPartTypeIconSafe();
			equippedPartInfoBoard.SetData(null, currentPartType, currentPartTypeIconSafe);
		}
		else
		{
			equippedPartInfoBoard.SetData(partData.PartID, partData.PartType, GetSpriteSafe(partData.PartID));
		}
		scrollKeyGuide.SetActive(equippedPartInfoBoard.IsScrollAvailable() || focusedPartInfoBoard.IsScrollAvailable());
	}

	private async UniTask FocusCategoryAsync(PartType focusPartType, bool shouldFocusCategory, bool force = false)
	{
		currentPartType = focusPartType;
		categoryList.Focus((int)focusPartType, !force);
		UpdateCategoryCursor(shouldFocusCategory);
		List<PlayerData.PartData> targetPartData = partDataDict[focusPartType];
		if (targetPartData.Count > 0)
		{
			await UniTask.WhenAll(Enumerable.Select(targetPartData, (PlayerData.PartData partData) => CacheItemSpriteAsync(partData.PartID)));
		}
		acquiredPartBoard.SetData(focusPartType, targetPartData, spriteCache, shouldFocusCategory);
		string text;
		if (acquiredPartBoard.TryMoveCursor(0, 0, out var partID))
		{
			text = partID;
			OnWatchedPartCell(currentPartType, text);
		}
		else
		{
			text = null;
		}
		UpdateFocusedInfoBoard(text, currentPartType);
		base.MenuCanvas.GuideGroup.ActivateEquipment(GetKeyGuideMode(text));
	}

	private void UpdateCategoryCursor(bool shouldFocusCategory)
	{
		int num = (int)((shouldFocusCategory && IsFocusingCategory) ? currentPartType : ((PartType)(-1)));
		DebugLog.GeneralLog($"UpdateCategoryCursor(shouldFocusCategory= {shouldFocusCategory}) : index= {num}");
		categoryList.SetCursor(num);
	}

	private async UniTask CacheItemSpriteAsync(string id)
	{
		if (id == "???")
		{
			id = "Unknown";
		}
		if (!spriteCache.ContainsKey(id))
		{
			AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(Address.ItemSprite(id));
			await handle.Task;
			if (!spriteCache.ContainsKey(id))
			{
				spriteCache[id] = handle.Task.Result;
			}
		}
	}

	private async UniTask CacheSpriteAsync(string id)
	{
		if (!spriteCache.ContainsKey(id))
		{
			AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(id);
			await handle.Task;
			if (!spriteCache.ContainsKey(id))
			{
				spriteCache[id] = handle.Task.Result;
			}
		}
	}

	private void UpdateFocusedInfoBoard(string id, PartType partType)
	{
		if (string.IsNullOrEmpty(id) || equippedPartInfoBoard.PartID == id)
		{
			Sprite currentPartTypeIconSafe = GetCurrentPartTypeIconSafe();
			focusedPartInfoBoard.SetData(null, partType, currentPartTypeIconSafe);
		}
		else
		{
			focusedPartInfoBoard.SetData(id, partType, GetSpriteSafe(id));
		}
		bool active = equippedPartInfoBoard.IsScrollAvailable() || focusedPartInfoBoard.IsScrollAvailable();
		scrollKeyGuide.SetActive(active);
	}

	private Sprite GetCurrentPartTypeIconSafe()
	{
		string text = Address.IconSprite(MenuCategory.Equipment, currentPartType.ToString());
		if (spriteCache.TryGetValue(text, out var value))
		{
			return value;
		}
		Debug.LogWarning("[MenuEquipmentPanel] 현재 파츠 타입 아이콘이 아직 캐시되지 않았습니다. key=" + text);
		return null;
	}

	private Sprite GetSpriteSafe(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		if (id == "???")
		{
			id = "Unknown";
		}
		if (spriteCache.TryGetValue(id, out var value))
		{
			return value;
		}
		Debug.LogWarning("[MenuEquipmentPanel] 스프라이트가 아직 캐시되지 않았습니다. id=" + id);
		return null;
	}

	private void EquipPart(string id)
	{
		PlayerData.PartData partData = PlayerData.EquippedPartList.FirstOrDefault((PlayerData.PartData data) => data.PartID == id);
		if (partData != null)
		{
			PlayerPart.Unequip(partData.PartType);
			acquiredPartBoard.OnUnequip();
			UpdateCategoryCursor(shouldFocusCategory: false);
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem_Show");
		}
		else
		{
			PlayerPart.Equip(id);
			Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Equip/SFX_UI_Equip_Gearup");
		}
		RefreshEquippedItemBoardAsync().Forget();
		UpdateFocusedInfoBoard(id, currentPartType);
		equippedPartBoard.PlayEquipParticle((int)currentPartType);
		equippedPartInfoBoard.PlayParticle();
		base.MenuCanvas.GuideGroup.ActivateEquipment(GetKeyGuideMode(id));
	}

	private void OnWatchedPartCell(PartType partType, string id)
	{
		Game.Instance.SaveData.WanderingData.RemoveUnwatchedPart(partType, id);
	}

	private Mode GetKeyGuideMode(string focusID)
	{
		if (MenuPanel.IsRest && !string.IsNullOrEmpty(focusID))
		{
			if (!PlayerData.EquippedPartList.Any((PlayerData.PartData data) => data.PartID == focusID))
			{
				return Mode.Cancel | Mode.Equip;
			}
			return Mode.Cancel | Mode.Unequip;
		}
		return Mode.Cancel;
	}
}
