using System.Linq;
using UnityEngine;

namespace SPR;

public class AerialPath : MonoBehaviour
{
	[SerializeField]
	private AerialPathNode prefab;

	private AerialPathNode root;

	public AerialPathNode Root => root ?? (root = base.transform.GetChild(0).GetComponent<AerialPathNode>());

	public AerialPathNode GetCloseNode(Vector3 position)
	{
		return (from n in GetComponentsInChildren<AerialPathNode>()
			orderby Vector3.Distance(n.Position, position)
			select n).FirstOrDefault();
	}
}
