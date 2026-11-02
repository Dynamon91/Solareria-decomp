using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SPR.Save;
using StaticData;
using UnityEngine;

namespace SPR.UI.Title;

public class DifficultyPanel : MonoBehaviour, IControllable
{
	[SerializeField]
	private Transform slotsParent;

	private List<DifficultySlot> slots;

	private int targetIndex;

	private SaveData targetSaveData;

	private DifficultySlot TargetSlot => slots[targetIndex];

	private Difficulty TargetDifficulty => TargetSlot.Difficulty;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private static FadeCanvas FadeCanvas => Game.Instance.UIManager.FadeCanvas;

	private static ConfirmPopup ConfirmPopup => Game.Instance.UIManager.FieldCanvas.ConfirmPopup;

	public bool IsValid => true;

	public void Initialize()
	{
		slots = slotsParent.GetComponentsInChildren<DifficultySlot>(includeInactive: true).ToList();
	}

	public void SetSaveData(SaveData saveData)
	{
		targetSaveData = saveData;
	}

	private void StartGame()
	{
		Game.Instance.SetDifficulty(TargetDifficulty, settingInGame: false);
		StartGame(targetSaveData);
	}

	private void StartGame(SaveData saveData)
	{
		saveData.StartRecordPlayTime();
		SceneDataModel model = (saveData.IsNewSaveData ? SceneDataModel.NewGameSceneDataModel() : SceneDataModel.RespawnModel(saveData.LastFieldID));
		LoadFieldAsync(model).Forget();
		SoundManager.PlayOneShot("event:/SFX/UI/System/SFX_UI_System_GameStart");
	}

	private async UniTask LoadFieldAsync(SceneDataModel model)
	{
		await FadeCanvas.FadeOut(0.4f, 0.2f);
		await InputManager.RemoveControlTarget(this);
		await InputManager.RemoveControlTarget(Game.Instance.TitleManager.SaveSlotPanel);
		await InputManager.RemoveControlTarget(Game.Instance.TitleManager.TitlePanel);
		Game.Instance.LoadFieldAsync(model).Forget();
	}

	public string ControllerName()
	{
		return base.gameObject.name;
	}

	public async UniTask Activate()
	{
		targetIndex = slots.FindIndex((DifficultySlot s) => s.Difficulty == Difficulty.Normal);
		for (int num = 0; num < slots.Count; num++)
		{
			DifficultySlot difficultySlot = slots[num];
			difficultySlot.SetData();
			difficultySlot.Focus(num == targetIndex);
		}
		base.gameObject.SetActive(value: true);
		SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Confirm");
	}

	public void Control()
	{
		int horizontalDownUI = InputManager.horizontalDownUI;
		if (horizontalDownUI != 0)
		{
			TargetSlot.Focus(value: false);
			int num = Mathf.Clamp(targetIndex + horizontalDownUI, 0, slots.Count - 1);
			if (num != targetIndex)
			{
				SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Move");
			}
			targetIndex = num;
			TargetSlot.Focus(value: true);
		}
		else if (InputManager.confirmDown)
		{
			ConfirmPopup.SetModel(new ConfirmModel("Difficulty", "Difficulty_Start", StartGame));
			InputManager.StackControlTarget(ConfirmPopup).Forget();
		}
		else if (InputManager.menuDown || InputManager.cancelDown)
		{
			InputManager.RemoveControlTarget(this).Forget();
			SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Confirm");
		}
	}

	public UniTask Pause()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Restart()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Deactivate()
	{
		targetSaveData = null;
		base.gameObject.SetActive(value: false);
		return UniTask.CompletedTask;
	}
}
