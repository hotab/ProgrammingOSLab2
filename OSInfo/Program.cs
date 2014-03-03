using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Management;

namespace Lab
{
    class Program
    {
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {

            /// DWORD->unsigned int  
            public uint dwLength;

            /// DWORD->unsigned int  
            public uint dwMemoryLoad;

            /// DWORDLONG->ULONGLONG->unsigned __int64  
            public ulong ullTotalPhys;

            /// DWORDLONG->ULONGLONG->unsigned __int64  
            public ulong ullAvailPhys;

            /// DWORDLONG->ULONGLONG->unsigned __int64  
            public ulong ullTotalPageFile;

            /// DWORDLONG->ULONGLONG->unsigned __int64  
            public ulong ullAvailPageFile;

            /// DWORDLONG->ULONGLONG->unsigned __int64  
            public ulong ullTotalVirtual;

            /// DWORDLONG->ULONGLONG->unsigned __int64  
            public ulong ullAvailVirtual;

            /// DWORDLONG->ULONGLONG->unsigned __int64  
            public ulong ullAvailExtendedVirtual;
        }
        
        [DllImport("kernel32.dll")]
        public static extern bool GlobalMemoryStatusEx(out MEMORYSTATUSEX stat);

        static public string DriveTypeToString(DriveType t)
        {
            switch (t)
            {
                case DriveType.CDRom:
                    return "CD Rom";
                case DriveType.Network:
                    return "Network";
                case DriveType.Fixed:
                    return "Fixed";
                case DriveType.Removable:
                    return "Removable";
                case DriveType.Ram:
                    return "Ram";
                case DriveType.NoRootDirectory:
                    return "No root directory";
            }
            return "Unknown";
        }

        static public string GetSystemName()
        {
            var name = (from x in new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem").Get().OfType<ManagementObject>()
                        select x.GetPropertyValue("Caption")).First();
            return name != null ? name.ToString() : "Unknown";
        }

        static public string GetMemoryType(UInt16 t)
        {
            switch (t)
            {
                case 1:
                    return "Other";
                case 2:
                    return "Unknown";
                case 3:
                    return "VRAM";
                case 4:
                    return "DRAM";
                case 5:
                    return "SRAM";
                case 6:
                    return "WRAM";
                case 7:
                    return "EDO RAM";
                case 8:
                    return "Burst Synchronous DRAM";
                case 9:
                    return "Pipelined Burst SRAM";
                case 10:
                    return "CDRAM";
                case 11:
                    return "3DRAM";
                case 12:
                    return "SDRAM";
                case 13:
                    return "SGRAM";
            }
            return "Unknown";
        }

        static public void OutputGPUInfo()
        {
            ManagementObjectSearcher searcher1 = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_VideoController");

            Console.WriteLine("**********GPU**********");

            ManagementObjectCollection adapters = searcher1.Get();

            Console.WriteLine("System has: {0} GPUs", adapters.Count);
            int counter = 1;
            foreach (ManagementObject queryObj in adapters)
            {
                if (queryObj["AdapterRAM"] == null) continue;
                Console.WriteLine("Adapter {0}:", counter);
                Console.WriteLine("\tName: {0}", queryObj["Caption"]);
                Console.WriteLine("\tType: {0}", queryObj["VideoProcessor"]);
                Console.WriteLine("\tAvailable memory: {0}", Math.Round(double.Parse(queryObj["AdapterRAM"].ToString()) / (1024 * 1024), 2) + " MB");
                Console.WriteLine("\tMemory type: {0}", GetMemoryType((UInt16)queryObj["VideoMemoryType"]));
                Console.WriteLine("\tColor depth: {0} bits", queryObj["CurrentBitsPerPixel"]);
                Console.WriteLine();
                ++counter;
            }

        }

        static public void OutputGeneralInfo()
        {
            //Processes info
            int total_running_processes = Process.GetProcesses().Length;
            ManagementObjectSearcher searcher1 = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BIOS");

            Console.WriteLine("**********General**********");

            Console.WriteLine("Operating system: " + GetSystemName() + Environment.OSVersion.ServicePack);
            Console.WriteLine("Operating system is 64 bit: " + Environment.Is64BitOperatingSystem);
            Console.WriteLine("Total running processes: " + total_running_processes);
            Console.WriteLine();

            Console.WriteLine("**********BIOSes**********");

            int counter = 1;
            foreach(var bios in searcher1.Get())
            {

                Console.WriteLine("BIOS {0}:", counter);
                ++counter;

                string manufacturer = (string)bios["Manufacturer"];
                string model = (string)bios["Name"];
                string description = (string)bios["Description"];
                bool isPrimaryBios = (bool)bios["PrimaryBIOS"];

                if (manufacturer != null) Console.WriteLine("\tManufacturer: " + manufacturer);
                if (model != null) Console.WriteLine("\tModel: " + model);
                if (description != null) Console.WriteLine("\tDescription: " + description);
                Console.WriteLine("\tThis is the primary BIOS: {0}", isPrimaryBios);
                Console.WriteLine();

            }

        }

        static public void OutputRAMInfo()
        {
            MEMORYSTATUSEX stat = new MEMORYSTATUSEX();
            stat.dwLength = 64;
            bool res = GlobalMemoryStatusEx(out stat);
            ulong avail_ram = stat.ullTotalPhys;
            avail_ram = avail_ram / 1024 / 1024;

            ulong free_ram = stat.ullAvailPhys;
            free_ram = free_ram / 1024 / 1024;

            Console.WriteLine("**********RAM info**********");
            Console.WriteLine("Total available memory: " + Math.Round((double)avail_ram / 1024, 2) + " GB");
            Console.WriteLine("Total free memory: " + Math.Round((double)free_ram / 1024, 2) + " GB");
            Console.WriteLine();


        }

        static public void OutputProcessorInfo()
        {
            int physicalCoreCount = 0;

            ManagementObjectCollection processorCollection = new ManagementObjectSearcher("Select * from Win32_Processor").Get();
            foreach (var item in processorCollection)
                physicalCoreCount += int.Parse(item["NumberOfCores"].ToString());
            

            Console.WriteLine("**********Processor info**********");
            Console.WriteLine("Processor logical cores count: " + Environment.ProcessorCount);
            Console.WriteLine("Processor physical cores count: " + physicalCoreCount);
            int counter = 1;
            foreach (var item in processorCollection)
            {
                Console.WriteLine("Processor {0}:", counter);
                ++counter;
                Console.WriteLine("\tCaption: {0}", item["Caption"]);
                Console.WriteLine("\tName: {0}", item["Name"]);
                Console.WriteLine("\tManufacturer: {0}", item["Manufacturer"]);
                Console.WriteLine("\tLoad: {0}%", item["LoadPercentage"]);
                Console.WriteLine("\tCurrent clock: {0} MHz", item["CurrentClockSpeed"]);
                Console.WriteLine("\tMax clock: {0} MHz",item["MaxClockSpeed"]);
                Console.WriteLine("\tCores: {0}", item["NumberOfCores"]);
                Console.WriteLine("\tCurrent voltage: {0}V", Convert.ToDouble(item["CurrentVoltage"])/10);
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        
        static public void OutputDrivesInfo()
        {
            //Disks
            string[] drives = Environment.GetLogicalDrives();

            Console.WriteLine("**********Drives info**********");
            ManagementObjectSearcher driveSearcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DiskDrive");
            Console.WriteLine("Physical drives info:");

            foreach (ManagementObject curDrive in driveSearcher.Get())
            {
                Console.WriteLine("\tDevice ID: {0}",curDrive["DeviceID"]);
                Console.WriteLine("\tInterface type: {0}", curDrive["InterfaceType"]);
                Console.WriteLine("\tManufacturer: {0}", curDrive["Manufacturer"]);
                Console.WriteLine("\tModel: {0}", curDrive["Model"]);
                Console.WriteLine("\tSize: {0} GB",Math.Round(System.Convert.ToDouble(curDrive["Size"]) / 1024 / 1024 / 1024, 2));
            }
            Console.WriteLine();
            Console.WriteLine("Logical drives list: " + string.Join(", ", drives));
            Console.WriteLine();
            Console.WriteLine("Total space on all available drives: ");
            for (int i = 0; i < drives.Length; i++)
            {
                DriveInfo info = new DriveInfo(drives[i]);

                if (info.IsReady)
                {
                    Console.WriteLine("\t{0} - {1} GB ({2})", drives[i], Math.Round((double)info.TotalSize / 1024 / 1024 / 1024, 2), DriveTypeToString(info.DriveType));
                }
            }
            Console.WriteLine();
        }
        
        static void Main(string[] args)
        {
            OutputGeneralInfo();
            OutputProcessorInfo();
            OutputRAMInfo();
            OutputGPUInfo();
            OutputDrivesInfo();
            Console.ReadKey();
        }
    }
}