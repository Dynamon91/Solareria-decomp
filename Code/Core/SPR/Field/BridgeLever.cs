using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;

namespace SPR.Field;

public class BridgeLever : InteractableLever
{
	[Space]
	[SerializeField]
	private BridgeGroup bridgeGroup;

	[SerializeField]
	private float bridgeLocalPosY;

	public override string ID => $"BridgeLever_{base.transform.position.ToVector2Int()}";

	private float EndPosY => base.transform.position.y + bridgeLocalPosY;

	public override void LoadState(int state)
	{
		base.LoadState(state);
		bridgeGroup.Initialize(state == 1, EndPosY);
	}

	protected override UniTask OnActivateAsync()
	{
		ChangeState(1);
		return bridgeGroup.ActivateAsync();
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.magenta;
		Vector3 position = base.transform.position;
		position.y += bridgeLocalPosY;
		Gizmos.DrawWireSphere(position, 1.55f);
	}
}
