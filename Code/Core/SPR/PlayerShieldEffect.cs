using Library;
using UnityEngine;

namespace SPR;

public class PlayerShieldEffect : MonoBehaviour
{
	private ParticleSystem ps;

	private int sign;

	public ParticleSystem ParticleSystem => ps ?? (ps = GetComponentInChildren<ParticleSystem>());

	public void Initialize()
	{
		base.transform.localPosition = Vector3.zero;
		sign = Game.Instance.Player.transform.localScale.x.GetSign();
	}

	public void Activate(bool playOnAwake)
	{
		ParticleSystem.MainModule main = ParticleSystem.main;
		main.playOnAwake = playOnAwake;
		base.gameObject.SetActive(value: true);
	}

	public void Deactivate()
	{
		ParticleSystem.MainModule main = ParticleSystem.main;
		main.playOnAwake = false;
		base.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		int num = Game.Instance.Player.transform.localScale.x.GetSign();
		if (sign != num)
		{
			Vector3 localScale = base.transform.localScale;
			localScale.x *= -1f;
			base.transform.localScale = localScale;
			sign = num;
		}
	}
}
