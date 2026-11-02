using FMODUnity;
using Library;
using UnityEngine;

namespace SPR.Field;

public class TumblableObject : MonoBehaviour, IDamageable
{
	[SerializeField]
	private int explosionForce;

	[SerializeField]
	private float forceCoefficient;

	[SerializeField]
	private float radius;

	[SerializeField]
	private EventReference tumbleEvent;

	private Rigidbody2D rb2D;

	private Rigidbody2D Rb2D => rb2D ?? (rb2D = GetComponent<Rigidbody2D>());

	public HitType HitType => HitType.Object;

	public Vector2 VisualCenter { get; }

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		float forceAmount = (Mathf.Log10((float)hitInfo.Damage / 10f) + 10f) * forceCoefficient;
		Vector2 normalized = (hitInfo.GetDirection(base.transform.position).ToVector2() + new Vector2(0f, 0.6f)).normalized;
		ApplyForce(forceAmount, normalized);
		return true;
	}

	public void Explode()
	{
		base.transform.SetParent(Game.Instance.FieldController.transform);
		Vector2 direction = Random.Range(0f, 180f).ToDirectionVector();
		ApplyForce(explosionForce, direction);
	}

	private void ApplyForce(float forceAmount, Vector2 direction)
	{
		Vector2 force = direction * forceAmount;
		Vector2 vector = Random.Range(0f, 360f).ToDirectionVector() * radius;
		Rb2D.AddForceAtPosition(force, (Vector2)base.transform.position + vector, ForceMode2D.Impulse);
		if (!tumbleEvent.IsNull)
		{
			Game.Instance.SoundManager.PlayOneShotAttached(tumbleEvent, base.transform);
		}
	}
}
