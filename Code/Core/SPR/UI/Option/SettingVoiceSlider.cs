using System.Collections.Generic;
using UnityEngine;

namespace SPR.UI.Option;

public class SettingVoiceSlider : SettingSlider
{
	private List<string> voiceTestClips = new List<string> { "Verita_110", "Verita_111", "Verita_112" };

	public override void Synchronize()
	{
		base.BarValue_0_10 = SettingItem.SettingData.VoiceVol;
		base.Synchronize();
	}

	public override void Save()
	{
		if (Changed)
		{
			SettingItem.SoundManager.SetVOVolume(base.BarValue_0_10);
			int index = Random.Range(0, voiceTestClips.Count);
			SettingItem.SoundManager.PlayVoice("Verita", voiceTestClips[index]);
			Changed = false;
		}
	}
}
