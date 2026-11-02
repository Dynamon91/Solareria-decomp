using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SPR.FSM;
using SPR.Save;
using UnityEngine;

namespace SPR;

public class Dummy : Creature, IStateSavable
{
	public enum State
	{
		Active,
		InActive,
		Hide
	}

	[Header("더미")]
	[SerializeField]
	private string dummyName;

	public override string ID => Name;

	public override string Name => "Dummy" + dummyName;

	public override EnemyState DefaultState => DummyAlertState;

	public DummyAlertState DummyAlertState { get; private set; }

	public State CurrentState { get; private set; }

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (base.StateMachine.CurrentState == null)
		{
			base.StateMachine.ChangeState(DefaultState);
		}
		int num = (hitInfo.AttackAttribute.HasFlag(AttackAttribute.FixedDamage) ? CalculateFixedDamage(hitInfo.Damage, hitInfo.FireDamage) : CalculateDamage(hitInfo.Damage, hitInfo.FireDamage));
		OnChangeHealth(-num);
		OnDamaged(hitInfo);
		return true;
	}

	public void InitializeState()
	{
		InitializeOnInstantiate(0, base.transform.position);
		base.gameObject.SetActive(value: true);
		Activate();
		DummyAlertState = new DummyAlertState(this, patrolWaitMinFrame, patrolWaitMaxFrame);
		int state = 0;
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			if (data.TryGetObjectStateNum(ID, out var state2))
			{
				state = state2;
			}
			else
			{
				data.SaveObjectState(ID, state);
			}
		}
		LoadState(state);
	}

	public virtual void LoadState(int state)
	{
		SetAnimation("idle");
		switch ((State)state)
		{
		case State.Active:
		case State.InActive:
			base.StateMachine.ChangeState(null);
			break;
		case State.Hide:
			base.StateMachine.ChangeState(null);
			HitBox.enabled = false;
			base.SkeletonAnimation.transform.localPosition = new Vector3(0f, 0f - HitBox.size.y);
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	public void ChangeState(int state)
	{
		if (CurrentState == State.Hide && state != 2)
		{
			SpawnAsync((State)state).Forget();
		}
		else
		{
			LoadState(state);
			CurrentState = (State)state;
		}
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	public override List<AttackSequence> GetAttackSequences()
	{
		if (base.ConnectedNodes == null || base.ConnectedNodes.Count == 0)
		{
			SetConnectedPath();
		}
		return base.GetAttackSequences();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence>();
	}

	private async UniTaskVoid SpawnAsync(State state)
	{
		EnableVFX("spawn");
		await UniTask.WaitUntil(() => base.VfxController.IsTimelineEnd("spawn"));
		base.SkeletonAnimation.transform.localPosition = Vector3.zero;
		CurrentState = state;
		HitBox.enabled = true;
	}
}
