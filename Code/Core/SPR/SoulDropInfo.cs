using System;
using UnityEngine;

namespace SPR;

[Serializable]
public struct SoulDropInfo
{
	[SerializeField]
	private FieldID fieldID;

	[SerializeField]
	private Vector2 worldPosition;

	[SerializeField]
	private AreaBox areaBox;

	public FieldID FieldID => fieldID;

	public Vector2 WorldPosition => worldPosition;

	public Vector2 AreaNormalizedPosition => GetAreaNormalizedPosition(areaBox, worldPosition);

	public bool IsValid => fieldID.SectorName != SectorName.None;

	public SoulDropInfo None => new SoulDropInfo(new FieldID(SectorName.None, -1), Vector2.zero, default(AreaBox));

	public SoulDropInfo(FieldID fieldID, Vector2 worldPos, AreaBox areaBox)
	{
		this.fieldID = fieldID;
		worldPosition = worldPos;
		this.areaBox = areaBox;
	}

	public static Vector2 GetAreaNormalizedPosition(AreaBox areaBox, Vector2 worldPosition)
	{
		float x = Mathf.InverseLerp(areaBox.LowerLeft.x, areaBox.UpperRight.x, worldPosition.x);
		float y = Mathf.InverseLerp(areaBox.LowerLeft.y, areaBox.UpperRight.y, worldPosition.y);
		return new Vector2(x, y);
	}
}
