using EnvDTE;
using EnvDTE80;
using System.IO;
using Process = System.Diagnostics.Process;
using Project = EnvDTE.Project;

namespace DevAssist
{
    //[Command(PackageGuids.guidPackageCmdSetString, PackageIds.OpenBinFolder)]
    internal sealed class OpenBinFolderCommand : BaseCommand<OpenBinFolderCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = await VS.GetServiceAsync<DTE, DTE2>();
            foreach (Project activeProject in (Array)dte.ActiveSolutionProjects)
            {
                var currentProjectPath = Path.GetDirectoryName(activeProject.FullName);
                var currentProjectOutputPath = activeProject
                    .ConfigurationManager
                    .ActiveConfiguration
                    .Properties.Item("OutputPath").Value.ToString();
                var currentProjectBinPath = Path.Combine(currentProjectPath, currentProjectOutputPath);

                var fileName = Directory.Exists(currentProjectBinPath) ? currentProjectBinPath : currentProjectPath;
                using (var _ = Process.Start(currentProjectPath)) { };
            }
        }
    }
}