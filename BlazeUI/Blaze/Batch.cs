using System;
using System.Collections.Generic;
using System.Threading;

namespace BlazeUI.Blaze;

public static class Batch
{
    public static TResult[] Select<TSource, TResult>(TSource[] sourceArray, Func<TSource, TResult> converter, int threads = 5, int batchCount = 500)
    {
        TResult[] resultArray = new TResult[sourceArray.Length];
        
        BatchList batch = Divide(sourceArray.Length, batchCount);
        
        Thread[] workers = new Thread[threads];
        for (int i = 0; i < threads; i++)
        {
            workers[i] = new(() =>
            {
                while (true)
                {
                    if (batch.TryGetNext(out BatchItem result))
                        for (int j = result.from; j < result.to; j++)
                            resultArray[j] = converter(sourceArray[j]);
                    else
                        break;
                }
            });
            workers[i].Start();
        }
        
        // poll whether the threads have finished
        while (!batch.Finished())
            Thread.Sleep(10);
        
        return resultArray;
    }

    private static BatchList Divide(int length, int batchCount)
    {
        int batchLength = length / batchCount + 1;
        List<BatchItem> batch = new();

        int at = 0;
        for (int current = 0; current < batchLength; current++)
        {
            int end = Math.Min(at + batchCount, length);
            batch.Add(new(at, end));
            at = end;
        }

        return new BatchList(batch);
    }

    private class BatchList(List<BatchItem> batch)
    {
        private readonly Mutex Mutex = new();
        private int next;

        public bool TryGetNext(out BatchItem item)
        {
            Mutex.WaitOne();
            if (next >= batch.Count)
            {
                item = new(0,0);
                Mutex.ReleaseMutex();
                return false;
            }
            
            item = batch[next++];
            Mutex.ReleaseMutex();
            return true;
        }

        public bool Finished()
        {
            return next >= batch.Count;
        }
    }
    
    private readonly struct BatchItem(int from, int to)
    {
        public readonly int from = from;
        public readonly int to = to;
    }
}