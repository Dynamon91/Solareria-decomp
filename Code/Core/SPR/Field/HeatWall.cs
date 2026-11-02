using System.Collections.Generic;
using System.Linq;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace SPR.Field;

public class HeatWall : MonoBehaviour
{
	[SerializeField]
	private Animator animator;

	[SerializeField]
	private EventReference activateEvent;

	private Collider2D coll;

	private bool isCollidingPlayer;

	private bool isOff;

	private EventInstance activateInstance;

	private readonly List<Collider2D> overlapResult = new List<Collider2D>();

	private readonly ContactFilter2D filter = default(ContactFilter2D).NoFilter();

	private const string AppearAnimName = "Tutorial Heat Wall_appear";

	private const string DisappearAnimName = "Tutorial Heat Wall_disappear";

	private Collider2D Coll => coll ?? (coll = GetComponent<Collider2D>());

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private void FixedUpdate()
	{
		if (isOff)
		{
			return;
		}
		overlapResult.Clear();
		Physics2D.OverlapCollider(Coll, filter, overlapResult);
		PlayerMover component;
		bool flag = overlapResult.Any((Collider2D c) => c.TryGetComponent<PlayerMover>(out component));
		if (flag)
		{
			if (!isCollidingPlayer)
			{
				InteractEnter();
			}
		}
		else if (isCollidingPlayer)
		{
			InteractExit();
		}
		isCollidingPlayer = flag;
	}

	private void InteractEnter()
	{
		ChangeAnimation("Tutorial Heat Wall_appear", "Tutorial Heat Wall_disappear");
		activateInstance = SoundManager.PlayOneShotAttached(activateEvent, base.transform);
	}

	private void InteractExit()
	{
		ChangeAnimation("Tutorial Heat Wall_disappear", "Tutorial Heat Wall_appear");
		if (activateInstance.isValid())
		{
			activateInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			activateInstance.release();
		}
	}

	private void ChangeAnimation(string currentName, string pastName)
	{
		float normalizedTime = 0f;
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		if (currentAnimatorStateInfo.IsName(pastName))
		{
			normalizedTime = 1f - Mathf.Clamp01(currentAnimatorStateInfo.normalizedTime);
		}
		animator.Play(currentName, 0, normalizedTime);
	}

	public void TurnOff()
	{
		isOff = true;
		if (animator.GetCurrentAnimatorStateInfo(0).IsName("Tutorial Heat Wall_appear"))
		{
			animator.speed = 0.5f;
			animator.Play("Tutorial Heat Wall_disappear");
		}
	}
}
