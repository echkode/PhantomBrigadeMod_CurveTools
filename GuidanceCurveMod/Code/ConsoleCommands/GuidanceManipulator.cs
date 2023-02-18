using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using PhantomBrigade.Data;
using PBDataHelperStats = PhantomBrigade.Data.DataHelperStats;

using QFSW.QC;

using UnityEngine;

namespace EchKode.PBMods.GuidanceCurve.ConsoleCommands
{
	using CommandList = List<(string QCName, string Description, MethodInfo Method)>;

	internal static class GuidanceManipulator
	{
		internal static CommandList Commands() => new CommandList()
		{
			("gd.extract-curves", "Extract all guidance input curves", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(ExtractAllCurves))),
			("gd.print-guidance-data", "Print guidance data for a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(PrintGuidanceData))),
			("gd.replace-guidance-curve", "Use a curve for guidance input on a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(ReplaceGuidanceCurve), new Type[] { typeof(string), typeof(bool), typeof(string) })),
			("gd.replace-guidance-curve", "Use a curve for guidance input on a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(ReplaceGuidanceCurve), new Type[] { typeof(string), typeof(bool), typeof(string), typeof(string) })),
			("gd.replace-guidance-constant", "Use a constant for guidance input on a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(ReplaceGuidanceCurve), new Type[] { typeof(string), typeof(bool), typeof(string), typeof(float) })),
			("gd.replace-guidance-linear", "Use a line for guidance input on a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(ReplaceGuidanceCurve), new Type[] { typeof(string), typeof(bool), typeof(string), typeof(float), typeof(float) })),
			("gd.set-projectile-speed", "Change speed of projectile on a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(SetProjectileSpeed))),
			("gd.set-throttle-force", "Change throttle force on a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(SetGuidanceAccelerationForce))),
			("gd.set-drag", "Change drag on a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(SetGuidanceDrag))),
			("gd.set-angular-drag", "Change angular drag on a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(SetGuidanceAngularDrag))),
			("gd.set-steering-force", "Change steering force on a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(SetGuidanceSteeringForce))),
			("gd.set-pitch-force", "Change pitch force on a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(SetGuidancePitchForce))),
			("gd.set-steering-pid", "Change steering PID on a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(SetGuidanceSteeringPID))),
			("gd.set-pitch-pid", "Change pitch PID on a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(SetGuidancePitchPID))),
			("gd.set-target-height", "Change target height on a unit's weapon", AccessTools.DeclaredMethod(typeof(GuidanceManipulator), nameof(SetGuidanceTargetHeight))),
		};

		static void ExtractAllCurves()
		{
			try
			{
				var outputDirectory = new DirectoryInfo(ModLink.guidanceCurveDirectory);
				var extracted = CurveExtractor.ExtractAll(outputDirectory);
				if (extracted.Count != 0)
				{
					QuantumConsole.Instance.LogToConsole($"Subsystems with guidance curves ({extracted.Count}):");
					foreach (var (subsystem, curves) in extracted)
					{
						var cnames = string.Join(", ", curves.Select(x => x.InputType));
						QuantumConsole.Instance.LogToConsole($"  {subsystem} ({cnames})");
					}
					return;
				}
			}
			catch (Exception ex)
			{
				QuantumConsole.Instance.LogToConsole(ex.Message);
			}

			QuantumConsole.Instance.LogToConsole("No guidance curves were extracted");
		}

		static void PrintGuidanceData(string unitID, bool primary)
		{
			if (!CombatInspector.CombatStateCheck())
			{
				return;
			}

			var (ok, unit) = CombatInspector.UnitCheck(unitID);
			if (!ok)
			{
				return;
			}

			var (sok, subsystem) = FindSubsystemWithGuidance(unit, primary);
			if (!sok)
			{
				return;
			}

			QuantumConsole.Instance.LogToConsole($"Subsystem: {subsystem.dataLinkSubsystem.data.key}");
			var rounds = subsystem.dataLinkSubsystem.data.statsProcessed["act_count"].value;
			QuantumConsole.Instance.LogToConsole($"act_count: {rounds}");
			var rmin = subsystem.dataLinkSubsystem.data.statsProcessed["wpn_range_min"].value;
			var rmax = subsystem.dataLinkSubsystem.data.statsProcessed["wpn_range_max"].value;
			QuantumConsole.Instance.LogToConsole($"wpn_range: {rmin} - {rmax}");
			var lifetime = subsystem.dataLinkSubsystem.data.statsProcessed.ContainsKey("wpn_proj_lifetime")
				? subsystem.dataLinkSubsystem.data.statsProcessed["wpn_proj_lifetime"].value
				: 0f;
			QuantumConsole.Instance.LogToConsole($"wpn_proj_lifetime: {lifetime}");
			var speed = subsystem.dataLinkSubsystem.data.statsProcessed.ContainsKey("wpn_speed")
				? subsystem.dataLinkSubsystem.data.statsProcessed["wpn_speed"].value
				: 0f;
			QuantumConsole.Instance.LogToConsole($"wpn_speed: {speed}");
			var radius = subsystem.dataLinkSubsystem.data.statsProcessed.ContainsKey("wpn_scatter_radius")
				? subsystem.dataLinkSubsystem.data.statsProcessed["wpn_scatter_radius"].value
				: 15f;
			QuantumConsole.Instance.LogToConsole($"wpn_scatter_radius: {radius}");
			var gd = subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData;
			QuantumConsole.Instance.LogToConsole($"drag: {gd.rigidbodyDrag}");
			QuantumConsole.Instance.LogToConsole($"angular drag: {gd.rigidbodyAngularDrag}");
			QuantumConsole.Instance.LogToConsole($"throttle force: {gd.driverAccelerationForce}");
			QuantumConsole.Instance.LogToConsole($"steering force: {gd.driverSteeringForce}");
			QuantumConsole.Instance.LogToConsole($"pitch force: {gd.driverPitchForce}");
			var (sp, si, sd) = gd.steeringPID == null
				? (0f, 0f, 0f)
				: (gd.steeringPID.proportionalGain, gd.steeringPID.integralGain, gd.steeringPID.derivativeGain);
			QuantumConsole.Instance.LogToConsole($"steering PID: {sp}/{si}/{sd}");
			var (pp, pi, pd) = gd.pitchPID == null
				? (0f, 0f, 0f)
				: (gd.pitchPID.proportionalGain, gd.pitchPID.integralGain, gd.pitchPID.derivativeGain);
			QuantumConsole.Instance.LogToConsole($"pitch PID: {pp}/{pi}/{pd}");
			QuantumConsole.Instance.LogToConsole($"target height: {gd.inputTargetHeightScale}");
			QuantumConsole.Instance.LogToConsole("Guidance inputs");
			PrintGuidanceInput("target height", gd.inputTargetHeight);
			PrintGuidanceInput("target blend", gd.inputTargetBlend);
			PrintGuidanceInput("target update", gd.inputTargetUpdate);
			PrintGuidanceInput("target offset", gd.inputTargetOffset);
			PrintGuidanceInput("steering", gd.inputSteering);
			PrintGuidanceInput("throttle", gd.inputThrottle);
		}

		static void PrintGuidanceInput(string label, IDataBlockGuidanceInput ic)
		{
			switch (ic)
			{
				case DataBlockGuidanceInputConstant c:
					QuantumConsole.Instance.LogToConsole($"  {label}: {c.value}");
					break;
				case DataBlockGuidanceInputLinear n:
					QuantumConsole.Instance.LogToConsole($"  {label}: {n.valueFrom} {n.valueTo}");
					break;
				case DataBlockGuidanceInputCurve _:
					QuantumConsole.Instance.LogToConsole($"  {label}: curve");
					break;
			}
		}

		static void ReplaceGuidanceCurve(string unitID, bool primary, string inputType) =>
			ReplaceGuidanceCurve(unitID, primary, inputType, ModLink.guidanceCurveDirectory);

		static void ReplaceGuidanceCurve(string unitID, bool primary, string inputType, string curveSourcePath)
		{
			if (!CombatInspector.CombatStateCheck())
			{
				return;
			}

			var (ok, unit) = CombatInspector.UnitCheck(unitID);
			if (!ok)
			{
				return;
			}

			if (!DataMultiLinkerSubsystem.GuidanceCurves.Any(entry => entry.InputType == inputType))
			{
				QuantumConsole.Instance.LogToConsole("Invalid input type");
				return;
			}

			var curveDirectoryPath = Path.Combine(curveSourcePath, inputType);
			if (!Directory.Exists(curveDirectoryPath))
			{
				QuantumConsole.Instance.LogToConsole("Path doesn't exist: " + curveDirectoryPath);
				return;
			}

			var (sok, subsystem) = FindSubsystemWithGuidance(unit, primary);
			if (!sok)
			{
				return;
			}

			var curvePath = Path.Combine(curveDirectoryPath, subsystem.dataLinkSubsystem.data.key + ".yaml");
			if (!File.Exists(curvePath))
			{
				QuantumConsole.Instance.LogToConsole("Curve file not found: " + curvePath);
				return;
			}

			var curve = UtilitiesYAML.ReadFromFile<AnimationCurveSerialized>(curvePath, false);
			if (curve == null)
			{
				QuantumConsole.Instance.LogToConsole("Unable to load curve from file: " + curvePath);
				return;
			}

			var block = new DataBlockGuidanceInputCurve
			{
				curveSerialized = curve,
				curve = new AnimationCurveContainer((AnimationCurve)curve)
			};
			var update = DataMultiLinkerSubsystem.GuidanceCurves.Single(entry => entry.InputType == inputType).Update;
			update(subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData, block);

			QuantumConsole.Instance.LogToConsole($"{inputType} curve updated on {subsystem.dataLinkSubsystem.data.key}");
		}

		static void ReplaceGuidanceCurve(string unitID, bool primary, string inputType, float constant)
		{
			if (!CombatInspector.CombatStateCheck())
			{
				return;
			}

			var (ok, unit) = CombatInspector.UnitCheck(unitID);
			if (!ok)
			{
				return;
			}

			if (!DataMultiLinkerSubsystem.GuidanceCurves.Any(entry => entry.InputType == inputType))
			{
				QuantumConsole.Instance.LogToConsole("Invalid input type");
				return;
			}

			var (sok, subsystem) = FindSubsystemWithGuidance(unit, primary);
			if (!sok)
			{
				return;
			}

			var block = new DataBlockGuidanceInputConstant
			{
				value = constant,
			};
			var update = DataMultiLinkerSubsystem.GuidanceCurves.Single(entry => entry.InputType == inputType).Update;
			update(subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData, block);

			QuantumConsole.Instance.LogToConsole($"{inputType} updated on {subsystem.dataLinkSubsystem.data.key}");
		}

		static void ReplaceGuidanceCurve(string unitID, bool primary, string inputType, float from, float to)
		{
			if (!CombatInspector.CombatStateCheck())
			{
				return;
			}

			var (ok, unit) = CombatInspector.UnitCheck(unitID);
			if (!ok)
			{
				return;
			}

			if (!DataMultiLinkerSubsystem.GuidanceCurves.Any(entry => entry.InputType == inputType))
			{
				QuantumConsole.Instance.LogToConsole("Invalid input type");
				return;
			}

			var (sok, subsystem) = FindSubsystemWithGuidance(unit, primary);
			if (!sok)
			{
				return;
			}

			var block = new DataBlockGuidanceInputLinear
			{
				valueFrom = from,
				valueTo = to,
			};
			var update = DataMultiLinkerSubsystem.GuidanceCurves.Single(entry => entry.InputType == inputType).Update;
			update(subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData, block);

			QuantumConsole.Instance.LogToConsole($"{inputType} updated on {subsystem.dataLinkSubsystem.data.key}");
		}

		static void SetProjectileSpeed(string unitID, bool primary, float speed)
		{
			if (!CombatInspector.CombatStateCheck())
			{
				return;
			}

			var (ok, unit) = CombatInspector.UnitCheck(unitID);
			if (!ok)
			{
				return;
			}

			var (sok, part, subsystem) = FindSubsystemWithProjectile(unit, primary);
			if (!sok)
			{
				return;
			}

			if (!subsystem.dataLinkSubsystem.data.statsProcessed.TryGetValue("wpn_speed", out var stat))
			{
				stat = new DataBlockSubsystemStat();
				subsystem.dataLinkSubsystem.data.statsProcessed["wpn_speed"] = stat;
			}
			stat.value = speed;
			PBDataHelperStats.RefreshStatCacheForPart(part);

			QuantumConsole.Instance.LogToConsole($"{subsystem.dataLinkSubsystem.data.key} wpn_speed: {speed}");
		}

		static void SetGuidanceAccelerationForce(string unitID, bool primary, float force)
		{
			if (!CombatInspector.CombatStateCheck())
			{
				return;
			}

			var (ok, unit) = CombatInspector.UnitCheck(unitID);
			if (!ok)
			{
				return;
			}

			var (sok, subsystem) = FindSubsystemWithGuidance(unit, primary);
			if (!sok)
			{
				return;
			}

			subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.driverAccelerationForce = force;
			QuantumConsole.Instance.LogToConsole($"{subsystem.dataLinkSubsystem.data.key} acceleration force: {force}");
		}

		static void SetGuidanceDrag(string unitID, bool primary, float drag)
		{
			if (!CombatInspector.CombatStateCheck())
			{
				return;
			}

			var (ok, unit) = CombatInspector.UnitCheck(unitID);
			if (!ok)
			{
				return;
			}

			var (sok, subsystem) = FindSubsystemWithGuidance(unit, primary);
			if (!sok)
			{
				return;
			}

			subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.rigidbodyDrag = drag;
			QuantumConsole.Instance.LogToConsole($"{subsystem.dataLinkSubsystem.data.key} drag: {drag}");
		}

		static void SetGuidanceAngularDrag(string unitID, bool primary, float drag)
		{
			if (!CombatInspector.CombatStateCheck())
			{
				return;
			}

			var (ok, unit) = CombatInspector.UnitCheck(unitID);
			if (!ok)
			{
				return;
			}

			var (sok, subsystem) = FindSubsystemWithGuidance(unit, primary);
			if (!sok)
			{
				return;
			}

			subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.rigidbodyAngularDrag = drag;
			QuantumConsole.Instance.LogToConsole($"{subsystem.dataLinkSubsystem.data.key} angular drag: {drag}");
		}

		static void SetGuidanceSteeringForce(string unitID, bool primary, float force)
		{
			if (!CombatInspector.CombatStateCheck())
			{
				return;
			}

			var (ok, unit) = CombatInspector.UnitCheck(unitID);
			if (!ok)
			{
				return;
			}

			var (sok, subsystem) = FindSubsystemWithGuidance(unit, primary);
			if (!sok)
			{
				return;
			}

			subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.driverSteeringForce = force;
			QuantumConsole.Instance.LogToConsole($"{subsystem.dataLinkSubsystem.data.key} steering force: {force}");
		}

		static void SetGuidancePitchForce(string unitID, bool primary, float force)
		{
			if (!CombatInspector.CombatStateCheck())
			{
				return;
			}

			var (ok, unit) = CombatInspector.UnitCheck(unitID);
			if (!ok)
			{
				return;
			}

			var (sok, subsystem) = FindSubsystemWithGuidance(unit, primary);
			if (!sok)
			{
				return;
			}

			subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.driverPitchForce = force;
			QuantumConsole.Instance.LogToConsole($"{subsystem.dataLinkSubsystem.data.key} pitch force: {force}");
		}

		static void SetGuidanceSteeringPID(string unitID, bool primary, float p, float i, float d)
		{
			if (!CombatInspector.CombatStateCheck())
			{
				return;
			}

			var (ok, unit) = CombatInspector.UnitCheck(unitID);
			if (!ok)
			{
				return;
			}

			var (sok, subsystem) = FindSubsystemWithGuidance(unit, primary);
			if (!sok)
			{
				return;
			}

			if (subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.steeringPID == null)
			{
				subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.steeringPID = new SimplePIDSettings();
			}
			subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.steeringPID.proportionalGain = p;
			subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.steeringPID.integralGain = i;
			subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.steeringPID.derivativeGain = d;
			QuantumConsole.Instance.LogToConsole($"{subsystem.dataLinkSubsystem.data.key} steering PID: {p}/{i}/{d}");
		}

		static void SetGuidancePitchPID(string unitID, bool primary, float p, float i, float d)
		{
			if (!CombatInspector.CombatStateCheck())
			{
				return;
			}

			var (ok, unit) = CombatInspector.UnitCheck(unitID);
			if (!ok)
			{
				return;
			}

			var (sok, subsystem) = FindSubsystemWithGuidance(unit, primary);
			if (!sok)
			{
				return;
			}

			if (subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.pitchPID == null)
			{
				subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.pitchPID = new SimplePIDSettings();
			}
			subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.pitchPID.proportionalGain = p;
			subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.pitchPID.integralGain = i;
			subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.pitchPID.derivativeGain = d;
			QuantumConsole.Instance.LogToConsole($"{subsystem.dataLinkSubsystem.data.key} pitch PID: {p}/{i}/{d}");
		}

		static void SetGuidanceTargetHeight(string unitID, bool primary, float height)
		{
			if (!CombatInspector.CombatStateCheck())
			{
				return;
			}

			var (ok, unit) = CombatInspector.UnitCheck(unitID);
			if (!ok)
			{
				return;
			}

			var (sok, subsystem) = FindSubsystemWithGuidance(unit, primary);
			if (!sok)
			{
				return;
			}

			subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData.inputTargetHeightScale = height;
			QuantumConsole.Instance.LogToConsole($"{subsystem.dataLinkSubsystem.data.key} target height: {height}");
		}

		static (bool, EquipmentEntity, EquipmentEntity) FindSubsystemWithProjectile(PersistentEntity unit, bool primary)
		{
			var socket = primary ? "equipment_right" : "equipment_left";
			var part = EquipmentUtility.GetPartInUnit(unit, socket);
			if (part == null)
			{
				QuantumConsole.Instance.LogToConsole("Unable to retrieve part on socket " + socket);
				return (false, null, null);
			}

			var subsystem = EquipmentUtility.GetSubsystemInPart(part, "internal_main_equipment");
			if (subsystem == null)
			{
				QuantumConsole.Instance.LogToConsole("Unable to find any subsystem attached to hardpoint internal_main_equipment");
				return (false, null, null);
			}

			if (!subsystem.hasDataLinkSubsystem || subsystem.dataLinkSubsystem?.data == null)
			{
				QuantumConsole.Instance.LogToConsole("Part isn't associated with a subsystem blueprint");
				return (false, null, null);
			}

			if (subsystem.dataLinkSubsystem.data.projectileProcessed == null)
			{
				QuantumConsole.Instance.LogToConsole("No projectile data on subsystem " + subsystem.dataLinkSubsystem.data.key);
				return (false, null, null);
			}

			return (true, part, subsystem);
		}

		static (bool, EquipmentEntity) FindSubsystemWithGuidance(PersistentEntity unit, bool primary)
		{
			var (ok, _, subsystem) = FindSubsystemWithProjectile(unit, primary);
			if (!ok)
			{
				return (false, null);
			}

			if (subsystem.dataLinkSubsystem.data.projectileProcessed.guidanceData == null)
			{
				QuantumConsole.Instance.LogToConsole("No guidance data on subsystem " + subsystem.dataLinkSubsystem.data.key);
				return (false, null);
			}

			return (true, subsystem);
		}
	}
}
