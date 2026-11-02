using System;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using SPR.UI;
using UnityEngine;

namespace SPR.Field;

public class LegendaryBounds : MonoBehaviour, IEventListener
{
	[SerializeField]
	private AreaBox activateBound;

	[SerializeField]
	private AreaBox deactivateBound;

	[SerializeField]
	private bool customActivateEvent;

	[SerializeField]
	private bool customDeactivateEvent;

	private HighCreature highCreature;

	private bool isFighting;

	private HighCreature HighCreature => highCreature ?? (highCreature = GetComponentInChildren<HighCreature>());

	private static PlayerMover PlayerMover => Game.Instance.Player.mover;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public event Action OnEnter;

	public event Action OnExit;

	private void Awake()
	{
		LazySingleton<MessageSystem>.Instance.Subscribe<LegendaryBoundsActivateEvent>(this);
		LazySingleton<MessageSystem>.Instance.Subscribe<LegendaryBoundsDeactivateEvent>(this);
	}

	private void OnDestroy()
	{
		LazySingleton<MessageSystem>.Instance.UnSubscribe<LegendaryBoundsActivateEvent>(this);
		LazySingleton<MessageSystem>.Instance.UnSubscribe<LegendaryBoundsDeactivateEvent>(this);
	}

	private void FixedUpdate()
	{
		if ((object)HighCreature == null || HighCreature.IsDead)
		{
			return;
		}
		if (!isFighting)
		{
			if (!customActivateEvent)
			{
				Activate();
			}
		}
		else if (!customDeactivateEvent)
		{
			Deactivate();
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(activateBound.Center, activateBound.Size.ToVector3());
		Gizmos.color = new Color(1f, 0.5f, 1f);
		Gizmos.DrawWireCube(deactivateBound.Center, deactivateBound.Size.ToVector3());
	}

	private void Activate()
	{
		if (!isFighting && PhysicsUtility.IsPlayerStay(activateBound.Center, activateBound.Size) && PlayerMover.IsOnGround)
		{
			BossBoard bossBoard = Game.Instance.UIManager.FieldCanvas.BossBoard;
			bossBoard.SetBoss(HighCreature);
			bossBoard.ShowAsync().Forget();
			this.OnEnter?.Invoke();
			SoundManager.PlayLegendaryCreatureBGM();
			isFighting = true;
		}
	}

	private void Deactivate()
	{
		if (isFighting && !PhysicsUtility.IsPlayerStay(deactivateBound.Center, deactivateBound.Size))
		{
			Game.Instance.UIManager.FieldCanvas.BossBoard.HideAsync(0.6f, deactivate: true).Forget();
			this.OnExit?.Invoke();
			SoundManager.StopLegendaryCreatureBGM();
			isFighting = false;
		}
	}

	public void OnEvent(global::Library.Event e)
	{
		if (customActivateEvent && e is LegendaryBoundsActivateEvent)
		{
			Activate();
		}
		if (customDeactivateEvent && e is LegendaryBoundsDeactivateEvent)
		{
			Deactivate();
		}
	}
}
