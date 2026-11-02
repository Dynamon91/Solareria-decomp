using System.Collections.Generic;
using UnityEngine;

namespace SPR;

[CreateAssetMenu(fileName = "FMODAsset.asset", menuName = "Sound Asset/AMB Asset")]
public class AMBAsset : FMODAsset
{
	[SerializeField]
	private List<FieldID> withoutDefaultAMBScenes = new List<FieldID>();

	public IReadOnlyList<FieldID> WithoutDefaultAMBScenes => withoutDefaultAMBScenes;
}
