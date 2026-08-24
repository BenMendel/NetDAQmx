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

    /// <summary>
    /// Replicates the exact behaviour of GetReferenceVoltageAsync from RUTesterNIDaqCommunication,
    /// including the full digital-output setup (analog-add latch + MUX508 enable) that routes the
    /// 2.5 V reference signal to ai7.
    ///
    /// IMPORTANT: Only valid when the NI-DAQ (USB-6008) is installed in RU Tester hardware (PN RD000311).
    /// Running on any other setup will produce meaningless results.
    ///
    /// Note: SetMasterAddLatch ORs the address byte with 0x80 for MezzanineID RU01/RU02/SelfTest.
    /// Adjust WriteDigitalPort(0, ...) calls in that block if your unit requires it.
    /// </summary>
    [SkippableFact]
    public async Task ReferenceVoltageTest()
    {
        Skip.If(Daqs.Length == 0, "No NI-DAQ devices connected.");

        var delay = TimeSpan.FromMilliseconds(0.5);

        foreach (var daq in Daqs)
        {
            // ── SetAnalogAddLatchAsync(AnalogAddLatchStates.All0 = 0) ──────────
            // → SetU49LatchAsync(0, 0) → SetLatchData(Low_ADD_Sel = 4, data = 0x00)

            // SetMasterAddLatch(masterAddLatchData = 4): WriteDataPortAsync(4)
            daq.WriteDigitalPort(0, 4);         await Task.Delay(delay);
            // ActivateDataLatchAsync(MasterCLK = 6, isOutputCLK = true)
            daq.WriteDigitalPort(1, 0x08);      await Task.Delay(delay); // disable decoder
            daq.WriteDigitalPort(1, 0x0E);      await Task.Delay(delay); // set address (0b1000|6)
            daq.WriteDigitalPort(1, 0x06);      await Task.Delay(delay); // enable decoder
            daq.WriteDigitalPort(1, 0x0E);      await Task.Delay(delay); // rising edge (CLK)
            daq.WriteDigitalOutput(1, 3, true); await Task.Delay(delay); // p1.3 rising edge

            // WriteDataPortAsync(0x00) — latch payload
            daq.WriteDigitalPort(0, 0x00);      await Task.Delay(delay);
            // ActivateDataLatchAsync(Master_Selector_0_CSn = 0, isOutputCLK = true)
            daq.WriteDigitalPort(1, 0x08);      await Task.Delay(delay); // disable decoder
            daq.WriteDigitalPort(1, 0x08);      await Task.Delay(delay); // set address (0b1000|0)
            daq.WriteDigitalPort(1, 0x00);      await Task.Delay(delay); // enable decoder
            daq.WriteDigitalPort(1, 0x08);      await Task.Delay(delay); // rising edge (CLK)

            // ── EnableU10(En_MUX508_Sys = 5) ─────────────────────────────────
            // SetMasterAddLatch(5): WriteDataPortAsync(5)
            daq.WriteDigitalPort(0, 0x05);      await Task.Delay(delay);
            // ActivateDataLatchAsync(MasterCLK = 6, isOutputCLK = true)
            daq.WriteDigitalPort(1, 0x08);      await Task.Delay(delay);
            daq.WriteDigitalPort(1, 0x0E);      await Task.Delay(delay);
            daq.WriteDigitalPort(1, 0x06);      await Task.Delay(delay);
            daq.WriteDigitalPort(1, 0x0E);      await Task.Delay(delay);
            daq.WriteDigitalOutput(1, 3, true); await Task.Delay(delay);
            // ActivateDataLatchAsync(Mux508_Select_n = 3, isOutputCLK = false)
            daq.WriteDigitalPort(1, 0x08);      await Task.Delay(delay); // disable decoder
            daq.WriteDigitalPort(1, 0x0B);      await Task.Delay(delay); // set address (0b1000|3)
            daq.WriteDigitalPort(1, 0x03);      await Task.Delay(delay); // enable decoder (no CLK)

            // ── Read 20 samples from ai7 (0–10 V, timeout = 10 s) ────────────
            double[] samples = new double[20];
            var mean = daq.ReadAnalogInput(7, 0, 10, samples, timeout: 10);

            var std = double.Sqrt(samples.Average(v => double.Pow(v - mean, 2)));
            double result = samples.Where(v => double.Abs(v - mean) <= 3 * std).Average();

            // ── Disable U88 ───────────────────────────────────────────────────
            daq.WriteDigitalOutput(1, 3, true); await Task.Delay(delay);

            Assert.InRange(result, 2.3, 2.7);
        }
    }
}
