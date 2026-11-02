using System;
using UnityEngine;

[Serializable]
public class JoystickData : IUserData
{
	[SerializeField]
	private float horizontalSensitivity;

	[SerializeField]
	private float verticalUpSensitivity;

	[SerializeField]
	private float verticalDownSensitivity;

	public float HorizontalRaw => horizontalSensitivity;

	public float VerticalUpRaw => verticalUpSensitivity;

	public float VerticalDownRaw => verticalDownSensitivity;

	public float HorizontalSensitivity
	{
		get
		{
			return Round(horizontalSensitivity * 100f, isUserReading: true);
		}
		set
		{
			horizontalSensitivity = Round(value / 100f, isUserReading: false);
		}
	}

	public float VerticalUpSensitivity
	{
		get
		{
			return Round(verticalUpSensitivity * 100f, isUserReading: true);
		}
		set
		{
			verticalUpSensitivity = Round(value / 100f, isUserReading: false);
		}
	}

	public float VerticalDownSensitivity
	{
		get
		{
			return Round(verticalDownSensitivity * 100f, isUserReading: true);
		}
		set
		{
			verticalDownSensitivity = Round(value / 100f, isUserReading: false);
		}
	}

	public JoystickData()
	{
		horizontalSensitivity = 0.356f;
		verticalUpSensitivity = 0.289f;
		verticalDownSensitivity = -0.53f;
	}

	public void RefineDataOnInitialize()
	{
	}

	public bool IsValid()
	{
		return true;
	}

	private float Round(float inputValue, bool isUserReading)
	{
		return MathF.Round(inputValue, isUserReading ? 1 : 2, MidpointRounding.AwayFromZero);
	}
}
