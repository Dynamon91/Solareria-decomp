using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkillData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public List<float> Coefficients { get; private set; }

	[field: SerializeField]
	public List<float> GroggyCoefficients { get; private set; }

	[field: SerializeField]
	public List<float> FireCoefficients { get; private set; }
}
