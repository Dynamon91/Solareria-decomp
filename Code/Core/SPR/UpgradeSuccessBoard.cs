using System.Collections.Generic;
using SPR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR;

public class UpgradeSuccessBoard : MonoBehaviour
{
	[SerializeField]
	private LocaleText successLocaleText;

	[SerializeField]
	private List<ParticleSystem> particles;

	private TextMeshProUGUI successTMPro;

	public void UpdateComponents()
	{
		if ((object)successTMPro == null)
		{
			successTMPro = successLocaleText.GetComponent<TextMeshProUGUI>();
		}
		successLocaleText.SetData("UI_Upgrade_Success");
		LayoutRebuilder.ForceRebuildLayoutImmediate(successLocaleText.RectTransform);
		int num = Mathf.CeilToInt(successTMPro.preferredWidth / 10f);
		foreach (ParticleSystem particle in particles)
		{
			ParticleSystem.ShapeModule shape = particle.shape;
			Vector3 scale = shape.scale;
			scale.x = num;
			shape.scale = scale;
			ParticleSystem.EmissionModule emission = particle.emission;
			ParticleSystem.Burst burst = emission.GetBurst(0);
			burst.count = (int)((float)num * 1.1f);
			emission.SetBurst(0, burst);
		}
	}
}
