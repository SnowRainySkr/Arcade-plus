using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Arcade.Gameplay.Chart;
using System.Globalization;

namespace Arcade.Gameplay
{
	public class ArcScoreManager : MonoBehaviour
	{
		public static ArcScoreManager Instance { get; private set; }
		private void Awake()
		{
			Instance = this;
		}

		public Text ScoreText;
		public Text ComboText;

		private int combo = 0;

		private void Update()
		{
			ComboText.text = "";
			ScoreText.text = "00000000";
			if (!ArcGameplayManager.Instance.IsLoaded) return;
			if (ArcGameplayManager.Instance.Chart == null) return;
			ScoreText.text = CalculateScore(ArcGameplayManager.Instance.ChartTiming).ToString("D8", CultureInfo.InvariantCulture);

			if (combo < 2) ComboText.text = "";
			else ComboText.text = combo.ToString(CultureInfo.InvariantCulture);
		}

		private double CalculateSingleScore()
		{
			int total = 0;
			total += ArcTapNoteManager.Instance.Taps.Where((tap) => !tap.NoInput()).Count();
			foreach (var hold in ArcHoldNoteManager.Instance.Holds)
			{
				if (hold.NoInput())
				{
					continue;
				}
				total += hold.JudgeTimings.Count;
			}
			foreach (var arc in ArcArcManager.Instance.Arcs)
			{
				if (arc.NoInput() || arc.Designant)
				{
					continue;
				}
				total += arc.JudgeTimings.Count;
				total += arc.ArcTaps.Count;
				if (arc.IsVariousSizedArctap)
				{
					total += 1;
				}
			}
			if (total == 0) return 0;
			return 10000000d / total;
		}
		private int CalculateCombo(int timing)
		{
			int note = 0;
			foreach (var tap in ArcTapNoteManager.Instance.Taps)
			{
				if (tap.NoInput())
				{
					continue;
				}
				if (tap.Timing <= timing)
				{
					note++;
				}
			}
			foreach (var hold in ArcHoldNoteManager.Instance.Holds)
			{
				if (hold.NoInput())
				{
					continue;
				}
				if (hold.Timing <= timing)
				{
					foreach (float t in hold.JudgeTimings)
					{
						if (t <= timing)
						{
							note++;
						}
					}
				}
			}
			foreach (var arc in ArcArcManager.Instance.Arcs)
			{
				if (arc.NoInput() || arc.Designant)
				{
					continue;
				}
				if (arc.Timing > timing) continue;
				foreach (float t in arc.JudgeTimings)
				{
					if (t <= timing)
					{
						note++;
					}
				}
				foreach (var arctap in arc.ArcTaps)
				{
					if (arctap.Timing <= timing)
					{
						note++;
					}
				}
				if (arc.IsVariousSizedArctap)
				{
					if (arc.Timing <= timing)
					{
						note++;
					}
				}
			}
			return note;
		}
		private int CalculateScore(int timing)
		{
			double single = CalculateSingleScore();
			if (single == 0)
			{
				combo = 0;
				return 0;
			}
			combo = CalculateCombo(timing);
			return (int)Math.Round((combo * single + combo));
		}
	}
}
