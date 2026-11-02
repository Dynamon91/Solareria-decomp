using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using UnityEngine;

namespace SPR;

public class ImahenBlockerLever : InteractableLever
{
	[SerializeField]
	private ParticleSystem doorHighlight;

	[SerializeField]
	private ParticleSystem leverHighlight;

	private ImahenLeverBlocker leverBlocker;

	public override string ID => Util.GetID(Game.FieldID, "ImahenBlockerLever", base.transform.position.ToVector2Int());

	private ImahenLeverBlocker LeverBlocker => leverBlocker ?? (leverBlocker = GetComponentInParent<ImahenLeverBlocker>());

	protected override async UniTask OnActivateAsync()
	{
		LeverBlocker.OnTriggered();
		ChangeState(1);
		await UniTask.CompletedTask;
	}

	public void DoorHighlight()
	{
		doorHighlight.Play();
		leverHighlight.Play();
	}
}
