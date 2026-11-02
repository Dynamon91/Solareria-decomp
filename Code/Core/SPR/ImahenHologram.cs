using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;

namespace SPR;

public class ImahenHologram : MonoBehaviour, IDamageable
{
	[SerializeField]
	private GameObject hologramObject;

	private Animator animator;

	private BoxCollider2D coll;

	private ParticleSystem brokenParticle;

	public HitType HitType => HitType.Object;

	public Vector2 VisualCenter => base.transform.position;

	private void Awake()
	{
		if (!hologramObject.IsNull())
		{
			animator = hologramObject.GetComponent<Animator>();
		}
		coll = GetComponent<BoxCollider2D>();
		brokenParticle = GetComponentInChildren<ParticleSystem>(includeInactive: true);
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		if (!hitInfo.Source.Name.Equals("Imahen_Chaser"))
		{
			return false;
		}
		coll.enabled = false;
		animator.Play("Imahen_Hologram_Destroyed");
		UniTask.WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Imahen_Hologram_Destroyed") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f).GetAwaiter().OnCompleted(delegate
		{
			hologramObject.SetActive(value: false);
		});
		brokenParticle.Play();
		return true;
	}
}
