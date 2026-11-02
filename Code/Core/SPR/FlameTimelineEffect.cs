using System.Collections.Generic;
using System.Linq;
using Library;
using UnityEngine;
using UnityEngine.Timeline;

namespace SPR;

public class FlameTimelineEffect : TimelineEffect
{
	private void OnDrawGizmosSelected()
	{
		List<PlayerFlameTrack> list = (from track in base.Director.playableAsset.CastObject<TimelineAsset>().GetOutputTracks()
			where track is PlayerFlameTrack
			select track).Cast<PlayerFlameTrack>().ToList();
		Vector2 vector = base.transform.position;
		Vector2 vector2 = vector + Vector2.right * 5f;
		foreach (PlayerFlameTrack item in list)
		{
			foreach (TimelineClip clip in item.GetClips())
			{
				HeatFlame.CurveParameter curveParameter = ((PlayerFlameClip)clip.asset).curveParameter;
				List<Vector2> list2 = new List<Vector2>(4) { vector };
				float radianFromStart = curveParameter.RadianFromStart;
				Vector2 vector3 = new Vector2(Mathf.Cos(radianFromStart) * curveParameter.radiusFromStart, Mathf.Sin(radianFromStart) * curveParameter.radiusFromStart);
				list2.Add(vector + vector3);
				radianFromStart = curveParameter.RadianFromEnd;
				vector3 = new Vector2(Mathf.Cos(radianFromStart) * curveParameter.radiusFromEnd, Mathf.Sin(radianFromStart) * curveParameter.radiusFromEnd);
				list2.Add(vector2 + vector3);
				list2.Add(vector2);
				List<Vector2> list3 = new List<Vector2>();
				int num = Util.SecondsToFrame(clip.duration);
				for (int num2 = 0; num2 <= num; num2++)
				{
					Vector3 bezierPosition = HeatFlame.GetBezierPosition(list2, num2, num);
					list3.Add(bezierPosition);
				}
				Gizmos.color = Color.magenta;
				Gizmos.DrawCube(list2[0], new Vector3(2f, 4f));
				for (int num3 = 1; num3 < list2.Count; num3++)
				{
					Gizmos.DrawSphere(list2[num3], 0.5f);
				}
				DrawLine(list3.ToList());
			}
		}
		static void DrawLine(List<Vector2> gizmoPoints)
		{
			for (int i = 0; i < gizmoPoints.Count - 1; i++)
			{
				Gizmos.DrawLine(gizmoPoints[i], gizmoPoints[i + 1]);
			}
		}
	}
}
