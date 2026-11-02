using System;
using Library;

namespace SPR.UI.Memory;

[Serializable]
public readonly struct MemoryData
{
	public readonly MemoryCategory Category;

	public readonly string ID;

	public readonly MemoryLevel Level;

	public bool IsValid => !ID.IsNullOrEmpty();

	public bool IsUnknown => Level == MemoryLevel.None;

	public MemoryData(MemoryCategory category, string id, MemoryLevel level)
	{
		Category = category;
		ID = id;
		Level = level;
	}

	public bool Equals(MemoryData other)
	{
		if (ID == other.ID)
		{
			return Level == other.Level;
		}
		return false;
	}
}
