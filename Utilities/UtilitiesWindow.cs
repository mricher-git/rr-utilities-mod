using Character;
using Game;
using Game.Messages;
using Game.State;
using System;
using System.Collections.Generic;
using System.Linq;
using UI;
using UI.Builder;
using UI.Common;
using UI.Console.Commands;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.InputSystem.Layouts.InputControlLayout;

namespace Utilities
{
	[RequireComponent(typeof(Window))]
	public class UtilitiesWindow : MonoBehaviour, IBuilderWindow
	{
		private Window _window;

		private static UtilitiesWindow _instance;

		private UIPanel _panel;

		private Action _rebuild;

		public UIBuilderAssets BuilderAssets { get; set; }

		public static UtilitiesWindow Shared
		{
			get
			{
				return WindowManager.Shared.GetWindow<UtilitiesWindow>();
			}
		}

		private static List<string> _teleportLocations;
		private static List<string> teleportLocations
		{
			get
			{
				if (_teleportLocations == null)
					_teleportLocations = SpawnPoint.All.Where(sp => sp.name.ToLower() != "ds").Select(sp => sp.name).OrderBy(sp => sp).Prepend("Location...").ToList();
				return _teleportLocations;
			}
		}
		private static List<string> _weatherPresets;
		private static List<string> weatherPresets
		{
			get
			{
				if (_weatherPresets == null)
					_weatherPresets = TimeWeather.WeatherIdLookup.Keys.Select(key => char.ToUpper(key[0]) + key.Substring(1)).OrderBy(x => x).ToList();
				return _weatherPresets;
			}
		}

		public void Show()
		{
			Populate();
			_window.ShowWindow();
		}

		private void Awake()
		{
			_window = GetComponent<Window>();
			_window.OnShownWillChange += delegate (bool shown)
			{
				if (!shown)
				{
				}
			};
		}

		private void OnDisable()
		{
			UIPanel panel = _panel;
			if (panel != null)
			{
				panel.Dispose();
			}
			_panel = null;
		}

		private void Populate()
		{
			_window.Title = "Utilities Mod";

			UIPanel panel = _panel;
			if (panel != null)
			{
				panel.Dispose();
			}
			_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, new Action<UIPanelBuilder>(BuildContent));
		}

		private void BuildContent(UIPanelBuilder builder)
		{
			_rebuild = builder.Rebuild;
			var stateManager = StateManager.Shared;

			builder.AddSection("Game Mode", builder2 =>
			{
				builder2.ButtonStrip((builder3) =>
				{
					builder3.Spacer();
					builder3.AddButtonSelectable("Sandbox", stateManager.GameMode == GameMode.Sandbox, () => { SetGameMode(GameMode.Sandbox); builder3.Rebuild(); });
					builder3.AddButtonSelectable("Company", stateManager.GameMode == GameMode.Company, () => { SetGameMode(GameMode.Company); builder3.Rebuild(); });
					builder3.Spacer();
				});
			});

			builder.AddSection("Time Controls", builder2 =>
			{
				builder2.AddLabel(() => TimeWeather.TimeOfDayString, UIPanelBuilder.Frequency.Fast);
				if (StateManager.CheckAuthorizedToSendMessage(default(WaitTime)))
				{
					builder2.ButtonStrip((builder3) =>
					{
						builder3.Spacer();
						builder3.AddButton("Wait 1 Hour", () => Wait(1));
						builder3.AddButton("Sleep", () => Sleep());
						builder3.Spacer(); 
					});
				}
			});

			builder.AddSection("Teleport Locations", builder2 =>
			{
				Dropdown? dropdown = null;
				dropdown = builder2.AddDropdown(teleportLocations, 0, index =>
				{
					if (index == 0) return;
					Teleport(teleportLocations[index]);
					dropdown?.SetValueWithoutNotify(0);
				}).GetComponent<Dropdown>();
				builder2.AddButton("Dispatch Station", () => Teleport("ds"));
			});

			builder.AddSection("Weather Presets", builder2 =>
			{
				for (int i = 0; i < weatherPresets.Count; i+=3)
				{
					builder2.ButtonStrip((builder3) =>
					{
						builder3.Spacer();
						for (int j = i; j < i + 3; j++)
						{
							builder3.AddButton(weatherPresets[j], () => SetWeather(j));
							builder3.Spacer();
						}
					});
				}
			});

			builder.AddExpandingVerticalSpacer();

			void SetWeather(int index)
			{
				StateManager.ApplyLocal(new PropertyChange("_game", "weatherId", new IntPropertyValue(TimeWeather.WeatherIdLookup[weatherPresets[index].ToLower()])));
			}

			void SetGameMode(GameMode gameMode)
			{
				StateManager.ApplyLocal(new PropertyChange("_game", "mode", new IntPropertyValue((int)gameMode)));
			}

			void Teleport(string location)
			{
				new TeleportCommand().Execute(new string[] { "/tp", location });
			}

			void Wait(float hours)
			{
				StateManager.ApplyLocal(new WaitTime { Hours = hours });
			}

			void Sleep()
			{
				float currentHours = TimeWeather.Now.Hours;
				int interchangeServeHour = stateManager.Storage.InterchangeServeHour;
				float hours = ((currentHours < (float)interchangeServeHour) ? ((float)interchangeServeHour - currentHours) : (24f - currentHours + (float)interchangeServeHour));
				StateManager.ApplyLocal(new WaitTime { Hours = hours });
			}
		}
	}
}
