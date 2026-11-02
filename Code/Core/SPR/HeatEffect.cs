using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR;

[Serializable]
public abstract class HeatEffect : IEquatable<HeatEffect>
{
	[SerializeField]
	protected int targetLevel;

	[SerializeField]
	protected int targetUnlockLevel;

	[field: SerializeField]
	public string ID { get; private set; }

	protected static ObjectPoolManager PoolManager => Game.Instance.ObjectPoolManager;

	protected static PlayerMover PlayerMover => Game.Instance.Player.mover;

	protected static PlayerController PlayerController => Game.Instance.Player;

	protected HeatEffect(string id)
	{
		ID = id;
	}

	public abstract void SetParameters<T>(params T[] parameters);

	public void Invoke(int level, CancellationToken token)
	{
		if (CanInvoke(level))
		{
			ActionAsync(token).Forget();
		}
	}

	public virtual void Cancel()
	{
	}

	protected virtual bool CanInvoke(int level)
	{
		if (level >= targetLevel)
		{
			return PlayerStat.HeatMaxLevel >= targetUnlockLevel;
		}
		return false;
	}

	protected abstract UniTask ActionAsync(CancellationToken token);

	public bool Equals(HeatEffect other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)this == other)
		{
			return true;
		}
		if (targetLevel == other.targetLevel)
		{
			return ID == other.ID;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((HeatEffect)obj);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(ID);
	}

	public static bool operator ==(HeatEffect left, HeatEffect right)
	{
		return object.Equals(left, right);
	}

	public static bool operator !=(HeatEffect left, HeatEffect right)
	{
		return !object.Equals(left, right);
	}
}
