using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Library;
using Rewired;
using SPR.Library;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SPR;

public class InputManager : Manager
{
	private readonly List<IControllable> _controlTargetStack = new List<IControllable>();

	private bool _readyToClearPlatformInput;

	[Header("Platform Input")]
	[SerializeField]
	private int debugHorizontal;

	public float horizontal;

	public int vertical;

	public bool jumpDown;

	public bool jumpUp;

	public bool attackDown;

	public bool attackUp;

	public bool attack;

	public bool dashDown;

	public bool dashUp;

	public bool healDown;

	public bool skillA;

	public bool skillB;

	public bool overheat;

	public bool action;

	public bool heat;

	public bool guardDown;

	public bool guardUp;

	public bool specialParry;

	public bool heatDown;

	public bool heatUp;

	public bool pauseDown;

	public float sightHorizontal;

	public float sightVertical;

	public bool ctrl;

	[Header("UI Input")]
	public int horizontalDownUI;

	public int horizontalUI;

	public int verticalDownUI;

	public int verticalUI;

	public float scroll;

	public bool confirmDown;

	public bool confirm;

	public bool confirmLong;

	public float confirmPressedTime;

	public bool interactDown;

	public int categoryUI;

	public bool menu;

	public bool menuDown;

	public bool cancelDown;

	public bool map;

	public bool detail;

	public int subCategory;

	public bool reset;

	public bool mode;

	public bool teleportLibrary;

	[Header("Values")]
	[SerializeField]
	private float mouseWheelSensitivity;

	[SerializeField]
	private float repeatRate;

	[SerializeField]
	private float repeatDelay;

	[SerializeField]
	private float verticalPositiveThreshold;

	[SerializeField]
	private float verticalNegativeThreshold;

	private float _hTimer;

	private bool _hDelayed;

	private float _vTimer;

	private bool _vDelayed;

	private int _pHorizontalUI;

	private int _pVerticalUI;

	public IControllable ControlTarget
	{
		get
		{
			if (_controlTargetStack.Count == 0)
			{
				return null;
			}
			List<IControllable> controlTargetStack = _controlTargetStack;
			if (!controlTargetStack[controlTargetStack.Count - 1].IsValid)
			{
				CleanUpControlStack();
				return ControlTarget;
			}
			List<IControllable> controlTargetStack2 = _controlTargetStack;
			return controlTargetStack2[controlTargetStack2.Count - 1];
		}
	}

	private float period => 1f / repeatRate;

	public bool AnyAxisInput
	{
		get
		{
			if (horizontal == 0f && vertical == 0 && sightHorizontal == 0f && sightVertical == 0f && horizontalUI == 0 && verticalUI == 0 && horizontalDownUI == 0 && verticalDownUI == 0 && categoryUI == 0)
			{
				return scroll != 0f;
			}
			return true;
		}
	}

	public bool RawAttackDown { get; private set; }

	public bool RawGuardDown { get; private set; }

	public bool RawHeatDown { get; private set; }

	public static InputMapper InputMapperKeyboard { get; } = new InputMapper();

	public static InputMapper InputMapperMouse { get; } = new InputMapper();

	public static InputMapper InputMapperGamepad { get; } = new InputMapper();

	public void Update()
	{
		if (_readyToClearPlatformInput)
		{
			ClearPlatformInput();
		}
		if (Application.isFocused)
		{
			ProcessInputs();
			ProcessUIInputs();
			ControlTarget?.Control();
			if (ReInput.controllers.GetAnyButtonDown() || AnyAxisInput)
			{
				RewiredHelper.CheckOtherControllerInput();
			}
			ClearUIInput();
			ScreenshotInput();
			CheckUIAxisRepeat();
		}
	}

	private void FixedUpdate()
	{
		if (Application.isFocused)
		{
			ProcessInputs();
			if (ControlTarget is PlayerController)
			{
				ControlTarget?.Control();
			}
			if (ReInput.controllers.GetAnyButtonDown() || AnyAxisInput)
			{
				RewiredHelper.CheckOtherControllerInput();
			}
			_readyToClearPlatformInput = true;
		}
	}

	protected override void OnInitialize()
	{
		RewiredHelper.Player.controllers.maps.SetAllMapsEnabled(state: true);
		InputMapperKeyboard.InputMappedEvent += OnInputMapped;
		InputMapperKeyboard.StoppedEvent += OnStopMapping;
		InputMapperMouse.InputMappedEvent += OnInputMapped;
		InputMapperMouse.StoppedEvent += OnStopMapping;
		InputMapperGamepad.InputMappedEvent += OnInputMapped;
		InputMapperGamepad.StoppedEvent += OnStopMapping;
		InputMapperKeyboard.options.checkForConflicts = false;
		InputMapperKeyboard.options.allowKeyboardKeysWithModifiers = false;
		InputMapperKeyboard.options.allowKeyboardModifierKeyAsPrimary = true;
		InputMapperMouse.options.checkForConflicts = false;
		InputMapperMouse.options.ignoreMouseXAxis = true;
		InputMapperMouse.options.ignoreMouseYAxis = true;
		InputMapperGamepad.options.checkForConflicts = false;
	}

	public override UniTask OnSceneLoaded(Scene loadedScene)
	{
		ClearPlatformInput();
		ClearUIInput();
		return default(UniTask);
	}

	public bool IsCurrentControlTarget(IControllable controllable)
	{
		return ControlTarget == controllable;
	}

	public bool ContainControlTarget(Type controllableType)
	{
		return _controlTargetStack.Any((IControllable c) => c != null && (c.GetType() == controllableType || c.GetType().IsSubclassOf(controllableType)));
	}

	private void ClearPlatformInput()
	{
		horizontal = 0f;
		vertical = 0;
		jumpDown = false;
		jumpUp = false;
		attackDown = false;
		attackUp = false;
		attack = false;
		guardDown = false;
		guardUp = false;
		dashDown = false;
		dashUp = false;
		healDown = false;
		skillA = false;
		skillB = false;
		overheat = false;
		action = false;
		heat = false;
		specialParry = false;
		heatDown = false;
		heatUp = false;
		pauseDown = false;
		sightHorizontal = 0f;
		sightVertical = 0f;
		ctrl = false;
		_readyToClearPlatformInput = false;
	}

	private void ClearUIInput()
	{
		_pHorizontalUI = horizontalUI;
		_pVerticalUI = verticalUI;
		horizontalDownUI = 0;
		verticalDownUI = 0;
		horizontalUI = 0;
		verticalUI = 0;
		categoryUI = 0;
		scroll = 0f;
		confirmDown = false;
		confirm = false;
		confirmLong = false;
		interactDown = false;
		menu = false;
		menuDown = false;
		cancelDown = false;
		map = false;
		detail = false;
		pauseDown = false;
		subCategory = 0;
		reset = false;
		mode = false;
		teleportLibrary = false;
	}

	private void ProcessInputs()
	{
		if (debugHorizontal == 0)
		{
			horizontal = GetAbsMax(horizontal, RewiredHelper.Player.GetAxisRaw("Horizontal"));
		}
		else
		{
			horizontal = debugHorizontal;
		}
		vertical = ConvertToInt((float)vertical + RewiredHelper.Player.GetAxisRaw("Vertical"), verticalPositiveThreshold, verticalNegativeThreshold);
		jumpDown = jumpDown || RewiredHelper.Player.GetButtonDown("Jump");
		jumpUp = jumpUp || RewiredHelper.Player.GetButtonUp("Jump");
		dashDown = dashDown || RewiredHelper.Player.GetButtonDown("Dash");
		dashUp = dashUp || RewiredHelper.Player.GetButtonUp("Dash");
		healDown = healDown || RewiredHelper.Player.GetButtonDown("Heal");
		ctrl = ctrl || RewiredHelper.Player.GetButton("Ctrl");
		RawAttackDown = RewiredHelper.Player.GetButtonDown("Attack");
		RawGuardDown = RewiredHelper.Player.GetButtonDown("Guard");
		RawHeatDown = RewiredHelper.Player.GetButtonDown("Heat");
		attack = attack || RewiredHelper.Player.GetButton("Attack");
		attackDown = !ctrl && (attackDown || RawAttackDown);
		attackUp = attackUp || RewiredHelper.Player.GetButtonUp("Attack");
		guardDown = !ctrl && (guardDown || RawGuardDown);
		guardUp = guardUp || RewiredHelper.Player.GetButtonUp("Guard");
		skillA = skillA || RewiredHelper.Player.GetButtonDown("SkillA") || (ctrl && RawAttackDown);
		skillB = skillB || RewiredHelper.Player.GetButtonDown("SkillB") || (ctrl && RawGuardDown);
		action = action || RewiredHelper.Player.GetButtonDown("Action");
		heat = heat || RewiredHelper.Player.GetButton("Heat");
		if (RewiredHelper.IsJoystickConnected)
		{
			overheat = overheat || (RewiredHelper.Player.GetButtonDown("Heat") && RewiredHelper.Player.GetButton("Guard")) || (RewiredHelper.Player.GetButton("Heat") && RewiredHelper.Player.GetButtonDown("Guard"));
			specialParry = specialParry || (RewiredHelper.Player.GetButton("Guard") && RewiredHelper.Player.GetButtonDown("Action")) || (RewiredHelper.Player.GetButtonDown("Guard") && RewiredHelper.Player.GetButton("Action"));
		}
		else
		{
			overheat = overheat || (RewiredHelper.Player.GetButton("Heat") && RewiredHelper.Player.GetButtonDown("Attack")) || (RewiredHelper.Player.GetButtonDown("Heat") && RewiredHelper.Player.GetButton("Attack"));
			specialParry = specialParry || (RewiredHelper.Player.GetButtonDown("Guard") && RewiredHelper.Player.GetButton("Heat")) || (RewiredHelper.Player.GetButton("Guard") && RewiredHelper.Player.GetButtonDown("Heat"));
		}
		heatDown = heatDown || RawHeatDown;
		heatUp = RewiredHelper.Player.GetButtonUp("Heat");
		sightHorizontal = RewiredHelper.Player.GetAxisRaw("SightHorizontal");
		sightVertical = RewiredHelper.Player.GetAxisRaw("SightVertical");
	}

	private void ProcessUIInputs()
	{
		horizontalUI = ConvertToInt(RewiredHelper.Player.GetAxisRaw("HorizontalUI"), 0.71f);
		verticalUI = ConvertToInt(RewiredHelper.Player.GetAxisRaw("VerticalUI"), 0.71f);
		horizontalDownUI = ((_pHorizontalUI == 0 && _pVerticalUI == 0) ? horizontalUI : 0);
		verticalDownUI = ((_pHorizontalUI == 0 && _pVerticalUI == 0) ? verticalUI : 0);
		bool buttonRepeating = RewiredHelper.Player.GetButtonRepeating("NextCategoryUI");
		bool buttonRepeating2 = RewiredHelper.Player.GetButtonRepeating("PreviousCategoryUI");
		categoryUI = (buttonRepeating ? 1 : (buttonRepeating2 ? (-1) : 0));
		bool buttonDown = RewiredHelper.Player.GetButtonDown("SubPreviousCategory");
		bool buttonDown2 = RewiredHelper.Player.GetButtonDown("SubNextCategory");
		subCategory = (buttonDown2 ? 1 : (buttonDown ? (-1) : 0));
		confirmDown = RewiredHelper.Player.GetButtonDown("ConfirmUI");
		confirm = RewiredHelper.Player.GetButton("ConfirmUI");
		confirmLong = RewiredHelper.Player.GetButtonLongPress("ConfirmUI");
		confirmPressedTime = (float)RewiredHelper.Player.GetButtonTimePressed("ConfirmUI");
		menu = RewiredHelper.Player.GetButton("Menu");
		menuDown = RewiredHelper.Player.GetButtonDown("Menu");
		scroll = RewiredHelper.Player.GetAxisRaw("Scroll") * mouseWheelSensitivity;
		map = RewiredHelper.Player.GetButtonDown("Map");
		detail = RewiredHelper.Player.GetButtonDown("Detail");
		reset = RewiredHelper.Player.GetButtonDown("Reset");
		mode = RewiredHelper.Player.GetButtonDown("Mode");
		cancelDown = RewiredHelper.Player.GetButtonDown("Cancel");
		pauseDown = RewiredHelper.Player.GetButtonDown("Pause");
		teleportLibrary = RewiredHelper.Player.GetButtonDown("Teleport");
		if (RewiredHelper.IsJoystickConnected)
		{
			interactDown = RewiredHelper.Player.GetButtonDown("Heat");
		}
		else
		{
			interactDown = RewiredHelper.Player.GetButtonDown("Action");
		}
	}

	public async UniTask StackControlTarget(IControllable target)
	{
		if (target == null)
		{
			DebugLog.ControlStackLogError("Null을 ControlTarget으로 넣을 수 없습니다! 무시합니다.");
			return;
		}
		if (ControlTarget == target)
		{
			DebugLog.ControlStackLogWarning(target.ControllerName() + "이 또 들어왔습니다! 무시합니다.");
			return;
		}
		IControllable controlTarget = ControlTarget;
		NullControllable nullControllable = new NullControllable(target, NullControllable.Purpose.Stack);
		_controlTargetStack.Add(nullControllable);
		ClearPlatformInput();
		ClearUIInput();
		if (controlTarget != null)
		{
			await controlTarget.Pause();
		}
		await target.Activate();
		int num = FindControlIndexFromLast(nullControllable);
		if (num < 0)
		{
			_controlTargetStack.Add(target);
			DebugLog.ControlStackLogWarning(target.ControllerName() + "을 Stack할 때 Stack을 위한 NullControllable을 찾을 수 없습니다!! 일단 제일 마지막에 Stack 했습니다.");
		}
		else
		{
			_controlTargetStack[num] = target;
		}
		ControlStackLog();
	}

	public async UniTask RemoveControlTarget(IControllable target)
	{
		int num = _controlTargetStack.LastIndexOf(target);
		if (num < 0)
		{
			DebugLog.ControlStackLogError(target.ControllerName() + "은 현재 Control Stack에 없습니다.");
			return;
		}
		bool removeLast = num == _controlTargetStack.Count - 1;
		IControllable controllable = _controlTargetStack[num];
		IControllable previousTarget = ((num - 1 >= 0) ? _controlTargetStack[num - 1] : null);
		NullControllable nullControllable = new NullControllable(target, NullControllable.Purpose.Remove);
		_controlTargetStack.Add(nullControllable);
		ClearPlatformInput();
		ClearUIInput();
		await controllable.Deactivate();
		if (previousTarget != null && removeLast && !(previousTarget.GetType() == typeof(NullControllable)) && (!(previousTarget is Component obj) || !obj.IsNull()))
		{
			await previousTarget.Restart();
		}
		_controlTargetStack.Remove(target);
		_controlTargetStack.Remove(nullControllable);
		ControlStackLog();
	}

	public async UniTask SwitchControlTarget(IControllable removeTarget, IControllable addTarget)
	{
		if (addTarget == null)
		{
			DebugLog.ControlStackLogError("Null을 ControlTarget으로 넣을 수 없습니다! 무시합니다.");
			return;
		}
		if (ControlTarget == addTarget)
		{
			DebugLog.ControlStackLogWarning(addTarget.ControllerName() + "이 이미 Control Stack의 마지막 Target입니다. 무시합니다.");
			return;
		}
		int removeTargetIndex = _controlTargetStack.LastIndexOf(removeTarget);
		if (removeTargetIndex < 0)
		{
			DebugLog.ControlStackLogError(removeTarget.ControllerName() + "은 현재 Control Stack에 없습니다. 무시합니다.");
			return;
		}
		NullControllable nullControllable = new NullControllable(addTarget, NullControllable.Purpose.Switch);
		bool removeLast = removeTargetIndex == _controlTargetStack.Count - 1;
		_controlTargetStack.Add(nullControllable);
		ClearPlatformInput();
		ClearUIInput();
		await removeTarget.Deactivate();
		await addTarget.Activate();
		if (!removeLast)
		{
			await addTarget.Pause();
		}
		_controlTargetStack[removeTargetIndex] = addTarget;
		_controlTargetStack.Remove(nullControllable);
		ControlStackLog();
	}

	public bool ContainsInControlStack(IControllable target)
	{
		return _controlTargetStack.Any((IControllable controllable) => controllable == target);
	}

	private int FindControlIndexFromLast(IControllable controllable)
	{
		for (int num = _controlTargetStack.Count - 1; num >= 0; num--)
		{
			IControllable controllable2 = _controlTargetStack[num];
			if (!(controllable2.GetType() != controllable.GetType()))
			{
				if (controllable.GetType() != typeof(NullControllable))
				{
					return num;
				}
				NullControllable nullControllable = (NullControllable)controllable2;
				NullControllable nullControllable2 = (NullControllable)controllable;
				if (nullControllable.OwnerName == nullControllable2.OwnerName && nullControllable.PurposeOfNull == nullControllable2.PurposeOfNull)
				{
					return num;
				}
			}
		}
		return -1;
	}

	private void ControlStackLog()
	{
		StringBuilder stringBuilder = new StringBuilder("");
		foreach (IControllable item in _controlTargetStack)
		{
			stringBuilder.Append((item == null) ? " - Null" : (" - " + item.ControllerName()));
		}
		if (_controlTargetStack.Count > 0)
		{
			stringBuilder.Remove(0, 2);
		}
		stringBuilder.Insert(0, "Control Stack Log : ");
		DebugLog.ControlStackLog(stringBuilder.ToString());
	}

	public async UniTask BlockControlForTask(UniTask task, string taskName)
	{
		IControllable controlTarget = ControlTarget;
		NullControllable nullControllable = new NullControllable(taskName);
		_controlTargetStack.Add(nullControllable);
		ControlStackLog();
		ClearPlatformInput();
		ClearUIInput();
		if (controlTarget != null)
		{
			await controlTarget.Pause();
		}
		await UniTask.WaitUntil(() => task.Status == UniTaskStatus.Succeeded);
		_controlTargetStack.Remove(nullControllable);
		IControllable controlTarget2 = ControlTarget;
		if (controlTarget2 != null)
		{
			await controlTarget2.Restart();
		}
		ControlStackLog();
	}

	private void CleanUpControlStack()
	{
		if (_controlTargetStack.Count((IControllable controllable) => !controllable.IsValid) == 0)
		{
			return;
		}
		List<IControllable> list = _controlTargetStack.Where((IControllable controllable) => !controllable.IsValid).ToList();
		StringBuilder stringBuilder = new StringBuilder("Control Stack에서 유효하지 않은 Controllable들을 제거합니다. 제거된 Controllable : ");
		foreach (IControllable item in list)
		{
			stringBuilder.Append(item.ControllerName());
			stringBuilder.Append(", ");
		}
		DebugLog.ControlStackLog(stringBuilder.ToString());
		_controlTargetStack.RemoveAll((IControllable controllable) => !controllable.IsValid);
	}

	private int ConvertToInt(float rawValue, float zeroThreshold)
	{
		if (Mathf.Abs(rawValue) <= zeroThreshold)
		{
			return 0;
		}
		if (!(rawValue > 0f))
		{
			return -1;
		}
		return 1;
	}

	private int ConvertToInt(float rawValue, float positiveThreshold, float negativeThreshold)
	{
		if (negativeThreshold < rawValue && rawValue < positiveThreshold)
		{
			return 0;
		}
		if (!(rawValue > 0f))
		{
			return -1;
		}
		return 1;
	}

	private float GetAbsMax(float a, float b)
	{
		if (!(Mathf.Abs(a) > Mathf.Abs(b)))
		{
			return b;
		}
		return a;
	}

	private bool ProcessAwakening()
	{
		if (RewiredHelper.IsJoystickConnected)
		{
			if (ctrl)
			{
				return RewiredHelper.Player.GetButtonDown("SkillC");
			}
			return false;
		}
		return false;
	}

	private void ScreenshotInput()
	{
	}

	private void CheckUIAxisRepeat()
	{
		if (_pHorizontalUI == 0)
		{
			_hTimer = 0f;
			_hDelayed = false;
		}
		else
		{
			_hTimer += Time.deltaTime;
			float num = (_hDelayed ? period : repeatDelay);
			if (_hTimer > num)
			{
				_hDelayed = true;
				_hTimer -= num;
				_pHorizontalUI = 0;
			}
		}
		if (_pVerticalUI == 0)
		{
			_vTimer = 0f;
			_vDelayed = false;
			return;
		}
		_vTimer += Time.deltaTime;
		float num2 = (_vDelayed ? period : repeatDelay);
		if (_vTimer > num2)
		{
			_vDelayed = true;
			_vTimer -= num2;
			_pVerticalUI = 0;
		}
	}

	private static void OnInputMapped(InputMapper.InputMappedEventData eventData)
	{
		DebugLog.ControllerLog($"Remapped {eventData.actionElementMap.actionDescriptiveName} : {eventData.actionElementMap.keyCode}");
		ReInput.userDataStore.Save();
	}

	private static void OnStopMapping(InputMapper.StoppedEventData eventData)
	{
		DebugLog.ControllerLog("Mapping Stopped");
	}
}
