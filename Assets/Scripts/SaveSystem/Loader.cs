using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DLS.Description;
using DLS.Game;
using UnityEngine;
using DLS.ColorStorage;
using static Seb.Helpers.ColHelper;

namespace DLS.SaveSystem
{
	public static class Loader
	{
		
        public static AppSettings LoadAppSettings()
		{
			if (File.Exists(SavePaths.AppSettingsPath))
			{
				string settingsString = File.ReadAllText(SavePaths.AppSettingsPath);
				return Serializer.DeserializeAppSettings(settingsString);
			}

			return AppSettings.Default();
		}

		public static Project LoadProject(string projectName)
		{
			ProjectDescription projectDescription = LoadProjectDescription(projectName);
			ChipLibrary chipLibrary = LoadChipLibrary(projectDescription);
			
			if (projectDescription.CustomPinColors == null || projectDescription.CustomPinColors.Count == 0)
			{
				DLS.Graphics.ContextMenu.pinColors = projectDescription.CustomPinColors;
				{
					projectDescription.CustomPinColors = new List<ColorWithName> {
				new ColorWithName(
				new Color(0.2f, 0.1f, 0.1f),  // LowColor
                new Color(0.95f, 0.3f, 0.31f), // HighColor
                Brighten(new Color(0.2f, 0.1f, 0.1f), 0.1f), // HoverColor
                "Red"
			),

			new ColorWithName(
				new Color(0.25f, 0.18f, 0.04f), // LowColor
                new Color(0.92f, 0.7f, 0.25f),  // HighColor
                Brighten(new Color(0.25f, 0.18f, 0.04f), 0.1f), // HoverColor
                "Yellow"
			),

			new ColorWithName(
				new Color(0.1f, 0.2f, 0.1f),   // LowColor
                new Color(0.25f, 0.66f, 0.31f), // HighColor
                Brighten(new Color(0.1f, 0.2f, 0.1f), 0.1f),  // HoverColor
                "Green"
			),

			new ColorWithName(
				new Color(0.1f, 0.14f, 0.35f), // LowColor
                new Color(0.2f, 0.5f, 1f),     // HighColor
                Brighten(new Color(0.1f, 0.14f, 0.35f), 0.1f), // HoverColor
                "Blue"
			),

			new ColorWithName(
				new Color(0.19f, 0.12f, 0.28f), // LowColor
                new Color(0.6f, 0.4f, 0.98f),   // HighColor
                Brighten(new Color(0.19f, 0.12f, 0.28f), 0.1f), // HoverColor
                "Violet"
			),

			new ColorWithName(
				new Color(0.25f, 0.1f, 0.25f), // LowColor
                new Color(0.84f, 0.33f, 0.9f),  // HighColor
                Brighten(new Color(0.25f, 0.1f, 0.25f), 0.1f), // HoverColor
                "Pink"
			),

			new ColorWithName(
				new Color(0.4f, 0.4f, 0.4f),   // LowColor
                new Color(0.9f, 0.9f, 0.9f),   // HighColor
                Brighten(new Color(0.4f, 0.4f, 0.4f), 0.1f),   // HoverColor
                "White"
			),

			new ColorWithName(
				new Color(0.027f, 0.537f, 0.651f),   // LowColor
                new Color(0f, 0.82f, 1f),   // HighColor
                Brighten(new Color(0.027f, 0.537f, 0.651f), 0.1f),   // HoverColor
                "Debugging"
			)};
					
				}
			}
                DLS.Graphics.ContextMenu.pinColors = projectDescription.CustomPinColors;
                DLS.Graphics.ContextMenu.UpdatePinColEntries();

				return new Project(projectDescription, chipLibrary);
		}
		

		public static bool ProjectExists(string projectName)
		{
			string path = SavePaths.GetProjectDescriptionPath(projectName);
			return File.Exists(path);
		}

		public static ProjectDescription LoadProjectDescription(string projectName)
		{
			string path = SavePaths.GetProjectDescriptionPath(projectName);
			if (!File.Exists(path)) throw new Exception("No project description found at " + path);

			ProjectDescription desc = Serializer.DeserializeProjectDescription(File.ReadAllText(path));
			desc.ProjectName = projectName; // Enforce name = directory name (in case player modifies manually -- operations like deleting projects rely on this)

			for (int i = 0; i < desc.StarredList.Count; i++)
			{
				StarredItem starred = desc.StarredList[i];
				starred.CacheDisplayStrings();
				desc.StarredList[i] = starred;
			}

			foreach (ChipCollection collection in desc.ChipCollections)
			{
				collection.UpdateDisplayStrings();
			}
            
            return desc;
		}

		// Get list of saved project descriptions (ordered by last save time)
		public static ProjectDescription[] LoadAllProjectDescriptions()
		{
			List<ProjectDescription> projectDescriptions = new();

			foreach (string dir in Directory.EnumerateDirectories(SavePaths.ProjectsPath))
			{
				try
				{
					string projectName = Path.GetFileName(dir);
					projectDescriptions.Add(LoadProjectDescription(projectName));
				}
				catch (Exception)
				{
					// Ignore invalid project directory
				}
			}

			projectDescriptions.Sort((a, b) => b.LastSaveTime.CompareTo(a.LastSaveTime));
			return projectDescriptions.ToArray();
		}

		static ChipLibrary LoadChipLibrary(ProjectDescription projectDescription)
		{
			string chipDirectoryPath = SavePaths.GetChipsPath(projectDescription.ProjectName);
			ChipDescription[] loadedChips = new ChipDescription[projectDescription.AllCustomChipNames.Length];

			if (!Directory.Exists(chipDirectoryPath) && loadedChips.Length > 0) throw new DirectoryNotFoundException(chipDirectoryPath);

			ChipDescription[] builtinChips = BuiltinChipCreator.CreateAllBuiltinChipDescriptions();
			HashSet<string> customChipNameHashset = new(ChipDescription.NameComparer);

			for (int i = 0; i < loadedChips.Length; i++)
			{
				string chipPath = Path.Combine(chipDirectoryPath, projectDescription.AllCustomChipNames[i] + ".json");
				string chipSaveString = File.ReadAllText(chipPath);

				ChipDescription chipDesc = Serializer.DeserializeChipDescription(chipSaveString);
				loadedChips[i] = chipDesc;
				customChipNameHashset.Add(chipDesc.Name);
			}
			
			
			// If built-in chip name conflicts with a custom chip, the built-in chip must have been added in a newer version.
			// In that case, simply exclude the built-in chip. TODO: warn player that they should rename their chip if they want access to new builtin version
			builtinChips = builtinChips.Where(b => !customChipNameHashset.Contains(b.Name)).ToArray();

			return new ChipLibrary(loadedChips, builtinChips);
		}
    }
}