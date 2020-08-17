using EasyModbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoboChess.Helpers;

namespace RoboChess
{
    public class Moxa
    {
        private ModbusClient Client;
        private ModbusServer Server;

        internal Moxa(int port, string IP)
        {
            Server = new ModbusServer() { Port = port };
            Client = new ModbusClient() { Port = port, IPAddress = IP };
            Server.Listen();
            Client.Connect();
            RedLight(true);
            GreenLight(false);
        }

        internal bool[] Read()
        {
            Connect();
            var inputs = Client.ReadDiscreteInputs(0, 3);
            Disconnect();
            return inputs;
        }

        private void Connect()
        {
            Client.Connect();
        }

        private void Disconnect()
        {
            Client.Disconnect();
        }

        internal void OpenGripper()
        {
            var complete = false;
            while (!complete)
            {
                try
                {
                    Client.Connect();
                    Client.WriteSingleCoil(0, false);
                    Task.Delay(200).Wait();
                    complete = true;
                    Client.Disconnect();
                }
                catch
                {
                }
            }
        }

        internal void CloseGripper()
        {
            var complete = false;
            while (!complete)
            {
                try
                {
                    Client.Connect();
                    Client.WriteSingleCoil(0, true);
                    Task.Delay(500).Wait();
                    complete = true;
                    Client.Disconnect();
                }
                catch
                {
                }
            }
        }

        internal void RedLight(bool turnOn)
        {
            var complete = false;
            while (!complete)
            {
                try
                {
                    Client.Connect();
                    Client.WriteSingleCoil(1, turnOn);
                    complete = true;
                    Client.Disconnect();
                }
                catch
                {
                }
            }
        }

        internal void GreenLight(bool turnOn)
        {
            var complete = false;
            while (!complete)
            {
                try
                {
                    Client.Connect();
                    Client.WriteSingleCoil(2, turnOn);
                    complete = true;
                    Client.Disconnect();
                }
                catch
                {
                }
            }
        }      
    }
}
