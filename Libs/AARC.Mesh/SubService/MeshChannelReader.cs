using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.SubService
{
    public static class MeshChannelReader
    {
        public static Task ReadTask(ChannelReader<byte[]> reader, Action<byte[]> OnPublish, ILogger logger, CancellationToken token)
        {
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var bytes = await reader.ReadAsync(token);
                        OnPublish(bytes);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "ChannelReader ERROR");
                }
                finally
                {
                    logger.LogInformation($"ChannelReader Exited CancellationRequested:{token.IsCancellationRequested}");
                }
            });
        }
    }
}
