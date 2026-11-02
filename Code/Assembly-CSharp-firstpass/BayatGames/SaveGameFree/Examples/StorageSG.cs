using System;

namespace BayatGames.SaveGameFree.Examples;

[Serializable]
public class StorageSG
{
	public DateTime myDateTime;

	public StorageSG()
	{
		myDateTime = DateTime.UtcNow;
	}
}
