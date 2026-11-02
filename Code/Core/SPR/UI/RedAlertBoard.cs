using FMODUnity;
using UnityEngine;

namespace SPR.UI;

public class RedAlertBoard : MonoBehaviour
{
	[Header("SFX")]
	[SerializeField]
	private EventReference alertLongEvent;

	[SerializeField]
	private EventReference alertShortEvent;

	private Animator animator;

	private bool isLong;

	private static readonly int Active = Animator.StringToHash("Active");

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public void SetAlert(bool value)
	{
		Animator.SetBool(Active, value);
		isLong = value;
	}

	public void PlaySFX()
	{
		SoundManager.PlayOneShot(isLong ? alertLongEvent : alertShortEvent);
		isLong = !isLong;
	}
}
