using System;
using FMODUnity;
using SPR.Save;
using UnityEngine;

namespace SPR.Field;

public class SotdaePlatform : MonoBehaviour, IInteractable
{
	[SerializeField]
	private BoxCollider2D boxCollider;

	[SerializeField]
	private EventReference appearEvent;

	private bool isAppear = true;

	private Animator animator;

	public string Name => base.name;

	public bool IsNull => false;

	public int InteractableOrder => 0;

	public bool IsAppear => Animator.GetCurrentAnimatorStateInfo(0).IsName("SotdaePlatform_appear");

	public BoxCollider2D Box2D => boxCollider;

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	private static PlayerMover Player => Game.Instance.Player.mover;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public event Action OnEnter;

	public event Action OnExit;

	public void AwakeObject(SaveData saveData)
	{
	}

	public void Interact()
	{
	}

	public void InteractEnter()
	{
		this.OnEnter?.Invoke();
	}

	public void InteractExit()
	{
		this.OnExit?.Invoke();
	}

	public void Appear()
	{
		if (!isAppear)
		{
			Animator.Play("SotdaePlatform_appear");
			SoundManager.PlayOneShotAttached(appearEvent, base.transform);
			isAppear = true;
		}
	}

	public void Disappear()
	{
		if (isAppear)
		{
			Animator.Play("Sotdae Platform disappear");
			SoundManager.PlayOneShotAttached(appearEvent, base.transform).setVolume(0.25f);
			isAppear = false;
		}
	}
}
