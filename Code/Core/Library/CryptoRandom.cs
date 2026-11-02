using System.Security.Cryptography;

namespace Library;

public static class CryptoRandom
{
	public static int Range(int minInclusive, int maxExclusive)
	{
		return RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);
	}

	public static bool RandomBool()
	{
		return Range(0, 2) == 1;
	}

	public static bool PercentSuccess(int percent)
	{
		if (percent <= 0)
		{
			return false;
		}
		if (percent >= 100)
		{
			return true;
		}
		return Range(0, 100) < percent;
	}

	public static int Radius(int radius)
	{
		return Range(-radius, radius + 1);
	}
}
