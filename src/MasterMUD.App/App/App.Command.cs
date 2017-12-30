using System;
using System.Linq;

namespace MasterMUD
{
    public sealed partial class App
    {
        public abstract class Command
        {
            public abstract string Name { get; }

            protected Command()
            {

            }
        }
    }
}