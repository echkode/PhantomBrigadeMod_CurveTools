// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.IO;

using UnityEngine;

namespace EchKode.PBMods.GuidanceCurve
{
	partial class ModLink
	{
		internal sealed class ModSettings
		{
			[System.Flags]
			internal enum LoggingFlag
			{
				None = 0,
				System = 0x1,
				All = 0xF,
			}

#pragma warning disable CS0649
			public LoggingFlag logging;
			public string guidanceCurveDirectory = Path.Combine(modPath, "Curves\\Guidance");
			public string damageFalloffCurveDirectory = Path.Combine(modPath, "Curves\\Damage");
#pragma warning restore CS0649

			internal bool IsLoggingEnabled(LoggingFlag flag) => (logging & flag) == flag;
		}

		internal static ModSettings Settings;

		internal static void LoadSettings()
		{
			var settingsPath = Path.Combine(modPath, "settings.yaml");
			Settings = UtilitiesYAML.ReadFromFile<ModSettings>(settingsPath, false);
			if (Settings == null)
			{
				Settings = new ModSettings();

				Debug.LogFormat(
					"Mod {0} ({1}) no settings file found, using defaults | path: {2}",
					modIndex,
					modId,
					settingsPath);
			}

			if (Settings.logging != ModSettings.LoggingFlag.None)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) diagnostic logging is on: {2}",
					modIndex,
					modId,
					Settings.logging);

				Debug.LogFormat(
					"Mod {0} ({1}) settings\n  guidance curves: {2}\n  damage falloff curves: {3}",
					modIndex,
					modId,
					Settings.guidanceCurveDirectory,
					Settings.damageFalloffCurveDirectory);
			}
		}
	}
}
