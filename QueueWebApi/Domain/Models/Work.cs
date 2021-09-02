using System;

namespace QueueWebApi.Domain.Models
{
    internal class Work
    {
        public string Id { get; private set; }
        public string Status { get; private set; }
        public string Data { get; set; }
        public DateTimeOffset RequestedAt { get; private set; }
        public DateTimeOffset CompletedAt { get; private set; }

        public Work()
        {
            Id = Nanoid.Nanoid.Generate();
            Status = "requested";
            RequestedAt = DateTimeOffset.Now;
        }

        public Work(string id, string status, string data)
            : this()
        {
            Id = id;
            Status = status;
            Data = data;
        }

        public void SetCompleted()
        {
            Status = "completed";
            CompletedAt = DateTimeOffset.Now;
        }
    }
}
