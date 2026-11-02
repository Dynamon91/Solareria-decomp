using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.Cutscene;

public class ImahenCutsceneLever : ImahenBlockerLever
{
	[SerializeField]
	private ImahenDivisionCutsceneProducer cutsceneProducer;

	protected override UniTask OnActivateAsync()
	{
		cutsceneProducer.PlayEnterCutscene();
		return base.OnActivateAsync();
	}
}
