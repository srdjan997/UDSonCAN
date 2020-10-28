using NUnit.Framework;
using UDSonCAN;
using System;
using System.Threading.Tasks;
using System.Threading;
namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            Console.WriteLine("Hello to App UDS on CAN!!");
            Console.WriteLine("\n");
            var test = new Program();
            Boolean res;
            //////////////////////////////////////////////////////
            res = test.TestCase1(test);
            Thread.Sleep(1000);
            Assert.IsTrue(res);
            if (res == true)
            {
                Console.WriteLine("TEST PASSED!!");
                Assert.Pass();
            }
            else
            {
                Console.WriteLine("TEST FAILED");
                Assert.Fail();
            } 

            
        }
    }
}