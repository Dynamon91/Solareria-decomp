using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using SPR.Save;
using StaticData;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Title;

public class SaveSlotPanel : MonoBehaviour, IControllable
{
	[SerializeField]
	private RectTransform slotGroupRect;

	[SerializeField]
	private float fadeDuration = 0.2f;

	[SerializeField]
	private List<GameObject> manageKeyGuideObjects;

	private TitleManager titleManager;

	private RectTransform groupParentRect;

	private LayoutGroup layoutGroup;

	private CanvasScaler canvasScaler;

	private RectTransform slotsParentRect;

	private SaveSlot[] saveSlots;

	private int targetIndex;

	private int maxLength;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static ConfirmPopup ConfirmPopup => Game.Instance.UIManager.FieldCanvas.ConfirmPopup;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private SaveSlot TargetSlot => saveSlots[targetIndex];

	private TitleManager TitleManager => titleManager ?? (titleManager = GetComponentInParent<TitleManager>(includeInactive: true));

	private RectTransform GroupParentRect => groupParentRect ?? (groupParentRect = slotGroupRect.parent as RectTransform);

	private LayoutGroup LayoutGroup => layoutGroup ?? (layoutGroup = slotGroupRect.GetComponent<LayoutGroup>());

	private CanvasScaler CanvasScaler => canvasScaler ?? (canvasScaler = TitleManager.GetComponent<CanvasScaler>());

	public bool IsValid => true;

	public void Initialize()
	{
		saveSlots = slotGroupRect.GetComponentsInChildren<SaveSlot>(includeInactive: true);
		maxLength = saveSlots.Length;
		for (int i = 1; i <= 4; i++)
		{
			AddressableHelper.PreloadSpriteAsync(Address.DifficultyIconSprite((Difficulty)i)).Forget();
		}
		for (int j = 0; j < maxLength; j++)
		{
			SaveData saveData = Game.Instance.GetSaveData(j);
			saveSlots[j].Initialize(saveData, fadeDuration);
		}
	}

	private void SetTargetSlot(int index)
	{
		targetIndex = Mathf.Clamp(index, 0, maxLength - 1);
		Game.SaveNum = targetIndex;
		manageKeyGuideObjects.ForEach(delegate(GameObject ob)
		{
			ob.SetActive(!TargetSlot.SaveData.IsNewSaveData);
		});
	}

	public string ControllerName()
	{
		return base.gameObject.name;
	}

	public async UniTask Activate()
	{
		saveSlots.Where((SaveSlot ss) => ss.gameObject.activeSelf).ForEach(delegate(SaveSlot ss)
		{
			ss.Refresh(instant: true);
		});
		SetTargetSlot(0);
		slotGroupRect.anchoredPosition = Vector2.zero;
		base.gameObject.SetActive(value: true);
		TargetSlot.Focus(value: true, instant: true);
		await UniTask.CompletedTask;
	}

	public async void Control()
	{
		int horizontalDownUI = InputManager.horizontalDownUI;
		if (horizontalDownUI != 0)
		{
			SaveSlot targetSlot = TargetSlot;
			SetTargetSlot(targetIndex + horizontalDownUI);
			if ((object)targetSlot != TargetSlot)
			{
				SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
				targetSlot.Focus(value: false);
				TargetSlot.Focus(value: true);
				await slotGroupRect.DOScrollXToTarget(TargetSlot.Rect, GroupParentRect, CanvasScaler, fadeDuration, LayoutGroup.padding.left).WaitAsync();
			}
			return;
		}
		if (InputManager.confirmDown)
		{
			SaveData saveData = TargetSlot.SaveData;
			if (saveData?.IsNewSaveData ?? true)
			{
				saveData = new SaveData();
				Game.DeleteSave(Game.SaveNum);
				TitleManager.DifficultyPanel.SetSaveData(saveData);
				InputManager.StackControlTarget(TitleManager.DifficultyPanel).Forget();
			}
			else
			{
				StartGame(saveData);
			}
		}
		if (InputManager.detail)
		{
			if (!(TargetSlot.SaveData?.IsNewSaveData ?? true))
			{
				ConfirmModel model = new ConfirmModel("DeleteSave", delegate
				{
					Game.DeleteSave(Game.SaveNum);
				});
				ConfirmPopup.SetModel(model);
				InputManager.StackControlTarget(ConfirmPopup).Forget();
			}
		}
		else if (InputManager.menuDown || InputManager.cancelDown)
		{
			InputManager.RemoveControlTarget(this).Forget();
			SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Interaction");
		}
	}

	private void StartGame(SaveData saveData)
	{
		saveData.StartRecordPlayTime();
		SceneDataModel model = (saveData.IsNewSaveData ? SceneDataModel.NewGameSceneDataModel() : SceneDataModel.RespawnModel(saveData.LastFieldID));
		StartGameWithModel(model).Forget();
		SoundManager.PlayOneShot("event:/SFX/UI/System/SFX_UI_System_GameStart");
	}

	public async UniTask Pause()
	{
		await UniTask.CompletedTask;
	}

	public async UniTask Restart()
	{
		Initialize();
		saveSlots.Where((SaveSlot ss) => ss.gameObject.activeSelf).ForEach(delegate(SaveSlot ss)
		{
			ss.Refresh(instant: false);
		});
		SetTargetSlot(targetIndex);
		TargetSlot.Focus(value: true, instant: true);
		await UniTask.CompletedTask;
	}

	public async UniTask Deactivate()
	{
		base.gameObject.SetActive(value: false);
		SaveSlot[] array = saveSlots;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Focus(value: false, instant: true);
		}
		await UniTask.CompletedTask;
	}

	private async UniTask StartGameWithModel(SceneDataModel model)
	{
		await InputManager.RemoveControlTarget(this);
		await InputManager.RemoveControlTarget(TitleManager.TitlePanel);
		Game.Instance.LoadFieldAsync(model).Forget();
	}
}
