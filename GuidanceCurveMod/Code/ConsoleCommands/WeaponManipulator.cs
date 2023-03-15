// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using HarmonyLib;

using QFSW.QC;

namespace EchKode.PBMods.GuidanceCurve.ConsoleCommands
{
	using CommandList = List<(string QCName, string Description, MethodInfo Method)>;

	internal static class WeaponManipulator
	{
		internal static CommandList Commands() => new CommandList()
		{
			("wpn.extract-damage-curves", "Extract damage falloff curves for all weapons", AccessTools.DeclaredMethod(typeof(WeaponManipulator), nameof(ExtractAllCurves))),
		};

		static void ExtractAllCurves()
		{
			try
			{
				var outputDirectory = new DirectoryInfo(ModLink.Settings.damageFalloffCurveDirectory);
				var extracted = CurveExtractor.ExtractAllDamageFalloff(outputDirectory);
				if (extracted.Count != 0)
				{
					QuantumConsole.Instance.LogToConsole($"Subsystems with damage falloff curves ({extracted.Count}):");
					foreach (var (subsystem, p) in extracted)
					{
						QuantumConsole.Instance.LogToConsole($"  {subsystem}: {p}");
					}
					return;
				}
			}
			catch (Exception ex)
			{
				QuantumConsole.Instance.LogToConsole(ex.Message);
			}

			QuantumConsole.Instance.LogToConsole("No damage falloff curves were extracted");
		}
	}
}
