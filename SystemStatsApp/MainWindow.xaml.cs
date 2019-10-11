using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SystemStatsApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected PerformanceCounter cpuCounter;
        protected PerformanceCounter ramCounter;
        List<PerformanceCounter> cpuCounters = new List<PerformanceCounter>();
        int cores = 1;
        int maxRam = 0;

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

        public void AnimationTimerElapsed(object source, ElapsedEventArgs e)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                CpuAvgBar.Value = CpuAvg;
                CpuAvgInc.Value = CpuAvgDelta > 0 ? CpuAvgDelta : 0;
                CpuAvgDec.Value = CpuAvgDelta < 0 ? -CpuAvgDelta : 0;
                CpuTopBar.Value = CpuTop;
                CpuTopInc.Value = CpuTopDelta > 0 ? CpuTopDelta : 0;
                CpuTopDec.Value = CpuTopDelta < 0 ? -CpuTopDelta : 0;
                MemBar.Value = Mem;
                MemInc.Value = MemDelta > 0 ? MemDelta : 0;
                MemDec.Value = MemDelta < 0 ? -MemDelta : 0;
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

            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            int procCount = System.Environment.ProcessorCount;
            for (int i = 0; i < procCount; i++)
            {
                System.Diagnostics.PerformanceCounter pc = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", i.ToString());
                cpuCounters.Add(pc);
            }

            System.Timers.Timer dataTimer = new System.Timers.Timer(1200);
            dataTimer.Elapsed += new ElapsedEventHandler(DataTimerElapsed);
            dataTimer.Start();
            System.Timers.Timer animationTimer = new System.Timers.Timer(200);
            dataTimer.Elapsed += new ElapsedEventHandler(AnimationTimerElapsed);
            dataTimer.Start();
        }
    }
}
