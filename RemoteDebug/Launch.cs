using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace RemoteDebug
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Launch
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("2081eaf5-db71-42d9-82ce-6a11fed73e88");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Launch"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private Launch(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Launch Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new Launch(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Get the open folder filename
            EnvDTE80.DTE2 dte = ServiceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            EnvDTE100.Solution4 solution = dte.Solution as EnvDTE100.Solution4;
            string filename = solution.FileName;

            // Parse settings XML
            string MachineName, Path;
            try
            {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.Load(filename + "\\.vs\\RemoteDebug.xml");
                System.Xml.XmlNode root = doc.SelectSingleNode("RemoteDebug");
                MachineName = root.SelectSingleNode("MachineName").InnerText;
                Path = root.SelectSingleNode("Path").InnerText;
            }
            catch (Exception exception)
            {
                // Report any exceptions as message box
                VsShellUtilities.ShowMessageBox(
                    this.ServiceProvider,
                    exception.Message,
                    "Remote Debug Launch",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                return;
            }

            // Get a pointer to the debug engine
            Guid guidDebugEngine = Microsoft.VisualStudio.VSConstants.DebugEnginesGuids.ManagedAndNative_guid;
            IntPtr pguidDebugEngine = Marshal.AllocCoTaskMem(Marshal.SizeOf(guidDebugEngine));
            Marshal.StructureToPtr(guidDebugEngine, pguidDebugEngine, false);

            // Setup target info
            VsDebugTargetInfo4[] pDebugTargets = new VsDebugTargetInfo4[1];
            pDebugTargets[0].bstrExe = Path;
            pDebugTargets[0].bstrRemoteMachine = MachineName;
            pDebugTargets[0].dlo = (uint)DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;
            pDebugTargets[0].guidLaunchDebugEngine = Guid.Empty;
            pDebugTargets[0].dwDebugEngineCount = 1;
            pDebugTargets[0].pDebugEngines = pguidDebugEngine;

            try
            {
                // Launch debugger
                IVsDebugger4 vsdbg = Package.GetGlobalService(typeof(IVsDebugger)) as IVsDebugger4;
                VsDebugTargetProcessInfo[] pLaunchResults = new VsDebugTargetProcessInfo[1];
                vsdbg.LaunchDebugTargets4(1, pDebugTargets, pLaunchResults);
            }
            catch (Exception exception)
            {
                // Report any exceptions as message box
                VsShellUtilities.ShowMessageBox(
                    this.ServiceProvider,
                    exception.Message,
                    "Remote Debug Launch",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }

            // Cleanup marshalled data
            if (pguidDebugEngine != IntPtr.Zero)
                Marshal.FreeCoTaskMem(pguidDebugEngine);
        }
    }
}
