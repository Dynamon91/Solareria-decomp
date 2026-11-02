using SPR.Field;
using UnityEngine;

namespace SPR;

public class ElevatorCeiling : MonoBehaviour, IRideable, IGround
{
	[SerializeField]
	private FieldElevator elevator;

	[SerializeField]
	private SoundManager.GroundMaterialType groundMaterial;

	public Vector2 Velocity => elevator.Velocity;

	public SoundManager.GroundMaterialType GroundMaterial => groundMaterial;

	public void OnStartRide()
	{
	}

	public void OnEndRide()
	{
	}
}
