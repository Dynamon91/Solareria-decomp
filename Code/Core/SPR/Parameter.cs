using System;
using DG.Tweening;
using UnityEngine;

namespace SPR;

[Serializable]
public class Parameter
{
	public enum Type
	{
		String,
		Int,
		Float,
		Bool,
		Ease
	}

	[SerializeField]
	private Type type;

	[SerializeField]
	private string description;

	[SerializeField]
	private string stringValue;

	[SerializeField]
	private int intValue;

	[SerializeField]
	private float floatValue;

	[SerializeField]
	private bool boolValue;

	[SerializeField]
	private Ease easeValue = Ease.Linear;

	public Type ValueType => type;

	public string String => stringValue;

	public int Int => intValue;

	public float Float => floatValue;

	public bool Bool => boolValue;

	public Ease Ease => easeValue;
}
