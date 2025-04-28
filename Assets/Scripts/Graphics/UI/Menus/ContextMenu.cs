using System;
using System.Linq;
using DLS.Description;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using DLS.ColorStorage;
using System.Collections.Generic;
using static Seb.Helpers.ColHelper;
using DLS.SaveSystem;

namespace DLS.Graphics
{
	public static class ContextMenu
	{
		const int pad = 10;
		const string menuDividerString = "#--#";
		static string interactionContextName;
		static bool bottomBarItemIsCollection;
		static Vector2 mouseOpenMenuPos;

		static MenuEntry[] activeContextMenuEntries;
		static readonly MenuEntry dividerMenuEntry = new(menuDividerString, null, null);
		static bool wasMouseOverMenu;
		static string contextMenuHeader;

		//static readonly MenuEntry[] pinColEntries = ((PinColour[])Enum.GetValues(typeof(PinColour))).Select(col =>
		//	new MenuEntry(Format(Enum.GetName(typeof(PinColour), col)), () => SetCol(col), CanSetCol)
		//).ToArray();

		//New Dynamic Pincolor loader
		public static List<ColorWithName> pinColors;
		

		static MenuEntry[] pinColEntries;

        // This method will add a custom color with its name
        public static void AddCustomColor(Color lowColor, Color highColor, Color hoverColor, string colorName)
		{
			pinColors.Add(new ColorWithName(lowColor, highColor, hoverColor, colorName));
			UpdatePinColEntries();  // Rebuild the context menu with the new color
		}

		// This method creates context menu entries based on the colors in pinColors
		public static void UpdatePinColEntries()
		{
			// Dynamically create menu entries based on the colors in pinColors list
			
            pinColEntries = pinColors.Select(colWithName =>
				new MenuEntry(colWithName.Name, () => SetCol(colWithName), CanSetCol, () => OpenColorEditPopup(colWithName),true)
			).ToArray();


			// Rebuild context menus that include pin color entries
			entires_inputDevPin = new[]
			{
		new MenuEntry(Format("EDIT"), OpenPinEditMenu, CanEditCurrentChip),
		new MenuEntry(Format("DELETE"), Delete, CanDelete),
		dividerMenuEntry
	}.Concat(pinColEntries).Concat(NewColEntry).ToArray();


			entries_subChipOutput = new[]
			{
				dividerMenuEntry
			}.Concat(pinColEntries).ToArray();
			
		}

        

        static void DeleteCustomColor(string colorName)
        {
			
        }

        static readonly MenuEntry deleteEntry = new(Format("DELETE"), Delete, CanDelete);
		static readonly MenuEntry openChipEntry = new(Format("OPEN"), OpenChip, CanOpenChip);
		static readonly MenuEntry labelChipEntry = new(Format("LABEL"), OpenChipLabelPopup, CanLabelChip);

		static readonly MenuEntry[] entries_customSubchip =
		{
			new(Format("VIEW"), EnterViewMode, CanEnterViewMode),
			openChipEntry,
			labelChipEntry,
			deleteEntry
		};

		static readonly MenuEntry[] entries_builtinSubchip =
		{
			labelChipEntry,
			deleteEntry
		};

		static readonly MenuEntry[] entries_builtinBus =
		{
			new(Format("FLIP"), FlipBus, CanFlipBus),
			labelChipEntry,
			deleteEntry
		};

		static readonly MenuEntry[] entries_builtinKeySubchip =
		{
			new(Format("REBIND"), OpenKeyBindMenu, CanEditCurrentChip),
			labelChipEntry,
			deleteEntry
		};

		static readonly MenuEntry[] entries_builtinRomSubchip =
		{
			new(Format("EDIT"), OpenRomEditMenu, CanEditCurrentChip),
			labelChipEntry,
			deleteEntry
		};
		
		static readonly MenuEntry[] entries_builtinPulseChip =
		{
			new(Format("EDIT"), OpenPulseEditMenu, CanEditCurrentChip),
			labelChipEntry,
			deleteEntry
		};


		static MenuEntry[] entries_subChipOutput = pinColEntries;

		static readonly MenuEntry[] NewColEntry =
		{
			new(Format("+ Custom"), () => NewCol(), CanSetCol)
		};

        static  MenuEntry[] entires_inputDevPin = new[]
		{
			new(Format("EDIT"), OpenPinEditMenu, CanEditCurrentChip),
			new(Format("DELETE"), Delete, CanDelete),
			dividerMenuEntry
		}.Concat(pinColEntries ?? Array.Empty<MenuEntry>())  // Ensure pinColEntries is not null
         .Concat(NewColEntry)  // Add "New Custom" entry at the end
		 .ToArray();


		static readonly MenuEntry[] entries_outputDevPin =
		{
			entires_inputDevPin[0],
			entires_inputDevPin[1]
		};

		static readonly MenuEntry[] entries_wire =
		{
			new(Format("EDIT"), EditWire, CanEditWire),
			new(Format("DELETE"), Delete, CanDelete)
		};

		static readonly MenuEntry[] entries_bottomBarChip =
		{
			openChipEntry,
			new(Format("UN-STAR"), UnstarBottomBarEntry, () => true)
		};

		static readonly MenuEntry[] entries_collectionPopupChip =
		{
			openChipEntry
		};

		static readonly MenuEntry[] entries_bottomBarCollection =
		{
			new(Format("UN-STAR"), UnstarBottomBarEntry, () => true)
		};

		public static bool IsOpen { get; private set; }
		public static IInteractable interactionContext { get; private set; }

		static string Format(string s)
		{
			s = char.ToUpper(s[0]) + s.Substring(1).ToLower();
			return s.PadRight(pad);
		}

		public static void Update()
		{
			bool inMenu = !(UIDrawer.ActiveMenu is UIDrawer.MenuType.None or UIDrawer.MenuType.BottomBarMenuPopup or UIDrawer.MenuType.ChipCustomization);
			if (inMenu)
			{
				CloseContextMenu();
			}
			else
			{
				HandleOpenMenuInput();

				// Draw
				if (IsOpen) DrawContextMenu(activeContextMenuEntries);

				// Close menu input
				if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) || KeyboardShortcuts.CancelShortcutTriggered)
				{
					CloseContextMenu();
				}
			}
		}

		static void HandleOpenMenuInput()
		{
			// Open menu input
			if (InputHelper.IsMouseDownThisFrame(MouseButton.Right) && !KeyboardShortcuts.CameraActionKeyHeld && !InteractionState.MouseIsOverUI)
			{
				bool inCustomizeMenu = UIDrawer.ActiveMenu == UIDrawer.MenuType.ChipCustomization;
				IInteractable hoverElement = InteractionState.ElementUnderMouse;

				bool openSubChipContextMenu = hoverElement is SubChipInstance && !inCustomizeMenu;
				bool openDevPinContextMenu = (hoverElement is PinInstance pin && pin.parent is DevPinInstance) || hoverElement is DevPinInstance;
				bool openWireContextMenu = hoverElement is WireInstance;
				bool openSubchipOutputPinContextMenu = hoverElement is PinInstance pin2 && pin2.parent is SubChipInstance && pin2.IsSourcePin && !pin2.IsBusPin;

				if (openSubChipContextMenu || openDevPinContextMenu || openWireContextMenu || openSubchipOutputPinContextMenu)
				{
					interactionContextName = string.Empty;
					interactionContext = hoverElement;
					string headerName = string.Empty;

					if (openSubChipContextMenu)
					{
						SubChipInstance subChip = (SubChipInstance)hoverElement;
						interactionContextName = subChip.Description.Name;

						if (subChip.ChipType == ChipType.Custom)
						{
							headerName = subChip.Description.Name;
							activeContextMenuEntries = entries_customSubchip;
						}
						else // builtin type
						{
							headerName = ChipTypeHelper.IsBusType(subChip.ChipType) ? "BUS" : subChip.Description.Name;
							if (subChip.ChipType is ChipType.Key) activeContextMenuEntries = entries_builtinKeySubchip;
							else if (ChipTypeHelper.IsRomType(subChip.ChipType)) activeContextMenuEntries = entries_builtinRomSubchip;
							else if (subChip.ChipType is ChipType.Pulse) activeContextMenuEntries = entries_builtinPulseChip;
							else if (ChipTypeHelper.IsBusType(subChip.ChipType)) activeContextMenuEntries = entries_builtinBus;
							else activeContextMenuEntries = entries_builtinSubchip;
						}

						Project.ActiveProject.controller.Select(interactionContext as IMoveable, false);
					}
					else if (openDevPinContextMenu)
					{
						if (interactionContext is DevPinInstance devPinInstance) interactionContext = devPinInstance.Pin;

						PinInstance activePin = (PinInstance)interactionContext;
						headerName = CreatePinHeaderName(activePin.Name);
						interactionContextName = activePin.Name;
						Project.ActiveProject.controller.Select(activePin.parent, false);
						UpdatePinColEntries();
						activeContextMenuEntries = activePin.IsSourcePin ? entires_inputDevPin : entries_outputDevPin;
					}
					else if (openWireContextMenu)
					{
						WireInstance wire = (WireInstance)interactionContext;
						if (wire.IsBusWire) headerName = "BUS LINE";
						else headerName = CreateWireHeaderString(wire);

						activeContextMenuEntries = entries_wire;
					}
					else if (openSubchipOutputPinContextMenu)
					{
						PinInstance pinContext = (PinInstance)interactionContext;
						headerName = CreatePinHeaderName(pinContext.Name);
						UpdatePinColEntries();
						activeContextMenuEntries = entries_subChipOutput;
					}
                    
                    SetContextMenuOpen(headerName);
				}
				else
				{
					CloseContextMenu();
				}
			}
		}

		static string CreateWireHeaderString(WireInstance wire)
		{
			string pinName = wire.SourcePin.Name;
			if (string.IsNullOrWhiteSpace(pinName)) return "WIRE";

			return "WIRE: " + pinName;
		}

		static string CreatePinHeaderName(string pinName)
		{
			if (string.IsNullOrWhiteSpace(pinName)) return "PIN";

			return "PIN: " + pinName;
		}

		public static void OpenBottomBarContextMenu(string name, bool isCollection, bool isFromInsideCollection)
		{
			interactionContextName = name;
			bottomBarItemIsCollection = isCollection;
			interactionContext = null;
			SetContextMenuOpen(name);

			if (isCollection)
			{
				activeContextMenuEntries = entries_bottomBarCollection;
			}
			else
			{
				activeContextMenuEntries = isFromInsideCollection ? entries_collectionPopupChip : entries_bottomBarChip;
			}
		}

		static void SetContextMenuOpen(string header)
		{
			mouseOpenMenuPos = UI.ScreenToUISpace(InputHelper.MousePos);
			contextMenuHeader = header.PadRight(pad);
			IsOpen = true;
		}


		static void DrawContextMenu(MenuEntry[] menuEntries)
		{
			Draw.StartLayer(Vector2.zero, 1, true);

			const float textOffsetX = 0.45f;
			ButtonTheme theme = DrawSettings.ActiveUITheme.MenuPopupButtonTheme;
			ButtonTheme headerTheme = DrawSettings.ActiveUITheme.MenuPopupButtonTheme;
			headerTheme.buttonCols.inactive = ColHelper.MakeCol(0.18f);
			headerTheme.textCols.inactive = Color.white;

			float menuWidth = Draw.CalculateTextBoundsSize(menuEntries[0].Text, theme.fontSize, theme.font).x + 1;
			float menuWidthHeader = Draw.CalculateTextBoundsSize(contextMenuHeader, theme.fontSize, theme.font).x + 1;
			menuWidth = Mathf.Max(menuWidth, menuWidthHeader);

			Draw.ID panelID = UI.ReservePanel();
			Vector2 buttonSize = new(menuWidth, 2);


			Vector2 pos = mouseOpenMenuPos;
			if (pos.x + menuWidth > UI.Width)
			{
				pos.x = UI.Width - menuWidth;
			}

			bool expandDown = pos.y >= UI.Height * 0.35f;
			float dirY = expandDown ? -1 : 1;
			Anchor anchor = expandDown ? Anchor.TopLeft : Anchor.BottomLeft;

			using (UI.BeginBoundsScope(true))
			{
				for (int i = 0; i < menuEntries.Length; i++)
				{
					int index = expandDown ? i : menuEntries.Length - i - 1;
					MenuEntry entry = menuEntries[index];

					if (index == 0 && expandDown) DrawHeader();

					if (entry.Text == menuDividerString)
					{
						pos.y += 0.5f * dirY;
						UI.DrawPanel(pos, new Vector2(menuWidth, 0.15f), ColHelper.MakeCol(0.6f), Anchor.CentreLeft);
						pos.y += 0.5f * dirY;
					}
					else
					{
                        UI.Button(entry.Text, theme, pos, buttonSize, entry.IsEnabled(), false, false, anchor, true, textOffsetX);

                        // Now manually check clicks
                        Bounds2D buttonBounds = UI.PrevBounds; // Get the last button bounds

                        bool mouseInsideButton = UI.MouseInsideBounds(buttonBounds);
                        bool leftClick = InputHelper.IsMouseDownThisFrame(MouseButton.Left);
                        bool rightClick = InputHelper.IsMouseDownThisFrame(MouseButton.Right);

                        if (mouseInsideButton)
                        {
                            if (leftClick)
                            {
                                entry.OnPress();
                            }
                            else if (rightClick && entry.rightClickAction != null)
                            {
                                if (entry.isColorItem)
                                {
                                    
                                    entry.rightClickAction();
                                }
                            }
                        }

                        pos.y += buttonSize.y * dirY;
                    }

					if (index == 0 && !expandDown) DrawHeader();
				}

				Bounds2D bounds = UI.GetCurrentBoundsScope();
				Vector2 menuSize = new(menuWidth, bounds.Height);
				UI.ModifyPanel(panelID, bounds.Centre, menuSize + Vector2.one * 0.5f, ColHelper.MakeCol(0.91f));
			}

			wasMouseOverMenu = UI.MouseInsideBounds(UI.PrevBounds);

			void DrawHeader()
			{
				UI.Button(contextMenuHeader, headerTheme, pos, buttonSize, false, false, false, anchor, true, textOffsetX);
				pos.y += buttonSize.y * dirY;
			}
		}

		static bool IsCustomChip() => !Project.ActiveProject.chipLibrary.IsBuiltinChip(interactionContextName);
		static bool CanEnterViewMode() => IsCustomChip();
		static bool CanLabelChip() => Project.ActiveProject.CanEditViewedChip;
		static void EnterViewMode() => Project.ActiveProject.EnterViewMode(interactionContext as SubChipInstance);

		static bool CanDelete() => Project.ActiveProject.CanEditViewedChip;
		static bool CanFlipBus() => Project.ActiveProject.CanEditViewedChip;

		static bool CanSetCol() => Project.ActiveProject.CanEditViewedChip && ((PinInstance)interactionContext).IsSourcePin && UIDrawer.ActiveMenu != UIDrawer.MenuType.ChipCustomization;

		static void FlipBus()
		{
			((SubChipInstance)interactionContext).FlipBus();
		}

		public static void SetCol(ColorWithName col)
		{
			PinInstance pin = (PinInstance)interactionContext;
			pin.Colour = col;
		}

		static void OpenChipLabelPopup()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipLabelPopup);
		}
        static void OpenColorEditPopup(ColorWithName Color)
        {
			
            CustomColorEditMenu.SetColor(Color, OnSaveColor, OnDeleteColor, OnCancelColor, false);
            UIDrawer.SetActiveMenu(UIDrawer.MenuType.ColorMenuPopup);
        }
		static void NewCol()
		{
            
			ColorWithName newColor = new ColorWithName(Color.black, Color.black, Color.black, "New Color");
            CustomColorEditMenu.SetColor(newColor, OnSaveColor, OnDeleteColor, OnCancelColor, true);
            UIDrawer.SetActiveMenu(UIDrawer.MenuType.ColorMenuPopup);
        }

        public static void EditWire()
		{
			Project.ActiveProject.controller.EnterWireEditMode((WireInstance)interactionContext);
		}

		static void Delete()
		{
			if (interactionContext is IMoveable moveable)
			{
				Project.ActiveProject.controller.Delete(moveable);
			}
			else if (interactionContext is WireInstance wire)
			{
				Project.ActiveProject.controller.DeleteWire(wire);
			}
			else if (interactionContext is PinInstance pin)
			{
				Project.ActiveProject.controller.Delete(pin.parent);
			}
		}

		static void OpenKeyBindMenu()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.RebindKeyChip);
		}

		static void OpenRomEditMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.RomEdit);
		
		static void OpenPulseEditMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.PulseEdit);

		static bool CanEditCurrentChip() => Project.ActiveProject.CanEditViewedChip;

		static bool CanEditWire() => CanEditCurrentChip();

		static void OpenPinEditMenu()
		{
			PinEditMenu.SetTargetPin((DevPinInstance)((PinInstance)interactionContext).parent);
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.PinRename);
		}

		static void OpenChip()
		{
			Project project = Project.ActiveProject;
			string chipToOpenName = interactionContextName;

			if (project.ActiveChipHasUnsavedChanges())
			{
				UnsavedChangesPopup.OpenPopup(OpenChipIfConfirmed);
			}
			else
			{
				OpenChipIfConfirmed(true);
			}

			void OpenChipIfConfirmed(bool confirm)
			{
				if (confirm)
				{
					project.LoadDevChipOrCreateNewIfDoesntExist(chipToOpenName);
				}
			}
		}

		static bool CanOpenChip() => IsCustomChip() && CanEditCurrentChip();

		public static void Reset()
		{
			CloseContextMenu();
		}

		public static void CloseContextMenu()
		{
			IsOpen = false;
		}

		public static bool HasFocus() => IsOpen && wasMouseOverMenu;

		public static void UnstarBottomBarEntry()
		{
			Project.ActiveProject.SetStarred(interactionContextName, false, bottomBarItemIsCollection, true);
		}

		public readonly struct MenuEntry
		{
			public readonly string Text;
			public readonly Action OnPress;
			public readonly Func<bool> IsEnabled;
			public readonly Action rightClickAction;
            public readonly bool isColorItem;

            public MenuEntry(string text, Action onPress, Func<bool> isEnabled, Action RightClickAction = null, bool IsColorItem = false)
			{
				Text = text;
				OnPress = onPress;
				IsEnabled = isEnabled;
				rightClickAction = RightClickAction;
                isColorItem = IsColorItem;
            }
		}

        // Called when the color is saved
        static void OnSaveColor(ColorWithName savedColor)
        {
            

            // Check if the color already exists by name
            int index = pinColors.FindIndex(color => color.Name == savedColor.Name);

            if (index != -1)
            {
                // Color exists, so update the existing entry
                pinColors[index] = savedColor;
                
            }
            else
            {
                // Color doesn't exist, so add a new entry
                pinColors.Add(savedColor);
                
            }

            // Rebuild context menu to reflect the updated list of colors
            UpdatePinColEntries();
            Project project = Project.ActiveProject;
			project.SaveCurrentProjectDescription();
        }

        // Called when the color is deleted
        static void OnDeleteColor(string colorName)
        {
            
            int index = pinColors.FindIndex(c => c.Name == colorName);
            if (index != -1)
            {
                pinColors.RemoveAt(index);
                UpdatePinColEntries();  // Rebuild context menu to reflect deletion
                
            }
            Project project = Project.ActiveProject;
            project.SaveCurrentProjectDescription();


        }

        // Called when the edit is canceled
        static void OnCancelColor()
        {
            
            int index = pinColors.FindIndex(c => c.Name == "New Color");
            if (index != -1)
            {
                pinColors.RemoveAt(index);
                UpdatePinColEntries();  // Rebuild context menu to reflect deletion

            }
            // You can choose to refresh the UI or not
        }

    }
}