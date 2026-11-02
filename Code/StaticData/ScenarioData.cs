using System;
using UnityEngine;

[Serializable]
public class ScenarioData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public string GroupID { get; private set; }

	[field: SerializeField]
	public int Index { get; private set; }

	[field: SerializeField]
	public ScriptType ScriptType { get; private set; }

	[field: SerializeField]
	public string Actor { get; private set; }

	[field: SerializeField]
	public string Option { get; private set; }

	[field: SerializeField]
	public string VoiceKey { get; private set; }

	[field: SerializeField]
	public string KO { get; private set; }

	[field: SerializeField]
	public string EN { get; private set; }

	[field: SerializeField]
	public string JA { get; private set; }

	[field: SerializeField]
	public string ZH_HANT { get; private set; }

	[field: SerializeField]
	public string ZH_HANS { get; private set; }

	[field: SerializeField]
	public string DE { get; private set; }

	[field: SerializeField]
	public string ES { get; private set; }

	[field: SerializeField]
	public string PT { get; private set; }

	[field: SerializeField]
	public string FR { get; private set; }
}
