using FMODUnity;
using UnityEngine;

namespace SPR.Field;

public class StuckableObject : MonoBehaviour, IDamageable
{
	[SerializeField]
	private ParticleSystem hitParticle;

	[SerializeField]
	private EventReference hitEvent;

	private Collider2D col;

	public HitType HitType => HitType.Object;

	public Vector2 VisualCenter => base.transform.position;

	private Collider2D Collider2D => col ?? (col = GetComponent<Collider2D>());

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	public virtual bool TryTakeDamage(HitInfo hitInfo)
	{
		hitParticle.Clear();
		hitParticle.transform.position = Collider2D.bounds.ClosestPoint((base.transform.position + Game.Instance.Player.transform.position) / 2f);
		hitParticle.Play();
		SoundManager.PlayOneShotAttached(hitEvent, base.transform);
		return true;
	}
}
