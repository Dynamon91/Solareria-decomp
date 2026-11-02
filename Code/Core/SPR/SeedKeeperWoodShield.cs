using FMODUnity;
using UnityEngine;

namespace SPR;

public class SeedKeeperWoodShield : MonoBehaviour
{
	[SerializeField]
	private GameObject activeGroup;

	[SerializeField]
	private ParticleSystem shieldBreak;

	[Header("SFX")]
	[SerializeField]
	private EventReference activateEvent;

	[SerializeField]
	private EventReference breakEvent;

	private bool isInitialized;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public bool IsActive { get; private set; }

	public void Initialize()
	{
		isInitialized = true;
		ActivateShield(active: true);
	}

	public void Generate()
	{
		if (isInitialized && !IsActive)
		{
			ActivateShield(active: true);
		}
	}

	public void Break()
	{
		if (IsActive)
		{
			ActivateShield(active: false);
		}
	}

	private void ActivateShield(bool active)
	{
		IsActive = active;
		activeGroup.SetActive(IsActive);
		shieldBreak.gameObject.SetActive(!IsActive);
		SoundManager.PlayOneShotAttached(IsActive ? activateEvent : breakEvent, base.transform);
	}
}
