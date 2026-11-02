using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using UnityEngine;

namespace SPR.Field;

public class GroundMaterialDivider : MonoBehaviour
{
	[Serializable]
	private class Info
	{
		[SerializeField]
		public AreaBox area;

		[SerializeField]
		public SoundManager.GroundMaterialType type;
	}

	[SerializeField]
	private List<Info> infos;

	public SoundManager.GroundMaterialType GetMaterialType(SoundManager.GroundMaterialType defaultValue)
	{
		using IEnumerator<Info> enumerator = infos.Where((Info i) => i.area.IsInArea(Game.Instance.Player.mover.VisualBottom)).GetEnumerator();
		if (enumerator.MoveNext())
		{
			return enumerator.Current.type;
		}
		return defaultValue;
	}

	private void OnDrawGizmosSelected()
	{
		if (infos.IsNullOrEmpty())
		{
			return;
		}
		foreach (Info info in infos)
		{
			Gizmos.color = info.type switch
			{
				SoundManager.GroundMaterialType.Grass => new Color(0f, 0.5f, 0f, 0.75f), 
				SoundManager.GroundMaterialType.Rock => new Color(0.5f, 0.5f, 0.5f, 0.75f), 
				SoundManager.GroundMaterialType.Wood => new Color(0.5f, 0.25f, 0f, 0.75f), 
				SoundManager.GroundMaterialType.Metal => new Color(0.5f, 0.75f, 0.75f, 0.75f), 
				_ => new Color(1f, 0f, 1f, 1f), 
			};
			Gizmos.DrawWireCube(info.area.Center, info.area.Size.ToVector3());
		}
	}
}
