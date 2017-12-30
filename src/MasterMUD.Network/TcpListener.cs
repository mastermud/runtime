using System;
using System.Collections.Generic;
using System.Text;

namespace MasterMUD.Network
{
    public sealed class TcpListener : MasterMUD.App.Plugin
    {
        public override string Name { get; } = nameof(TcpListener);

        public TcpListener()
        {

        }

        public override void Start()
        {
            if (false == base.Active)
            {
                base.Start();

                if (true == base.Active)
                    App.Log($"{this.Name} activated.");
            }
        }

        public override void Stop()
        {
            if (true == base.Active)
            {
                base.Stop();

                if (false == base.Active)
                    App.Log($"{this.Name} de-activated.");
            }
        }
    }
}