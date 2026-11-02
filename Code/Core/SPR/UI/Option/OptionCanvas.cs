using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.UI.Option;

public class OptionCanvas : AutoScaledCanvas, IControllable
{
	[SerializeField]
	private List<OptionPanel> optionPanels;

	private OptionPanel currentOptionPanel;

	private OptionCategory currentCategory;

	private static InputManager InputManager => Game.Instance.InputManager;

	public bool IsValid => true;

	public void Initialize()
	{
		optionPanels.ForEach(delegate(OptionPanel panel)
		{
			panel.Initialize();
		});
		base.gameObject.SetActive(value: false);
	}

	public void SetTargetCategory(OptionCategory category)
	{
		currentCategory = category;
		currentOptionPanel = optionPanels[(int)category];
	}

	public string ControllerName()
	{
		return "Option Canvas";
	}

	public UniTask Activate()
	{
		base.gameObject.SetActive(value: true);
		InputManager.StackControlTarget(currentOptionPanel).Forget();
		return UniTask.CompletedTask;
	}

	public void Control()
	{
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
		base.gameObject.SetActive(value: false);
		return UniTask.CompletedTask;
	}

	public async UniTaskVoid DeactivateForce()
	{
		await InputManager.RemoveControlTarget(currentOptionPanel);
		await InputManager.RemoveControlTarget(this);
	}

	public async UniTask ActivateInputSettingPanel(OptionCategory category)
	{
		if (category >= OptionCategory.GamepadDefault)
		{
			SetTargetCategory(category);
			InputManager.StackControlTarget(currentOptionPanel).Forget();
		}
	}
}
