using SPR.UI.Field;
using UnityEngine;

namespace SPR;

public abstract class StatusEffect
{
	protected readonly IDamageable Target;

	public abstract StatusEffectType EffectType { get; protected set; }

	public bool IsDone => StackCount <= 0;

	public abstract bool IsStackable { get; }

	public int CurrentFrame { get; protected set; }

	public int DurationFrame { get; protected set; }

	public int StackCount { get; protected set; }

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	protected static PlayerStatusBoard PlayerStatusBoard => Game.Instance.UIManager.FieldCanvas.PlayerStatusBoard;

	protected static ObjectPoolManager ObjectPoolManager => Game.Instance.ObjectPoolManager;

	protected static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	protected StatusEffect(IDamageable target, int durationFrame)
	{
		Target = target;
		DurationFrame = durationFrame;
		StackCount = 1;
		CurrentFrame = 0;
	}

	protected abstract void ApplyEffect();

	protected virtual void OnRunning()
	{
	}

	public abstract void OnAdd();

	public abstract void Update(StatusEffect elementEffect);

	public abstract void OnRemove(StatusEffectController statusEffectController, bool forceRemove);

	public void Running()
	{
		if (IsDone)
		{
			return;
		}
		CurrentFrame++;
		OnRunning();
		if (CurrentFrame >= DurationFrame)
		{
			ApplyEffect();
			StackCount = Mathf.Max(StackCount - 1, 0);
			if (StackCount > 0)
			{
				CurrentFrame = 0;
			}
		}
	}

	public void ClearStack()
	{
		StackCount = 0;
	}

	public virtual void SetEffectLoop(bool value)
	{
	}
}
