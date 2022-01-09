using System;
using System.Net.NetworkInformation;
using Microsoft.Win32;

namespace HWID_Changer
{
    class Program
    {
        public static string RandomId(int length)
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string result = "";
            Random random = new Random();

            for (int i = 0; i < length; i++)
            {
                result += chars[random.Next(chars.Length)];
            }

            return result;
        }

        public static string RandomMac()
        {
            string chars = "ABCDEF0123456789";
            string windows = "26AE";
            string result = "";
            Random random = new Random();

            result += chars[random.Next(chars.Length)];
            result += windows[random.Next(windows.Length)];

            for (int i = 0; i < 5; i++)
            {
                result += "-";
                result += chars[random.Next(chars.Length)];
                result += chars[random.Next(chars.Length)];
                
            }

            return result;
        }

        public static void Enable_LocalAreaConection(string adapterId, bool enable = true)
        {
            string interfaceName = "Ethernet";
            foreach (NetworkInterface i in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (i.Id == adapterId)
                {
                    interfaceName = i.Name;
                    break;
                }
            }

            string control;
            if (enable)
                control = "enable";
            else
                control = "disable";

            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("netsh", $"interface set interface \"{interfaceName}\" {control}");
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = psi;
            p.Start();
            p.WaitForExit();
        }

        public static void SpoofDisks()
        {
            using RegistryKey ScsiPorts = Registry.LocalMachine.OpenSubKey("HARDWARE\\DEVICEMAP\\Scsi");
            foreach (string port in ScsiPorts.GetSubKeyNames())
            {
                using RegistryKey ScsiBuses = Registry.LocalMachine.OpenSubKey($"HARDWARE\\DEVICEMAP\\Scsi\\{port}");
                foreach (string bus in ScsiBuses.GetSubKeyNames())
                {
                    using RegistryKey ScsuiBus = Registry.LocalMachine.OpenSubKey($"HARDWARE\\DEVICEMAP\\Scsi\\{port}\\{bus}\\Target Id 0\\Logical Unit Id 0", true);
                    if (ScsuiBus != null)
                    {
                        if (ScsuiBus.GetValue("DeviceType").ToString() == "DiskPeripheral")
                        {
                            string identifier = RandomId(14);
                            string serialNumber = RandomId(14);

                            ScsuiBus.SetValue("DeviceIdentifierPage", Encoding.UTF8.GetBytes(serialNumber));
                            ScsuiBus.SetValue("Identifier", identifier);
                            ScsuiBus.SetValue("InquiryData", Encoding.UTF8.GetBytes(identifier));
                            ScsuiBus.SetValue("SerialNumber", serialNumber);
                        }
                    }
                }
            }

            using RegistryKey DiskPeripherals = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\MultifunctionAdapter\\0\\DiskController\\0\\DiskPeripheral");
            foreach (string disk in DiskPeripherals.GetSubKeyNames())
            {
                using RegistryKey DiskPeripheral = Registry.LocalMachine.OpenSubKey($"HARDWARE\\DESCRIPTION\\System\\MultifunctionAdapter\\0\\DiskController\\0\\DiskPeripheral\\{disk}", true);
                DiskPeripheral.SetValue("Identifier", $"{RandomId(8)}-{RandomId(8)}-A");
            }
        }

        public static void SpoofGUIDs()
        {
            using RegistryKey HardwareGUID = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\IDConfigDB\\Hardware Profiles\\0001", true);
            HardwareGUID.SetValue("HwProfileGuid", $"{{{Guid.NewGuid()}}}");

            using RegistryKey MachineGUID = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography", true);
            MachineGUID.SetValue("MachineGuid", Guid.NewGuid().ToString());

            using RegistryKey MachineId = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\SQMClient", true);
            MachineId.SetValue("MachineId", $"{{{Guid.NewGuid()}}}");

            using RegistryKey SystemInfo = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\SystemInformation", true);
            SystemInfo.SetValue("BIOSReleaseDate", "14/12/2020");
            SystemInfo.SetValue("BIOSVersion", RandomId(10));
            SystemInfo.SetValue("ComputerHardwareId", $"{{{Guid.NewGuid()}}}");
            SystemInfo.SetValue("ComputerHardwareIds", $"{{{Guid.NewGuid()}}}\n{{{Guid.NewGuid()}}}\n{{{Guid.NewGuid()}}}\n");
            SystemInfo.SetValue("SystemManufacturer", RandomId(15));
            SystemInfo.SetValue("SystemProductName", RandomId(6));

            using RegistryKey Update = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate", true);
            Update.SetValue("SusClientId", Guid.NewGuid().ToString());
            Update.SetValue("SusClientIdValidation", Encoding.UTF8.GetBytes(RandomId(25)));
        }

        public static bool SpoofMAC()
        {
            bool err = false;

            using RegistryKey NetworkAdapters = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}");
            foreach (string adapter in NetworkAdapters.GetSubKeyNames())
            {
                if (adapter != "Properties")
                {
                    try
                    {
                        using RegistryKey NetworkAdapter = Registry.LocalMachine.OpenSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{4d36e972-e325-11ce-bfc1-08002be10318}}\\{adapter}", true);
                        if (NetworkAdapter.GetValue("BusType") != null)
                        {
                            NetworkAdapter.SetValue("NetworkAddress", RandomMac());
                            string adapterId = NetworkAdapter.GetValue("NetCfgInstanceId").ToString();
                            Enable_LocalAreaConection(adapterId, false);
                            Enable_LocalAreaConection(adapterId, true);

                        }
                    }
                    catch (System.Security.SecurityException ex)
                    {
                        Console.WriteLine("\n[X] Start the spoofer in admin mode to spoof your MAC address!");
                        err = true;
                        break;
                    }
                }
            }

            return err;
        }

        public static void Menu()
        {
            Console.WriteLine("\nAvailable spoofing options:\n [1] Disks\n [2] GUIDs\n [3] MAC address\n [4] Exit");
            Console.Write("Select an option: ");
            string input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    SpoofDisks();
                    Console.WriteLine("\n[+] Disks spoofed");
                    Menu();
                    break;

                case "2":
                    SpoofGUIDs();
                    Console.WriteLine("\n[+] GUIDs spoofed");
                    Menu();
                    break;

                case "3":
                    bool err = SpoofMAC();
                    if (!err) Console.WriteLine("[+] MAC address spoofed");
                    Menu();
                    break;

                case "4":
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine("\n[X] Invalid option!");
                    Menu();
                    break;
            }
        }

        static void Main()
        {
            Console.WriteLine("     ██╗  ██╗██╗    ██╗██╗██████╗      ██████╗██╗  ██╗ █████╗ ███╗   ██╗ ██████╗ ███████╗██████╗ \n     ██║  ██║██║    ██║██║██╔══██╗    ██╔════╝██║  ██║██╔══██╗████╗  ██║██╔════╝ ██╔════╝██╔══██╗\n     ███████║██║ █╗ ██║██║██║  ██║    ██║     ███████║███████║██╔██╗ ██║██║  ███╗█████╗  ██████╔╝\n     ██╔══██║██║███╗██║██║██║  ██║    ██║     ██╔══██║██╔══██║██║╚██╗██║██║   ██║██╔══╝  ██╔══██╗\n     ██║  ██║╚███╔███╔╝██║██████╔╝    ╚██████╗██║  ██║██║  ██║██║ ╚████║╚██████╔╝███████╗██║  ██║\n     ╚═╝  ╚═╝ ╚══╝╚══╝ ╚═╝╚═════╝      ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═══╝ ╚═════╝ ╚══════╝╚═╝  ╚═╝\n");
            Console.WriteLine("                                          --by LockBlock--\n");

            Menu();
        }
    }
}
