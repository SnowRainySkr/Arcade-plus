using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Arcade.Compose.Editing;
using Arcade.Compose.Operation;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Compose
{
	public class AdeCursorInfo : MonoBehaviour
	{
		private bool enableInfo;

		public Text InfoText;
		public GameObject InfoGameObject;
		public bool EnableInfo
		{
			get
			{
				return enableInfo;
			}
			set
			{
				if (enableInfo != value)
				{
					InfoGameObject.SetActive(value);
					enableInfo = value;
				}
			}
		}

		void Update()
		{
			AdeCursorManager cursor = AdeCursorManager.Instance;
			EnableInfo = cursor.WallEnabled || cursor.TrackEnabled;
			string content = string.Empty;
			if (!EnableInfo) return;
			content += $"音乐时间: {(cursor.AttachedTiming + ArcGameplayManager.Instance.ChartAudioOffset).ToString(CultureInfo.InvariantCulture)}\n";
			content += $"谱面时间: {cursor.AttachedTiming.ToString(CultureInfo.InvariantCulture)}";
			if (cursor.WallEnabled)
			{
				Vector3 pos = cursor.AttachedWallPoint;
				content += $"\n坐标: ({ArcAlgorithm.WorldXToArc(pos.x).ToString("f2", CultureInfo.InvariantCulture)},{ArcAlgorithm.WorldYToArc(pos.y).ToString("f2", CultureInfo.InvariantCulture)})";
			}
			if (AdeClickToCreate.Instance.Enable && AdeClickToCreate.Instance.Mode != ClickToCreateMode.Idle)
			{
				content += $"\n点立得: {AdeClickToCreate.Instance.Mode.ToString()}";
				if (AdeClickToCreate.Instance.Mode == ClickToCreateMode.Arc)
				{
					content += $"\n{AdeClickToCreate.Instance.CurrentArcColor}/{AdeClickToCreate.Instance.CurrentArcIsVoid}/{AdeClickToCreate.Instance.CurrentArcCurveType}";
				}
				if (AdeClickToCreate.Instance.Mode == ClickToCreateMode.ArcTap)
				{
					content += $"\n{AdeClickToCreate.Instance.CurrentArctapMode}";
				}
			}
			if (AdeSelectNoteOperation.Instance.RangeSelectPosition != null)
			{
				content += $"\n段落选择起点: {AdeSelectNoteOperation.Instance.RangeSelectPosition?.ToString(CultureInfo.InvariantCulture)}";
			}
			if (AdeSelectionManager.Instance.SelectedNotes.Count == 1 && AdeSelectionManager.Instance.SelectedNotes[0] is ArcArc arc)
			{
				if (arc.EndTiming - arc.Timing != 0)
				{
					float p = ((float)(cursor.AttachedTiming - arc.Timing)) / ((float)(arc.EndTiming - arc.Timing));
					if (p >= 0 && p <= 1)
					{
						float x = ArcAlgorithm.X(arc.XStart, arc.XEnd, p, arc.CurveType);
						float y = ArcAlgorithm.Y(arc.YStart, arc.YEnd, p, arc.CurveType);
						content += $"\nArc: {(p * 100).ToString("f2", CultureInfo.InvariantCulture)}%, {x.ToString("f2", CultureInfo.InvariantCulture)}, {y.ToString("f2", CultureInfo.InvariantCulture)}";
					}
				}
			}
			InfoText.text = content;
		}
	}
}