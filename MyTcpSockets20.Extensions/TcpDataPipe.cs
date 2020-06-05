using System;
using System.Collections.Generic;

namespace MyTcpSockets.Extensions
{

    public class TcpDataPipe
    {
        private readonly List<(byte[] data, int len)> _incomingPackages = new List<(byte[] data, int len)>();

        public int CurrentPackageIndex { get; private set; } = -1;

        public int MemoryChunkCount => _incomingPackages.Count;

        public int TotalSize { get; private set; }

        public int StartPosition { get; private set; }
        public int EndPositionAtPackageIndex { get; private set; } = -1;

        public int UnCommittedPosition { get; private set; } = -1;

        public int UnCommittedAmount => UnCommittedPosition - StartPosition + 1;

        public int RemainsSize => TotalSize - UnCommittedPosition - 1;


        public void Add(byte[] data, int len)
        {

            _incomingPackages.Add((data, len));
            TotalSize += len;
            if (CurrentPackageIndex < 0)
                CurrentPackageIndex = 0;
        }

        public void Add(byte[] data)
        {
            Add(data, data.Length);
        }

        public bool EndOfData => RemainsSize <= 0;

        public bool AdvancePosition()
        {
            if (EndOfData)
                return false;

            UnCommittedPosition++;
            EndPositionAtPackageIndex++;

            if (EndPositionAtPackageIndex == _incomingPackages[CurrentPackageIndex].len)
            {
                EndPositionAtPackageIndex = 0;
                CurrentPackageIndex++;
            }

            return true;
        }

        public void AdvancePosition(int amountTimes)
        {
            if (UnCommittedPosition + amountTimes > TotalSize)
                throw new Exception("Advancing the position will make it beyond the data");


            UnCommittedPosition += amountTimes;
            EndPositionAtPackageIndex += amountTimes;

            while (EndPositionAtPackageIndex >= _incomingPackages[CurrentPackageIndex].len)
            {
                EndPositionAtPackageIndex -= _incomingPackages[CurrentPackageIndex].len;
                CurrentPackageIndex++;
            }

        }

        private void RemoveFirstElement()
        {
            var itm = _incomingPackages[0];

            _incomingPackages.RemoveAt(0);

            TotalSize -= itm.len;
            StartPosition -= itm.len;
            UnCommittedPosition -= itm.len;
        }

        public byte CurrentElement => UnCommittedPosition >= StartPosition
            ? _incomingPackages[CurrentPackageIndex].data[EndPositionAtPackageIndex]
            : throw new Exception("No Uncommitted Bytes");

        private byte[] GetByteArray(int size)
        {
            return new byte[size];
        }

        private byte[] CompileMultiPieces(int size)
        {
            var result = GetByteArray(size);

            var remainingSlice = _incomingPackages[0]
                .data.AsMemory(StartPosition, _incomingPackages[0].len - StartPosition);

            remainingSlice.CopyTo(result);
            var position = remainingSlice.Length;
            var remainingSize = size - remainingSlice.Length;


            var index = 1;


            while (remainingSize > 0)
            {

                var remainingResult = result.AsMemory(position, result.Length - position);

                if (remainingSize <= _incomingPackages[index].len)
                {
                    _incomingPackages[index].data.AsMemory(0, remainingSize).CopyTo(remainingResult);
                    break;
                }

                _incomingPackages[index].data.CopyTo(remainingResult);

                position += _incomingPackages[index].len;
                remainingSize -= _incomingPackages[index].len;
                index++;
            }

            return result;
        }

        private void Gc()
        {
            StartPosition = UnCommittedPosition + 1;

            if (_incomingPackages.Count == 0)
                return;

            while (CurrentPackageIndex > 0)
            {
                RemoveFirstElement();
                CurrentPackageIndex--;
            }

            if (StartPosition == _incomingPackages[0].len)
            {
                RemoveFirstElement();
                EndPositionAtPackageIndex = -1;
            }
        }



        public byte[] GetUncommittedAmountAndCommit()
        {
            try
            {
                return CurrentPackageIndex == 0
                    ? _incomingPackages[0].data.AsMemory(StartPosition, UnCommittedAmount).ToArray()
                    : CompileMultiPieces(UnCommittedAmount);
            }
            finally
            {
                Gc();
            }
        }

        public override string ToString()
        {
            if (UnCommittedAmount <= 0)
                return $"[0].{StartPosition} -> [{CurrentPackageIndex}].{EndPositionAtPackageIndex}";

            return
                $"[0].{StartPosition}={_incomingPackages[0].data[StartPosition]} -> [{CurrentPackageIndex}].{EndPositionAtPackageIndex}={_incomingPackages[CurrentPackageIndex].data[EndPositionAtPackageIndex]}";
        }
    }
}