using UnityEngine;

namespace SPR;

public class PoolingParticleOnSpineMesh : PoolingParticle
{
	public void SetMesh(Mesh mesh)
	{
		ParticleSystem.ShapeModule shape = base.ParticleSystem.shape;
		shape.shapeType = ParticleSystemShapeType.Mesh;
		shape.mesh = mesh;
		if (base.ParticleSystem.isPlaying)
		{
			base.ParticleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			base.ParticleSystem.Play();
		}
	}

	public void SetMeshRenderer(MeshRenderer mesh)
	{
		ParticleSystem.ShapeModule shape = base.ParticleSystem.shape;
		shape.shapeType = ParticleSystemShapeType.MeshRenderer;
		shape.meshRenderer = mesh;
		if (base.ParticleSystem.isPlaying)
		{
			base.ParticleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			base.ParticleSystem.Play();
		}
	}
}
