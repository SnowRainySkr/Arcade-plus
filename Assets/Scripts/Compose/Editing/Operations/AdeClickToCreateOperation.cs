using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Arcade.Compose.Command;
using Arcade.Compose.Editing;
using Arcade.Compose.MarkingMenu;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using Arcade.Util.UniTaskHelper;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcade.Compose.Operation
{
	public enum ClickToCreateMode
	{
		Idle = 0,
		Tap = 1,
		Hold = 2,
		Arc = 3,
		ArcTap = 4
	}
	public enum ClickToCreateArctapMode
	{
		OnArc = 0,
		Single = 1
	}
	public class AdeClickToCreate : AdeOperation
	{
		public static AdeClickToCreate Instance { get; private set; }

		public MarkingMenuItem Delete;
		public MarkingMenuItem[] Entry;
		public MarkingMenuItem[] ClickToCreateItems;

		public override bool IsOnlyMarkingMenu => enable;
		public bool Enable
		{
			get
			{
				return enable;
			}
			set
			{
				if (value && !ArcGameplayManager.Instance.IsLoaded)
				{
					AdeToast.Instance.Show("请先加载谱面");
					return;
				}
				if (enable != value)
				{
					AdeCursorManager.Instance.VisibleWhenIdle = value;
					Mode = ClickToCreateMode.Idle;
					enable = value;
				}
			}
		}
		public override MarkingMenuItem[] MarkingMenuItems
		{
			get
			{
				if (!enable) return Entry;
				else
				{
					List<MarkingMenuItem> items = new List<MarkingMenuItem>();
					items.AddRange(ClickToCreateItems);
					if (AdeSelectionManager.Instance.SelectedNotes.Count != 0)
					{
						items.Add(Delete);
					}
					return items.ToArray();
				}
			}
		}
		private ClickToCreateMode mode;
		public ClickToCreateMode Mode
		{
			get
			{
				if (!ArcGameplayManager.Instance.IsLoaded) return ClickToCreateMode.Idle;
				return mode;
			}
			set
			{
				if (mode != value)
				{
					mode = value;
					ArctapMode = ClickToCreateArctapMode.OnArc;
				}
				if (mode == ClickToCreateMode.Idle)
				{
					AdeCursorManager.Instance.ArcTapCursorEnabled = false;
				}
			}
		}
		private ClickToCreateArctapMode arctapMode;
		public ClickToCreateArctapMode ArctapMode
		{
			get
			{
				if (!ArcGameplayManager.Instance.IsLoaded) return ClickToCreateArctapMode.OnArc;
				if (Mode != ClickToCreateMode.ArcTap) return ClickToCreateArctapMode.OnArc;
				return arctapMode;
			}
			set
			{
				if (arctapMode != value)
				{
					arctapMode = value;
				}
			}
		}

		public string CurrentArcColor
		{
			get
			{
				return currentArcColor == 0 ? "蓝" : "红";
			}
		}
		public string CurrentArcIsVoid
		{
			get
			{
				return currentArcIsVoid ? "虚" : "实";
			}
		}
		public string CurrentArcCurveType
		{
			get
			{
				return currentArcCurveType.ToString();
			}
		}
		public string CurrentArctapMode
		{
			get
			{
				return arctapMode == ClickToCreateArctapMode.OnArc ? "加在 Arc 上" : "单独创建";
			}
		}

		private bool enable = false;
		private int currentArcColor;
		private bool currentArcIsVoid;
		private ArcLineType currentArcLineType
		{
			get
			{
				if (currentArcIsVoid)
				{
					return ArcLineType.TrueIsVoid;
				}
				return ArcLineType.FalseNotVoid;
			}
		}

		private ArcCurveType currentArcCurveType = ArcCurveType.S;

		private void Awake()
		{
			Instance = this;
		}
		private void Update()
		{
			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ToggleClickToCreate))
			{
				Enable = !Enable;
			}
			if (!enable) return;

			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateIdle))
			{
				SetClickToCreateMode(ClickToCreateMode.Idle);
			}
			else if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateTap))
			{
				SetClickToCreateMode(ClickToCreateMode.Tap);
			}
			else if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateHold))
			{
				SetClickToCreateMode(ClickToCreateMode.Hold);
			}
			else if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateArc))
			{
				SetClickToCreateMode(ClickToCreateMode.Arc);
			}
			else if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateArctap))
			{
				SetClickToCreateMode(ClickToCreateMode.ArcTap);
			}

			if (Mode == ClickToCreateMode.Idle) return;

			if (Mode == ClickToCreateMode.Arc)
			{
				if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateArcVoid))
				{
					SwitchIsVoid();
				}
				if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateArcColor))
				{
					SwitchColor();
				}
				if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateArcCurveType))
				{
					SwitchType();
				}
			}
			if (Mode == ClickToCreateMode.ArcTap)
			{
				if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateArctapMode))
				{
					SwitchArctapMode();
				}
			}

			UpdateArcTapCursor();
		}

		private void UpdateArcTapCursor()
		{
			if (Mode != ClickToCreateMode.ArcTap || ArctapMode != ClickToCreateArctapMode.OnArc)
			{
				AdeCursorManager.Instance.ArcTapCursorEnabled = false;
				return;
			}
			ArcArc currentArc = GetCurrentArc();
			if (currentArc == null)
			{
				AdeCursorManager.Instance.ArcTapCursorEnabled = false;
				return;
			}
			int timing = AdeCursorManager.Instance.AttachedTiming;
			bool canAddArcTap = MayAddArcTapOnArc();
			AdeCursorManager.Instance.ArcTapCursorEnabled = canAddArcTap;
			AdeCursorManager.Instance.ArcTapCursorIsSfx = currentArc.IsSfx;
			if (!canAddArcTap) return;
			float t = 1f * (timing - currentArc.Timing) / (currentArc.EndTiming - currentArc.Timing);
			Vector2 gizmo = new Vector3(ArcAlgorithm.ArcXToWorld(ArcAlgorithm.X(currentArc.XStart, currentArc.XEnd, t, currentArc.CurveType)),
									ArcAlgorithm.ArcYToWorld(ArcAlgorithm.Y(currentArc.YStart, currentArc.YEnd, t, currentArc.CurveType)) - 0.5f);
			AdeCursorManager.Instance.ArcTapCursorPosition = gizmo;
		}
		public void SwitchColor()
		{
			if (!AdeOperationManager.Instance.HasOngoingOperation)
			{
				currentArcColor = currentArcColor == 0 ? 1 : 0;
			}
		}
		public void SwitchIsVoid()
		{

			if (!AdeOperationManager.Instance.HasOngoingOperation)
			{
				currentArcIsVoid = !currentArcIsVoid;
			}
		}
		public void SwitchType()
		{
			if (!AdeOperationManager.Instance.HasOngoingOperation)
			{
				switch (currentArcCurveType)
				{
					case ArcCurveType.S:
						currentArcCurveType = ArcCurveType.B;
						break;
					case ArcCurveType.B:
						currentArcCurveType = ArcCurveType.Si;
						break;
					case ArcCurveType.Si:
						currentArcCurveType = ArcCurveType.So;
						break;
					case ArcCurveType.So:
						currentArcCurveType = ArcCurveType.SiSi;
						break;
					case ArcCurveType.SiSi:
						currentArcCurveType = ArcCurveType.SoSo;
						break;
					case ArcCurveType.SoSo:
						currentArcCurveType = ArcCurveType.SiSo;
						break;
					case ArcCurveType.SiSo:
						currentArcCurveType = ArcCurveType.SoSi;
						break;
					case ArcCurveType.SoSi:
						currentArcCurveType = ArcCurveType.S;
						break;
				}
			}
		}
		public void SwitchArctapMode()
		{

			if (!AdeOperationManager.Instance.HasOngoingOperation)
			{
				switch (ArctapMode)
				{
					case ClickToCreateArctapMode.OnArc:
						ArctapMode = ClickToCreateArctapMode.Single;
						break;
					case ClickToCreateArctapMode.Single:
						ArctapMode = ClickToCreateArctapMode.OnArc;
						break;
				}
			}
		}
		public void SetClickToCreateMode(int mode)
		{
			ClickToCreateMode newMode = (ClickToCreateMode)mode;
			SetClickToCreateMode(newMode);
		}
		public void SetClickToCreateMode(ClickToCreateMode newMode)
		{
			if (!AdeOperationManager.Instance.HasOngoingOperation)
			{
				Mode = newMode;
			}
		}
		public void SetClickToCreateArctapMode(int mode)
		{
			ClickToCreateArctapMode newMode = (ClickToCreateArctapMode)mode;
			SetClickToCreateArctapMode(newMode);
		}
		public void SetClickToCreateArctapMode(ClickToCreateArctapMode newMode)
		{
			if (!AdeOperationManager.Instance.HasOngoingOperation)
			{
				ArctapMode = newMode;
			}
		}
		public void SetArcCurveTypeMode(int type)
		{
			if (!AdeOperationManager.Instance.HasOngoingOperation)
			{
				currentArcCurveType = (ArcCurveType)type;
			}
		}

		private ArcArc GetCurrentArc()
		{
			var notes = AdeSelectionManager.Instance.SelectedNotes;
			if (notes.Count != 1)
			{
				return null;
			}
			var note = notes[0];
			if (note is ArcArc arc)
			{
				if (arc.IsVoid)
				{
					return arc;
				}
			}
			return null;
		}

		public bool MayAddArcTapOnArc()
		{
			if (Mode != ClickToCreateMode.ArcTap)
			{
				return false;
			}
			if (!AdeCursorManager.Instance.IsTrackHit)
			{
				return false;
			}
			ArcArc currentArc = GetCurrentArc();
			if (currentArc != null)
			{
				int timing = AdeCursorManager.Instance.AttachedTiming;
				if (currentArc.Timing <= timing && currentArc.EndTiming >= timing && currentArc.Timing < currentArc.EndTiming)
				{
					return true;
				}
			}
			return false;
		}

		private async UniTask ExecuteAddHold(CancellationToken cancellationToken)
		{
			int track = AdeCursorManager.Instance.AttachedTrack;
			int timing = AdeCursorManager.Instance.AttachedTiming;
			var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
			ArcHold note = new ArcHold() { Timing = timing, Track = track, EndTiming = timing, TimingGroup = timingGroup };
			AdeCommandManager.Instance.Prepare(new AddArcEventCommand(note));

			try
			{
				Action<int> updateEndTiming = (int timing) =>
				{
					if (timing > note.Timing)
					{
						note.EndTiming = timing;
					}
				};
				while (true)
				{
					var endTiming = await AdeCursorManager.Instance.SelectTiming(Progress.Create(updateEndTiming), cancellationToken);
					if (endTiming > note.Timing)
					{
						updateEndTiming(endTiming);
						note.CalculateJudgeTimings();
						ArcGameplayManager.Instance.ResetJudge();
						break;
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				AdeCommandManager.Instance.Cancel();
				throw ex;
			}
			AdeCommandManager.Instance.Commit();
			AdeSelectionManager.Instance.SelectNote(note);
		}

		private async UniTask ExecuteAddArc(CancellationToken cancellationToken)
		{
			int track = AdeCursorManager.Instance.AttachedTrack;
			int timing = AdeCursorManager.Instance.AttachedTiming;
			var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
			ArcArc note = new ArcArc() { Timing = timing, EndTiming = timing, Color = currentArcColor, Effect = "none", LineType = currentArcLineType, CurveType = currentArcCurveType, TimingGroup = timingGroup };
			AdeCommandManager.Instance.Prepare(new AddArcEventCommand(note));

			try
			{
				Action<Vector2> updateStartCoordinate = (coord) =>
				{
					note.XStart = coord.x;
					note.YStart = coord.y;
					note.XEnd = coord.x;
					note.YEnd = coord.y;
					note.Rebuild();
					ArcArcManager.Instance.CalculateArcRelationship();
				};
				var startCoord = await AdeCursorManager.Instance.SelectCoordinate(note.Timing, Progress.Create(updateStartCoordinate), cancellationToken);
				updateStartCoordinate(startCoord);

				Action<int> updateEndTiming = (int timing) =>
				{
					if (timing >= note.Timing)
					{
						note.EndTiming = timing;
						note.Rebuild();
						note.CalculateJudgeTimings();
						ArcGameplayManager.Instance.ResetJudge();
						ArcArcManager.Instance.CalculateArcRelationship();
					}
				};
				while (true)
				{
					var endTiming = await AdeCursorManager.Instance.SelectTiming(Progress.Create(updateEndTiming), cancellationToken, true);
					if (endTiming >= note.Timing)
					{
						updateEndTiming(endTiming);
						break;
					}
				}

				Action<Vector2> updateEndCoordinate = (coord) =>
				{
					note.XEnd = coord.x;
					note.YEnd = coord.y;
					note.Rebuild();
					ArcArcManager.Instance.CalculateArcRelationship();
				};
				var endCoord = await AdeCursorManager.Instance.SelectCoordinate(note.EndTiming, Progress.Create(updateEndCoordinate), cancellationToken);
				updateEndCoordinate(endCoord);
			}
			catch (OperationCanceledException ex)
			{
				AdeCommandManager.Instance.Cancel();
				throw ex;
			}
			AdeCommandManager.Instance.Commit();
			AdeSelectionManager.Instance.SelectNote(note);
		}

		private async UniTask ExecuteAddSingleArctap(CancellationToken cancellationToken)
		{
			int timing = AdeCursorManager.Instance.AttachedTiming;
			var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
			ArcArc arc = new ArcArc() { Timing = timing, EndTiming = timing + 1, Color = currentArcColor, Effect = "none", LineType = ArcLineType.TrueIsVoid, CurveType = ArcCurveType.S, TimingGroup = timingGroup };
			ArcArcTap arctap = new ArcArcTap() { Timing = timing };
			AdeCommandManager.Instance.Prepare(new BatchCommand(new ICommand[]{
				new AddArcEventCommand(arc),
				new AddArcTapCommand(arc,arctap),
			}, "添加单个 ArcTap"));
			try
			{
				Action<Vector2> updateStartCoordinate = (coord) =>
				{
					arc.XStart = coord.x;
					arc.YStart = coord.y;
					arc.XEnd = coord.x;
					arc.YEnd = coord.y;
					arc.Rebuild();
					ArcArcManager.Instance.CalculateArcRelationship();
				};
				var startCoord = await AdeCursorManager.Instance.SelectCoordinate(arc.Timing, Progress.Create(updateStartCoordinate), cancellationToken);
				updateStartCoordinate(startCoord);
			}
			catch (OperationCanceledException ex)
			{
				AdeCommandManager.Instance.Cancel();
				throw ex;
			}
			AdeCommandManager.Instance.Commit();
			AdeSelectionManager.Instance.SelectNote(arctap);
		}

		public override AdeOperationResult TryExecuteOperation()
		{
			if (!Enable)
			{
				return false;
			}
			if (!AdeGameplayContentInputHandler.InputActive)
			{
				return false;
			}
			if (Mouse.current.leftButton.wasPressedThisFrame)
			{
				if (AdeCursorManager.Instance.IsTrackHit)
				{
					if (mode == ClickToCreateMode.Tap)
					{
						int track = AdeCursorManager.Instance.AttachedTrack;
						int timing = AdeCursorManager.Instance.AttachedTiming;
						var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
						ArcTap note = new ArcTap() { Timing = timing, Track = track, TimingGroup = timingGroup };
						AdeCommandManager.Instance.Add(new AddArcEventCommand(note));
						return true;
					}
					else if (mode == ClickToCreateMode.ArcTap)
					{
						if (arctapMode == ClickToCreateArctapMode.OnArc)
						{
							ArcArc currentArc = GetCurrentArc();
							if (currentArc != null)
							{
								int timing = AdeCursorManager.Instance.AttachedTiming;
								if (currentArc.Timing <= timing && currentArc.EndTiming >= timing && currentArc.Timing < currentArc.EndTiming)
								{
									ArcArcTap note = new ArcArcTap() { Timing = timing };
									AdeCommandManager.Instance.Add(new AddArcTapCommand(currentArc, note));
									return true;
								}
							}
						}
						else if (arctapMode == ClickToCreateArctapMode.Single)
						{
							var cancellation = new CancellationTokenSource();
							return AdeOperationResult.FromOngoingOperation(new AdeOngoingOperation
							{
								task = ExecuteAddSingleArctap(cancellation.Token).WithExceptionLogger(),
								cancellation = cancellation,
							});
						}
					}
					else if (mode == ClickToCreateMode.Hold)
					{
						var cancellation = new CancellationTokenSource();
						return AdeOperationResult.FromOngoingOperation(new AdeOngoingOperation
						{
							task = ExecuteAddHold(cancellation.Token).WithExceptionLogger(),
							cancellation = cancellation,
						});
					}
					else if (mode == ClickToCreateMode.Arc)
					{
						var cancellation = new CancellationTokenSource();
						return AdeOperationResult.FromOngoingOperation(new AdeOngoingOperation
						{
							task = ExecuteAddArc(cancellation.Token).WithExceptionLogger(),
							cancellation = cancellation,
						});
					}
				}
			}
			return false;
		}
	}
}
