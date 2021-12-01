using System;

namespace Resilient.Domain.Models
{
    internal class Work
    {
        public string Id { get; private set; }
        public string Data { get; set; }
        public DateTimeOffset RequestedAt { get; private set; }
        public DateTimeOffset CompletedAt { get; private set; }

        public Work()
        {
            Id = Identity.Generate();
            RequestedAt = DateTimeOffset.Now;
        }

        public Work(string id, string data)
            : this()
        {
            Id = id;
            Data = data;
        }

        public void SetCompleted()
        {
            CompletedAt = DateTimeOffset.Now;
        }
    }
}
