using UnityEngine;

namespace SPR.Field;

public class GroundMaterialSelector : MonoBehaviour, IGround
{
	[SerializeField]
	private SoundManager.GroundMaterialType groundMaterial;

	public SoundManager.GroundMaterialType GroundMaterial => groundMaterial;
}
