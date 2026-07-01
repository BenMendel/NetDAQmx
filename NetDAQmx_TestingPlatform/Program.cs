using NetDAQmx;

namespace NetDAQmx_TestingPlatform;

internal class Program
{
    /// <summary>
    /// Tested on: NI-9481, NI-9485.
    /// </summary>
    static void Main(string[] args)
    {
        var connectedDevices = NIDAQ.GetSystemDevices();
        if (connectedDevices.Length == 0)
        {
            Console.WriteLine("No NI-DAQ devices connected.");
            return;
        }

        uint relay = 7;        // 0-3 for NI-9481; 0-7 for NI-9485
        NIDAQ daq = new(connectedDevices[0]);

        // Drive the relay line low to close the relay (active-low relay logic).
        // WriteDigitalOutput(port, channel, value=false) → drives line low → relay closes.
        bool closeRelay = false;
        daq.WriteDigitalOutput(0, relay, !closeRelay);

        // Read the line state back through a DI channel.
        bool lineIsHigh = daq.ReadDigitalInput(0, relay);

        Console.WriteLine(lineIsHigh ? "Line high (relay open)" : "Line low (relay closed)");
    }
}
