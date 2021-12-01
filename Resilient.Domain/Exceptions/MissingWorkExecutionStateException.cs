using System;
using System.Runtime.Serialization;

namespace Resilient.Domain.Exceptions
{
    [Serializable]
    public class MissingWorkExecutionStateException : Exception
    {
        private const string message = "Work was scheduled but we are " +
            "missing its execution state.";

        public MissingWorkExecutionStateException()
            : base(message)
        {
        }

        protected MissingWorkExecutionStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
