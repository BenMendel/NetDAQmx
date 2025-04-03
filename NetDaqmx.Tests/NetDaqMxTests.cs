using NetDAQmx;

namespace NetDaqmx.Tests;

public class NetDaqMxTests
{
    private NIDAQ[] Daqs;
    public NetDaqMxTests()
    {
        var connectedDevices = NIDAQ.GetSystemDevices();
        Assert.NotEmpty(connectedDevices);

        Daqs = new NIDAQ[connectedDevices.Length]; // Use NI-MAX to assign this name.
        for (int i = 0; i < connectedDevices.Length; i++)
        {
            Daqs[i] = new(connectedDevices[i]);
        }
    }

    [Fact]
    public void WritePort0Test()
    {
        foreach (var daq in Daqs)
        {
            daq.WritePort(0, 255);
            Assert.Equal(255, daq.ReadPort(0));

            for (byte i = 0; i < byte.MaxValue; i++)
            {
                daq.WritePort(0, i);

                Assert.Equal(i, daq.ReadPort(0));
            }
        }
    }

    [Fact]
    public void AnalogInputTest()
    {
        foreach (var daq in Daqs)
        {
            double[] analogReads = new double[10];
            var analogInputAverage = daq.GetAnalogInputSingleLine(0, 0, 10, analogReads);
        }
    }
}