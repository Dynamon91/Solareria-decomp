using System;
using UnityEngine;

namespace SPR;

[Serializable]
public struct MoveTiming
{
	[Tooltip("플레이어 위치 실시간 추적")]
	[SerializeField]
	private bool keepTrackPlayerPos;

	[Tooltip("플레이어간의 최대 거리")]
	[SerializeField]
	private float distance;

	[Tooltip("최대 이동 속도")]
	[SerializeField]
	private float speed;

	[SerializeField]
	private Timing timing;

	public bool KeepTrackPlayerPos => keepTrackPlayerPos;

	public float Distance => distance;

	public float Speed => speed;

	public int Start => timing.Start;

	public int End => timing.End;

	public int Duration => timing.Duration;
}
