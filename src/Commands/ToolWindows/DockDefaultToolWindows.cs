namespace DevAssist
{
    internal class DockDefaultToolWindowsCommand : BaseCommand<DockDefaultToolWindowsCommand>
    {
        private async Task ShowTaskListAsync()
        {
            //JAVIERS: DELAY THIS EXECUTION TO OPEN THE WINDOW AFTER EVERYTHING IS LOADED
            await System.Threading.Tasks.Task.Delay(1000);

            //var window = Dte.Windows.Item(EnvDTE.Constants.vsWindowKindTaskList);
            //window.Activate();
        }
    }
}