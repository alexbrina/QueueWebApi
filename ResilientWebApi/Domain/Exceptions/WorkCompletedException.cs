using System;
using System.Runtime.Serialization;

namespace ResilientWebApi.Domain.Exceptions
{
    [Serializable]
    public class WorkCompletedException : Exception
    {
        private const string message = "Work already completed.";

        public WorkCompletedException(Exception innerException)
            : base(message, innerException)
        {
        }

        protected WorkCompletedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
