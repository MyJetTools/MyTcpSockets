using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTcpSockets.DataSender
{
    public class ProducerConsumer<T>
    {
        private readonly Queue<TaskCompletionSource<T>> _items = new Queue<TaskCompletionSource<T>>();

        private TaskCompletionSource<T> _lastItem;

        public ProducerConsumer()
        {
            _lastItem = new TaskCompletionSource<T>();
            _items.Enqueue(_lastItem);
        }
        
        public void Produce(T itm)
        {
            lock (_items)
            {
                if (_lastItem == null)
                    return;
                
                var newItem = new TaskCompletionSource<T>();
                _items.Enqueue(newItem);
                _lastItem.SetResult(itm);
                _lastItem = newItem;
            }
        }

        public void Stop()
        {
            lock (_items)
            {
                if (_lastItem == null)
                    return;
                
                _lastItem.SetException(new Exception("Stop Producer Consumer with Type: "+typeof(T)));
                _lastItem = null;
            }
        }

        private TaskCompletionSource<T> GetNextItem()
        {
            lock (_items)
            {
                return _items.Dequeue();
            }
                
        }


        public int Count 
        {
            get
            {
                lock (_items)
                    return _items.Count - 1;
            }
        }
        
        public async Task<T> ConsumeAsync()
        {
            var nextItem = GetNextItem();
            var result = await nextItem.Task;
            return result;
        }
    }
}