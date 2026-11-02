namespace SPR;

public abstract class Part
{
	public PartType PartType => PartData.PartType;

	public string ID
	{
		get
		{
			string name = GetType().Name;
			return name.Substring(0, name.Length - 4);
		}
	}

	public int Level => Game.Instance.SaveData.PlayerData.GetPartLevel(ID);

	protected static PlayerController Player => Game.Instance.Player;

	protected static PlayerMover PlayerMover => Game.Instance.Player.mover;

	protected PartData PartData => DataManager.Instance.GetPartData(ID);

	public virtual void OnEquip()
	{
	}

	public virtual void OnUnequip()
	{
		Reset();
	}

	public virtual void OnFixedUpdate()
	{
	}

	public abstract void Reset();
}
