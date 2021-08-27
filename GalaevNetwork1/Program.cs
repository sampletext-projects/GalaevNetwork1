using System;
using System.Collections;
using System.Text;
using System.Threading;

namespace GalaevNetwork1
{
    public delegate void PostToFirstWT(BitArray message);

    public delegate void PostToSecondWT(BitArray message);

    public static class Program
    {
        private static void Main(string[] args)
        {   
            ConsoleHelper.WriteToConsole("Главный поток", "");
            Semaphore firstReceiveSemaphore = new Semaphore(0, 1);
            Semaphore secondReceiveSemaphore = new Semaphore(0, 1);
            FirstThread firstThread = new FirstThread(ref secondReceiveSemaphore, ref firstReceiveSemaphore);
            SecondThread secondThread = new SecondThread(ref firstReceiveSemaphore, ref secondReceiveSemaphore);
            Thread threadFirst = new Thread(firstThread.FirstThreadMain);
            Thread threadSecond = new Thread(secondThread.SecondThreadMain);
            PostToFirstWT postToFirstWt = firstThread.ReceiveData;
            PostToSecondWT postToSecondWt = secondThread.ReceiveData;
            threadFirst.Start(postToSecondWt);
            threadSecond.Start(postToFirstWt);
            Console.ReadLine();
        }
    }
}