using System;
using System.Collections.Generic;
using System.Text;

namespace MasterMUD.Interfaces
{
    public interface IFeature
    {
        bool Active { get; }
        string Name { get; }
        void Start();
        void Stop();
    }
}
