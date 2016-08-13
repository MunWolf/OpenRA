#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public abstract class UnitOrderTargeter : IOrderTargeter
	{
		readonly string cursor;
		readonly bool targetEnemyUnits, targetAllyUnits;

		public UnitOrderTargeter(string order, int priority, string cursor, bool targetEnemyUnits, bool targetAllyUnits, bool targetTerrain)
		{
			OrderID = order;
			OrderPriority = priority;
			this.cursor = cursor;
			this.targetEnemyUnits = targetEnemyUnits;
			this.targetAllyUnits = targetAllyUnits;
		}

		public string OrderID { get; private set; }
		public int OrderPriority { get; private set; }
		public bool? ForceAttack = null;
		public bool TargetOverridesSelection(TargetModifiers modifiers) { return true; }

		public abstract bool CanTargetTerrain(Actor self, IEnumerable<WPos> target, TargetModifiers modifiers, ref string cursor);
		public abstract bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor);
		public abstract bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor);

		public bool CanTarget(Actor self, Target target, ref IEnumerable<UIOrder> uiOrders, ref TargetModifiers modifiers)
		{
			if (target.Type == TargetType.Invalid)
				return false;

			if (ForceAttack != null && modifiers.HasModifier(TargetModifiers.ForceAttack) != ForceAttack)
				return false;

			if (target.Type == TargetType.Terrain)
				return CheckUIOrders(ref uiOrders);

			var owner = target.Type == TargetType.FrozenActor ? target.FrozenActor.Owner : target.Actor.Owner;
			var playerRelationship = self.Owner.Stances[owner];

			if (!modifiers.HasModifier(TargetModifiers.ForceAttack) && playerRelationship == Stance.Ally && !targetAllyUnits)
				return false;

			if (!modifiers.HasModifier(TargetModifiers.ForceAttack) && playerRelationship == Stance.Enemy && !targetEnemyUnits)
				return false;

			return CheckUIOrders(ref uiOrders);
		}

		public virtual bool CheckUIOrders(ref IEnumerable<UIOrder> uiOrders) { return true; }

		public bool SetupTarget(Actor self, Target target, List<Actor> othersAtTarget, ref IEnumerable<UIOrder> uiOrders, ref TargetModifiers modifiers, ref string cursor)
		{
			cursor = this.cursor;
			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
			if (target.Type == TargetType.Terrain)
				return CanTargetTerrain(self, target.Positions, modifiers, ref cursor);

			return target.Type == TargetType.FrozenActor ?
				CanTargetFrozenActor(self, target.FrozenActor, modifiers, ref cursor) :
				CanTargetActor(self, target.Actor, modifiers, ref cursor);
		}

		public virtual void OrderIssued(Actor self) { }

		public virtual bool IsQueued { get; protected set; }
	}

	public class TargetTypeOrderTargeter : UnitOrderTargeter
	{
		readonly HashSet<string> targetTypes;

		public TargetTypeOrderTargeter(HashSet<string> targetTypes, string order, int priority, string cursor, bool targetEnemyUnits, bool targetAllyUnits)
			: base(order, priority, cursor, targetEnemyUnits, targetAllyUnits, false)
		{
			this.targetTypes = targetTypes;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			return targetTypes.Overlaps(target.GetEnabledTargetTypes());
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return target.TargetTypes.Overlaps(targetTypes);
		}

		public override bool CanTargetTerrain(Actor self, System.Collections.Generic.IEnumerable<WPos> target, TargetModifiers modifiers, ref string cursor)
		{
			return false;
		}
	}
}
