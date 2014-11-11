using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Diagnostics;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
    public class PerformanceMonitor
    {
        public PerformanceMonitor()
        {
            Name = Guid.NewGuid().ToString();
            ReportingFrequency = 100;
        }

        public PerformanceMonitor(string name)
        {
            Name = name;
            ReportingFrequency = 100;
        }

        public PerformanceMonitor(string name, int reportingFrequency)
        {
            Name = name;
            ReportingFrequency = reportingFrequency;
        }

        private long startTime;
        private bool inIteration;
        public int ReportingFrequency { get; set; }
        public string Name { get; set; }
        public int Iterations { get; private set; }
        public double TotalCompletionTimeInMs { get; private set; }
        public double AverageCompletionTimeInMs
        {
            get
            {
                return TotalCompletionTimeInMs / Iterations;
            }
        }

        public void Start()
        {
            if (!inIteration)
            {
                inIteration = true;
                Iterations++;
                startTime = Environment.TickCount;
            }
        }

        public void Stop()
        {
            if (inIteration)
            {
                double completionTimeInMs = Environment.TickCount - startTime;
                TotalCompletionTimeInMs += completionTimeInMs;
                if (Iterations % ReportingFrequency == 0)
                {
                    ClientLogger.Debug("{0} action completed: iteration = {1}, completionTime = {2}, averageCompletionTime = {3:0.000}", Name, Iterations, completionTimeInMs, AverageCompletionTimeInMs);
                }
                inIteration = false;
            }
        }

    }
}
