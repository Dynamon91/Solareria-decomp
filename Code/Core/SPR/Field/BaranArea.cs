using System.Collections.Generic;
using System.Linq;
using Library;
using UnityEngine;

namespace SPR.Field;

public class BaranArea : Area, IEventListener
{
	[Header("디버그")]
	[SerializeField]
	private bool myosoEnabled;

	[Header("모소 조력")]
	[SerializeField]
	private int pauseFrame;

	[SerializeField]
	private float supportHealthPercentage;

	[SerializeField]
	private int defenceReduction;

	[Header("모소 조력 연출")]
	[SerializeField]
	private Vector2 myosoSpawnOffset;

	[Header("References")]
	[SerializeField]
	private MyosoCombat myoso;

	private Baran baran;

	private MyosoEventState myosoEventState;

	private readonly List<int> myosoRange = new List<int>();

	public float SupportHealthPercentage => supportHealthPercentage;

	private Baran Baran => baran ?? (baran = GetComponentInChildren<Baran>());

	private MyosoCombat Myoso => myoso ?? (myoso = GetComponentInChildren<MyosoCombat>());

	private void Awake()
	{
		LazySingleton<MessageSystem>.Instance.Subscribe<MyosoSupportEvent>(this);
		for (int i = areaBox.LowerLeft.x + Myoso.Width; i <= areaBox.UpperRight.x - Myoso.Width; i++)
		{
			myosoRange.Add(i);
		}
		myosoEnabled = Trigger.CheckTrigger("Trigger_MyosoHelpFightBaran");
	}

	private void OnDestroy()
	{
		LazySingleton<MessageSystem>.Instance.UnSubscribe<MyosoSupportEvent>(this);
	}

	public void OnEvent(global::Library.Event e)
	{
		if (myosoEnabled && !Baran.IsAttack10Progressing && e is MyosoSupportEvent myosoSupportEvent)
		{
			if (myosoSupportEvent.IsPyronAction)
			{
				MyosoAppear();
			}
			else if (!myosoEventState.HasFlag(myosoSupportEvent.MyosoEventState))
			{
				myosoEventState |= myosoSupportEvent.MyosoEventState;
				MyosoAppear();
			}
		}
	}

	private void MyosoAppear()
	{
		if (!myoso.gameObject.activeSelf)
		{
			MyosoTeleport();
			Myoso.PauseFrame = pauseFrame;
			Myoso.DefenseReduction = defenceReduction;
			Myoso.Appear(new BaranPierceState(Baran, new HitInfo(Myoso, "Baran"), pauseFrame, defenceReduction)).Forget();
		}
	}

	private void CheckMyosoOverlapLightning()
	{
		if (!Myoso.gameObject.activeSelf || Baran.Attack9List.All((LightningAttack a) => !a.gameObject.activeSelf))
		{
			return;
		}
		foreach (LightningAttack item in Baran.Attack9List.Where((LightningAttack a) => a.gameObject.activeSelf))
		{
			if (item.Box2D.bounds.Intersects(Myoso.Box2D.bounds))
			{
				MyosoTeleport();
				break;
			}
		}
	}

	private void MyosoTeleport()
	{
		float x = ((Baran.Position.x < base.AreaBox.Center.x) ? (Baran.Position.x + myosoSpawnOffset.x) : (Baran.Position.x - myosoSpawnOffset.x));
		float y = base.AreaBox.Center.y + myosoSpawnOffset.y;
		Myoso.Position = new Vector2(x, y);
	}

	private void OnDrawGizmosSelected()
	{
		Vector2 vector = base.AreaBox.Center + new Vector2(0f, myosoSpawnOffset.y);
		Gizmos.color = Color.yellow;
		Gizmos.DrawCube(vector, Vector3.one);
	}
}
