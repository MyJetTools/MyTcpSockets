using System.Linq;
using NUnit.Framework;

namespace MyTcpSockets.Extensions.Tests
{
    public class TcpDataPipeTests
    {
        [Test]
        public void TestSizesCalculation()
        {
            var myTcpPipe = new TcpDataPipe();
            
            myTcpPipe.Add(new byte[]{1,2,3,4,5});

            Assert.AreEqual(5, myTcpPipe.TotalSize);
            Assert.AreEqual(5, myTcpPipe.RemainsSize);
            Assert.AreEqual(0, myTcpPipe.UnCommittedAmount);

            Assert.AreEqual(0, myTcpPipe.StartPosition);
            Assert.AreEqual(-1, myTcpPipe.UnCommittedPosition);
            
            
            myTcpPipe.AdvancePosition();
            
            Assert.AreEqual(5, myTcpPipe.TotalSize);
            Assert.AreEqual(4, myTcpPipe.RemainsSize);
            Assert.AreEqual(1, myTcpPipe.UnCommittedAmount);
            
            Assert.AreEqual(0, myTcpPipe.StartPosition);
            Assert.AreEqual(0, myTcpPipe.UnCommittedPosition);
            
            Assert.AreEqual(1, myTcpPipe.CurrentElement);
            
            myTcpPipe.AdvancePosition();
            
            Assert.AreEqual(5, myTcpPipe.TotalSize);
            Assert.AreEqual(3, myTcpPipe.RemainsSize);
            Assert.AreEqual(2, myTcpPipe.UnCommittedAmount);

            Assert.AreEqual(0, myTcpPipe.StartPosition);
            Assert.AreEqual(1, myTcpPipe.UnCommittedPosition);
            
            Assert.AreEqual(2, myTcpPipe.CurrentElement);

            var dataFromEnumerate = myTcpPipe.GetUncommittedAmountAndCommit();
            
            new byte[]{1,2}.AsReadOnlyMemory().ArraysAreEqual(dataFromEnumerate);

        }

        [Test]
        public void AdvancePositionsSeveralTimesAndStayAtTheSameIndex()
        {
            var myTcpPipe = new TcpDataPipe();
            
            myTcpPipe.Add(new byte[]{1,2,3,4,5});
            
            myTcpPipe.AdvancePosition(3);
            
            Assert.AreEqual(5, myTcpPipe.TotalSize);
            Assert.AreEqual(2, myTcpPipe.RemainsSize);
            Assert.AreEqual(3, myTcpPipe.UnCommittedAmount);
            
            Assert.AreEqual(0, myTcpPipe.StartPosition);
            Assert.AreEqual(2, myTcpPipe.UnCommittedPosition);
            
            Assert.AreEqual(3, myTcpPipe.CurrentElement);
            
            Assert.IsFalse(myTcpPipe.EndOfData);

            var data1 = myTcpPipe.GetUncommittedAmountAndCommit();
            new byte[]{1,2,3}.AsReadOnlyMemory().ArraysAreEqual(data1);
            
            myTcpPipe.AdvancePosition(2);
            
            var data2 = myTcpPipe.GetUncommittedAmountAndCommit();
            new byte[]{4,5}.AsReadOnlyMemory().ArraysAreEqual(data2);
            
            Assert.AreEqual(0, myTcpPipe.TotalSize);
            Assert.AreEqual(0, myTcpPipe.MemoryChunkCount);
            Assert.IsTrue(myTcpPipe.EndOfData);

        }
        
        [Test]
        public void TestMultipleChunksAndAdvancingOneChunkByOne()
        {
            var myTcpPipe = new TcpDataPipe();
            
            myTcpPipe.Add(new byte[]{1,2,3,4,5});
            myTcpPipe.Add(new byte[]{6,7,8,9,10});
            myTcpPipe.Add(new byte[]{11,12,13,14,15});

            
            myTcpPipe.AdvancePosition(3);
            
            var data = myTcpPipe.GetUncommittedAmountAndCommit();
            new byte[]{1,2,3}.AsReadOnlyMemory().ArraysAreEqual(data);
            Assert.AreEqual(15, myTcpPipe.TotalSize);

            myTcpPipe.AdvancePosition(3);
            
            data = myTcpPipe.GetUncommittedAmountAndCommit();
            new byte[]{4,5,6}.AsReadOnlyMemory().ArraysAreEqual(data);
            Assert.AreEqual(10, myTcpPipe.TotalSize);
            
            myTcpPipe.AdvancePosition(7);
            
            data = myTcpPipe.GetUncommittedAmountAndCommit();
            new byte[]{7,8,9,10,11,12,13}.AsReadOnlyMemory().ArraysAreEqual(data);
            Assert.AreEqual(5, myTcpPipe.TotalSize);
            
            
            myTcpPipe.AdvancePosition(2);
            data = myTcpPipe.GetUncommittedAmountAndCommit();
            new byte[]{14,15}.AsReadOnlyMemory().ArraysAreEqual(data);
            Assert.AreEqual(0, myTcpPipe.TotalSize);

        }        
        
        [Test]
        public void TestMultipleChunksAndAdvancingThroughChunks()
        {
            var myTcpPipe = new TcpDataPipe();
            
            myTcpPipe.Add(new byte[]{1,2,3,4,5});
            myTcpPipe.Add(new byte[]{6,7,8,9,10});
            myTcpPipe.Add(new byte[]{11,12,13,14,15});
            
            myTcpPipe.AdvancePosition(3);
            
            var data = myTcpPipe.GetUncommittedAmountAndCommit();
            new byte[]{1,2,3}.AsReadOnlyMemory().ArraysAreEqual(data);
            Assert.AreEqual(15, myTcpPipe.TotalSize);

            myTcpPipe.AdvancePosition(10);
            
            data = myTcpPipe.GetUncommittedAmountAndCommit();
            new byte[]{4,5,6,7,8,9,10,11,12,13}.AsReadOnlyMemory().ArraysAreEqual(data);
            Assert.AreEqual(5, myTcpPipe.TotalSize);
            
            
            myTcpPipe.AdvancePosition(2);
            data = myTcpPipe.GetUncommittedAmountAndCommit();
            new byte[]{14,15}.AsReadOnlyMemory().ArraysAreEqual(data);
            Assert.AreEqual(0, myTcpPipe.TotalSize);

        }

        [Test]
        public void TestAdvanceOneByOne()
        {
            var myTcpPipe = new TcpDataPipe();

            myTcpPipe.Add(new byte[] {1, 2, 3, 4, 5}, 3);
            myTcpPipe.Add(new byte[] {6, 7, 8, 9, 10}, 3);
            myTcpPipe.Add(new byte[] {11, 12, 13, 14, 15}, 3);

            myTcpPipe.AdvancePosition();
            var c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(1, c.ToArray()[0]);
            Assert.AreEqual(9, myTcpPipe.TotalSize);  

            myTcpPipe.AdvancePosition();
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(2, c.ToArray()[0]);
            Assert.AreEqual(9, myTcpPipe.TotalSize);  

            myTcpPipe.AdvancePosition();
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(3, c.ToArray()[0]);
            Assert.AreEqual(6, myTcpPipe.TotalSize);
            
            myTcpPipe.AdvancePosition();
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(6, c.ToArray()[0]);
            Assert.AreEqual(6, myTcpPipe.TotalSize);            

            myTcpPipe.AdvancePosition();
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(7, c.ToArray()[0]);
            Assert.AreEqual(6, myTcpPipe.TotalSize);            

            myTcpPipe.AdvancePosition();
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(8, c.ToArray()[0]);
            Assert.AreEqual(3, myTcpPipe.TotalSize);     
            
            myTcpPipe.AdvancePosition();
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(11, c.ToArray()[0]);
            Assert.AreEqual(3, myTcpPipe.TotalSize);    
            
            myTcpPipe.AdvancePosition();
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(12, c.ToArray()[0]);
            Assert.AreEqual(3, myTcpPipe.TotalSize);                

            myTcpPipe.AdvancePosition();
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(13, c.ToArray()[0]);
            Assert.AreEqual(0, myTcpPipe.TotalSize);     
        }
        
        [Test]
        public void TestAdvanceOneByOneThroughOtherFunctions()
        {
            var myTcpPipe = new TcpDataPipe();

            myTcpPipe.Add(new byte[] {1, 2, 3, 4, 5}, 3);
            myTcpPipe.Add(new byte[] {6, 7, 8, 9, 10}, 4);
            myTcpPipe.Add(new byte[] {11, 12, 13, 14, 15}, 3);

            myTcpPipe.AdvancePosition(1);
            var c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(1, c[0]);
            Assert.AreEqual(10, myTcpPipe.TotalSize);  

            myTcpPipe.AdvancePosition(1);
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(2, c[0]);
            Assert.AreEqual(10, myTcpPipe.TotalSize);  

            myTcpPipe.AdvancePosition(1);
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(3, c[0]);
            Assert.AreEqual(7, myTcpPipe.TotalSize);
            
            myTcpPipe.AdvancePosition(1);
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(6, c[0]);
            Assert.AreEqual(7, myTcpPipe.TotalSize);            

            myTcpPipe.AdvancePosition(1);
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(7, c[0]);
            Assert.AreEqual(7, myTcpPipe.TotalSize);            

            myTcpPipe.AdvancePosition(1);
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(8, c[0]);
            Assert.AreEqual(7, myTcpPipe.TotalSize);     
            
            myTcpPipe.AdvancePosition(1);
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(9, c[0]);
            Assert.AreEqual(3, myTcpPipe.TotalSize);   
            
            myTcpPipe.AdvancePosition(1);
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(11, c[0]);
            Assert.AreEqual(3, myTcpPipe.TotalSize);    
            
            myTcpPipe.AdvancePosition(1);
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(12, c[0]);
            Assert.AreEqual(3, myTcpPipe.TotalSize);                

            myTcpPipe.AdvancePosition(1);
            c = myTcpPipe.GetUncommittedAmountAndCommit();
            Assert.AreEqual(13, c[0]);
            Assert.AreEqual(0, myTcpPipe.TotalSize);     

        }
        
        
        [Test]
        public void TestGetEverythingByOneRequest()
        {
            var myTcpPipe = new TcpDataPipe();

            myTcpPipe.Add(new byte[] {1, 2, 3, 4, 5}, 3);
            myTcpPipe.Add(new byte[] {6, 7, 8, 9, 10}, 3);
            myTcpPipe.Add(new byte[] {11, 12, 13, 14, 15}, 3);

            myTcpPipe.AdvancePosition(9);
            var data = myTcpPipe.GetUncommittedAmountAndCommit();
            new byte[]{1,2,3,6,7,8,11,12,13}.AsReadOnlyMemory().ArraysAreEqual(data);
            Assert.AreEqual(0, myTcpPipe.TotalSize);  

        }    
        
        
        
        [Test]
        public void TestAdvancingOneByOneAndThenCommit()
        {
            var myTcpPipe = new TcpDataPipe();

            myTcpPipe.Add(new byte[] {1, 2, 3});
            myTcpPipe.Add(new byte[] {4, 5, 6});
            myTcpPipe.Add(new byte[] {7, 8, 9});

            Assert.AreEqual(0, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(-1, myTcpPipe.EndPositionAtPackageIndex);
            

            myTcpPipe.AdvancePosition();
            Assert.AreEqual(1, myTcpPipe.CurrentElement);

            Assert.AreEqual(0, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(0, myTcpPipe.EndPositionAtPackageIndex);            

            myTcpPipe.AdvancePosition();
            Assert.AreEqual(2, myTcpPipe.CurrentElement);

            Assert.AreEqual(0, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(1, myTcpPipe.EndPositionAtPackageIndex);  

            myTcpPipe.AdvancePosition();
            Assert.AreEqual(3, myTcpPipe.CurrentElement);

            Assert.AreEqual(0, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(2, myTcpPipe.EndPositionAtPackageIndex);  

            
            myTcpPipe.AdvancePosition();
            Assert.AreEqual(4, myTcpPipe.CurrentElement);

            Assert.AreEqual(1, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(0, myTcpPipe.EndPositionAtPackageIndex);  


            myTcpPipe.AdvancePosition();
            Assert.AreEqual(5, myTcpPipe.CurrentElement);

            Assert.AreEqual(1, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(1, myTcpPipe.EndPositionAtPackageIndex);              

            myTcpPipe.AdvancePosition();
            Assert.AreEqual(6, myTcpPipe.CurrentElement);

            Assert.AreEqual(1, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(2, myTcpPipe.EndPositionAtPackageIndex);              
            
            
            myTcpPipe.AdvancePosition();
            Assert.AreEqual(7, myTcpPipe.CurrentElement);

            Assert.AreEqual(2, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(0, myTcpPipe.EndPositionAtPackageIndex);              
            
            myTcpPipe.AdvancePosition();
            Assert.AreEqual(8, myTcpPipe.CurrentElement);

            Assert.AreEqual(2, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(1, myTcpPipe.EndPositionAtPackageIndex);              

            myTcpPipe.AdvancePosition();
            Assert.AreEqual(9, myTcpPipe.CurrentElement);

            Assert.AreEqual(2, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(2, myTcpPipe.EndPositionAtPackageIndex);

            var data = myTcpPipe.GetUncommittedAmountAndCommit();
            new byte[]{1,2,3,4,5,6,7,8,9}.AsReadOnlyMemory().ArraysAreEqual(data);
            
            Assert.AreEqual(0, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(-1, myTcpPipe.EndPositionAtPackageIndex);
            Assert.AreEqual(0, myTcpPipe.StartPosition);
        }
        
        
        [Test]
        public void TestAdvancingOneByOneAndThenCommitThenSecondLineJustStarted()
        {
            var myTcpPipe = new TcpDataPipe();

            myTcpPipe.Add(new byte[] {1, 2, 3});
            

            Assert.AreEqual(0, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(-1, myTcpPipe.EndPositionAtPackageIndex);
            

            myTcpPipe.AdvancePosition();
            Assert.AreEqual(1, myTcpPipe.CurrentElement);

            Assert.AreEqual(0, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(0, myTcpPipe.EndPositionAtPackageIndex);            

            myTcpPipe.AdvancePosition();
            Assert.AreEqual(2, myTcpPipe.CurrentElement);

            Assert.AreEqual(0, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(1, myTcpPipe.EndPositionAtPackageIndex);  

            var advanced =  myTcpPipe.AdvancePosition();
            Assert.IsTrue(advanced);
            Assert.IsTrue(myTcpPipe.EndOfData);
            Assert.AreEqual(3, myTcpPipe.CurrentElement);

            Assert.AreEqual(0, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(2, myTcpPipe.EndPositionAtPackageIndex);  


           
            advanced =  myTcpPipe.AdvancePosition();
            Assert.IsFalse(advanced);
            
            myTcpPipe.Add(new byte[] {4, 5, 6});
            
            advanced =  myTcpPipe.AdvancePosition();
            Assert.IsTrue(advanced);
            
            
            Assert.AreEqual(4, myTcpPipe.CurrentElement);

            Assert.AreEqual(1, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(0, myTcpPipe.EndPositionAtPackageIndex);  


            var data = myTcpPipe.GetUncommittedAmountAndCommit();
            new byte[]{1,2,3,4}.AsReadOnlyMemory().ArraysAreEqual(data);
            Assert.AreEqual(0, myTcpPipe.CurrentPackageIndex);
            Assert.AreEqual(1, myTcpPipe.StartPosition);
            Assert.AreEqual(0, myTcpPipe.EndPositionAtPackageIndex);

            
        }
    
    }
}