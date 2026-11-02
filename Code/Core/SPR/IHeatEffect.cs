using System;

namespace SPR;

public interface IHeatEffect
{
	HeatSkillType HeatSkillType { get; }

	event Action OnUse;
}
