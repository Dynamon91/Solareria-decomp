using UnityEngine;
using UnityEngine.UI;

namespace VinTools.BetterRuleTiles.Sample;

public class CustomPropertyCanvas : MonoBehaviour
{
	public CustomPropertyTest propertyTest;

	public Text valueText;

	private void OnEnable()
	{
		if (valueText == null)
		{
			valueText = Object.FindObjectOfType<Text>();
		}
		if (propertyTest == null)
		{
			propertyTest = Object.FindObjectOfType<CustomPropertyTest>();
		}
		if (propertyTest != null)
		{
			propertyTest.onReadValue += DisplayValue;
		}
	}

	private void OnDisable()
	{
		if (propertyTest != null)
		{
			propertyTest.onReadValue -= DisplayValue;
		}
	}

	private void DisplayValue(string msg)
	{
		valueText.text = "Move your cursor over the tile to see it's values.\n\n" + msg;
	}
}
