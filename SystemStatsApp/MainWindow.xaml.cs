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
        //List<float> AvailableCPU = new List<float>();
        //List<float> AvailableRAM = new List<float>();

        protected PerformanceCounter cpuCounter;
        protected PerformanceCounter ramCounter;
        List<PerformanceCounter> cpuCounters = new List<PerformanceCounter>();
        int cores = 0;
        float lastRam = -1;
        float lastCpuAvg = -1;
        float lastCpuMax = -1;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void TimerElapsed(object source, ElapsedEventArgs e)
        {
            float cpuAvg = cpuCounter.NextValue();
            float sum = 0;
            float cpuMax = 0;
            float cpuAvgDelta = 0;
            float cpuMaxDelta = 0;
            float ramDelta = 0;
            foreach (PerformanceCounter c in cpuCounters)
            {
                var value = c.NextValue();
                sum = sum + value;
                cpuMax = Math.Max(cpuMax, value);
            }
            sum = sum / (cores);
            float ram = ramCounter.NextValue();
            
            if (lastRam > -1)
            {
                // ram stores Free memory, but we want delta to increase as memory use increases
                ramDelta = lastRam - ram;
                cpuAvgDelta = cpuAvg - lastCpuAvg;
                cpuMaxDelta = cpuMax - lastCpuMax;
            }
            lastRam = ram;
            lastCpuAvg = cpuAvg;
            lastCpuMax = cpuMax;

            CpuAvg.Dispatcher.BeginInvoke((Action)(() =>
            {
                CpuAvg.Value = cpuAvg;
                CpuAvgInc.Value = cpuAvgDelta > 0 ? cpuAvgDelta : 0;
                CpuAvgDec.Value = cpuAvgDelta < 0 ? -cpuAvgDelta : 0;
                CpuMax.Value = cpuMax;
                CpuMaxInc.Value = cpuMaxDelta > 0 ? cpuMaxDelta : 0;
                CpuMaxDec.Value = cpuMaxDelta < 0 ? -cpuMaxDelta : 0;
                Mem.Value = ram;
                MemInc.Value = ramDelta > 0 ? ramDelta : 0;
                MemDec.Value = ramDelta < 0 ? -ramDelta : 0;
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

            try
            {
                System.Timers.Timer t = new System.Timers.Timer(1200);
                t.Elapsed += new ElapsedEventHandler(TimerElapsed);
                t.Start();
            }
            catch (Exception ex)
            {
                //StatusLabel.Text = ex.ToString();
            }
        }
    }
}
