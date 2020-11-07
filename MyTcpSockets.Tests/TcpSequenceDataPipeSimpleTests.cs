using MyTcpSockets.Extensions;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class TcpSequenceDataPipeSimpleTests
    {

        [SetUp]
        public void Init()
        {
            TestUtils.PrepareTest();
        }
        
        
        [Test]
        public void TestSingleCalculation()
        {
            var seqDataPipe = new TcpDataPipeSequence();

            var initArray = new byte[] {1, 2, 3};
            seqDataPipe.PushData( new TcpDataPiece(initArray));

            Assert.AreEqual(0, seqDataPipe.UncommittedSize);
            Assert.AreEqual(0, seqDataPipe.ListIndex);
            Assert.AreEqual(0, seqDataPipe.ItemIndex);
            
           var (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

           Assert.IsTrue(hasNextElement);
           Assert.AreEqual(nextElement, 1);
           Assert.AreEqual(1, seqDataPipe.UncommittedSize);
           Assert.AreEqual(0, seqDataPipe.ListIndex);
           Assert.AreEqual(1, seqDataPipe.ItemIndex);
           
           (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

           Assert.IsTrue(hasNextElement);
           Assert.AreEqual(nextElement, 2);
           Assert.AreEqual(2, seqDataPipe.UncommittedSize);
           Assert.AreEqual(0, seqDataPipe.ListIndex);
           Assert.AreEqual(2, seqDataPipe.ItemIndex);
           
           (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

           Assert.IsTrue(hasNextElement);
           Assert.AreEqual(nextElement, 3);
           Assert.AreEqual(3, seqDataPipe.UncommittedSize);
           Assert.AreEqual(1, seqDataPipe.ListIndex);
           Assert.AreEqual(0, seqDataPipe.ItemIndex);
           
           (hasNextElement, _) = seqDataPipe.GetNextElement();

           Assert.IsFalse(hasNextElement);
           Assert.AreEqual(3, seqDataPipe.UncommittedSize);
           Assert.AreEqual(1, seqDataPipe.ListIndex);
           Assert.AreEqual(0, seqDataPipe.ItemIndex);

           var result = seqDataPipe.GetUncommittedSequence();
           
           result.ArraysAreEqual(initArray);
           
           Assert.AreEqual(0, seqDataPipe.UncommittedSize);
           Assert.AreEqual(0, seqDataPipe.ListIndex);
           Assert.AreEqual(0, seqDataPipe.ItemIndex);

        }
        
       [Test]
        public void TestMultiSequence()
        {
            var seqDataPipe = new TcpDataPipeSequence();

            var initArray = new byte[] {1, 2};
            seqDataPipe.PushData( new TcpDataPiece(initArray));
            
            var initArray2 = new byte[] {3, 4};
            seqDataPipe.PushData( new TcpDataPiece(initArray2));


            Assert.AreEqual(0, seqDataPipe.UncommittedSize);
            Assert.AreEqual(0, seqDataPipe.ListIndex);
            Assert.AreEqual(0, seqDataPipe.ItemIndex);
            
           var (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

           Assert.IsTrue(hasNextElement);
           Assert.AreEqual(nextElement, 1);
           Assert.AreEqual(1, seqDataPipe.UncommittedSize);
           Assert.AreEqual(0, seqDataPipe.ListIndex);
           Assert.AreEqual(1, seqDataPipe.ItemIndex);
           
           (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

           Assert.IsTrue(hasNextElement);
           Assert.AreEqual(nextElement, 2);
           Assert.AreEqual(2, seqDataPipe.UncommittedSize);
           Assert.AreEqual(1, seqDataPipe.ListIndex);
           Assert.AreEqual(0, seqDataPipe.ItemIndex);
           
           (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

           Assert.IsTrue(hasNextElement);
           Assert.AreEqual(nextElement, 3);
           Assert.AreEqual(3, seqDataPipe.UncommittedSize);
           Assert.AreEqual(1, seqDataPipe.ListIndex);
           Assert.AreEqual(1, seqDataPipe.ItemIndex);
           
           (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

           Assert.IsTrue(hasNextElement);
           Assert.AreEqual(nextElement, 4);
           Assert.AreEqual(4, seqDataPipe.UncommittedSize);
           Assert.AreEqual(2, seqDataPipe.ListIndex);
           Assert.AreEqual(0, seqDataPipe.ItemIndex);

           var result = seqDataPipe.GetUncommittedSequence();
           
           result.ArraysAreEqual(new byte[]{1,2,3,4});
           
           Assert.AreEqual(0, seqDataPipe.UncommittedSize);
           Assert.AreEqual(0, seqDataPipe.ListIndex);
           Assert.AreEqual(0, seqDataPipe.ItemIndex);

        }


        [Test]
        public void TestMultiSequenceAndNotComplete()
        {
            var seqDataPipe = new TcpDataPipeSequence();

            var initArray = new byte[] {1, 2};
            seqDataPipe.PushData(new TcpDataPiece(initArray));

            var initArray2 = new byte[] {3, 4};
            seqDataPipe.PushData(new TcpDataPiece(initArray2));


            Assert.AreEqual(0, seqDataPipe.UncommittedSize);
            Assert.AreEqual(0, seqDataPipe.ListIndex);
            Assert.AreEqual(0, seqDataPipe.ItemIndex);

            var (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

            Assert.IsTrue(hasNextElement);
            Assert.AreEqual(nextElement, 1);
            Assert.AreEqual(1, seqDataPipe.UncommittedSize);
            Assert.AreEqual(0, seqDataPipe.ListIndex);
            Assert.AreEqual(1, seqDataPipe.ItemIndex);

            (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

            Assert.IsTrue(hasNextElement);
            Assert.AreEqual(nextElement, 2);
            Assert.AreEqual(2, seqDataPipe.UncommittedSize);
            Assert.AreEqual(1, seqDataPipe.ListIndex);
            Assert.AreEqual(0, seqDataPipe.ItemIndex);

            (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

            Assert.IsTrue(hasNextElement);
            Assert.AreEqual(nextElement, 3);
            Assert.AreEqual(3, seqDataPipe.UncommittedSize);
            Assert.AreEqual(1, seqDataPipe.ListIndex);
            Assert.AreEqual(1, seqDataPipe.ItemIndex);

            var result = seqDataPipe.GetUncommittedSequence();

            result.ArraysAreEqual(new byte[] {1, 2, 3});

            Assert.AreEqual(0, seqDataPipe.UncommittedSize);
            Assert.AreEqual(0, seqDataPipe.ListIndex);
            Assert.AreEqual(1, seqDataPipe.ItemIndex);


            (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

            Assert.IsTrue(hasNextElement);
            Assert.AreEqual(nextElement, 4);
            Assert.AreEqual(1, seqDataPipe.UncommittedSize);
            Assert.AreEqual(1, seqDataPipe.ListIndex);
            Assert.AreEqual(0, seqDataPipe.ItemIndex);

            result = seqDataPipe.GetUncommittedSequence();

            result.ArraysAreEqual(new byte[] {4});

            Assert.AreEqual(0, seqDataPipe.UncommittedSize);
            Assert.AreEqual(0, seqDataPipe.ListIndex);
            Assert.AreEqual(0, seqDataPipe.ItemIndex);

        }

        [Test]
        public void TestThreeSequenceAndNotComplete()
        {
            var seqDataPipe = new TcpDataPipeSequence();


            seqDataPipe.PushData(new TcpDataPiece(new byte[] {1, 2}));


            seqDataPipe.PushData(new TcpDataPiece(new byte[] {3, 4}));
            seqDataPipe.PushData(new TcpDataPiece(new byte[] {5, 6}));


            Assert.AreEqual(0, seqDataPipe.UncommittedSize);
            Assert.AreEqual(0, seqDataPipe.ListIndex);
            Assert.AreEqual(0, seqDataPipe.ItemIndex);

            var (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

            Assert.IsTrue(hasNextElement);
            Assert.AreEqual(nextElement, 1);
            Assert.AreEqual(1, seqDataPipe.UncommittedSize);
            Assert.AreEqual(0, seqDataPipe.ListIndex);
            Assert.AreEqual(1, seqDataPipe.ItemIndex);

            (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

            Assert.IsTrue(hasNextElement);
            Assert.AreEqual(nextElement, 2);
            Assert.AreEqual(2, seqDataPipe.UncommittedSize);
            Assert.AreEqual(1, seqDataPipe.ListIndex);
            Assert.AreEqual(0, seqDataPipe.ItemIndex);

            (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

            Assert.IsTrue(hasNextElement);
            Assert.AreEqual(nextElement, 3);

            (hasNextElement, nextElement) = seqDataPipe.GetNextElement();
            Assert.IsTrue(hasNextElement);
            Assert.AreEqual(nextElement, 4);

            (hasNextElement, nextElement) = seqDataPipe.GetNextElement();
            Assert.IsTrue(hasNextElement);
            Assert.AreEqual(nextElement, 5);


            var result = seqDataPipe.GetUncommittedSequence();

            result.ArraysAreEqual(new byte[] {1, 2, 3, 4, 5});

            Assert.AreEqual(0, seqDataPipe.UncommittedSize);
            Assert.AreEqual(0, seqDataPipe.ListIndex);
            Assert.AreEqual(1, seqDataPipe.ItemIndex);


            (hasNextElement, nextElement) = seqDataPipe.GetNextElement();

            Assert.IsTrue(hasNextElement);
            Assert.AreEqual(nextElement, 6);
            Assert.AreEqual(1, seqDataPipe.UncommittedSize);
            Assert.AreEqual(1, seqDataPipe.ListIndex);
            Assert.AreEqual(0, seqDataPipe.ItemIndex);

            result = seqDataPipe.GetUncommittedSequence();

            result.ArraysAreEqual(new byte[] {6});

            Assert.AreEqual(0, seqDataPipe.UncommittedSize);
            Assert.AreEqual(0, seqDataPipe.ListIndex);
            Assert.AreEqual(0, seqDataPipe.ItemIndex);
        }

    }
}