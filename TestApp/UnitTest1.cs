using Microsoft.VisualStudio.TestTools.UnitTesting;
using UDSonCAN;
using System;
using System.Threading.Tasks;
using System.Threading;
namespace TestApp
{
    [TestClass]
    public class UnitTest1
    {
        //private Boolean tested = true;
        [TestMethod]
        public void TestMethod1()
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
            }
            else Console.WriteLine("TEST FAILED");
            
            
        }
        /*[TestMethod]
        public void TestMethod2()
        {
                   
            var test = new Program();
            Boolean res;
            //////////////////////////////////////////////////////            
            res = test.TestCase2(test);
            Thread.Sleep(1000);
            if (res == true)
            {
                Console.WriteLine("TEST PASSED!!");
            }
            else Console.WriteLine("TEST FAILED");
            Assert.IsTrue(res);

    
        }*/
    }
}
