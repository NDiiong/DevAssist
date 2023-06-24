global using Community.VisualStudio.Toolkit;
global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.Completion;
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.CodeAnalysis.CSharp.Syntax;
global using Microsoft.CodeAnalysis.Tags;
global using Microsoft.VisualStudio.Shell;
global using System;
global using System.Collections.Immutable;
global using System.Linq;
global using System.Runtime.InteropServices;
global using System.Threading;
global using System.Threading.Tasks;
global using Task = System.Threading.Tasks.Task;

namespace DevAssist
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(DevAssistPackage.PackageGuidString)]
    public sealed class DevAssistPackage : AsyncPackage
    {
        public const string PackageGuidString = "a50daca1-ee7e-472a-9d48-c2879b05068d";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }
    }
}