using System.Collections.Generic;
using UnityEngine;

namespace SPR;

[CreateAssetMenu(fileName = "FMODAsset.asset", menuName = "Sound Asset/FMOD Asset")]
public class FMODAsset : ScriptableObject
{
	[SerializeField]
	private List<SoundManager.SectorEventInfo> sectorEventInfos = new List<SoundManager.SectorEventInfo>();

	[SerializeField]
	private List<SoundManager.SpecialEventInfo> specialEventInfos = new List<SoundManager.SpecialEventInfo>();

	public IReadOnlyList<SoundManager.SectorEventInfo> SectorEventInfos => sectorEventInfos;

	public IReadOnlyList<SoundManager.SpecialEventInfo> SpecialEventInfos => specialEventInfos;
}
