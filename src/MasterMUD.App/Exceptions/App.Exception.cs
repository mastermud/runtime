namespace MasterMUD
{
    public sealed partial class App
    {
        internal sealed class Exception : System.Exception
        {
            protected internal Exception() : base()
            {

            }

            protected internal Exception(string message) : base(message)
            {

            }

            protected internal Exception(string message, System.Exception innerException) : base(message, innerException)
            {

            }
        }
    }
}
