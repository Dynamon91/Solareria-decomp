using System;
using UnityEngine;

[Serializable]
public struct TimeScaleEventParameter
{
	[Tooltip("이벤트 시작 후 타임 스케일 바꾸기까지의 프레임 수")]
	[SerializeField]
	private int predelayFrame;

	[Tooltip("변화된 Time Scale을 유지하는 프레임 수")]
	[SerializeField]
	private int changedScaleFrame;

	[Tooltip("Time Scale 목표. 기존 값은 1로, 1 미만은 느리게, 1 초과는 빠르게 시간이 흐릅니다.")]
	[SerializeField]
	private float changedScale;

	[Tooltip("Time Scale을 복구하는데 걸리는 프레임 수")]
	[SerializeField]
	private int resetFrame;

	[SerializeField]
	private int cameraMinAngle;

	[SerializeField]
	private int cameraMaxAngle;

	[SerializeField]
	private int cameraZoomSize;

	public int PredelayFrame => predelayFrame;

	public int ChangedScaleFrame => changedScaleFrame;

	public float ChangedScale => changedScale;

	public int ResetFrame => resetFrame;

	public int CameraMinAngle => cameraMinAngle;

	public int CameraMaxAngle => cameraMaxAngle;

	public int CameraZoomSize => cameraZoomSize;
}
