using Coffee.UIExtensions;
using Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class StunPoint : MonoBehaviour
{
	[SerializeField]
	private GameObject fill;

	[SerializeField]
	private Image line;

	[SerializeField]
	private UIParticle particle;

	private bool isActive;

	private Animator animator;

	private RectTransform rectTransform;

	private RectTransform fillRectTransform;

	private Image fillImage;

	public bool IsFilled => fill.activeSelf;

	private void Awake()
	{
		isActive = true;
		animator = GetComponent<Animator>();
		rectTransform = GetComponent<RectTransform>();
		fillRectTransform = fill.GetComponent<RectTransform>();
		fillImage = fill.GetComponent<Image>();
	}

	public void SetType(StunPointGroup.Type type)
	{
		if (type != StunPointGroup.Type.Boss)
		{
			rectTransform.sizeDelta = Vector2.one * 40f;
			fillRectTransform.sizeDelta = Vector2.one * -14f;
			fillImage.color = new Color(0.75f, 0f, 0f);
			line.color = Color.white;
		}
	}

	public void SetActive(bool active)
	{
		if (isActive == active)
		{
			animator.Play(active ? "Stun_appear 1f" : "Stun_disappear 1f");
			return;
		}
		isActive = active;
		if (animator.IsNull())
		{
			fill.SetActive(active);
			return;
		}
		string stateName = (active ? "Stun_appear" : "Stun_disappear");
		animator.Play(stateName);
		if (active)
		{
			particle.Play();
		}
	}
}
