#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class SelectableInfo : ITraitInfo
	{
		public readonly int Priority = 10;
		public readonly int[] Bounds = null;
		[VoiceReference] public readonly string Voice = null;

		public object Create(ActorInitializer init) { return new Selectable(init.self); }
	}

	public class Selectable : IPostRenderSelection
	{
		Actor self;

		public Selectable(Actor self) { this.self = self; }

		public void RenderAfterWorld(WorldRenderer wr)
		{
			var bounds = self.Bounds.Value;

			var xy = new float2(bounds.Left, bounds.Top);
			var Xy = new float2(bounds.Right, bounds.Top);

			wr.DrawSelectionBox(self, Color.White);
			DrawHealthBar(self, xy, Xy);
			DrawExtraBars(self, xy, Xy);
			DrawUnitPath(self);
		}

		public void DrawRollover(WorldRenderer wr, Actor self)
		{
			var bounds = self.Bounds.Value;

			var xy = new float2(bounds.Left, bounds.Top);
			var Xy = new float2(bounds.Right, bounds.Top);

			DrawHealthBar(self, xy, Xy);
			DrawExtraBars(self, xy, Xy);
		}

		void DrawExtraBars(Actor self, float2 xy, float2 Xy)
		{
			foreach (var extraBar in self.TraitsImplementing<ISelectionBar>())
			{
				var value = extraBar.GetValue();
				if (value != 0)
				{
					xy.Y += 4;
					Xy.Y += 4;
					DrawSelectionBar(self, xy, Xy, extraBar.GetValue(), extraBar.GetColor());
				}
			}
		}

		void DrawSelectionBar(Actor self, float2 xy, float2 Xy, float value, Color barColor)
		{
			if (!self.IsInWorld) return;

			var health = self.TraitOrDefault<Health>();
			if (health == null || health.IsDead) return;

			DrawBar(xy, Xy);

			var z = float2.Lerp(xy, Xy, value);
			DrawBar(xy, z, barColor);
		}

		void DrawHealthBar(Actor self, float2 xy, float2 Xy)
		{
			if (!self.IsInWorld) return;

			var health = self.TraitOrDefault<Health>();
			if (health == null || health.IsDead) return;

			DrawBar(xy, Xy);

			var healthColor = (health.DamageState == DamageState.Critical) ? Color.Red :
							  (health.DamageState == DamageState.Heavy) ? Color.Yellow : Color.LimeGreen;
			var z = float2.Lerp(xy, Xy, (float)health.HP / health.MaxHP);
			DrawBar(xy, z, healthColor);

			if (health.DisplayHp != health.HP)
			{
				var deltaColor = Color.OrangeRed;
				var zz = float2.Lerp(xy, Xy, (float)health.DisplayHp / health.MaxHP);
				DrawBar(z, zz, Color.OrangeRed);
			}
		}

		void DrawBar(float2 xy, float2 Xy, Color c, Color c2)
		{
			var wlr = Game.Renderer.WorldLineRenderer;
			wlr.DrawLine(xy + new float2(0, -4), Xy + new float2(0, -4), c, c);
			wlr.DrawLine(xy + new float2(0, -3), Xy + new float2(0, -3), c2, c2);
			wlr.DrawLine(xy + new float2(0, -2), Xy + new float2(0, -2), c, c);
		}

		void DrawBar(float2 xy, float2 Xy)
		{
			var c = Color.FromArgb(128, 30, 30, 30);
			var c2 = Color.FromArgb(128, 10, 10, 10);
			DrawBar(xy, Xy, c, c2);
		}

		void DrawBar(float2 xy, float2 Xy, Color c)
		{
			var c2 = Color.FromArgb(255, c.R / 2, c.G / 2, c.B / 2);
			DrawBar(xy, Xy, c, c2);
		}

		void DrawUnitPath(Actor self)
		{
			if (self.World.LocalPlayer == null ||!self.World.LocalPlayer.PlayerActor.Trait<DeveloperMode>().PathDebug) return;

			var activity = self.GetCurrentActivity();
			var mobile = self.TraitOrDefault<IMove>();
			if (activity != null && mobile != null)
			{
				var alt = new float2(0, -mobile.Altitude);
				var targets = activity.GetTargets(self);
				var start = self.CenterLocation + alt;

				var c = Color.Green;

				var wlr = Game.Renderer.WorldLineRenderer;
				foreach (var step in targets.Select(p => p.CenterLocation))
				{
					var stp = step + alt;
					wlr.DrawLine(stp + new float2(-1, -1), stp + new float2(-1, 1), c, c);
					wlr.DrawLine(stp + new float2(-1, 1), stp + new float2(1, 1), c, c);
					wlr.DrawLine(stp + new float2(1, 1), stp + new float2(1, -1), c, c);
					wlr.DrawLine(stp + new float2(1, -1), stp + new float2(-1, -1), c, c);
					wlr.DrawLine(start, stp, c, c);
					start = stp;
				}
			}
		}

	}
}
