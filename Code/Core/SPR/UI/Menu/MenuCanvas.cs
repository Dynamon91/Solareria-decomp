using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI.Field;
using SPR.UI.Map;
using SPR.UI.Memory;
using UnityEngine;

namespace SPR.UI.Menu;

public class MenuCanvas : AutoScaledCanvas, IControllable
{
	[SerializeField]
	private MenuCategoryList categoryList;

	[SerializeField]
	private List<MenuPanel> menuPanels;

	[SerializeField]
	private ControlGuideGroup guideGroup;

	private MenuCategory currentCategory;

	private MenuMapPanel mapPanel;

	private MenuMemoryPanel memoryPanel;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	public ControlGuideGroup GuideGroup => guideGroup;

	public MenuMapPanel MapPanel => mapPanel ?? (mapPanel = menuPanels.First((MenuPanel p) => p.Category == MenuCategory.Map) as MenuMapPanel);

	public MenuMemoryPanel MemoryPanel => memoryPanel ?? (memoryPanel = menuPanels.First((MenuPanel p) => p.Category == MenuCategory.Memory) as MenuMemoryPanel);

	public CategoryList CategoryList => categoryList;

	public bool IsValid => true;

	public void Initialize()
	{
		base.gameObject.SetActive(value: true);
		categoryList.Initialize();
		categoryList.SetCategoryActive(menuPanels.Select((MenuPanel panel) => panel.IsValid).ToList());
		menuPanels.ForEach(delegate(MenuPanel panel)
		{
			panel.Deactivate();
			panel.Initialize();
			panel.gameObject.SetActive(value: true);
			panel.gameObject.SetActive(value: false);
		});
		base.gameObject.SetActive(value: false);
	}

	public void FocusCategory(MenuCategory category, bool force = false)
	{
		if (!force && currentCategory == category)
		{
			return;
		}
		MenuCategory beforeCategory = currentCategory;
		MenuCategory afterCategory = category;
		MenuPanel menuPanel = menuPanels.Find((MenuPanel panel) => panel.Category == beforeCategory);
		MenuPanel menuPanel2 = menuPanels.Find((MenuPanel panel) => panel.Category == afterCategory);
		if (!menuPanel2.IsValid)
		{
			afterCategory += (afterCategory - beforeCategory).GetSign();
			menuPanel2 = menuPanels.Find((MenuPanel panel) => panel.Category == afterCategory);
		}
		currentCategory = afterCategory;
		categoryList.Focus((int)currentCategory);
		if (InputManager.ContainsInControlStack(menuPanel))
		{
			InputManager.SwitchControlTarget(menuPanel, menuPanel2).Forget();
		}
		else
		{
			InputManager.StackControlTarget(menuPanel2).Forget();
		}
	}

	public void SetDefaultOpenCategory(MenuCategory category)
	{
		foreach (MenuPanel menuPanel in menuPanels)
		{
			if (menuPanel.Category == category)
			{
				currentCategory = category;
				return;
			}
		}
		DebugLog.GeneralLogError($"{category}에 해당하는 메뉴가 UI에 존재하지 않습니다!");
	}

	public string ControllerName()
	{
		return base.gameObject.name;
	}

	public UniTask Activate()
	{
		SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem_Show");
		base.gameObject.SetActive(value: true);
		categoryList.SetCategoryActive(menuPanels.Select((MenuPanel panel) => panel.IsValid).ToList());
		FocusCategory(currentCategory, force: true);
		FieldCanvas.HideAllAsync(instant: false).Forget();
		return default(UniTask);
	}

	public void Control()
	{
	}

	public UniTask Pause()
	{
		return default(UniTask);
	}

	public UniTask Restart()
	{
		return default(UniTask);
	}

	public UniTask Deactivate()
	{
		SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem_Hide");
		base.gameObject.SetActive(value: false);
		return default(UniTask);
	}
}
