using System.Collections.Generic;
using System.IO;

using PhantomBrigade.Data;
using PBDataMultiLinkerSubsystem = PhantomBrigade.Data.DataMultiLinkerSubsystem;

using UnityEngine;

namespace EchKode.PBMods.GuidanceCurve
{
	using GuidanceInputProp = System.Func<DataBlockGuidanceData, IDataBlockGuidanceInput>;
	using GuidanceInputUpdater = System.Action<DataBlockGuidanceData, IDataBlockGuidanceInput>;
	using CurveUpdatePredicate = System.Func<string, DataContainerSubsystem, bool>;
	using CurveTypeUpdater = System.Action<string, DataContainerSubsystem, AnimationCurveSerialized, string>;

	internal static class DataMultiLinkerSubsystem
	{
		internal static readonly List<(string InputType, GuidanceInputProp, GuidanceInputUpdater Update)> GuidanceCurves
			= new List<(string, GuidanceInputProp, GuidanceInputUpdater)>()
			{
				("InputTargetHeight", gd => gd.inputTargetHeight, (gd, ic) => gd.inputTargetHeight = ic),
				("InputTargetBlend", gd => gd.inputTargetBlend,(gd, ic) => gd.inputTargetBlend = ic),
				("InputTargetUpdate", gd => gd.inputTargetUpdate,(gd, ic) => gd.inputTargetUpdate = ic),
				("InputTargetOffset", gd => gd.inputTargetOffset,(gd, ic) => gd.inputTargetOffset = ic),
				("InputSteering", gd => gd.inputSteering,(gd, ic) => gd.inputSteering = ic),
				("InputThrottle", gd => gd.inputThrottle,(gd, ic) => gd.inputThrottle = ic),
			};

		internal static void UpdateGuidanceCurves()
		{
			var curveDirectoryPath = ModLink.guidanceCurveDirectory;
			if (!Directory.Exists(curveDirectoryPath))
			{
				return;
			}

			Debug.LogFormat(
				"Mod {0} ({1}) loading missile guidance curves from {2}",
				ModLink.modIndex,
				ModLink.modId,
				curveDirectoryPath);

			foreach (var (inputType, _, update) in GuidanceCurves)
			{
				var cfp = Path.Combine(curveDirectoryPath, inputType);
				if (!Directory.Exists(cfp))
				{
					continue;
				}

				Debug.LogFormat(
					"Mod {0} ({1}) loading {2} guidance curves from {3}",
					ModLink.modIndex,
					ModLink.modId,
					inputType,
					cfp);

				UpdateCurves(
					inputType,
					cfp,
					HasGuidanceData,
					(k, s, c, t) => UpdateGuidanceCurve(k, s, update, c, t));
			}
		}

		internal static bool HasGuidanceData(string key, DataContainerSubsystem subsystem)
		{
			if (subsystem?.projectile.guidanceData == null)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) {2} does not have guidance data",
					ModLink.modIndex,
					ModLink.modId,
					key);
				return false;
			}
			return true;
		}

		internal static void UpdateGuidanceCurve(
			string key,
			DataContainerSubsystem subsystem,
			GuidanceInputUpdater update,
			AnimationCurveSerialized curve,
			string inputType)
		{
			var block = new DataBlockGuidanceInputCurve
			{
				curveSerialized = curve,
				curve = new AnimationCurveContainer((AnimationCurve)curve)
			};
			update(subsystem.projectile.guidanceData, block);

			Debug.LogFormat(
				"Mod {0} ({1}) replaced {3} curve for {2}",
				ModLink.modIndex,
				ModLink.modId,
				key,
				inputType);
		}

		internal static void UpdateCurves(
			string curveType,
			string curveDirectoryPath,
			CurveUpdatePredicate canUpdate,
			CurveTypeUpdater update)
		{
			foreach (var n in Directory.EnumerateFiles(curveDirectoryPath, "*.yaml"))
			{
				var key = Path.GetFileNameWithoutExtension(n);
				if (!PBDataMultiLinkerSubsystem.data.ContainsKey(key))
				{
					Debug.LogWarningFormat(
						"Mod {0} ({1}) found invalid key for {3} curve | key: {2}",
						ModLink.modIndex,
						ModLink.modId,
						key,
						curveType);
					continue;
				}

				var subsystem = PBDataMultiLinkerSubsystem.data[key];
				if (!canUpdate(key, subsystem))
				{
					continue;
				}

				var curve = UtilitiesYAML.ReadFromFile<AnimationCurveSerialized>(n, false);
				if (curve == null)
				{
					Debug.LogWarningFormat(
						"Mod {0} ({1}) unable to load {3} curve for {2}",
						ModLink.modIndex,
						ModLink.modId,
						key,
						curveType);
					continue;
				}

				update(key, subsystem, curve, curveType);
			}
		}
	}
}
