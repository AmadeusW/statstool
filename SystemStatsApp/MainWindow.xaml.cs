﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Timers;
using System.Windows;

namespace SystemStatsApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int ProbingInterval = 1200;
        private const int AnimationInterval = 100;
        private const int AnimationFrames = 12; // ProbingInterval / AnimationInterval
        protected PerformanceCounter cpuCounter;
        protected PerformanceCounter ramCounter;
        List<PerformanceCounter> cpuCounters = new List<PerformanceCounter>();
        int cores = 1;
        float AvailableRam = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        public float CpuAvg;
        public float PreviousCpuAvg;
        public float CpuAvgDelta;
        public float PreviousCpuAvgDelta;
        public float CpuTop;
        public float PreviousCpuTop;
        public float CpuTopDelta;
        public float PreviousCpuTopDelta;
        public float Mem;
        public float PreviousMem;
        public float MemDelta;
        public float PreviousMemDelta;

        public void DataTimerElapsed(object source, ElapsedEventArgs e)
        {
            PreviousCpuAvg = CpuAvg;
            PreviousCpuAvgDelta = CpuAvgDelta;
            PreviousCpuTop = CpuTop;
            PreviousCpuTopDelta = CpuTopDelta;
            PreviousMem = Mem;
            PreviousMemDelta = MemDelta;

            CpuAvg = cpuCounter.NextValue();
            Mem = ramCounter.NextValue();

            float cpuTopBuilder = 0;
            foreach (PerformanceCounter c in cpuCounters)
            {
                var value = c.NextValue();
                cpuTopBuilder = Math.Max(cpuTopBuilder, value);
            }
            CpuTop = cpuTopBuilder;
            
            if (PreviousMem > -1)
            {
                // ram stores Free memory, but we want delta to increase as memory use increases
                MemDelta = PreviousMem - Mem;
                CpuAvgDelta = CpuAvg - PreviousCpuAvg;
                CpuTopDelta = CpuTop - PreviousCpuTopDelta;
            }
        }

        int animationFrame = 0;
        public void AnimationTimerElapsed(object source, ElapsedEventArgs e)
        {
            if (animationFrame == AnimationFrames)
                animationFrame = 0;
            animationFrame++; // we want range 1-6

            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                var cpuAvgMovement = (CpuAvg - PreviousCpuAvg) / AnimationFrames;
                var targetCpuAvg = PreviousCpuAvg + cpuAvgMovement * animationFrame;
                var cpuAvgDeltaMovement = (CpuAvgDelta - PreviousCpuAvgDelta) / AnimationFrames;
                var targetCpuAvgDelta = PreviousCpuAvgDelta + cpuAvgDeltaMovement * animationFrame;

                CpuAvgBar.Value = targetCpuAvg;
                CpuAvgInc.Value = targetCpuAvgDelta > 0 ? targetCpuAvgDelta : 0;
                CpuAvgDec.Value = targetCpuAvgDelta < 0 ? -targetCpuAvgDelta : 0;

                var cpuTopMovement = (CpuTop - PreviousCpuTop) / AnimationFrames;
                var targetCpuTop = PreviousCpuTop + cpuTopMovement * animationFrame;
                var cpuTopDeltaMovement = (CpuTopDelta - PreviousCpuTopDelta) / AnimationFrames;
                var targetCpuTopDelta = PreviousCpuTopDelta + cpuTopDeltaMovement * animationFrame;

                CpuTopBar.Value = targetCpuTop;
                CpuTopInc.Value = targetCpuTopDelta > 0 ? targetCpuTopDelta : 0;
                CpuTopDec.Value = targetCpuTopDelta < 0 ? -targetCpuTopDelta : 0;

                var MemMovement = (Mem - PreviousMem) / AnimationFrames;
                var targetMem = PreviousMem + MemMovement * animationFrame;
                var memDeltaMovement = (MemDelta - PreviousMemDelta) / AnimationFrames;
                var targetMemDelta = PreviousMemDelta + memDeltaMovement * animationFrame;

                MemBar.Value = targetMem;
                MemInc.Value = targetMemDelta > 0 ? targetMemDelta : 0;
                MemDec.Value = targetMemDelta < 0 ? -targetMemDelta : 0;
            }));
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            foreach (var item in new ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                cores = cores + int.Parse(item["NumberOfLogicalProcessors"].ToString());
            }
            ObjectQuery winQuery = new ObjectQuery("SELECT * FROM CIM_OperatingSystem");

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(winQuery);

            foreach (ManagementObject item in searcher.Get())
            {
                Console.WriteLine("Total Physical Memory = " + item["TotalVisibleMemorySize"]);
                Console.WriteLine("Total Virtual Memory = " + item["TotalVirtualMemorySize"]);
                AvailableRam = float.Parse(item["TotalVisibleMemorySize"].ToString());
                MemBar.Maximum = AvailableRam/1024; // convert KB to MB
                MemInc.Maximum = MemBar.Maximum / 10;
                MemDec.Maximum = MemBar.Maximum / 10;
            }

            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            int procCount = Environment.ProcessorCount;
            for (int i = 0; i < procCount; i++)
            {
                PerformanceCounter pc = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                cpuCounters.Add(pc);
            }

            Timer dataTimer = new Timer(ProbingInterval);
            dataTimer.Elapsed += new ElapsedEventHandler(DataTimerElapsed);
            dataTimer.Start();
            Timer animationTimer = new Timer(AnimationInterval);
            animationTimer.Elapsed += new ElapsedEventHandler(AnimationTimerElapsed);
            animationTimer.Start();
        }
    }
}
