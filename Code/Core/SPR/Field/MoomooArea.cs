using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using Library;
using UnityEngine;

namespace SPR.Field;

public class MoomooArea : LegendaryArea, IEventListener
{
	[Header("BGM")]
	[SerializeField]
	private SoundManager.BGMType bgmType;

	[Header("SFX")]
	[SerializeField]
	private EventReference witherEvent;

	[SerializeField]
	private EventReference burnEvent;

	private Moomoo moomoo;

	private List<MoomooBreakableTree> breakableTrees = new List<MoomooBreakableTree>();

	private Moomoo Moomoo => moomoo ?? (moomoo = GetComponentInChildren<Moomoo>());

	protected override void OnInitialize()
	{
		breakableTrees = GetComponentsInChildren<MoomooBreakableTree>().ToList();
		base.LegendaryBounds.OnEnter += PlayBGM;
		base.LegendaryBounds.OnEnter += OnEnterLegendary;
		base.LegendaryBounds.OnExit += OnExitLegendary;
		LazySingleton<MessageSystem>.Instance.Subscribe<MoomooDeathEvent>(this);
	}

	public void BurnTree()
	{
		Area.SoundManager.PlayOneShot(burnEvent);
		foreach (MoomooBreakableTree breakableTree in breakableTrees)
		{
			breakableTree.ChangeState(2);
		}
	}

	private void PlayBGM()
	{
		Area.SoundManager.PlayEnemyWaveBGM(bgmType);
		Area.SoundManager.SetEnemyWaveParameter(1, 3);
	}

	public void OnEvent(global::Library.Event e)
	{
		if (e is MoomooDeathEvent)
		{
			Area.SoundManager.PlayOneShot(witherEvent);
		}
	}

	private void OnDestroy()
	{
		LazySingleton<MessageSystem>.Instance.UnSubscribe<MoomooDeathEvent>(this);
	}

	protected override void OnEnterLegendary()
	{
		if ((object)Moomoo != null && !Moomoo.IsDead)
		{
			Moomoo.StateMachine.ChangeState(Moomoo.DefaultState);
			Moomoo.SetInitialFatigue();
		}
	}

	protected override void OnExitLegendary()
	{
		if ((object)Moomoo != null && !Moomoo.IsDead)
		{
			Moomoo.StateMachine.ChangeState(null);
		}
	}
}
