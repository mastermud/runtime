using System;
using System.Collections.Generic;
using System.Text;

namespace MasterMUD
{
    public sealed partial class App
    {
        public interface IModule
        {
            string Name { get; }
        }

        public abstract class Module : IModule
        {
            public abstract string Name { get; }
        }
    }
}