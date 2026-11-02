using UnityEngine;

namespace SPR;

public class SanctuaryFlame : MonoBehaviour
{
	private class FlameDefaultInfo
	{
		public readonly float SimulationSpeed;

		public readonly Vector2 Size;

		public readonly Vector2 Speed;

		public FlameDefaultInfo(ParticleSystem target)
		{
			ParticleSystem.MainModule main = target.main;
			SimulationSpeed = main.simulationSpeed;
			Size = new Vector2(main.startSize.constantMin, main.startSize.constantMax);
			Speed = new Vector2(main.startSpeed.constantMin, main.startSpeed.constantMax);
		}
	}

	[SerializeField]
	private ParticleSystem attack;

	[SerializeField]
	private ParticleSystem sustain;

	[SerializeField]
	private float magnitude = 1f;

	private ParticleSystem.MainModule attackMain;

	private ParticleSystem.MainModule sustainMain;

	private FlameDefaultInfo attackDefault;

	private FlameDefaultInfo sustainDefault;

	public void OnAwake()
	{
		attackMain = attack.main;
		sustainMain = sustain.main;
		attackDefault = new FlameDefaultInfo(attack);
		sustainDefault = new FlameDefaultInfo(sustain);
	}

	public void TurnOn()
	{
		ChangeMagnitude(magnitude);
		attack.Play();
		sustain.Play();
	}

	public void TurnOff()
	{
		sustainMain.simulationSpeed = attackDefault.SimulationSpeed * Mathf.Max(1f, magnitude);
		sustain.Stop();
	}

	public void ChangeMagnitude(float value)
	{
		magnitude = value;
		ApplyMagnitude(ref attackMain, attackDefault);
		ApplyMagnitude(ref sustainMain, sustainDefault);
	}

	private void ApplyMagnitude(ref ParticleSystem.MainModule main, FlameDefaultInfo info)
	{
		main.simulationSpeed = info.SimulationSpeed * Mathf.Max(1f, magnitude);
		ParticleSystem.MinMaxCurve startSize = main.startSize;
		startSize.constantMin = info.Size.x * magnitude;
		startSize.constantMax = info.Size.y * magnitude;
		main.startSize = startSize;
		ParticleSystem.MinMaxCurve startSpeed = main.startSpeed;
		startSpeed.constantMin = info.Speed.x * magnitude;
		startSpeed.constantMax = info.Speed.y * magnitude;
		main.startSpeed = startSpeed;
	}

	private void OnDrawGizmos()
	{
		attack.transform.localScale = base.transform.localScale;
		sustain.transform.localScale = base.transform.localScale;
	}
}
