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

using System.Drawing;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;
using OpenRA.Mods.Common.Orders;
using System.Collections.Generic;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Provides access to the attack-move command, which will make the actor automatically engage viable targets while moving to the destination.")]
	class AttackMoveInfo : ITraitInfo, IIssueOrderInfo, Requires<IMoveInfo>
	{
		[FieldLoader.LoadUsing("LoadOrders")]
		[Desc("Information for displaying the orders for the trait.")]
		public readonly Dictionary<string, OrderInfo> Orders = null;

		[VoiceReference]
		public readonly string Voice = "Action";

		static object LoadOrders(MiniYaml yaml)
		{
			var orders = new Dictionary<string, OrderInfo>();
			var orderCollection = yaml.Nodes.Find(n => n.Key == "Orders");
			if (orderCollection == null)
				return orders;

			var order = orderCollection.Value.Nodes.Find(n => n.Key == "AttackMove");
			if (order != null)
				orders.Add(order.Key, FieldLoader.Load<OrderInfo>(order.Value));

			return orders;
		}

		public Dictionary<string, OrderInfo> IssuableOrders { get { return Orders; } }

		public object Create(ActorInitializer init) { return new AttackMove(init.Self, this); }
	}

	class AttackMove : IResolveOrder, IIssueOrder, IOrderVoice, INotifyIdle, ISync
	{
		[Sync] public CPos _targetLocation { get { return TargetLocation.HasValue ? TargetLocation.Value : CPos.Zero; } }
		public CPos? TargetLocation = null;

		readonly IMove move;
		readonly AttackMoveInfo info;

		public AttackMove(Actor self, AttackMoveInfo info)
		{
			move = self.Trait<IMove>();
			this.info = info;
		}

		public IIssueOrderInfo OrderInfo
		{
			get { return info; }
		}

		public System.Collections.Generic.IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new UIOrderTargeter("AttackMove", 5, "attackmove", true, true, true); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			switch (order.OrderID)
			{
				case "AttackMove":
					var newOrder = new Order("AttackMove", self, queued);
					newOrder.TargetLocation = self.World.Map.CellContaining(target.CenterPosition);
					return newOrder;
				default:
					return null;
			}
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "AttackMove")
				return info.Voice;

			return null;
		}

		void Activate(Actor self)
		{
			self.CancelActivity();
			self.QueueActivity(new AttackMoveActivity(self, move.MoveTo(TargetLocation.Value, 1)));
		}

		public void TickIdle(Actor self)
		{
			// This might cause the actor to be stuck if the target location is unreachable
			if (TargetLocation.HasValue && self.Location != TargetLocation.Value)
				Activate(self);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			TargetLocation = null;

			if (order.OrderString == "AttackMove")
			{
				TargetLocation = move.NearestMoveableCell(order.TargetLocation);
				self.SetTargetLine(Target.FromCell(self.World, TargetLocation.Value), Color.Red);
				Activate(self);
			}
		}
	}
}
