using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI.Equipment;
using SPR.UI.Menu;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SPR.UI.Upgrade;

public class PartUpgradePanel : UpgradePanel
{
	[SerializeField]
	private CategoryList categoryList;

	[SerializeField]
	private AcquiredPartBoard acquiredPartBoard;

	private PartUpgradeModel model;

	private PartType currentPartType;

	private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

	protected override bool UsingOhn => true;

	private PartUpgradeBoard PartUpgradeBoard => upgradeBoard as PartUpgradeBoard;

	private Sprite CurrentPartTypeIcon => spriteCache[Address.IconSprite(MenuCategory.Equipment, currentPartType.ToString())];

	private bool IsFocusingCategory => acquiredPartBoard.CurrentIndex == -1;

	protected override string ConfirmType => "Part";

	public override string ControllerName()
	{
		return "Part Upgrade Panel";
	}

	public override void Initialize(IService serviceObject, GameObject vcam, Unit arg)
	{
		base.Initialize(serviceObject, vcam, arg);
		categoryList.Initialize();
		acquiredPartBoard.Initialize();
	}

	public override bool HasStock()
	{
		return PartUpgradeBoard.HasStock();
	}

	protected override async UniTask OnActivateAsync()
	{
		PartUpgradeBoard.InitializeAsync().Forget();
		currentPartType = PartType.Weapon;
		acquiredPartBoard.ResetFocus();
		for (int i = 0; i < 4; i++)
		{
			PartType partType = (PartType)i;
			string id = Address.IconSprite(MenuCategory.Equipment, partType.ToString());
			await CacheSpriteAsync(id);
		}
		await FocusCategoryAsync(currentPartType, shouldFocusCategory: false);
	}

	protected override UniTask OnDeactivateAsync()
	{
		model = null;
		return UniTask.CompletedTask;
	}

	public override void Control()
	{
		int categoryUI = ServicePanel<Unit>.InputManager.categoryUI;
		int horizontalDownUI = ServicePanel<Unit>.InputManager.horizontalDownUI;
		int num = (IsFocusingCategory ? Util.GetPrioritizedControl(categoryUI, horizontalDownUI) : Util.GetNotOverlappedControl(categoryUI, horizontalDownUI));
		if (num != 0)
		{
			currentPartType = (PartType)Mathf.Repeat((float)(currentPartType + num), 4f);
			FocusCategoryAsync(currentPartType, categoryUI == 0).Forget();
			return;
		}
		base.Control();
		if ((ServicePanel<Unit>.InputManager.menuDown || ServicePanel<Unit>.InputManager.cancelDown) && !IsFocusingCategory)
		{
			acquiredPartBoard.ResetFocusIndex(out var _);
			SelectPart(null);
		}
	}

	protected override void OnControlInput(int horizontal, int vertical)
	{
		MoveCursor(horizontal, vertical);
	}

	private void MoveCursor(int horizontal, int vertical)
	{
		if (acquiredPartBoard.TryMoveCursor(horizontal, vertical, out var partID))
		{
			SelectPart(partID);
		}
	}

	private void SelectPart(string id)
	{
		OnWatchedPartCell(currentPartType, id);
		UpdateBoard(id, currentPartType);
		UpdateCategoryCursor(IsFocusingCategory);
		ServicePanel<Unit>.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
	}

	protected override void OnUpgradeSuccess()
	{
		int num = ServicePanel<Unit>.PlayerStat.Money - PartUpgradeBoard.Cost;
		ServicePanel<Unit>.ConfirmPopup.SetModel(new FormatConfirmModel(ConfirmType, OnConfirmInput, null, num.ToString()));
		ServicePanel<Unit>.InputManager.StackControlTarget(ServicePanel<Unit>.ConfirmPopup).Forget();
	}

	protected override void OnConfirmInput()
	{
		if (IsFocusingCategory)
		{
			MoveCursor(0, -1);
			return;
		}
		PartUpgradeBoard.OnProceedUpgrade();
		acquiredPartBoard.OnUpgradeLevel();
		CheckSoldOutScenario();
	}

	protected override void UpdateControlGuide()
	{
	}

	public override void UpdateData()
	{
	}

	private async UniTask FocusCategoryAsync(PartType partType, bool shouldFocusCategory, bool force = false)
	{
		currentPartType = partType;
		categoryList.Focus((int)partType, !force);
		UpdateCategoryCursor(shouldFocusCategory);
		List<PlayerData.PartData> partDataList = new List<PlayerData.PartData>();
		PlayerData.PartData equippedPartData = ServicePanel<Unit>.PlayerData.EquippedPartList.FirstOrDefault((PlayerData.PartData data) => data.PartType == partType);
		if (equippedPartData != null)
		{
			partDataList.Add(equippedPartData);
			partDataList.AddRange(ServicePanel<Unit>.PlayerData.AcquiredPartList.Where((PlayerData.PartData data) => data.PartType == partType && data.PartID != equippedPartData.PartID));
		}
		else
		{
			partDataList.AddRange(ServicePanel<Unit>.PlayerData.AcquiredPartList.Where((PlayerData.PartData data) => data.PartType == partType));
		}
		if (!partDataList.IsNullOrEmpty())
		{
			await UniTask.WhenAll(Enumerable.Select(partDataList, (PlayerData.PartData partData) => CacheItemSpriteAsync(partData.PartID)).ToList());
		}
		acquiredPartBoard.SetData(partType, partDataList, spriteCache, shouldFocusCategory);
		string id;
		if (acquiredPartBoard.TryMoveCursor(0, 0, out var partID))
		{
			id = partID;
			OnWatchedPartCell(currentPartType, id);
		}
		else
		{
			id = null;
		}
		UpdateBoard(id, currentPartType);
	}

	private void UpdateCategoryCursor(bool shouldFocusCategory)
	{
		categoryList.SetCursor((int)((shouldFocusCategory && IsFocusingCategory) ? currentPartType : ((PartType)(-1))));
	}

	private void OnWatchedPartCell(PartType partType, string id)
	{
		Game.Instance.SaveData.WanderingData.RemoveUnwatchedPart(partType, id);
	}

	private void UpdateBoard(string id, PartType partType)
	{
		if (model == null)
		{
			model = new PartUpgradeModel
			{
				ID = id,
				PartType = partType,
				Sprite = (id.IsNullOrEmpty() ? CurrentPartTypeIcon : spriteCache[id])
			};
		}
		else
		{
			model.ID = id;
			model.PartType = partType;
			model.Sprite = (id.IsNullOrEmpty() ? CurrentPartTypeIcon : spriteCache[id]);
		}
		PartUpgradeBoard.SetData(model);
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
			spriteCache.Add(id, handle.Task.Result);
		}
	}

	private async UniTask CacheSpriteAsync(string id)
	{
		if (!spriteCache.ContainsKey(id))
		{
			AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(id);
			await handle.Task;
			spriteCache.Add(id, handle.Task.Result);
		}
	}
}
