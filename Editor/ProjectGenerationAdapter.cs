using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VSCodeEditor;

namespace trit.LanguageServerScriptEditor
{
    internal sealed class ProjectGenerationAdapter
    {
        readonly ProjectGeneration projectGeneration;
        readonly IGenerator generator;

        internal ProjectGenerationAdapter()
        {
            var projectDir = Directory.GetParent(Application.dataPath).FullName;
            projectGeneration = new ProjectGeneration(projectDir);
            generator = projectGeneration;
        }

        internal string ProjectDirectory => projectGeneration.ProjectDirectory;

        internal bool SolutionExists()
        {
            return projectGeneration.SolutionExists();
        }

        internal IAssemblyNameProvider AssemblyNameProvider => generator.AssemblyNameProvider;

        internal void SyncAll()
        {
            AssetDatabase.Refresh();
            projectGeneration.Sync();
        }

        internal void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            projectGeneration.SyncIfNeeded(
                addedFiles.Union(deletedFiles).Union(movedFiles).Union(movedFromFiles).ToList(),
                importedFiles);
        }
    }
}
