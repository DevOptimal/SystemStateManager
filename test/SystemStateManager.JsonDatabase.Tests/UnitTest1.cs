using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace DevOptimal.SystemStateManager.JsonDatabase.Tests
{
    [TestClass]
    public class UnitTest1
    {
        // Create a new Mutex. The creating thread does not own the
        // Mutex.
        private const int numIterations = 1;
        private const int numThreads = 3;

        [TestMethod]
        public void TestMutex()
        {
            // Create the threads that will use the protected resource.
            for (int i = 0; i < numThreads; i++)
            {
                var myThread = new Thread(new ThreadStart(MyThreadProc))
                {
                    Name = string.Format("Thread{0}", i + 1)
                };
                myThread.Start();
            }
            Thread.Sleep(10000);
            // The main thread exits, but the application continues to
            // run until all foreground threads have exited.
        }

        private static void MyThreadProc()
        {
            for (int i = 0; i < numIterations; i++)
            {
                UseResource();
            }
        }

        // This method represents a resource that must be synchronized
        // so that only one thread at a time can enter.
        private static void UseResource()
        {
            var mut = new Mutex(false, @"Global\foobar");

            // Wait until it is safe to enter.
            mut.WaitOne();
            try
            {
                Console.WriteLine("{0} has entered the protected area", Thread.CurrentThread.Name);

                // Place code to access non-reentrant resources here.

                // Simulate some work.
                Thread.Sleep(500);

                Console.WriteLine("{0} is leaving the protected area\r\n", Thread.CurrentThread.Name);
            }
            finally
            {
                // Release the Mutex.
                mut.ReleaseMutex();
            }
        }
    }
}