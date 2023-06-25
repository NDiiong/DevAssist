global using Community.VisualStudio.Toolkit;
global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.Completion;
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.CodeAnalysis.CSharp.Syntax;
global using Microsoft.CodeAnalysis.Tags;
global using Microsoft.VisualStudio;
global using Microsoft.VisualStudio.Shell;
global using System;
global using System.Collections.Immutable;
global using System.Linq;
global using System.Runtime.InteropServices;
global using System.Threading;
global using System.Threading.Tasks;
global using Task = System.Threading.Tasks.Task;
using EnvDTE;

namespace DevAssist
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(DevAssistPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.VsEditorFactoryGuid.TextEditor_string, PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration("DevAssist", "DevAssist", "1.0")]
    public sealed class DevAssistPackage : AsyncPackage
    {
        public const string PackageGuidString = "a50daca1-ee7e-472a-9d48-c2879b05068d";

        private readonly Lazy<DTE> _dte = new(() =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
        }, true);

        private DTE Dte => _dte.Value;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        private async Task ShowTaskListAsync()
        {
            //JAVIERS: DELAY THIS EXECUTION TO OPEN THE WINDOW AFTER EVERYTHING IS LOADED
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            await System.Threading.Tasks.Task.Delay(1000);

            var window = Dte.Windows.Item(EnvDTE.Constants.vsWindowKindTaskList);

            window.Activate();
        }
    }
}