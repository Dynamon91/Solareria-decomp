using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<EnemyData> EnemyDataList { get; private set; }

	[field: SerializeField]
	public List<EnemyStatData> EnemyStatDataList { get; private set; }

	[field: SerializeField]
	public List<Enemy2Data> Enemy2DataList { get; private set; }

	[field: SerializeField]
	public List<EnemyStat2Data> EnemyStat2DataList { get; private set; }

	[field: SerializeField]
	public List<Enemy3Data> Enemy3DataList { get; private set; }

	[field: SerializeField]
	public List<EnemyStat3Data> EnemyStat3DataList { get; private set; }

	[field: SerializeField]
	public List<EnemyWetData> EnemyWetDataList { get; private set; }

	[field: SerializeField]
	public List<EnemySporeData> EnemySporeDataList { get; private set; }

	[field: SerializeField]
	public List<EnemyDropData> EnemyDropDataList { get; private set; }

	[field: SerializeField]
	public List<EnemyDrop2Data> EnemyDrop2DataList { get; private set; }

	[field: SerializeField]
	public List<EnemyDrop3Data> EnemyDrop3DataList { get; private set; }

	[field: SerializeField]
	public List<EnemyMultiplierData> EnemyMultiplierDataList { get; private set; }
}
