using GradientLoadBalancing2.Models;
using System;
using System.Diagnostics;

namespace GradientLoadBalancing2
{
    internal class Program
    {
        static Random random = new Random();

        static int GenerateGaussianTaskTime(int min, int max)
        {
            double mean = (min + max) / 2.0;
            double stdDev = (max - min) / 6.0;
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            double randNormal = mean + stdDev * randStdNormal;

            return (int)Math.Clamp(randNormal, min, max);
        }

        static async Task Main()
        {
            int dimensions = 4;
            int numberOfTasks = 100;

            Cluster cluster = new Cluster(dimensions);
            GradientLoadBalancer loadBalancer = new GradientLoadBalancer(cluster, dimensions);
            int[] taskTimes = new int[numberOfTasks];

            for (int i = 0; i < numberOfTasks; i++)
            {
                taskTimes[i] = GenerateGaussianTaskTime(5, 10);
            }

            // With load balancing
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var addTasksTask = Task.Run(async () =>
            {
                foreach (var taskTime in taskTimes)
                {
                    await loadBalancer.AddTaskAsync(taskTime);
                }
            });

            var balanceTask = Task.Run(async () =>
            {
                while (cluster.WorkerNodes.Any(w => w.Queue.Any()))
                {
                    await loadBalancer.BalanceLoadAsync();
                    await Task.Delay(10); // periodically balance the load
                }
            });

            var processingTask = cluster.ProcessAllTasksAsync();

            await Task.WhenAll(addTasksTask, balanceTask, processingTask);

            int totalProcessingTimeWithDistribution = processingTask.Result;

            stopwatch.Stop();
            TimeSpan timeWithDistribution = stopwatch.Elapsed;

            Console.WriteLine("Time with load balancing: " + timeWithDistribution.TotalMilliseconds + " ms");
            //Console.WriteLine("Total processing time with load balancing: " + totalProcessingTimeWithDistribution);
            Console.WriteLine("Average load: " + cluster.GetAverageLoad());
            Console.WriteLine("Average load deviation percentage: " + cluster.GetLoadDeviationPercentage() + "%");
            cluster.PrintWorkerNodeStatistics();

            //Without load balancing
           cluster = new Cluster(dimensions);
            stopwatch.Reset();
            stopwatch.Start();

            int totalProcessingTimeWithoutDistribution = await cluster.SimulateProcessingWithoutDistributionAsync(taskTimes);

            stopwatch.Stop();
            TimeSpan timeWithoutDistribution = stopwatch.Elapsed;

            Console.WriteLine("Time without load balancing: " + timeWithoutDistribution.TotalMilliseconds + " ms");
            //Console.WriteLine("Total processing time without load balancing: " + totalProcessingTimeWithoutDistribution);
            cluster.PrintWorkerNodeStatistics();
        }

    }
}

