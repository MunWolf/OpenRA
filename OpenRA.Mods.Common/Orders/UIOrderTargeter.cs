using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.Common.Orders
{
	public class UIOrderTargeter : UnitOrderTargeter
	{
		UIOrder uiOrder;

		public UIOrderTargeter(string order, int priority, string cursor, bool targetEnemyUnits, bool targetAllyUnits, bool targetTerrain)
			: base(order, priority, cursor, targetEnemyUnits, targetAllyUnits, targetTerrain) {}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, OpenRA.Traits.FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return true;
		}

		public override bool CanTargetTerrain(Actor self, System.Collections.Generic.IEnumerable<WPos> target, TargetModifiers modifiers, ref string cursor)
		{
			return true;
		}

		public override bool CheckUIOrders(ref IEnumerable<UIOrder> uiOrders)
		{
			uiOrder = uiOrders.FirstOrDefault(o => o.Order == OrderID);
			return uiOrder != null;
		}

		public override void OrderIssued(Actor self)
		{
			uiOrder.Resolve();
			uiOrder = null;
		}
	}
}
