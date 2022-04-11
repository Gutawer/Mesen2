using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Mesen.Debugger.Controls;
using Mesen.Debugger.ViewModels;
using Mesen.Interop;
using System.ComponentModel;
using Mesen.Config;
using Avalonia.Input;
using Mesen.Localization;
using Mesen.Debugger.Labels;
using Avalonia.Interactivity;
using Mesen.Utilities;
using System.Linq;
using Avalonia.Threading;

namespace Mesen.Debugger.Windows
{
	public class EventViewerWindow : Window, INotificationHandler
	{
		private EventViewerViewModel _model;
		private PixelPoint? _prevMousePos = null;

		[Obsolete("For designer only")]
		public EventViewerWindow() : this(CpuType.Snes) { }

		public EventViewerWindow(CpuType cpuType)
		{
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif

			PictureViewer viewer = this.FindControl<ScrollPictureViewer>("picViewer").InnerViewer;
			_model = new EventViewerViewModel(cpuType, viewer, this);
			_model.Config.LoadWindowSettings(this);
			DataContext = _model;

			if(Design.IsDesignMode) {
				return;
			}

			viewer.PointerMoved += Viewer_PointerMoved;
			viewer.PointerLeave += Viewer_PointerLeave;
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			_model.Config.SaveWindowSettings(this);
		}

		private void OnSettingsClick(object sender, RoutedEventArgs e)
		{
			_model.Config.ShowSettingsPanel = !_model.Config.ShowSettingsPanel;
		}

		private void Viewer_PointerLeave(object? sender, PointerEventArgs e)
		{
			if(sender is PictureViewer viewer) {
				ToolTip.SetTip(viewer, null);
				ToolTip.SetIsOpen(viewer, false);
			}
			_prevMousePos = null;
			_model.GridHighlightPoint = null;
		}

		private void Viewer_PointerMoved(object? sender, PointerEventArgs e)
		{
			if(sender is PictureViewer viewer) {
				PixelPoint? point = viewer.GetGridPointFromMousePoint(e.GetCurrentPoint(viewer).Position);
				if(point == _prevMousePos) {
					return;
				}
				_prevMousePos = point;

				DebugEventInfo? evt = null;
				if(point != null) {
					evt = DebugApi.GetEventViewerEvent(_model.CpuType, (ushort)point.Value.Y, (ushort)point.Value.X);
					if(evt.Value.ProgramCounter == UInt32.MaxValue) {
						evt = null;
					}
					_model.UpdateHighlightPoint(point.Value, evt);
				}

				if(point != null && evt != null) {
					//Force tooltip to update its position
					ToolTip.SetHorizontalOffset(viewer, 14);
					ToolTip.SetHorizontalOffset(viewer, 15);

					ToolTip.SetTip(viewer, new DynamicTooltip() { Items = GetTooltipData(evt.Value) });
					ToolTip.SetIsOpen(viewer, true);
				} else {
					ToolTip.SetTip(viewer, null);
					ToolTip.SetIsOpen(viewer, false);
				}
			}
		}

		private TooltipEntries GetTooltipData(DebugEventInfo evt)
		{
			TooltipEntries entries = new();
			entries.AddEntry("Type", ResourceHelper.GetEnumText(evt.Type));
			entries.AddEntry("Scanline", evt.Scanline.ToString());
			entries.AddEntry(_model.CpuType == CpuType.Snes ? "H-Clock" : "Cycle", evt.Cycle.ToString());
			entries.AddEntry("PC", "$" + evt.ProgramCounter.ToString("X" + _model.CpuType.GetAddressSize()));

			switch(evt.Type) {
				case DebugEventType.Register:
					bool isWrite = evt.Operation.Type == MemoryOperationType.Write || evt.Operation.Type == MemoryOperationType.DmaWrite;
					bool isDma = evt.Operation.Type == MemoryOperationType.DmaWrite || evt.Operation.Type == MemoryOperationType.DmaRead;

					CodeLabel? label = LabelManager.GetLabel(new AddressInfo() { Address = (int)evt.Operation.Address, Type = MemoryType.SnesMemory });
					string registerText = "$" + evt.Operation.Address.ToString("X4");
					if(label != null) {
						registerText = label.Label + " (" + registerText + ")";
					}

					entries.AddEntry("Register", registerText + (isWrite ? " (Write)" : " (Read)") + (isDma ? " (DMA)" : ""));
					entries.AddEntry("Value", "$" + evt.Operation.Value.ToString("X2"));
					break;
			}

			string details = EventViewerViewModel.GetEventDetails(evt, false);
			if(details.Length > 0) {
				entries.AddEntry("Details", details);
			}

			return entries;
		}

		public void ProcessNotification(NotificationEventArgs e)
		{
			switch(e.NotificationType) {
				case ConsoleNotificationType.GameLoaded:
					RomInfo romInfo = EmuApi.GetRomInfo();
					if(!romInfo.CpuTypes.Contains(_model.CpuType)) {
						_model.CpuType = romInfo.ConsoleType.GetMainCpuType();
					}

					Dispatcher.UIThread.Post(() => {
						_model.UpdateConfig();
					});
					break;

				case ConsoleNotificationType.EventViewerRefresh:
					if(_model.Config.AutoRefresh) {
						_model.RefreshData(false);
					}
					break;

				case ConsoleNotificationType.CodeBreak:
					if(_model.Config.RefreshOnBreakPause) {
						_model.RefreshData(true);
					}
					break;
			}
		}
	}
}
