using System.Globalization;
using TMPro;
using UnityEngine;

namespace SPR.UI.Menu;

public class StatusLabel : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI statusText;

	public void SetLevel(int level)
	{
		statusText.text = $"{level}";
	}

	public void SetData(float value)
	{
		statusText.text = value.ToString(CultureInfo.InvariantCulture);
	}

	public void SetData(string value)
	{
		statusText.text = value;
	}

	public void SetFillData(float current, float max)
	{
		statusText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
	}

	public void SetFillData(float current, float max, float additional)
	{
		statusText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)} ({GetAdditionalString(additional)})";
	}

	public void SetPowerData(float value, bool useFloat)
	{
		value = (useFloat ? value : ((float)Mathf.FloorToInt(value)));
		statusText.text = $"{value}";
	}

	public void SetPowerData(float value, float additional = 0f)
	{
		statusText.text = $"{value} ({GetAdditionalString(additional)})";
	}

	private string GetAdditionalString(float value)
	{
		if (!(value > 0f))
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}
		return $"+{value}";
	}
}
