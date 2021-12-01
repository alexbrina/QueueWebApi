using System;
using System.Runtime.Serialization;

namespace Resilient.Domain.Exceptions
{
    [Serializable]
    public class WorkCompletedException : Exception
    {
        private const string message = "Work already completed.";

        public WorkCompletedException()
            : base(message)
        {
        }

        protected WorkCompletedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
