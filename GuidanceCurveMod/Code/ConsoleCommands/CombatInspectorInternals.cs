using System;
using System.Text;

using PhantomBrigade;

using QFSW.QC;

namespace EchKode.PBMods.GuidanceCurve.ConsoleCommands
{
	static partial class CombatInspector
	{
		private sealed class UnitInfo
		{
			public int PersistentId;
			public int CombatId;
			public string Name;
			public string Faction;
			public string Flags;
			public float Level;
			public float Rating;
			public string Preset;
		}

		internal static bool CombatStateCheck()
		{
			if (IDUtility.IsGameState(GameStates.combat))
			{
				return true;
			}

			QuantumConsole.Instance.LogToConsole("Command only available from combat");
			return false;
		}

		internal static (bool, PersistentEntity) UnitCheck(string unitID)
		{
			if (string.IsNullOrEmpty(unitID))
			{
				QuantumConsole.Instance.LogToConsole("Command requires a unit ID argument");
				return (false, null);
			}

			unitID = unitID.ToUpperInvariant();
			if (!unitID.StartsWith("P-") && !unitID.StartsWith("C-"))
			{
				QuantumConsole.Instance.LogToConsole("Unit ID must begin with P- for a persistent identifier or C- for a combat identifier");
				return (false, null);
			}

			var prefix = unitID.Substring(0, 2);
			if (!int.TryParse(unitID.Substring(2).TrimEnd(), out var id))
			{
				QuantumConsole.Instance.LogToConsole($"Invalid unit ID: the part after {prefix} must be an integer");
				return (false, null);
			}

			Func<int, (bool, PersistentEntity)> find = FindPersistentEntity;
			if (prefix == "C-")
			{
				find = FindPersistentEntityFromCombatId;
			}
			var (ok, unit) = find(id);
			if (!ok)
			{
				QuantumConsole.Instance.LogToConsole("No unit in combat has identifier: {unitId}");
			}

			return (ok, unit);
		}

		private static (bool, PersistentEntity) FindPersistentEntity(int id)
		{
			var entity = IDUtility.GetPersistentEntity(id);
			return (entity != null, entity);
		}

		private static (bool, PersistentEntity) FindPersistentEntityFromCombatId(int id)
		{
			var ce = IDUtility.GetCombatEntity(id);
			var entity = IDUtility.GetLinkedPersistentEntity(ce);
			return (entity != null, entity);
		}

		private static string CompileUnitFlags(PersistentEntity unit, CombatEntity combatant)
		{
			var pilot = IDUtility.GetLinkedPilot(unit);
			var sb = new StringBuilder()
				.Append(combatant?.isPlayerControllable ?? false ? 'P' : '-')
				.Append(combatant?.isAIControllable ?? false ? 'A' : '-')
				.Append(unit.isHidden ? 'h' : '-')
				.Append(unit.isUnitDeployed ? 'd' : '-')
				.Append(combatant?.hasLandingData ?? false ? 'l' : '-')
				.Append(unit.isCombatParticipant ? 'p' : '-')
				.Append(combatant?.isCrashing ?? false ? 'C' : '-')
				.Append(unit.isDestroyed ? 'D' : '-')
				.Append(unit.isWrecked ? 'W' : '-')
				.Append(pilot?.isKnockedOut ?? false
					? 'U'
					: pilot?.isDeceased ?? false
						? 'X'
						: pilot == null || pilot.isEjected
							? 'E'
							: '-');
			return sb.ToString();
		}
	}
}
