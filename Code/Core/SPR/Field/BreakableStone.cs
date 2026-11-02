using FMODUnity;
using Library;
using UnityEngine;
using UnityEngine.Serialization;

namespace SPR.Field;

public class BreakableStone : BreakableWall
{
	[Header("Switch")]
	[SerializeField]
	private SubArea switchSubArea;

	[Header("Disables")]
	[FormerlySerializedAs("subAreas")]
	[SerializeField]
	private SubArea[] disableSubAreas;

	[FormerlySerializedAs("areaChangers")]
	[SerializeField]
	private AreaChanger[] disableAreaChangers;

	[Header("VFX")]
	[SerializeField]
	private ParticleSystem hitEffect;

	[SerializeField]
	private ParticleSystem breakEffect;

	[Header("SFX")]
	[SerializeField]
	private EventReference hitEvent;

	[SerializeField]
	private EventReference breakEvent;

	[Header("Reference")]
	[SerializeField]
	private GameObject crackLight;

	protected override string BreakAnimationName => "broken";

	private static FieldController FieldController => Game.Instance.FieldController;

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (hitInfo.AttackerType == HitType.Player)
		{
			base.SkeletonAnimation.SetAnimationInstantly("hit", loop: false);
			hitEffect.Play();
		}
		return base.TryTakeDamage(hitInfo);
	}

	protected override void OnLoadBrokenState()
	{
		base.OnLoadBrokenState();
		crackLight.SetActive(value: false);
		SubArea[] array = disableSubAreas;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: false);
		}
		AreaChanger[] array2 = disableAreaChangers;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].gameObject.SetActive(value: false);
		}
	}

	protected override void OnBreak()
	{
		base.OnBreak();
		crackLight.SetActive(value: false);
		base.SkeletonAnimation.SetAnimationInstantly(BreakAnimationName, loop: false);
		breakEffect.Play();
		FieldController.ChangeAreaTo(switchSubArea.IsNull() ? FieldController.MainArea : switchSubArea);
	}

	protected override void PlaySfx(bool isBreak)
	{
		BreakableObject.SoundManager.PlayOneShotAttached(isBreak ? breakEvent : hitEvent, base.transform);
	}
}
