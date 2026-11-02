using System.Linq;
using Coffee.UIExtensions;
using UnityEngine;

namespace SPR.UI.Trait;

public class PoolingUIParticle : PoolingObject
{
	private RectTransform rectTransform;

	private UIParticle uiParticle;

	public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	private UIParticle UIParticle => uiParticle ?? (uiParticle = GetComponent<UIParticle>());

	private void Update()
	{
		if (UIParticle.particles.All((ParticleSystem p) => !p.IsAlive()))
		{
			ReturnToPool();
		}
	}
}
