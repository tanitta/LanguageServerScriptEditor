using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;
using VSCodeEditor;

namespace LanguageServerScriptEditor
{
    [InitializeOnLoad]
    public class LanguageServerScriptEditor : IExternalCodeEditor
    {
        const string EditorName = "Language Server Script Editor";
        const string EditorPath = "LanguageServerScriptEditor";
        const string MarkerFileName = "LanguageServerScriptEditor.marker";

        readonly ProjectGenerationAdapter projectGeneration;

        static LanguageServerScriptEditor()
        {
            var editor = new LanguageServerScriptEditor(new ProjectGenerationAdapter());
            CodeEditor.Register(editor);

            if (IsSelectedEditorPath(CodeEditor.CurrentEditorInstallation) && !editor.projectGeneration.SolutionExists())
            {
                editor.projectGeneration.SyncAll();
            }
        }

        LanguageServerScriptEditor(ProjectGenerationAdapter adapter)
        {
            projectGeneration = adapter;
        }

        public CodeEditor.Installation[] Installations => new[]
        {
            new CodeEditor.Installation
            {
                Name = EditorName,
                Path = GetInstallationPath()
            }
        };

        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            if (IsSelectedEditorPath(editorPath))
            {
                installation = new CodeEditor.Installation
                {
                    Name = EditorName,
                    Path = GetInstallationPath()
                };
                return true;
            }

            installation = default;
            return false;
        }

        public void Initialize(string editorInstallationPath)
        {
            if (!IsSelectedEditorPath(editorInstallationPath))
            {
                return;
            }

            if (!projectGeneration.SolutionExists())
            {
                projectGeneration.SyncAll();
            }
        }

        public void OnGUI()
        {
            var settings = LanguageServerEditorSettings.instance;

            EditorGUILayout.LabelField("Launch command template");
            settings.CommandTemplate = EditorGUILayout.TextField("Command Template", settings.CommandTemplate);

            EditorGUILayout.HelpBox("Placeholders: $(File) $(Line) $(Column)", MessageType.Info);

            EditorGUILayout.LabelField("Generate .csproj files for:");
            EditorGUI.indentLevel++;
            SettingsButton(ProjectGenerationFlag.Embedded, "Embedded packages", "");
            SettingsButton(ProjectGenerationFlag.Local, "Local packages", "");
            SettingsButton(ProjectGenerationFlag.Registry, "Registry packages", "");
            SettingsButton(ProjectGenerationFlag.Git, "Git packages", "");
            SettingsButton(ProjectGenerationFlag.BuiltIn, "Built-in packages", "");
#if UNITY_2019_3_OR_NEWER
            SettingsButton(ProjectGenerationFlag.LocalTarBall, "Local tarball", "");
#endif
            SettingsButton(ProjectGenerationFlag.Unknown, "Packages from unknown sources", "");
            SettingsButton(ProjectGenerationFlag.PlayerAssemblies, "Player projects", "For each player project generate an additional csproj with the name 'project-player.csproj'");
            RegenerateProjectFiles();
            EditorGUI.indentLevel--;
        }

        void RegenerateProjectFiles()
        {
            var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(new GUILayoutOption[] { }));
            rect.width = 252;
            if (GUI.Button(rect, "Regenerate project files"))
            {
                projectGeneration.SyncAll();
            }
        }

        void SettingsButton(ProjectGenerationFlag preference, string guiMessage, string toolTip)
        {
            var prevValue = projectGeneration.AssemblyNameProvider.ProjectGenerationFlag.HasFlag(preference);
            var newValue = EditorGUILayout.Toggle(new GUIContent(guiMessage, toolTip), prevValue);
            if (newValue != prevValue)
            {
                projectGeneration.AssemblyNameProvider.ToggleProjectGeneration(preference);
            }
        }

        public bool OpenProject(string path, int line, int column)
        {
            var template = LanguageServerEditorSettings.instance.CommandTemplate;
            if (string.IsNullOrWhiteSpace(template))
            {
                UnityEngine.Debug.LogWarning("LanguageServerScriptEditor: Command Template is empty.");
                return false;
            }

            var resolvedLine = line <= 0 ? 1 : line;
            var resolvedColumn = column <= 0 ? 1 : column;
            var resolvedPath = string.IsNullOrEmpty(path) ? projectGeneration.ProjectDirectory : Path.GetFullPath(path);

            var tokens = CommandLineTokenizer.Tokenize(template);
            if (tokens.Count == 0)
            {
                UnityEngine.Debug.LogWarning("LanguageServerScriptEditor: Command Template did not produce a command.");
                return false;
            }

            var fileName = ReplaceTokens(tokens[0], resolvedPath, resolvedLine, resolvedColumn);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                UnityEngine.Debug.LogWarning("LanguageServerScriptEditor: Command Template did not produce an executable.");
                return false;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            for (var i = 1; i < tokens.Count; i++)
            {
                var argument = ReplaceTokens(tokens[i], resolvedPath, resolvedLine, resolvedColumn);
                if (!string.IsNullOrEmpty(argument))
                {
                    startInfo.ArgumentList.Add(argument);
                }
            }

            try
            {
                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"LanguageServerScriptEditor: Failed to start command. {ex}");
                return false;
            }
        }

        public void SyncAll()
        {
            if (!IsSelected)
            {
                return;
            }

            projectGeneration.SyncAll();
        }

        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            if (!IsSelected)
            {
                return;
            }

            projectGeneration.SyncIfNeeded(addedFiles, deletedFiles, movedFiles, movedFromFiles, importedFiles);
        }

        static string ReplaceTokens(string input, string file, int line, int column)
        {
            return input
                .Replace("$(File)", file)
                .Replace("$(Line)", line.ToString())
                .Replace("$(Column)", column.ToString());
        }

        static bool IsSelectedEditorPath(string editorPath)
        {
            if (string.IsNullOrEmpty(editorPath))
            {
                return false;
            }

            var expected = GetInstallationPath();
            if (string.Equals(editorPath, expected, StringComparison.Ordinal))
            {
                return true;
            }

            return string.Equals(editorPath, EditorPath, StringComparison.Ordinal);
        }

        static bool IsSelected
        {
            get
            {
                return CodeEditor.CurrentEditor is LanguageServerScriptEditor
                    && IsSelectedEditorPath(CodeEditor.CurrentEditorInstallation);
            }
        }

        static string GetInstallationPath()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(LanguageServerScriptEditor).Assembly);
            if (packageInfo == null)
            {
                return EditorPath;
            }

            var markerPath = Path.Combine(packageInfo.assetPath, MarkerFileName);
            return Path.GetFullPath(markerPath);
        }

        static class CommandLineTokenizer
        {
            internal static List<string> Tokenize(string commandLine)
            {
                var tokens = new List<string>();
                if (string.IsNullOrWhiteSpace(commandLine))
                {
                    return tokens;
                }

                var current = new StringBuilder();
                var inQuotes = false;
                var quoteChar = '\0';

                for (var i = 0; i < commandLine.Length; i++)
                {
                    var c = commandLine[i];
                    if (c == '"' || c == '\'')
                    {
                        if (inQuotes && c == quoteChar)
                        {
                            inQuotes = false;
                            continue;
                        }

                        if (!inQuotes)
                        {
                            inQuotes = true;
                            quoteChar = c;
                            continue;
                        }
                    }

                    if (char.IsWhiteSpace(c) && !inQuotes)
                    {
                        if (current.Length > 0)
                        {
                            tokens.Add(current.ToString());
                            current.Clear();
                        }
                        continue;
                    }

                    current.Append(c);
                }

                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                }

                return tokens;
            }
        }
    }
}
