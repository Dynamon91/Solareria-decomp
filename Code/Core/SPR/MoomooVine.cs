using System.Collections.Generic;
using FSM;
using Library;
using SPR.FSM;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class MoomooVine : Enemy, IEventListener
{
	[SerializeField]
	private SkeletonAnimation[] vineSkeletons;

	[SerializeField]
	private GenericAttackSequence<MoomooVine> attackRise1;

	public override EnemyState DefaultState => null;

	public override HitType HitType => HitType.None;

	public override string Name => "MoomooVine";

	public override Direction FacingDirection => Direction.None;

	protected override void Awake()
	{
		base.StateMachine = new SimpleStateMachine();
		base.VfxController.InitializeVFXDictionary();
		LazySingleton<MessageSystem>.Instance.Subscribe<MoomooVineEvent>(this);
	}

	protected override void OnDestroy()
	{
		LazySingleton<MessageSystem>.Instance.UnSubscribe<MoomooVineEvent>(this);
	}

	public void OnEvent(global::Library.Event e)
	{
		if (e is MoomooVineEvent)
		{
			base.StateMachine.ChangeState(new MoomooVineAttackState(this, attackRise1));
		}
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		return false;
	}

	public void SetAnimationGroup(string animationName)
	{
		SkeletonAnimation[] array = vineSkeletons;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetAnimationInstantly(animationName, loop: false);
		}
	}

	public void SetRandomAnimationGroup()
	{
		SkeletonAnimation[] array = vineSkeletons;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetAnimation($"small_thorn_{CryptoRandom.Range(1, 3)}", 0, loop: false);
		}
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attackRise1 };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return null;
	}
}
