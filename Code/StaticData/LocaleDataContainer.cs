using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocaleDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<LocaleData> LocaleDataList { get; private set; }

	[field: SerializeField]
	public List<LocaleData> ItemLocaleDataList { get; private set; }

	[field: SerializeField]
	public List<LocaleData> StoreDataList { get; private set; }

	[field: SerializeField]
	public List<LocaleData> FieldInteractDataList { get; private set; }

	[field: SerializeField]
	public List<LocaleData> MemoryDataList { get; private set; }

	[field: SerializeField]
	public List<LocaleData> AchievementDataList { get; private set; }
}
