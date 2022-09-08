using System;
using System.IO.Ports;
using System.Linq;

namespace SerialPortTest
{
    class Program
    {
        static readonly byte[] cmdConnCheck = { (byte)ComCmd.ConnCheck, 0, 0, 0, 0, 0 };
        static readonly byte[] cmdConnAccept = { (byte)ComCmd.ConnAccept, 0, 0, 0, 0, 0 };

        static readonly byte[] cmdDimensionFind = { (byte)ComCmd.DimensionFind, 0, 0, 0, 0, 0 };

        static readonly int dataArrLength = 6;
        static SerialPort serialPort;
        static Random r = new Random();
        enum ComCmd : byte
        {
            ConnCheck = 0xCC,
            ConnAccept = 0xCA,

            DimensionFind = 0xDF,
            DimensionDo = 0xDD,
        }

        static void Main(string[] args)
        {
            int portN = EnterPortName();

            serialPort = new SerialPort("COM" + portN,
                                        9600,
                                        Parity.None,
                                        8,
                                        StopBits.One);

            serialPort.DtrEnable = true;//i am online
            //serialPort.RtsEnable = true;//i want to send, why not RTR (Ready To Receive)?

            //serialPort.DsrHolding get only, companion online 
            //serialPort.CtsHolding get only, companion ready to read


            serialPort.Handshake = Handshake.None;
            serialPort.DataReceived += SerialPort_DataReceived;

            try
            {
                if (!(serialPort.IsOpen)) serialPort.Open();

                ConsoleKeyInfo keyInfo;
                do
                {
                    keyInfo = Console.ReadKey(true);
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.C:
                            {
                                Console.WriteLine("I want to test the Connection");
                                SendArray(cmdConnCheck);
                            }
                            break;

                        case ConsoleKey.D:
                            {
                                Console.WriteLine("I want to send Dimension");
                                SendArray(DoDimension());
                            }
                            break;

                        case ConsoleKey.F:
                            {
                                Console.WriteLine("I want to find Dimension");
                                SendArray(cmdDimensionFind);
                            }
                            break;

                        case ConsoleKey.F2:
                            {
                                Console.WriteLine("[! test]: I want to send problem arrary: CC + DF");

                                var a = cmdConnCheck;
                                a[a.Length - 1] = GetCheckSum(a);
                                var b = cmdDimensionFind;
                                b[b.Length - 1] = GetCheckSum(b);

                                var v = new byte[a.Length + b.Length];
                                a.CopyTo(v, 0);
                                b.CopyTo(v, dataArrLength);
                                
                                if (!serialPort.IsOpen)
                                {
                                    Console.WriteLine("ERROR: serial port is not open");
                                    return;
                                }
                                serialPort.Write(v, 0, v.Length);
                            }
                            break;
                    }
                } while (keyInfo.Key != ConsoleKey.Escape);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error opening/writing to serial port: " + ex.Message, "Error!");
            }

            serialPort.Close();
            Console.WriteLine("Port closed");

            Console.ReadKey();
        }

        static byte GetCheckSum(byte[] arr)
        {
            int sum = 0;
            for (int i = 0; i < arr.Length - 1; ++i)
            {
                sum += arr[i];
            }
            return (byte)(sum % 256);//???
        }

        private static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = sender as SerialPort;
            if (null == sp) return;

            while (sp.BytesToRead >= dataArrLength)
            {
                byte[] dataFromСompanion = new byte[dataArrLength];
                sp.Read(dataFromСompanion, 0, dataArrLength);

                if (dataFromСompanion[dataArrLength - 1] != GetCheckSum(dataFromСompanion))
                {
                    Console.WriteLine("ERROR: Wrong check sum");
                    return;
                }

                switch (dataFromСompanion[0])
                {
                    case (byte)ComCmd.ConnAccept:
                        {
                            Console.WriteLine("-> companion online");
                        }
                        break;

                    case (byte)ComCmd.ConnCheck:
                        {
                            Console.WriteLine("-> companion wants to test the Connection");
                            SendArray(cmdConnAccept);
                        }
                        break;

                    case (byte)ComCmd.DimensionFind:
                        {
                            Console.WriteLine("-> companion need Dimension");
                            SendArray(DoDimension());
                        }
                        break;

                    case (byte)ComCmd.DimensionDo:
                        {
                            Console.WriteLine("-> companion sent Dimension");
                            Console.WriteLine(dataFromСompanion[1]);
                            Console.WriteLine(dataFromСompanion[2]);
                            Console.WriteLine(dataFromСompanion[3]);
                            Console.WriteLine(dataFromСompanion[4]);
                        }
                        break;
                }//sw
            }//while
        }//event
        static void SendArray(byte[] arr)
        {
            if (!serialPort.IsOpen)
            {
                Console.WriteLine("ERROR: serial port is not open");
                return;
            }
            arr[arr.Length - 1] = GetCheckSum(arr);
            serialPort.Write(arr, 0, arr.Length);
        }
        static byte[] DoDimension()
        {
            byte[] arr = new byte[dataArrLength];
            r.NextBytes(arr);
            arr[0] = (byte)ComCmd.DimensionDo;
            return arr;
        }

        //--------------------------------------------------------
        static int EnterPortName()
        {
            //info
            bool isInt = false;
            string s = "";
            do
            {
                Console.WriteLine("Enter port number:");
                s = Console.ReadLine();
                isInt = s.All(char.IsDigit) && (s != "");
            }
            while (!isInt);

            int portN;
            int.TryParse(s, out portN);

            Console.WriteLine("Port number is " + portN);
            return portN;
        }
    }
}
