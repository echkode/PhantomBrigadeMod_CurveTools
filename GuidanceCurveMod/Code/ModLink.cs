// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System;
using System.IO;

using HarmonyLib;

using PBModManager = PhantomBrigade.Mods.ModManager;

using UnityEngine;

namespace EchKode.PBMods.GuidanceCurve
{
	public class ModLink : PhantomBrigade.Mods.ModLink
	{
		internal static int modIndex;
		internal static string modId;
		internal static string modPath;
		internal static string guidanceCurveDirectory;

		public override void OnLoad(Harmony harmonyInstance)
		{
			// Uncomment to get a file on the desktop showing the IL of the patched methods.
			// Output from FileLog.Log() will trigger the generation of that file regardless if this is set so
			// FileLog.Log() should be put in a guard.
			//Harmony.DEBUG = true;

			modIndex = PBModManager.loadedMods.Count;
			modId = metadata.id;
			modPath = metadata.path;
			guidanceCurveDirectory = Path.Combine(modPath, "Curves\\Guidance");

			var patchAssembly = typeof(ModLink).Assembly;
			Debug.LogFormat(
				"Mod {0} ({1}) is executing OnLoad | Using HarmonyInstance.PatchAll on assembly ({2}) | Directory: {3} | Full path: {4}",
				modIndex,
				modId,
				patchAssembly.FullName,
				metadata.directory,
				metadata.path);
			harmonyInstance.PatchAll(patchAssembly);

			if (Harmony.DEBUG)
			{
				FileLog.Log($"{new string('=', 20)} Start [{DateTime.Now:u}] {new string('=', 20)}");
				FileLog.Log("!!! PBMods patches applied");
			}

			ConsoleCommands.Console.RegisterCommands();

			Debug.LogFormat(
				"Mod {0} ({1}) is initialized",
				modIndex,
				modId);
		}
	}
}
