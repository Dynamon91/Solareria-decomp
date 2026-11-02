namespace Library;

public class WorldParticleEvent : Event
{
	public enum WorldParticle
	{
		PrimordialFlameLightningSign,
		PrimordialFlamePyronActionAura,
		DarkChasaForceTeleportStart,
		DarkChasaForceTeleportEnd
	}

	public WorldParticle Particle { get; }

	public WorldParticleEvent(WorldParticle particle)
	{
		Particle = particle;
	}
}
