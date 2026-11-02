using System;
using Cinemachine;
using Library;
using UnityEngine;

namespace SPR;

[Serializable]
public class CameraPanningController
{
	[Header("Configuration")]
	[SerializeField]
	private float moveSpeed = 5f;

	[SerializeField]
	private float returnSpeed = 8f;

	[SerializeField]
	private Vector2 maxOffset = new Vector2(5f, 3f);

	private Camera mainCamera;

	private CinemachineBrain cineBrain;

	private CinemachineVirtualCamera currentVcam;

	private CinemachineCameraOffset cachedOffsetComp;

	private IVcamConfiner cachedArea;

	private FieldController cachedFieldController;

	private Vector2 currentOffset;

	private Vector2? panningAnchor;

	private Camera MainCamera
	{
		get
		{
			if (!mainCamera.IsNull())
			{
				return mainCamera;
			}
			return mainCamera = Camera.main;
		}
	}

	private CinemachineBrain CineBrain
	{
		get
		{
			if (!cineBrain.IsNull())
			{
				return cineBrain;
			}
			return cineBrain = MainCamera.GetComponent<CinemachineBrain>();
		}
	}

	private static PlayerMover PlayerMover => Game.Instance.Player.mover;

	private static PlayerStateMachine PSM => PlayerMover.PSM;

	private static PlayerControlParameter PCP => PlayerControlParameter.Instance;

	public void Control(Vector2 sightInput)
	{
		if (CineBrain.IsNull() || CineBrain.IsBlending)
		{
			return;
		}
		CinemachineVirtualCamera cinemachineVirtualCamera = CineBrain.ActiveVirtualCamera as CinemachineVirtualCamera;
		if (!cinemachineVirtualCamera.IsNull())
		{
			if (currentVcam != cinemachineVirtualCamera)
			{
				UpdateContext(cinemachineVirtualCamera);
			}
			ProcessPanning(sightInput);
		}
	}

	private void UpdateContext(CinemachineVirtualCamera vcam)
	{
		currentVcam = vcam;
		cachedOffsetComp = vcam.gameObject.AddOrGetComponent<CinemachineCameraOffset>();
		cachedArea = vcam.transform.parent?.GetComponent<IVcamConfiner>();
		cachedFieldController = Game.Instance.FieldController;
		panningAnchor = null;
		currentOffset = Vector2.zero;
	}

	private void ProcessPanning(Vector2 sightInput)
	{
		bool num = PSM.CurrentState == PSM.MoveSightState;
		bool flag = sightInput.sqrMagnitude > PCP.MoveSightInputThreshold;
		Vector2 vector;
		if (num && flag)
		{
			Vector2 valueOrDefault = panningAnchor.GetValueOrDefault();
			if (!panningAnchor.HasValue)
			{
				valueOrDefault = (Vector2)MainCamera.transform.position - currentOffset;
				panningAnchor = valueOrDefault;
			}
			Vector2 rawTargetPos = PlayerMover.VisualCenter + new Vector2(sightInput.x * maxOffset.x, sightInput.y * maxOffset.y);
			vector = CalculateTargetOffset(sightInput, rawTargetPos, panningAnchor.Value);
		}
		else
		{
			panningAnchor = null;
			vector = Vector2.zero;
		}
		float num2 = ((vector == Vector2.zero) ? returnSpeed : moveSpeed);
		currentOffset = Vector2.Lerp(currentOffset, vector, Time.deltaTime * num2);
		ApplyOffsetToComponent();
	}

	private Vector2 CalculateTargetOffset(Vector2 sightInput, Vector2 rawTargetPos, Vector2 panningAnchor)
	{
		Vector2 result = new Vector2(sightInput.x * maxOffset.x, sightInput.y * maxOffset.y);
		if (cachedArea == null || cachedArea.ConfineLookAround)
		{
			return result;
		}
		if (!cachedFieldController.IsNull() && !cachedFieldController.MainArea.IsNull())
		{
			return CalculateSafePosition(rawTargetPos, cachedFieldController.MainArea.ConfineCollider) - panningAnchor;
		}
		return result;
	}

	private Vector2 CalculateSafePosition(Vector2 targetPos, Collider2D confineCollider)
	{
		if (confineCollider.IsNull())
		{
			return targetPos;
		}
		float orthographicSize = currentVcam.m_Lens.OrthographicSize;
		float num = (MainCamera.IsNull() ? 1.7777778f : MainCamera.aspect);
		float num2 = orthographicSize * num;
		float num3 = orthographicSize;
		Bounds bounds = confineCollider.bounds;
		float num4 = bounds.min.x + num2;
		float num5 = bounds.max.x - num2;
		float num6 = bounds.min.y + num3;
		float num7 = bounds.max.y - num3;
		if (num4 > num5)
		{
			num4 = (num5 = bounds.center.x);
		}
		if (num6 > num7)
		{
			num6 = (num7 = bounds.center.y);
		}
		return new Vector2(Mathf.Clamp(targetPos.x, num4, num5), Mathf.Clamp(targetPos.y, num6, num7));
	}

	private void ApplyOffsetToComponent()
	{
		if (!cachedOffsetComp.IsNull())
		{
			cachedOffsetComp.m_Offset = currentOffset;
			bool flag = cachedArea != null && cachedArea.ConfineLookAround;
			cachedOffsetComp.m_ApplyAfter = (flag ? CinemachineCore.Stage.Aim : CinemachineCore.Stage.Finalize);
		}
	}
}
