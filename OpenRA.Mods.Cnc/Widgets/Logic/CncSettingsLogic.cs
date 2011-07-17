#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncSettingsLogic
	{	
		enum PanelType
		{
			General,
			Input
		}

		PanelType Settings = PanelType.General;
		CncColorPickerPaletteModifier playerPalettePreview;
		World world;
		
		[ObjectCreator.UseCtor]
		public CncSettingsLogic([ObjectCreator.Param] Widget widget,
		                        [ObjectCreator.Param] World world,
		                        [ObjectCreator.Param] Action onExit)
		{
			this.world = world;
			var panel = widget.GetWidget("SETTINGS_PANEL");
					
			// General pane
			var generalButton = panel.GetWidget<ButtonWidget>("GENERAL_BUTTON");
			generalButton.OnClick = () => Settings = PanelType.General;
			generalButton.IsDisabled = () => Settings == PanelType.General;
			
			var generalPane = panel.GetWidget("GENERAL_CONTROLS");
			generalPane.IsVisible = () => Settings == PanelType.General;
			
			var gameSettings = Game.Settings.Game;
			var playerSettings = Game.Settings.Player;
			var debugSettings = Game.Settings.Debug;
			var graphicsSettings = Game.Settings.Graphics;
			var soundSettings = Game.Settings.Sound;
			
			// Player profile
			var nameTextfield = generalPane.GetWidget<TextFieldWidget>("NAME_TEXTFIELD");
			nameTextfield.Text = playerSettings.Name;
			
			playerPalettePreview = world.WorldActor.Trait<CncColorPickerPaletteModifier>();
			playerPalettePreview.Ramp = playerSettings.ColorRamp;
			
			var colorDropdown = generalPane.GetWidget<DropDownButtonWidget>("COLOR_DROPDOWN");
			colorDropdown.OnMouseDown = _ => ShowColorPicker(colorDropdown, playerSettings);
			colorDropdown.GetWidget<ColorBlockWidget>("COLORBLOCK").GetColor = () => playerSettings.ColorRamp.GetColor(0);
			
			// Debug
			var perftextCheckbox = generalPane.GetWidget<CheckboxWidget>("PERFTEXT_CHECKBOX");
			perftextCheckbox.IsChecked = () => debugSettings.PerfText;
			perftextCheckbox.OnClick = () => debugSettings.PerfText ^= true;
			
			var perfgraphCheckbox = generalPane.GetWidget<CheckboxWidget>("PERFGRAPH_CHECKBOX");
			perfgraphCheckbox.IsChecked = () => debugSettings.PerfGraph;
			perfgraphCheckbox.OnClick = () => debugSettings.PerfGraph ^= true;
			
			var matchtimerCheckbox = generalPane.GetWidget<CheckboxWidget>("MATCHTIME_CHECKBOX");
			matchtimerCheckbox.IsChecked = () => gameSettings.MatchTimer;
			matchtimerCheckbox.OnClick = () => gameSettings.MatchTimer ^= true;
			
			var checkunsyncedCheckbox = generalPane.GetWidget<CheckboxWidget>("CHECKUNSYNCED_CHECKBOX");
			checkunsyncedCheckbox.IsChecked = () => debugSettings.SanityCheckUnsyncedCode;
			checkunsyncedCheckbox.OnClick = () => debugSettings.SanityCheckUnsyncedCode ^= true;
			
			// Video
			var windowModeDropdown = generalPane.GetWidget<DropDownButtonWidget>("MODE_DROPDOWN");
			windowModeDropdown.OnMouseDown = _ => ShowWindowModeDropdown(windowModeDropdown, graphicsSettings);
			windowModeDropdown.GetText = () => graphicsSettings.Mode == WindowMode.Windowed ? 
				"Windowed" : graphicsSettings.Mode == WindowMode.Fullscreen ? "Fullscreen" : "Pseudo-Fullscreen";
			
			generalPane.GetWidget("WINDOW_RESOLUTION").IsVisible = () => graphicsSettings.Mode == WindowMode.Windowed;
			var windowWidth = generalPane.GetWidget<TextFieldWidget>("WINDOW_WIDTH");
			windowWidth.Text = graphicsSettings.WindowedSize.X.ToString();
			
			var windowHeight = generalPane.GetWidget<TextFieldWidget>("WINDOW_HEIGHT");
			windowHeight.Text = graphicsSettings.WindowedSize.Y.ToString();

			// Audio
			var soundSlider = generalPane.GetWidget<SliderWidget>("SOUND_SLIDER");
			soundSlider.OnChange += x => { soundSettings.SoundVolume = x; Sound.SoundVolume = x;};
			soundSlider.Value = soundSettings.SoundVolume;
			
			var musicSlider = generalPane.GetWidget<SliderWidget>("MUSIC_SLIDER");
			musicSlider.OnChange += x => { soundSettings.MusicVolume = x; Sound.MusicVolume = x; };
			musicSlider.Value = soundSettings.MusicVolume;
			
			var shellmapMusicCheckbox = generalPane.GetWidget<CheckboxWidget>("SHELLMAP_MUSIC");
			shellmapMusicCheckbox.IsChecked = () => gameSettings.ShellmapMusic;
			shellmapMusicCheckbox.OnClick = () => gameSettings.ShellmapMusic ^= true;
			
			
			// Input pane
			var inputPane = panel.GetWidget("INPUT_CONTROLS");
			inputPane.IsVisible = () => Settings == PanelType.Input;

			var inputButton = panel.GetWidget<ButtonWidget>("INPUT_BUTTON");
			inputButton.OnClick = () => Settings = PanelType.Input;
			inputButton.IsDisabled = () => Settings == PanelType.Input;
				
			inputPane.GetWidget<CheckboxWidget>("CLASSICORDERS_CHECKBOX").IsDisabled = () => true;
			
			var scrollSlider = inputPane.GetWidget<SliderWidget>("SCROLLSPEED_SLIDER");
			scrollSlider.Value = gameSettings.ViewportEdgeScrollStep;
			scrollSlider.OnChange += x => gameSettings.ViewportEdgeScrollStep = x;
			
			var edgescrollCheckbox = inputPane.GetWidget<CheckboxWidget>("EDGESCROLL_CHECKBOX");
			edgescrollCheckbox.IsChecked = () => gameSettings.ViewportEdgeScroll;
			edgescrollCheckbox.OnClick = () => gameSettings.ViewportEdgeScroll ^= true;
			
			var mouseScrollDropdown = inputPane.GetWidget<DropDownButtonWidget>("MOUSE_SCROLL");
			mouseScrollDropdown.OnMouseDown = _ => ShowMouseScrollDropdown(mouseScrollDropdown, gameSettings);
			mouseScrollDropdown.GetText = () => gameSettings.MouseScroll.ToString();
			
			var teamchatCheckbox = inputPane.GetWidget<CheckboxWidget>("TEAMCHAT_CHECKBOX");
			teamchatCheckbox.IsChecked = () => gameSettings.TeamChatToggle;
			teamchatCheckbox.OnClick = () => gameSettings.TeamChatToggle ^= true;
			
			panel.GetWidget<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				playerSettings.Name = nameTextfield.Text;
				int x = graphicsSettings.WindowedSize.X, y = graphicsSettings.WindowedSize.Y;
				int.TryParse(windowWidth.Text, out x);
				int.TryParse(windowHeight.Text, out y);
				graphicsSettings.WindowedSize = new int2(x,y);
				Game.Settings.Save();
				Widget.CloseWindow();
				onExit();
			};
		}
		
		bool ShowColorPicker(DropDownButtonWidget color, PlayerSettings s)
		{
			Action<ColorRamp> onSelect = c =>
			{
				s.ColorRamp = c;
				color.RemovePanel();
			};
			
			Action<ColorRamp> onChange = c =>
			{
				playerPalettePreview.Ramp = c;
			};
			
			var colorChooser = Game.LoadWidget(world, "COLOR_CHOOSER", null, new WidgetArgs()
			{
				{ "onSelect", onSelect },
				{ "onChange", onChange },
				{ "initialRamp", s.ColorRamp }
			});
			
			color.AttachPanel(colorChooser);
			return true;
		}
		
		bool ShowWindowModeDropdown(DropDownButtonWidget dropdown, GraphicSettings s)
		{
			var options = new Dictionary<string, WindowMode>()
			{
				{ "Pseudo-Fullscreen", WindowMode.PseudoFullscreen },
				{ "Fullscreen", WindowMode.Fullscreen },
				{ "Windowed", WindowMode.Windowed },
			};
			
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
				                                  () => s.Mode == options[o],
				                                  () => s.Mode = options[o]);
				item.GetWidget<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};
			
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys.ToList(), setupItem);
			return true;
		}
		
		
		bool ShowMouseScrollDropdown(DropDownButtonWidget dropdown, GameSettings s)
		{
			var options = new Dictionary<string, MouseScrollType>()
			{
				{ "Disabled", MouseScrollType.Disabled },
				{ "Standard", MouseScrollType.Standard },
				{ "Inverted", MouseScrollType.Inverted },
			};
			
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
				                                  () => s.MouseScroll == options[o],
				                                  () => s.MouseScroll = options[o]);
				item.GetWidget<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};
			
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys.ToList(), setupItem);
			return true;
		}
	}
}
