using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using DLS.ColorStorage;
using DLS.Graphics;
using System;
using static Seb.Helpers.ColHelper;
using Seb.Helpers;

namespace DLS.Graphics
{
    public static class CustomColorEditMenu
    {
        const string MaxNameLength = "12345678901"; // 11 characters for Color Name
        const string MaxColorLength = "255"; // Max length for RGB values (0-255)
        static ColorWithName colorBeingEdited;
        static Action<ColorWithName> onSaveCallback;
        static Action<string> onDeleteCallback;
        static Action onCancelCallback;
        private static bool isValidatingAllInputs = false; //used for save button checks
        private static bool IsNewColor = false; 

        static string colorName;
        static readonly UIHandle ID_NameField = new UIHandle("CustomColorEditMenu_NameField");

        // Declare variables for the color values (Low and High Color R, G, B)
        private static int lowR, lowG, lowB;
        private static int highR, highG, highB;

        static InputFieldState IlowR, IlowG, IlowB;

        // Declare UIHandles correctly using the string ID and int ID
        private static readonly UIHandle ID_LowR = new UIHandle("LowR", 0);
        private static readonly UIHandle ID_LowG = new UIHandle("LowG", 1);
        private static readonly UIHandle ID_LowB = new UIHandle("LowB", 2);
        private static readonly UIHandle ID_HighR = new UIHandle("HighR", 3);
        private static readonly UIHandle ID_HighG = new UIHandle("HighG", 4);
        private static readonly UIHandle ID_HighB = new UIHandle("HighB", 5);
        private static readonly UIHandle ID_ColorName = new UIHandle("ColorName", 6);
        private static readonly UIHandle[] UI_IDs = 
            {
            ID_LowR,
            ID_LowG,
            ID_LowB,
            ID_HighR,
            ID_HighG,
            ID_HighB,
            ID_ColorName
        };

        static int focusedRowIndex;

        static readonly string[] CancelSaveDeleteButtonNames =
        {
            "CANCEL", "SAVE", "DELETE"
        };
        static readonly string[] CancelSaveButtonNames =
        {
            "CANCEL", "SAVE"
        };
                

        static bool[] ButtonGroupInteractStates = { true, true, true };
        static bool[] ButtonGroupInteractStates2 = { true, true };

        // Called when the menu is opened
        public static void OnMenuOpened()
        {
            UI.GetInputFieldState(ID_ColorName).SetFocus(true); //sets focus on the Name field
            UI.GetInputFieldState(ID_ColorName).SetText(colorName); //Sets Color Name Text
            UI.GetInputFieldState(ID_LowR).SetText(lowR.ToString()); //Sets LowR Value Text
            UI.GetInputFieldState(ID_LowG).SetText(lowG.ToString()); //Sets LowG Value Text
            UI.GetInputFieldState(ID_LowB).SetText(lowB.ToString()); //Sets LowB Value Text
            UI.GetInputFieldState(ID_HighR).SetText(highR.ToString()); //Sets HighR Value Text
            UI.GetInputFieldState(ID_HighG).SetText(highG.ToString()); //Sets HighG Value Text
            UI.GetInputFieldState(ID_HighB).SetText(highB.ToString()); //Sets HighB Value Text
            UI.GetInputFieldState(ID_LowR).SetFocus(false);// Remove focus from the LowR field
            UI.GetInputFieldState(ID_LowG).SetFocus(false);// Remove focus from the LowG field  
            UI.GetInputFieldState(ID_LowB).SetFocus(false);// Remove focus from the LowB field  
            UI.GetInputFieldState(ID_HighR).SetFocus(false);// Remove focus from the HighR field    
            UI.GetInputFieldState(ID_HighG).SetFocus(false);// Remove focus from the HighG field    
            UI.GetInputFieldState(ID_HighB).SetFocus(false);// Remove focus from the HighB field    
            focusedRowIndex = 6;
            // IDS_inputRow = new UIHandle[7]
        }

        // Draws the menu on the screen
        public static void DrawMenu()
        {
            // Background overlay
            UI.DrawFullscreenPanel(DrawSettings.ActiveUITheme.MenuBackgroundOverlayCol);
            

            // Get the theme settings
            DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
            InputFieldTheme inputTheme = DrawSettings.ActiveUITheme.CustomColorTheme;

            // Reserve space for the panel
            Draw.ID panelID = UI.ReservePanel();

            // Begin bounds for the UI elements inside the menu
            using (UI.BeginBoundsScope(true))
            {
                Vector2 startPos = UI.Centre + new Vector2(-2,3) * 5; //Save for new line entries
                Vector2 pos = startPos;
                Vector2 unpaddedSizeC = Draw.CalculateTextBoundsSize(MaxColorLength, inputTheme.fontSize, inputTheme.font);
                Vector2 unpaddedSizeN = Draw.CalculateTextBoundsSize(MaxNameLength, inputTheme.fontSize, inputTheme.font);
                float labelSpacing = 1.5f;
                Vector2 inputFieldSizeC = unpaddedSizeC + new Vector2(1.5f, 2.25f);
                Color textCol = Color.white;

                // --- Draw "Low Color" Row ---
                UI.DrawText("Low Colour", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.TextCentre, textCol);
                pos.y -= 1f;

                // Low color inputs (R, G, B)
                InputFieldState IlowR = UI.InputField(ID_LowR, inputTheme, pos, inputFieldSizeC, lowR.ToString(), Anchor.TopLeft, 0.5f, ValidateColorInput);
                string slowR = IlowR.text;
               
                int.TryParse(slowR, out lowR);
              
                pos.x += inputFieldSizeC.x + labelSpacing;
                InputFieldState IlowG = UI.InputField(ID_LowG, inputTheme, pos, inputFieldSizeC, lowG.ToString(), Anchor.TopLeft, 0.5f, ValidateColorInput);
                string slowG = IlowG.text;
                int.TryParse(slowG, out lowG);
                 
                pos.x += inputFieldSizeC.x + labelSpacing;
                InputFieldState IlowB = UI.InputField(ID_LowB, inputTheme, pos, inputFieldSizeC, lowB.ToString(), Anchor.TopLeft, 0.5f, ValidateColorInput);
                string slowB = IlowB.text;
                int.TryParse(slowB, out lowB);
                
                
                // Reset position after low color row
                pos.x = startPos.x;
                pos.y -= 1f + inputFieldSizeC.y;

                // --- Draw "High Color" Row ---
                UI.DrawText("High Colour", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.TextCentre, textCol);
                pos.y -= 1f;

                // High color inputs (R, G, B)
                InputFieldState IhighR = UI.InputField(ID_HighR, inputTheme, pos, inputFieldSizeC, highR.ToString(), Anchor.TopLeft, 0.5f, ValidateColorInput);
                string shighR = IhighR.text;
                int.TryParse(shighR, out highR);
                
                
                pos.x += inputFieldSizeC.x + labelSpacing;
                InputFieldState IhighG = UI.InputField(ID_HighG, inputTheme, pos, inputFieldSizeC, highG.ToString(), Anchor.TopLeft, 0.5f, ValidateColorInput);
                string shighG = IhighG.text;
                int.TryParse(shighG, out highG);
                
                pos.x += inputFieldSizeC.x + labelSpacing;
                InputFieldState IhighB = UI.InputField(ID_HighB, inputTheme, pos, inputFieldSizeC, highB.ToString(), Anchor.TopLeft, 0.5f, ValidateColorInput);
                string shighB = IhighB.text;
                int.TryParse(shighB, out highB);
               
                
                // Reset position after high color row
                pos.x = startPos.x;
                pos.y -= 1f + inputFieldSizeC.y;

                // --- Draw "Color Name" Row ---
                UI.DrawText("Colour Name", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.TextCentre, textCol);
                pos.y -= 1f;

                // Color name input field
                InputFieldState IcolorName = UI.InputField(ID_ColorName, inputTheme, pos, new Vector2(22f, 2.25f), colorName, Anchor.TopLeft, 0.5f, ValidateColorNameInput);
                colorName = IcolorName.text;

                // --- Buttons ---
                bool saveButtonEnables = ValidateAllInputs(IlowR.text,IlowB.text,IlowG.text,IhighR.text,IhighG.text,IhighB.text);
                ButtonGroupInteractStates[1] = saveButtonEnables; // Save button
                string[] buttonGroupNames = !IsNewColor ? CancelSaveDeleteButtonNames : CancelSaveButtonNames;
                Vector2 buttonsTopLeft = pos + Vector2.down * 3;
                int buttonIndex = UI.HorizontalButtonGroup(buttonGroupNames, ButtonGroupInteractStates, theme.ButtonTheme, buttonsTopLeft, 20f, DrawSettings.DefaultButtonSpacing, 0, Anchor.TopLeft);

                // Draw the reserved panel
                MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());

                // Handle button actions
                if (KeyboardShortcuts.CancelShortcutTriggered || buttonIndex == 0)
                {
                    CancelEdit();  // Handle cancel action
                }
                else if (KeyboardShortcuts.ConfirmShortcutTriggered || buttonIndex == 1)
                {
                    SaveEdit(new ColorWithName
                    {
                        Name = colorName,
                        LowColor = new Color(lowR, lowG, lowB),
                        HighColor = new Color(highR, highG, highB)
                    });  // Handle confirm action with the new color details
                }
                else if (KeyboardShortcuts.ConfirmShortcutTriggered || buttonIndex == 2)
                {
                    DeleteColor(colorName);  // Handle delete action
                }
                
                
                // Focus next/prev field with keyboard shortcuts
                bool changeLine = KeyboardShortcuts.ConfirmShortcutTriggered || InputHelper.IsKeyDownThisFrame(KeyCode.Tab);
                if (changeLine)
                {
                    bool goPrevLine = InputHelper.ShiftIsHeld;
                    int jumpToRowIndex = focusedRowIndex + (goPrevLine ? -1 : 1);
                    if (jumpToRowIndex <0)
                    {
                        jumpToRowIndex = 6;
                    }
                    else if(jumpToRowIndex > 6)
                    {
                        jumpToRowIndex = 0;
                    }
                    if(jumpToRowIndex == 6)
                    {
                        UI.GetInputFieldState(UI_IDs[focusedRowIndex]).SetFocus(false);
                        UI.GetInputFieldState(ID_ColorName).SetFocus(true); //sets focus on the Name field
                        UI.GetInputFieldState(ID_ColorName).SetText(colorName); //Sets Color Name Text
                    }
                    else if (jumpToRowIndex == 0)
                    {
                        UI.GetInputFieldState(UI_IDs[focusedRowIndex]).SetFocus(false);
                        UI.GetInputFieldState(ID_LowR).SetFocus(true); //sets focus on the LowR field
                        UI.GetInputFieldState(ID_LowR).SetText(lowR.ToString()); //Sets LowR Value Text
                    }
                    else if (jumpToRowIndex == 1)
                    {
                        UI.GetInputFieldState(UI_IDs[focusedRowIndex]).SetFocus(false);
                        UI.GetInputFieldState(ID_LowG).SetFocus(true); //sets focus on the LowG field
                        UI.GetInputFieldState(ID_LowG).SetText(lowG.ToString()); //Sets LowG Value Text
                    }
                    else if (jumpToRowIndex == 2)
                    {
                        UI.GetInputFieldState(UI_IDs[focusedRowIndex]).SetFocus(false);
                        UI.GetInputFieldState(ID_LowB).SetFocus(true); //sets focus on the LowB field
                        UI.GetInputFieldState(ID_LowB).SetText(lowB.ToString()); //Sets LowB Value Text
                    }
                    else if (jumpToRowIndex == 3)
                    {
                        UI.GetInputFieldState(UI_IDs[focusedRowIndex]).SetFocus(false);
                        UI.GetInputFieldState(ID_HighR).SetFocus(true); //sets focus on the HighR field
                        UI.GetInputFieldState(ID_HighR).SetText(highR.ToString()); //Sets HighR Value Text
                    }
                    else if (jumpToRowIndex == 4)
                    {
                        UI.GetInputFieldState(UI_IDs[focusedRowIndex]).SetFocus(false);
                        UI.GetInputFieldState(ID_HighG).SetFocus(true); //sets focus on the HighG field
                        UI.GetInputFieldState(ID_HighG).SetText(highG.ToString()); //Sets HighG Value Text
                    }
                    else if (jumpToRowIndex == 5)
                    {
                        UI.GetInputFieldState(UI_IDs[focusedRowIndex]).SetFocus(false);
                        UI.GetInputFieldState(ID_ColorName).SetFocus(false);
                        UI.GetInputFieldState(ID_HighB).SetFocus(true); //sets focus on the HighB field
                        UI.GetInputFieldState(ID_HighB).SetText(highB.ToString()); //Sets HighB Value Text
                    }
                    focusedRowIndex = jumpToRowIndex;
                    
                }
            }
        }

        // Sets the color to be edited and callback functions
        public static void SetColor(ColorWithName color, Action<ColorWithName> onSave, Action<string> onDelete, Action onCancel, bool isNewColor)
        {
            colorBeingEdited = color;
            onSaveCallback = onSave;
            onDeleteCallback = onDelete;
            onCancelCallback = onCancel;
            IsNewColor = isNewColor;
            // Convert the color values to integers (0-255) for display
            lowR = (int)MathF.Round(color.LowColor.r * 255f) ;
            lowG = (int)MathF.Round(color.LowColor.g * 255f);
            lowB = (int)MathF.Round(color.LowColor.b * 255f);
            highR = (int)MathF.Round(color.HighColor.r * 255f);
            highG = (int)MathF.Round(color.HighColor.g * 255f);
            highB = (int)MathF.Round(color.HighColor.b * 255f);

            colorName = color.Name;
        }

        // This cancels the edit and closes the menu
        static void CancelEdit()
        {
            onCancelCallback?.Invoke(); // Trigger the cancel callback
            UIDrawer.SetActiveMenu(UIDrawer.MenuType.None); // Close the menu
        }

        // This saves the edited color and triggers the save callback
        static void SaveEdit(ColorWithName colorBeingEdited)
        {
            colorBeingEdited.LowColor = new Color(lowR/255f, lowG/255f, lowB/255f);
            colorBeingEdited.HighColor = new Color(highR/255f, highG/255f, highB/255f);
            colorBeingEdited.HoverColor = Brighten(colorBeingEdited.LowColor, 0.1f); // Brighten the low color for hover effect
            colorBeingEdited.Name = colorName;
            onSaveCallback?.Invoke(colorBeingEdited); // Trigger the save callback with the updated color

            // Optionally, you can refresh the list
            //ContextMenu.UpdatePinColEntries();

            UIDrawer.SetActiveMenu(UIDrawer.MenuType.None); // Close the menu
        }

        // This deletes the color and triggers the delete callback
        static void DeleteColor(string colorName)
        {

            onDeleteCallback?.Invoke(colorName); // Trigger the delete callback with the deleted color

            UIDrawer.SetActiveMenu(UIDrawer.MenuType.None); // Close the menu
        }

        // Helper to parse floats
        private static float ParseFloat(string input)
        {
            if (float.TryParse(input, out float result))
            {
                return result;
            }
            return 0f;  // Default value if parsing fails
        }


        // Validate function for color input (R, G, B)
        // Validate function for color input (RGB)
        private static bool ValidateColorInput(string input)
        {
            // If we are inside ValidateAllInputs, treat empty inputs as invalid
            if (isValidatingAllInputs)
            {
                return !string.IsNullOrEmpty(input) && int.TryParse(input, out int value) && value >= 0 && value <= 255;
            }

            // If not in ValidateAllInputs, allow empty input (i.e., user can delete the last digit)
            return true;
        }

        // Validate function for color name input
        private static bool ValidateColorNameInput(string input)
        {
            // If we are inside ValidateAllInputs, treat empty names as invalid
            if (isValidatingAllInputs)
            {
                // Ensure the name is not empty, not just whitespace, and does not exceed 12 characters
                return !string.IsNullOrWhiteSpace(input) && input.Length <= 12;
            }

            // If not in ValidateAllInputs, allow empty input (i.e., user can delete the last character)
            // But enforce max length of 12 for non-empty input
            if (input.Length > 12)
            {
                return false;  // Invalid: Name is too long
            }

            return true;
        }

        // Check all inputs and decide if the save button should be enabled
        private static bool ValidateAllInputs(string lowR, string lowG, string lowB, string highR, string highG, string highB)
        {
            isValidatingAllInputs = true;
            // Check if all RGB inputs are valid
            
            bool areColorsValid = ValidateColorInput(lowR) && ValidateColorInput(lowG) && ValidateColorInput(lowB) &&
                          ValidateColorInput(highR) && ValidateColorInput(highG) && ValidateColorInput(highB);

            // Check if the color name is valid
            bool isColorNameValid = ValidateColorNameInput(colorName);

            isValidatingAllInputs = false;
            // Enable the save button only if all inputs are valid
            
            return areColorsValid && isColorNameValid;
        }



    }
}
