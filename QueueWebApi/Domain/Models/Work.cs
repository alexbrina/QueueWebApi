using System;

namespace QueueWebApi.Domain.Models
{
    internal class Work
    {
        public long Id { get; private set; }
        public string Status { get; private set; }
        public string Data { get; set; }
        public DateTimeOffset RequestedAt { get; private set; }
        public DateTimeOffset CompletedAt { get; private set; }

        public Work()
        {
            Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Status = "requested";
            RequestedAt = DateTimeOffset.Now;
        }

        public Work(long id, string status, string data)
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
