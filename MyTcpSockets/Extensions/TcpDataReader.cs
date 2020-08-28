using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{

    public class TcpDataReader
    {
        private ITcpDataPipe _tcpDataPipe;
        
        private List<TcpDataPiece> _incomingPackages = new List<TcpDataPiece>();

        public void NewPackage(byte[] data)
        {
            var newPackage = new TcpDataPiece(data, data.Length);
            if (_tcpDataPipe == null)
                _incomingPackages.Add(newPackage);
            else
                _tcpDataPipe.PushData(newPackage);
        }

        public void NewPackage(byte[] data, int len)
        {
            var newPackage = new TcpDataPiece(data, len);
            if (_tcpDataPipe == null)
                _incomingPackages.Add(newPackage);
            else
                _tcpDataPipe.PushData(newPackage);
        }


        private void InitDataPipe(ITcpDataPipe dataPipe)
        {

            _tcpDataPipe = dataPipe;
  
            foreach (var incomingPackage in _incomingPackages)
                dataPipe.PushData(incomingPackage);

            _incomingPackages = null;
        }
        

        public ValueTask<ReadOnlyMemory<byte>> ReadAsyncAsync(int size, CancellationToken token)
        {

            if (_tcpDataPipe == null)
                InitDataPipe(new TcpDataPipeBySize());

            if (_tcpDataPipe is TcpDataPipeBySize readerBySizes)
                return readerBySizes.ReadAsyncAsync(size, token);
            
            throw new Exception($"Reader is already in {_tcpDataPipe.GetType()} mode");
 
        }

        public ValueTask<ReadOnlyMemory<byte>> ReadWhileWeGetSequenceAsync(byte[] sequence, CancellationToken token)
        {
            if (_tcpDataPipe == null)
                InitDataPipe(new TcpDataPipeSequence());
            
            if (_tcpDataPipe is TcpDataPipeSequence sequenceReader)
                return sequenceReader.ReadWhileWeGetSequenceAsync(sequence, token);
            
            throw new Exception($"Reader is already in {_tcpDataPipe.GetType()} mode");

        }

        public async Task StopAsync()
        {

            if (_tcpDataPipe != null)
                await _tcpDataPipe.StopAsync();
        }
    }
}