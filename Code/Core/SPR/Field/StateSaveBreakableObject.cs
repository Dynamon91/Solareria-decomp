using SPR.Save;

namespace SPR.Field;

public abstract class StateSaveBreakableObject : BreakableObject, IStateSavable, IImpassable
{
	protected const int DefaultState = 0;

	public abstract string ID { get; }

	protected virtual int BrokenState => base.OriginalHitCount;

	protected override void Initialize()
	{
	}

	protected override void OnDamaged()
	{
		ChangeState(Count);
	}

	public void LoadState(int state)
	{
		if (state == BrokenState)
		{
			Count = BrokenState;
			base.Collider2D.enabled = false;
			OnLoadBrokenState();
		}
		else
		{
			base.Collider2D.enabled = true;
			Count = state;
			OnLoadNormalState();
		}
	}

	protected abstract void OnLoadBrokenState();

	protected abstract void OnLoadNormalState();

	public virtual void ChangeState(int state)
	{
		OnChangeState();
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	protected virtual void OnChangeState()
	{
	}
}
