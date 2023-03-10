// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.IO;

using PhantomBrigade.Data;
using PBDataMultiLinkerSubsystem = PhantomBrigade.Data.DataMultiLinkerSubsystem;

namespace EchKode.PBMods.GuidanceCurve
{
	using ExtractorList = List<(string Subsystem, List<(string InputType, string PathName)> Curves)>;

	static class CurveExtractor
	{
		internal static ExtractorList ExtractAll(DirectoryInfo outputDirectory)
		{
			if (!outputDirectory.Exists)
			{
				throw new System.ArgumentException("Output directory should exist: " + outputDirectory.FullName, nameof(outputDirectory));
			}

			foreach (var (inputType, _, _) in DataMultiLinkerSubsystem.GuidanceCurves)
			{
				outputDirectory.CreateSubdirectory(inputType);
			}

			var extracted = new ExtractorList();
			foreach (var kvp in PBDataMultiLinkerSubsystem.data)
			{
				var curves = ExtractCurves(kvp.Key, kvp.Value, outputDirectory);
				if (curves.Count != 0)
				{
					extracted.Add((kvp.Key, curves));
				}
			}
			return extracted;
		}

		static List<(string, string)> ExtractCurves(string key, DataContainerSubsystem subsystem, DirectoryInfo outputDirectory)
		{
			var curves = new List<(string, string)>();
			if (subsystem.projectileProcessed == null)
			{
				return curves;
			}
			if (subsystem.projectileProcessed.guidanceData == null)
			{
				return curves;
			}

			var gd = subsystem.projectileProcessed.guidanceData;
			foreach (var (inputType, prop, _) in DataMultiLinkerSubsystem.GuidanceCurves)
			{
				var (ok, pathName) = SaveCurve(key, inputType, prop(gd), outputDirectory);
				if (ok)
				{
					curves.Add((inputType, pathName));
				}
			}

			return curves;
		}

		static (bool, string)
			SaveCurve(string key, string name, IDataBlockGuidanceInput input, DirectoryInfo outputDirectory)
		{
			if (input == null)
			{
				return (false, "");
			}
			if (!(input is DataBlockGuidanceInputCurve ib))
			{
				return (false, "");
			}

			ib.OnBeforeSerialization();
			if (ib.curveSerialized == null)
			{
				return (false, "");
			}

			var pathName = Path.Combine(outputDirectory.FullName, name, key + ".yaml");
			UtilitiesYAML.SaveToFile(pathName, ib.curveSerialized);
			return (true, pathName);
		}

		internal static List<(string Key, string Path)> ExtractAllDamageFalloff(DirectoryInfo outputDirectory)
		{
			if (!outputDirectory.Exists)
			{
				throw new System.ArgumentException("Output directory should exist: " + outputDirectory.FullName, nameof(outputDirectory));
			}

			var subsystems = new List<(string, string)>();
			foreach (var kvp in PBDataMultiLinkerSubsystem.data)
			{
				if (kvp.Value.projectileProcessed == null)
				{
					continue;
				}
				if (kvp.Value.projectileProcessed.falloff == null)
				{
					continue;
				}
				var (ok, p) = SaveCurve(kvp.Key, kvp.Value.projectileProcessed.falloff, outputDirectory);
				if (!ok)
				{
					continue;
				}
				subsystems.Add((kvp.Key, p));
			}

			return subsystems;
		}

		static (bool, string) SaveCurve(string key, DataBlockProjectileDamageFalloff falloff, DirectoryInfo outputDirectory)
		{
			falloff.OnBeforeSerialization();
			if (falloff.curveSerialized == null)
			{
				return (false, "");
			}
			var pathName = Path.Combine(outputDirectory.FullName, key + ".yaml");
			UtilitiesYAML.SaveToFile(pathName, falloff.curveSerialized);
			return (true, pathName);
		}
	}
}
