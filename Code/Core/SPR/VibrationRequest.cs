using System;
using UnityEngine;

namespace SPR;

[Serializable]
public struct VibrationRequest
{
	public Guid Id { get; private set; }

	public float LowMotor { get; private set; }

	public float HighMotor { get; private set; }

	public float EndTime { get; private set; }

	public VibrationRequest(float lowMotor, float highMotor, float duration)
	{
		Id = Guid.NewGuid();
		LowMotor = lowMotor;
		HighMotor = highMotor;
		EndTime = Time.time + duration;
	}
}
