using System.Collections.Generic;
using UnityEngine;

namespace SPR;

[CreateAssetMenu(fileName = "FMODAsset.asset", menuName = "Sound Asset/BGM Asset")]
public class BGMAsset : FMODAsset
{
	[SerializeField]
	private List<SoundManager.Sound3DAttributeInfo> sound3DAttributeInfos = new List<SoundManager.Sound3DAttributeInfo>();

	public IReadOnlyList<SoundManager.Sound3DAttributeInfo> Sound3DAttributeInfos => sound3DAttributeInfos;
}
