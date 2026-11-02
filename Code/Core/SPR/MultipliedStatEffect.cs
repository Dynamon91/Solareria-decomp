namespace SPR;

public class MultipliedStatEffect : StatEffect
{
	public MultipliedStatEffect(PartEffectData data, int level, float multiplier)
		: base(data, level)
	{
		if (HasValue)
		{
			base.Value *= multiplier;
		}
		else
		{
			base.Rate *= multiplier;
		}
	}
}
