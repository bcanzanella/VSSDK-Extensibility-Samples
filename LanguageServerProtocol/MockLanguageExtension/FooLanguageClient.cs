using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MockLanguageExtension
{
    public class CustomMessageTarget
    {
        [JsonRpcMethod("changed")]
        public void OnTextDocumentChanged(JToken arg)
        {
            Debug.WriteLine(arg);
        }

        [JsonRpcMethod("open")]
        public void TextDocumentDidChange(JToken arg)
        {
            Debug.WriteLine(arg);
        }
    }

    [ContentType("foo")]
    [Export(typeof(ILanguageClient))]
    public class FooLanguageClient : ILanguageClient, ILanguageClientCustomMessage
    {
        internal const string UiContextGuidString = "DE885E15-D44E-40B1-A370-45372EFC23AA";

        private Guid uiContextGuid = new Guid(UiContextGuidString);

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public FooLanguageClient()
        {
            Instance = this;
            CustomMessageTarget = new CustomMessageTarget();
        }

        internal static FooLanguageClient Instance
        {
            get;
            set;
        }

        internal JsonRpc Rpc
        {
            get;
            set;
        }

        public string Name => "Foo Language Extension";

        public IEnumerable<string> ConfigurationSections
        {
            get
            {
                yield return "foo";
            }
        }

        public object InitializationOptions => null;

        public IEnumerable<string> FilesToWatch => null;

        public object MiddleLayer => null;

        public object CustomMessageTarget { get; }

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            await System.Threading.Tasks.Task.Yield();
            Connection connection = null;

            var assembly = Assembly.GetAssembly(typeof(FooLanguageClient));
            var fileName = Path.Combine(Path.GetDirectoryName(assembly.Location), @"server.exe");
            var arguments = @"--stdio --nolazy";

            var process = new System.Diagnostics.Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                }
            };

            if (process.Start())
            {
                connection = new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
            }

            return connection;
        }

        public async System.Threading.Tasks.Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            this.Rpc = rpc;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Sets the UI context so the custom command will be available.
            var monitorSelection = ServiceProvider.GlobalProvider.GetService(typeof(IVsMonitorSelection)) as IVsMonitorSelection;
            if (monitorSelection != null)
            {
                if (monitorSelection.GetCmdUIContextCookie(ref this.uiContextGuid, out uint cookie) == VSConstants.S_OK)
                {
                    monitorSelection.SetCmdUIContext(cookie, 1);
                }
            }
        }

        public async System.Threading.Tasks.Task OnLoadedAsync()
        {
            await StartAsync?.InvokeAsync(this, EventArgs.Empty);
        }

        public System.Threading.Tasks.Task OnServerInitializedAsync()
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task OnServerInitializeFailedAsync(Exception e)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
