using Cysharp.Threading.Tasks;
using SPR.Cutscene;
using UnityEngine;

namespace SPR.Field;

public class CutsceneItemObject : ItemObject
{
	[SerializeField]
	private PlayerWeaponCutsceneProducer cutsceneProducer;

	public override void LoadState(int state)
	{
	}

	public override void Interact()
	{
		cutsceneProducer.PlayTimelineAsync(0, blockControl: true).Forget();
		ChangeState(1);
	}
}
