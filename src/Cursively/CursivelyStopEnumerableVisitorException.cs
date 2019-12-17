using System;
using System.Diagnostics.CodeAnalysis;

namespace Cursively
{
    [Serializable]
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors")]
    [SuppressMessage("Design", "CA1064:Exceptions should be public")]
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
    internal sealed class CursivelyStopEnumerableVisitorException : Exception
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
    {
        public CursivelyStopEnumerableVisitorException()
            : base("The enumerable is about to throw an exception on its own thread, so there's no need for this thread to continue.")
        {
        }
    }
}
