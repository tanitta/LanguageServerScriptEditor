using UnityEditor;
using UnityEngine;

namespace trit.LanguageServerScriptEditor
{
    [FilePath("ProjectSettings/LanguageServerEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class LanguageServerEditorSettings : ScriptableSingleton<LanguageServerEditorSettings>
    {
        internal const string DefaultCommandTemplate = "code \"{file}\" -g {line}:{col}";

        [SerializeField] string commandTemplate = DefaultCommandTemplate;

        internal string CommandTemplate
        {
            get => string.IsNullOrWhiteSpace(commandTemplate) ? DefaultCommandTemplate : commandTemplate;
            set
            {
                commandTemplate = value;
                Save(true);
            }
        }
    }
}
