using UnityEngine.Playables;

namespace SPR.Field;

public class SolinLabElevatorLever : ElevatorLever
{
	private PlayableDirector director;

	public override int InteractableOrder => 11;

	protected override bool CanInteractOnce => false;

	private PlayableDirector Director => director ?? (director = GetComponent<PlayableDirector>());

	public override void LoadState(int state)
	{
		base.LoadState(state);
		if (state == 0)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	protected override void OnChangeState(int state)
	{
		if (state == 1 && !base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: true);
			Director.Play();
		}
	}
}
