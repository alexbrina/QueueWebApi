using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Resilient.Application
{
    internal static class ChannelExtensions
    {
        /// <summary>
        /// Try emptying this channel by reading all items from it.
        /// </summary>
        /// <remarks>
        /// One thing to note about this approach is that there will be N or
        /// more items left behind, where N is the number of concurrent consumers
        /// reading from this channel. It happens because these consumers will
        /// capture at least one item each before the cleaner consumer consumes
        /// them all.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">Target channel</param>
        /// <param name="cancelationToken">Cancelation token</param>
        /// <returns>The number of items cleaned</returns>
        public static async Task<int> Clear<T>(this Channel<T> channel,
            CancellationToken cancelationToken)
        {
            if (channel.Reader.Count == 0)
            {
                return 0;
            }

            var count = 0;
            while (await channel.Reader.WaitToReadAsync(cancelationToken))
            {
                channel.Reader.TryRead(out T _);

                count++;

                if (channel.Reader.Count == 0)
                {
                    break;
                }
            }

            return count;
        }
    }
}
