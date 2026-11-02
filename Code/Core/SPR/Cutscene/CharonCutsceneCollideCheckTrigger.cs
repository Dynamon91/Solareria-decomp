using System.Linq;
using Library;
using UnityEngine;
using UnityEngine.Events;

namespace SPR.Cutscene;

public class CharonCutsceneCollideCheckTrigger : MonoBehaviour
{
	[SerializeField]
	private UnityEvent onCollide;

	[SerializeField]
	private Direction targetNormal;

	private void OnCollisionEnter2D(Collision2D coll)
	{
		if (coll.gameObject.TryGetComponent<PlayerMover>(out var _) && CheckTargetNormal(coll))
		{
			onCollide?.Invoke();
		}
	}

	private bool CheckTargetNormal(Collision2D coll)
	{
		if (targetNormal == Direction.None)
		{
			return true;
		}
		Vector2 vector = coll.contacts.Aggregate(default(Vector2), (Vector2 current, ContactPoint2D cp) => current + cp.normal);
		return vector.ToDirection(includeDiagonal: false) == targetNormal;
	}
}
