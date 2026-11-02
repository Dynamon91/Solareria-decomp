using System;
using UnityEngine;

[Serializable]
public struct AreaNumber
{
	[SerializeField]
	private int sectorNum;

	[SerializeField]
	private int areaNum;

	public int SectorNum => sectorNum;

	public int AreaNum => areaNum;

	public bool IsNull => sectorNum < 0;

	public static AreaNumber NullArea => new AreaNumber(-1, -1);

	public AreaNumber(int sectorNum, int areaNum)
	{
		this.sectorNum = sectorNum;
		this.areaNum = areaNum;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is AreaNumber areaNumber))
		{
			return false;
		}
		if (sectorNum == areaNumber.sectorNum)
		{
			return areaNum == areaNumber.areaNum;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(sectorNum, areaNum);
	}

	public static bool operator ==(AreaNumber a, AreaNumber b)
	{
		if (a.sectorNum == b.sectorNum)
		{
			return a.areaNum == b.areaNum;
		}
		return false;
	}

	public static bool operator !=(AreaNumber a, AreaNumber b)
	{
		return !(a == b);
	}

	public static bool operator >(AreaNumber a, AreaNumber b)
	{
		if (a.sectorNum > b.sectorNum)
		{
			return true;
		}
		if (a.sectorNum < b.sectorNum)
		{
			return false;
		}
		return a.areaNum > b.areaNum;
	}

	public static bool operator <(AreaNumber a, AreaNumber b)
	{
		if (a.sectorNum < b.sectorNum)
		{
			return true;
		}
		if (a.sectorNum > b.sectorNum)
		{
			return false;
		}
		return a.areaNum < b.areaNum;
	}

	public override string ToString()
	{
		return $"({sectorNum}, {areaNum})";
	}
}
