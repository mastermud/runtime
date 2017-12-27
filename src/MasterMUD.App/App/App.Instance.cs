using System;
using System.Linq;
using MasterMUD.Interfaces;

namespace MasterMUD
{
    public sealed partial class App
    {
        public System.DateTime Activated { get; } = System.DateTime.Now;

        private readonly System.IDisposable ActivationSubscription;

        /// <summary>
        ///     Ensures only one running instance is allowed.
        /// </summary>
        private readonly System.Threading.Mutex Mutex;

        /// <summary>
        ///     Keeps the application alive forever.
        /// </summary>
        private readonly System.Threading.EventWaitHandle EventWaitHandle;

        internal System.Collections.Concurrent.ConcurrentDictionary<string, MasterMUD.Interfaces.IFeature> Features { get; }

        private App()
        {
            this.EventWaitHandle = new System.Threading.EventWaitHandle(initialState: false, mode: System.Threading.EventResetMode.ManualReset);
            this.Features = new System.Collections.Concurrent.ConcurrentDictionary<string, MasterMUD.Interfaces.IFeature>(System.StringComparer.OrdinalIgnoreCase);
            this.Mutex = new System.Threading.Mutex(initiallyOwned: true, name: nameof(MasterMUD), createdNew: out var createdNew);

            if (!createdNew)
                throw new System.InvalidProgramException(Properties.Resources.StaticConstructorDuplicated);

            var args = System.Environment.GetCommandLineArgs();
            var fi = new System.IO.FileInfo(args[0]);
            var dir = new System.IO.DirectoryInfo(fi.DirectoryName);
            var pluginsPath = System.IO.Path.Combine(dir.FullName, "Plugins");
            var pluginsDir = System.IO.Directory.Exists(pluginsPath) ? new System.IO.DirectoryInfo(pluginsPath) : System.IO.Directory.CreateDirectory(pluginsPath);
            var dlls = pluginsDir.GetFiles("*.dll", System.IO.SearchOption.AllDirectories);

            foreach (var dll in dlls)
                foreach (var feature in System.Reflection.Assembly.LoadFrom(dll.FullName).GetTypes().Where(type => false == type.IsInterface && type.GetInterface(nameof(IFeature)) != null).Select(type => (IFeature)System.Activator.CreateInstance(type)).Where(feature => true == this.Features.TryAdd(feature.Name, feature)))
                    App.Log($"Feature '{feature.Name}' added.");

            this.ActivationSubscription = System.Reactive.Linq.Observable.Interval(System.TimeSpan.FromSeconds(6)).Subscribe(t => App.Log($"Tick-{t + 1}"));

            if (System.Environment.UserInteractive)
            {
                // TODO: Environment configuration and initialization.
                System.Console.Title = Properties.Resources.Title;
                System.Console.Clear();
                System.Console.CursorVisible = false;
                System.Console.TreatControlCAsInput = false;
            }
        }

        private void Terminate()
        {
            try
            {
                this.EventWaitHandle.Set();
            }
            finally
            {
                try
                {
                    this.EventWaitHandle.Reset();
                }
                finally
                {
                    this.ActivationSubscription.Dispose();
                }
            }
        }
    }
}