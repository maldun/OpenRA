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
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Mods.RA;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncIngameChromeLogic
	{
		enum MenuType { None, Cheats }
		MenuType menu = MenuType.None;
		
		Widget ingameRoot;
		World world;
		
		void AddChatLine(Color c, string from, string text)
		{
			ingameRoot.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").AddLine(c, from, text);
		}
		
		public void UnregisterEvents()
		{
			Game.AddChatLine -= AddChatLine;
			Game.BeforeGameStart -= UnregisterEvents;

			if (world.LocalPlayer != null)
			{
				var queueTabs = ingameRoot.GetWidget<ProductionTabsWidget>("PRODUCTION_TABS");
				world.ActorAdded += queueTabs.ActorChanged;
				world.ActorRemoved += queueTabs.ActorChanged;
			}
		}
		
		ProductionQueue QueueForType(World world, string type)
		{
			return world.ActorsWithTrait<ProductionQueue>()
				.Where(p => p.Actor.Owner == world.LocalPlayer)
				.Select(p => p.Trait).FirstOrDefault(p => p.Info.Type == type);
		}
		
		[ObjectCreator.UseCtor]
		public CncIngameChromeLogic([ObjectCreator.Param] Widget widget,
		                            [ObjectCreator.Param] World world )
		{
			this.world = world;
			world.WorldActor.Trait<CncMenuPaletteEffect>()
				.Fade(CncMenuPaletteEffect.EffectType.None);
			
			Game.AddChatLine += AddChatLine;
			Game.BeforeGameStart += UnregisterEvents;
			
			ingameRoot = widget.GetWidget("INGAME_ROOT");
			
			if (world.LocalPlayer != null)
			{
				var playerWidgets = widget.GetWidget("PLAYER_WIDGETS");
				playerWidgets.IsVisible = () => true;

				var sidebarRoot = playerWidgets.GetWidget("SIDEBAR_BACKGROUND");
				var playerResources = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();
				sidebarRoot.GetWidget<LabelWidget>("CASH_DISPLAY").GetText = () =>
					"${0}".F(playerResources.DisplayCash + playerResources.DisplayOre);
				
				var buildPalette = playerWidgets.GetWidget<ProductionPaletteWidget>("PRODUCTION_PALETTE");
				var queueTabs = playerWidgets.GetWidget<ProductionTabsWidget>("PRODUCTION_TABS");

				world.ActorAdded += queueTabs.ActorChanged;
				world.ActorRemoved += queueTabs.ActorChanged;

				var queueTypes = sidebarRoot.GetWidget("PRODUCTION_TYPES");

				var buildingTab = queueTypes.GetWidget<ButtonWidget>("BUILDING");
				buildingTab.OnClick = () => queueTabs.QueueType = "Building";
				buildingTab.IsDisabled = () => queueTabs.Groups["Building"].Tabs.Count == 0;

				var defenseTab = queueTypes.GetWidget<ButtonWidget>("DEFENSE");
				defenseTab.OnClick = () => queueTabs.QueueType = "Defense";
				defenseTab.IsDisabled = () => queueTabs.Groups["Defense"].Tabs.Count == 0;

				var infantryTab = queueTypes.GetWidget<ButtonWidget>("INFANTRY");
				infantryTab.OnClick = () => queueTabs.QueueType = "Infantry";
				infantryTab.IsDisabled = () => queueTabs.Groups["Infantry"].Tabs.Count == 0;

				var vehicleTab = queueTypes.GetWidget<ButtonWidget>("VEHICLE");
				vehicleTab.OnClick = () => queueTabs.QueueType = "Vehicle";
				vehicleTab.IsDisabled = () => queueTabs.Groups["Vehicle"].Tabs.Count == 0;

				var aircraftTab = queueTypes.GetWidget<ButtonWidget>("AIRCRAFT");
				aircraftTab.OnClick = () => queueTabs.QueueType = "Aircraft";
				aircraftTab.IsDisabled = () => queueTabs.Groups["Aircraft"].Tabs.Count == 0;
			}
			ingameRoot.GetWidget<ButtonWidget>("OPTIONS_BUTTON").OnClick = () =>
			{
				if (menu != MenuType.None)
				{
					Widget.CloseWindow();
					menu = MenuType.None;
				}
				
				ingameRoot.IsVisible = () => false;
				Game.LoadWidget(world, "INGAME_MENU", Widget.RootWidget, new WidgetArgs()
				{
					{ "onExit", () => ingameRoot.IsVisible = () => true }
				});
			};
			
			var cheatsButton = ingameRoot.GetWidget<ButtonWidget>("CHEATS_BUTTON");
			cheatsButton.OnClick = () =>
			{
				if (menu != MenuType.None)
					Widget.CloseWindow();
				
				menu = MenuType.Cheats;
				Game.OpenWindow("CHEATS_PANEL", new WidgetArgs() {{"onExit", () => menu = MenuType.None }});
			};
			cheatsButton.IsVisible = () => world.LocalPlayer != null && world.LobbyInfo.GlobalSettings.AllowCheats;
			
			var postgameBG = ingameRoot.GetWidget("POSTGAME_BG");
			postgameBG.IsVisible = () =>
			{
				return world.LocalPlayer != null && world.LocalPlayer.WinState != WinState.Undefined;
			};
			
			postgameBG.GetWidget<LabelWidget>("TEXT").GetText = () =>
			{
				var state = world.LocalPlayer.WinState;
				return (state == WinState.Undefined)? "" :
								((state == WinState.Lost)? "YOU ARE DEFEATED" : "YOU ARE VICTORIOUS");
			};
		}
	}
}
