// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Data;

using QFSW.QC;

namespace EchKode.PBMods.GuidanceCurve.ConsoleCommands
{
	using CommandList = List<(string QCName, string Description, MethodInfo Method)>;

	static partial class CombatInspector
	{
		internal static CommandList Commands() => new CommandList()
		{
			("com.list-units", "List units in combat and some info about each", AccessTools.DeclaredMethod(typeof(CombatInspector), nameof(ListUnits))),
		};

		static void ListUnits()
		{
			if (!CombatStateCheck())
			{
				return;
			}

			ScenarioUtility.GetCombatParticipantUnits()
				.Select(unit =>
				{
					var combatant = IDUtility.GetLinkedCombatEntity(unit);
					return new UnitInfo()
					{
						PersistentId = unit.id.id,
						CombatId = combatant?.id.id ?? 0,
						Name = unit.hasNameInternal ? unit.nameInternal.s : "<no-name>",
						Preset = unit.hasDataKeyUnitPreset ? unit.dataKeyUnitPreset.s : "<none>",
						Faction = unit.faction.s,
						Flags = CompileUnitFlags(unit, combatant),
						Level = DataHelperStats.GetAverageUnitLevel(unit),
						Rating = DataHelperStats.GetAverageUnitRating(unit),
					};
				})
				.OrderBy(info => info.PersistentId)
				.ToList()
				.ForEach(info =>
				{
					var msg = $"P-{info.PersistentId}/C-{info.CombatId} [{info.Flags}] L{info.Level:F1} R{info.Rating:F1} [faction={info.Faction}; preset={info.Preset}]";
					QuantumConsole.Instance.LogToConsole(msg);
				});
		}
	}
}
