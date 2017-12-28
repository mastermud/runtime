using System;
using System.Collections.Generic;
using System.Text;

namespace MasterMUD
{
    public sealed partial class App
    {

        public interface IPlugin
        {
            bool Active { get; }
            string Name { get; }
            void Start();
            void Stop();
        }

        public abstract class Plugin : IPlugin
        {
            public virtual bool Active { get; protected set; }

            public abstract string Name { get; }

            public virtual async void Start()
            {
                if (true == this.Active)
                {
                    App.Log($"{this.Name} already started.");
                    return;
                }

                this.Active = true;

                await System.Threading.Tasks.Task.Yield();
            }

            public virtual async void Stop()
            {
                if (false == this.Active)
                {
                    App.Log($"{this.Name} already stopped.");
                    return;
                }

                await System.Threading.Tasks.Task.Yield();

                this.Active = false;
            }
        }
    }
}