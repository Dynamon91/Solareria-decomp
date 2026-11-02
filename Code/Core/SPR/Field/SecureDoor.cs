using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR.Field;

public class SecureDoor : SpineObject, IStateSavable
{
	private enum State
	{
		Closed,
		Opening,
		Opened,
		Closing
	}

	[SerializeField]
	private int detectRange;

	[SerializeField]
	private Fog doorFog;

	[SerializeField]
	private EventReference openEvent;

	[SerializeField]
	private EventReference closeEvent;

	private State doorState;

	private const int NeverOpened = 0;

	private const int OpenedOnceOrMore = 1;

	private static PlayerController Player => Game.Instance.Player;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public string ID => Util.GetID(Game.FieldID, Name, base.transform.position.ToVector2Int());

	public override string Name => "SecureDoor";

	protected override void Update()
	{
		base.Update();
		Vector2 visualCenter = Player.mover.VisualCenter;
		Vector3 position = base.transform.position;
		SetOpenState(Mathf.Abs(visualCenter.x - position.x) <= (float)detectRange && Mathf.Abs(visualCenter.y - position.y) <= (float)detectRange);
	}

	public void LoadState(int state)
	{
		if (state == 0)
		{
			doorFog?.FadeOutAsync(instant: true).Forget();
		}
		else
		{
			doorFog?.FadeInAsync(instant: true).Forget();
		}
		SetAnimationInstantly("close_loop");
		doorState = State.Closed;
	}

	public void ChangeState(int state)
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	private void SetOpenState(bool open)
	{
		State state = doorState;
		if (state != State.Opening && state != State.Closing && open != (doorState == State.Opened))
		{
			if (open)
			{
				doorFog?.FadeInAsync(instant: false).Forget();
				ChangeState(1);
			}
			OpenCloseAsync(open).Forget();
		}
	}

	private async UniTask OpenCloseAsync(bool open)
	{
		doorState = (open ? State.Opening : State.Closing);
		SoundManager.PlayOneShotAttached(open ? openEvent : closeEvent, base.transform);
		string animationName = (open ? "open" : "close");
		await SetAnimationAsync(animationName, 5f);
		doorState = (open ? State.Opened : State.Closed);
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(base.transform.position, new Vector3(2 * detectRange, 2 * detectRange, 0f));
		Vector2 v = base.transform.position;
		base.transform.position = v.SnapToHalf();
	}
}
