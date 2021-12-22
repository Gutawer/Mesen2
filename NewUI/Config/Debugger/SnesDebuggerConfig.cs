﻿using Avalonia;
using Avalonia.Media;
using Mesen.Debugger;
using Mesen.Interop;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Linq;
using System.Reactive;
using Mesen.ViewModels;

namespace Mesen.Config
{
	public class SnesDebuggerConfig : ViewModelBase
	{
		[Reactive] public bool BreakOnBrk { get; set; } = false;
		[Reactive] public bool BreakOnCop { get; set; } = false;
		[Reactive] public bool BreakOnWdm { get; set; } = false;
		[Reactive] public bool BreakOnStp { get; set; } = false;

		public void ApplyConfig()
		{
			ConfigApi.SetDebuggerFlag(DebuggerFlags.BreakOnBrk, BreakOnBrk);
			ConfigApi.SetDebuggerFlag(DebuggerFlags.BreakOnCop, BreakOnCop);
			ConfigApi.SetDebuggerFlag(DebuggerFlags.BreakOnWdm, BreakOnWdm);
			ConfigApi.SetDebuggerFlag(DebuggerFlags.BreakOnStp, BreakOnStp);
		}
	}
}
