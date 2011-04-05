#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Air
{
	public class PlaneInfo : AircraftInfo
	{
		public override object Create( ActorInitializer init ) { return new Plane( init, this ); }
	}

	public class Plane : Aircraft, IIssueOrder, IResolveOrder, IOrderVoice, ITick, INotifyDamage, ISync
	{
		[Sync]
		public int2 RTBPathHash;
		
		public Plane( ActorInitializer init, PlaneInfo info ) : base( init, info ) { }

		bool firstTick = true;
		public void Tick(Actor self)
		{
			if (firstTick)
			{
				firstTick = false;
				if (self.Trait<IMove>().Altitude == 0)
				{	
					/* not spawning in the air, so try to assoc. with our afld. this is a hack. */
					var afld = self.World.FindUnits(self.CenterLocation, self.CenterLocation)
						.FirstOrDefault( a => a.HasTrait<Reservable>() );

					if (afld != null)
					{
						var res = afld.Trait<Reservable>();
						if (res != null)
							reservation = res.Reserve(afld, self, this);
					}
				}
			}
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterOrderTargeter<Building>( "Enter", 5, false, true,
					target => AircraftCanEnter( target ), target => !Reservable.IsReserved( target ) );

				yield return new AircraftMoveOrderTargeter();
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "Enter" )
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			if( order.OrderID == "Move" )
				return new Order( order.OrderID, self, queued ) { TargetLocation = Util.CellContaining( target.CenterLocation ) };

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Move" || order.OrderString == "Enter") ? "Move" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				UnReserve();
				
				var target = self.World.ClampToWorld(order.TargetLocation);
				self.SetTargetLine(Target.FromCell(target), Color.Green);
				self.CancelActivity();
				self.QueueActivity(Fly.ToCell(target));
                self.QueueActivity(new FlyCircle());
			}

			else if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor)) return;

				UnReserve();

				var info = self.Info.Traits.Get<PlaneInfo>();
				self.SetTargetLine(Target.FromOrder(order), Color.Green);

				self.CancelActivity();
				self.QueueActivity(new ReturnToBase(self, order.TargetActor));
				self.QueueActivity(
					info.RearmBuildings.Contains(order.TargetActor.Info.Name)
						? (Activity)new Rearm() : new Repair(order.TargetActor));
			}
			else if (order.OrderString == "Stop")
			{
				UnReserve();
				self.CancelActivity();
			}
			else
			{
				// Game.Debug("Unreserve due to unhandled order: {0}".F(order.OrderString));
				UnReserve();
			}
		}
	}

	class AircraftMoveOrderTargeter : IOrderTargeter
	{
		public string OrderID { get { return "Move"; } }
		public int OrderPriority { get { return 4; } }

		public bool CanTargetActor(Actor self, Actor target, bool forceAttack, bool forceMove, bool forceQueued, ref string cursor)
		{
			return false;
		}

		public bool CanTargetLocation(Actor self, int2 location, List<Actor> actorsAtLocation, bool forceAttack, bool forceMove, bool forceQueued, ref string cursor)
		{
			IsQueued = forceQueued;
			cursor = self.World.Map.IsInMap(location) ? "move" : "move-blocked";
			return true;
		}
		public bool IsQueued { get; protected set; }
	}
}
