using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SPR.Field;

public class MovingPlatformLever : InteractableLever
{
	[SerializeField]
	private MovingPlatformController controller;

	[SerializeField]
	private CinemachineVirtualCamera vcam;

	[SerializeField]
	private Light2D light2D;

	[SerializeField]
	private SpriteRenderer lightRenderer;

	[SerializeField]
	private Animator lightAnimator;

	[SerializeField]
	private int positionIndex;

	private float originIntensity;

	private static readonly int State = Animator.StringToHash("State");

	public override string ID => $"MovingPlatformLever_{Game.Instance.CurrentFieldID.ToString()}_{base.transform.position.ToVector2Int()}";

	protected override bool CanInteractOnce => false;

	private void Awake()
	{
		originIntensity = light2D.intensity;
	}

	public override void LoadState(int state)
	{
		base.LoadState(state);
		InteractDistance = ((state == 0) ? (-2f) : 2f);
		base.Animator.SetInteger(State, state);
		SetLightActive(CurrentState == 0);
	}

	protected override async UniTask OnActivateAsync()
	{
		ChangeState(CurrentState ^ 1);
		InteractDistance = ((CurrentState == 0) ? (-2f) : 2f);
		int index = ((CurrentState == 0) ? (positionIndex - 1) : positionIndex);
		CinemachineVirtualCamera interactVcam = ((CurrentState == 0) ? null : vcam);
		await Game.Instance.InputManager.BlockControlForTask(controller.MoveAsync(interactVcam, index), "Moving Platform");
		base.Animator.SetInteger(State, CurrentState);
		SetLightActive(CurrentState == 0);
		await ReturnHandleAsync((CurrentState == 0) ? 0f : 90f);
	}

	protected override async UniTask OnCancelAsync()
	{
		SetLightActive(CurrentState == 0);
		await ReturnHandleAsync((CurrentState == 0) ? 0f : 90f);
	}

	private void SetLightActive(bool value)
	{
		float duration = 0.25f;
		lightRenderer.DOAlpha(value ? 1f : 0f, duration);
		light2D.DoLightIntensity(value ? originIntensity : 0f, duration);
		lightAnimator.SetBool("Active", value);
	}
}
