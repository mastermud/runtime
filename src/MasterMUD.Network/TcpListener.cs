using System;
using System.Collections.Generic;
using System.Text;

namespace MasterMUD.Network
{
    public sealed class TcpListener : MasterMUD.Interfaces.IFeature
    {
        public bool Active { get; private set; }

        public string Name { get; } = "MasterMUD Listener";

        public TcpListener()
        {

        }

        public void Start()
        {
            if (true == this.Active)
                return;

            this.Active = true;
            App.Log($"{Name} activated.");
        }

        public void Stop()
        {
            if (false == this.Active)
                return;

            this.Active = false;
            App.Log($"{Name} de-activated.");
        }
    }
}
