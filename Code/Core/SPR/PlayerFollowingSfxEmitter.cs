using FMODUnity;
using Library;
using UnityEngine;

namespace SPR;

[RequireComponent(typeof(StudioEventEmitter))]
public class PlayerFollowingSfxEmitter : MonoBehaviour
{
	[SerializeField]
	private AreaBox areaBox;

	[SerializeField]
	private Vector2 offset;

	[SerializeField]
	private AnimationCurve curve;

	[SerializeField]
	private bool isAxisX;

	private StudioEventEmitter emitter;

	private void Awake()
	{
		emitter = GetComponent<StudioEventEmitter>();
	}

	private void Update()
	{
		Vector2 vector = areaBox.Clamp(Game.Instance.Player.mover.VisualCenter + offset);
		base.transform.position = vector;
		float volume = curve.Evaluate(isAxisX ? vector.x : vector.y);
		emitter.EventInstance.setVolume(volume);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(areaBox.Center, areaBox.Size.ToVector3());
	}
}
