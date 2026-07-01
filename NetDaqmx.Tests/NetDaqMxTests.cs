using NetDAQmx;

namespace NetDaqmx.Tests;

public class NetDaqMxTests
{
    private readonly NIDAQ[] Daqs;

    public NetDaqMxTests()
    {
        var connectedDevices = NIDAQ.GetSystemDevices();
        Daqs = new NIDAQ[connectedDevices.Length];
        for (int i = 0; i < connectedDevices.Length; i++)
            Daqs[i] = new(connectedDevices[i]);
    }

    [SkippableFact]
    public void WritePort0Test()
    {
        Skip.If(Daqs.Length == 0, "No NI-DAQ devices connected.");

        foreach (var daq in Daqs)
        {
            daq.WriteDigitalPort(0, 255);
            Assert.Equal(255, daq.ReadDigitalPort(0));

            for (byte i = 0; i < byte.MaxValue; i++)
            {
                daq.WriteDigitalPort(0, i);
                Assert.Equal(i, daq.ReadDigitalPort(0));
            }
        }
    }

    [SkippableFact]
    public void AnalogInputTest()
    {
        Skip.If(Daqs.Length == 0, "No NI-DAQ devices connected.");

        foreach (var daq in Daqs)
        {
            double[] analogReads = new double[10];
            var analogInputAverage = daq.ReadAnalogInput(0, 0, 10, analogReads);
        }
    }
}
