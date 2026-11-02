using UnityEngine;

namespace SPR.Field;

public enum TargetType
{
	[InspectorName("랜덤")]
	Random,
	[InspectorName("바람 정령 암살자")]
	WindAssassin,
	[InspectorName("바람 정령 기사")]
	WindKnight,
	[InspectorName("감염된 나무지기")]
	InfectedTreeGuard
}
