using System.Collections.Generic;
using Library;
using UnityEngine;

namespace SPR;

public class AerialPathNode : MonoBehaviour
{
	[SerializeField]
	private bool root;

	[SerializeField]
	private bool end;

	[SerializeField]
	private List<AerialPathNode> linkedNodes;

	public bool IsRoot => root;

	public bool IsEnd => end;

	public Vector3 Position => base.transform.position;

	public AerialPathNode GetNextNode(bool reverse = false)
	{
		if (!reverse)
		{
			if (linkedNodes.Count != 1)
			{
				return linkedNodes[CryptoRandom.Range(1, linkedNodes.Count)];
			}
			return linkedNodes[0];
		}
		if (linkedNodes.Count != 1)
		{
			return linkedNodes[CryptoRandom.Range(0, linkedNodes.Count - 1)];
		}
		return linkedNodes[0];
	}
}
