using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class TcpDataReaderTestHugeAmounts
    {
        [Test]
        public async Task TestReadyAmountWayBiggerThenAmountOfBuffer()
        {
            var dataReader = new TcpDataReader(64);

            var list = new List<byte>();
            for (byte i = 0; i < 255; i++)
                list.Add(i);

            var src = list.ToArray();
            var writeTask = dataReader.NewPackageAsync(src);

            
            var result = await dataReader.ReadAsyncAsync(255, new CancellationTokenSource().Token);
            src.ArraysAreEqual(result.AsArray());
            dataReader.CommitReadData(result);

            await writeTask;

        } 
    }
}