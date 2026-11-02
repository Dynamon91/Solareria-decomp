using System;
using Library;
using SPR.ObjectPool;
using Spine;
using UnityEngine;

namespace SPR;

public class EarthDruidVineObject : SpineObject, IPoolableObject
{
	[Serializable]
	private class VineData
	{
		[SerializeField]
		private bool showGizmos;

		[SerializeField]
		private Zone zone;

		public bool ShowGizmos => showGizmos;

		public Zone Zone => zone;
	}

	[SerializeField]
	private string address;

	[SerializeField]
	private VineData[] vineData;

	private int index;

	private BoxCollider2D box2D;

	public override string Name => "";

	public string PrefabKey => address;

	public GameObject GameObject => base.gameObject;

	public float Width => Box2D.size.x;

	public BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public void InitializeBeforeUse()
	{
		index = CryptoRandom.Range(0, 5);
		base.gameObject.SetActive(value: true);
		SetAnimation($"thorn_{index + 1}_appear", loop: false);
		AddAnimation($"thorn_{index + 1}_clear", loop: false);
		AddAnimationEvent(AnimState.Complete, AnimEndEvent);
	}

	public void ReturnToPool()
	{
		RemoveAnimationEvent(AnimState.Complete, AnimEndEvent);
		Game.Instance.ObjectPoolManager.Return(this);
	}

	private void AnimEndEvent(TrackEntry track)
	{
		if (track.Animation.Name == $"thorn_{index + 1}_clear")
		{
			ReturnToPool();
		}
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		VineData[] array = this.vineData;
		foreach (VineData vineData in array)
		{
			if (vineData.ShowGizmos)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawWireCube(vineData.Zone.Center, vineData.Zone.Size);
			}
		}
	}
}
