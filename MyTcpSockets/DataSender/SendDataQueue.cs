using System;
using System.Collections.Generic;

namespace MyTcpSockets.DataSender
{
    public class SendDataQueue
    {
        private readonly object _lockObject = new object();
        private readonly List<ReadOnlyMemory<byte>> _queue = new List<ReadOnlyMemory<byte>>();
        
        public int Length { get; private set; }
        public void Enqueue(ReadOnlyMemory<byte> data)
        {
            lock (_lockObject)
            {
                _queue.Add(data);
                Length += data.Length;
            }
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                _queue.Clear();
                Length = 0;
            }
        }

        public ReadOnlyMemory<byte> Dequeue(byte[] sharedBuffer)
        {
            lock (_lockObject)
            {
                if (_queue.Count == 0)
                    return Array.Empty<byte>();

                if (_queue.Count == 1 && sharedBuffer.Length > _queue[0].Length)
                {
                    var result = _queue[0];
                    Length = 0;
                    _queue.Clear();
                    return result;
                }


                var remainsLength = sharedBuffer.Length > Length ? Length : sharedBuffer.Length;
                var index = 0;

                while (remainsLength>0)
                {

                    if (_queue[0].Length <= remainsLength)
                    {
                        var fullChunk = _queue[0];
                        _queue.RemoveAt(0);
                        fullChunk.CopyTo(sharedBuffer.AsMemory(index, remainsLength));

                        Length -= fullChunk.Length;
                        index += fullChunk.Length;
                        remainsLength -= fullChunk.Length;
                        continue;
                    }

                    var chunk = _queue[0];;
                
                    chunk.Slice(0, remainsLength).CopyTo(sharedBuffer.AsMemory(index, remainsLength));
                    _queue[0] = chunk.Slice(remainsLength, chunk.Length - remainsLength);
                    Length -= remainsLength;
                    index += remainsLength;
                    remainsLength =0;
                }
            
                return new ReadOnlyMemory<byte>(sharedBuffer, 0, index);
            }

        }
        
    }
}