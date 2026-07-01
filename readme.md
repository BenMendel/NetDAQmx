# NetDAQmx

A lightweight C# / .NET 10 wrapper around `nicaiu.dll` (the NI-DAQmx ANSI C library). It exposes a clean, typed API for digital I/O, analog I/O, and edge counting without pulling in the full NI-DAQmx .NET SDK.

> **Platform:** x86 only — required by `nicaiu.dll`.

---

## Requirements

| Requirement | Notes |
|---|---|
| [NI-DAQmx driver](https://www.ni.com/en/support/downloads/drivers/download.ni-daq-mx.html) | Installs `nicaiu.dll`. Tested with NI-DAQmx 2024+. |
| .NET 10 SDK | x86 target (`<Platforms>x86</Platforms>`) |
| NI-MAX | Use to verify the device alias (e.g. `Dev1`) before use |

---

## Installation

Add the project reference or NuGet package to your `.csproj`:

```xml
<PackageReference Include="NetDAQmx" Version="2.0.0-*" />
```

Ensure your consuming project also targets **x86**:

```xml
<Platforms>x86</Platforms>
```

---

## Quick start — NI USB-6008 OEM

The USB-6008 OEM is **software-timed only** — no hardware sample clock for AI/AO. All operations below work within that constraint.

### Enumerate connected devices

```csharp
string[] devices = NIDAQ.GetSystemDevices();
// e.g. ["Dev1"] — matches the alias shown in NI-MAX
```

### Construct a device handle

```csharp
var daq = new NIDAQ("Dev1");
```

---

### Analog input (ai0–ai7)

The USB-6008 AI range is ±10 V (RSE, single-ended referred to AGND). Pass the expected voltage window as `minValue`/`maxValue`.

```csharp
// Single-shot read on ai0, 0–5 V window
double voltage = daq.ReadAnalogInput(channel: 0, minValue: 0.0, maxValue: 5.0);
Console.WriteLine($"ai0 = {voltage:F4} V");

// Averaged read — fills a buffer then returns the mean
var buffer = new double[16];
double average = daq.ReadAnalogInput(channel: 1, minValue: 0.0, maxValue: 10.0, buffer: buffer);
Console.WriteLine($"ai1 avg = {average:F4} V  (n={buffer.Length})");

// Async variant
double v = await daq.ReadAnalogInputAsync(channel: 0, minValue: 0.0, maxValue: 5.0);
```

---

### Analog output (ao0–ao1)

USB-6008 AO range is 0–5 V, ~150 S/s, software-timed.

```csharp
// Output 2.5 V on ao0 (default range 0–5 V)
daq.WriteAnalogOutput(channel: 0, value: 2.5);

// Explicit range
daq.WriteAnalogOutput(channel: 1, value: 1.0, minVal: 0.0, maxVal: 5.0);

// Async variant
await daq.WriteAnalogOutputAsync(channel: 0, value: 3.3);
```

---

### Digital output

USB-6008 has 12 configurable DIO lines: **port0** (P0.0–P0.7, 8 lines) and **port1** (P1.0–P1.3, 4 lines).

```csharp
// Drive P0.2 high
daq.WriteDigitalOutput(port: 0, channel: 2, value: true);

// Drive P1.0 low
daq.WriteDigitalOutput(port: 1, channel: 0, value: false);

// Write all 8 lines of port0 at once via bit mask
// 0b10101010 → lines 1,3,5,7 high; lines 0,2,4,6 low
daq.WriteDigitalPort(port: 0, data: 0b10101010);

// Async variants
await daq.WriteDigitalOutputAsync(port: 0, channel: 3, value: true);
await daq.WriteDigitalPortAsync(port: 0, data: 0xFF);
```

---

### Digital input

```csharp
// Read P0.5
bool isHigh = daq.ReadDigitalInput(port: 0, channel: 5);

// Read all 8 lines of port0 as a byte
byte portState = daq.ReadDigitalPort(port: 0);
bool line3High = (portState & (1 << 3)) != 0;

// Async variants
bool state = await daq.ReadDigitalInputAsync(port: 0, channel: 0);
byte snap   = await daq.ReadDigitalPortAsync(port: 0);
```

---

### Counter input (ctr0)

USB-6008 has one 32-bit counter (`ctr0`) that counts rising edges up to 5 MHz. Each call creates a fresh task, so the count resets on every call.

```csharp
// Count rising edges on Dev1/ctr0
uint count = daq.ReadEdgeCount("Dev1/ctr0");
Console.WriteLine($"Edge count: {count}");

// Async
uint n = await daq.ReadEdgeCountAsync("Dev1/ctr0");
```

---

### Device utilities

```csharp
// Returns false for USB-6008 AI/AO (software-timed only)
bool hwTimed = daq.SupportsHardwareTiming();

// Width of port0 — returns 8 for USB-6008
uint lines = daq.GetPortWidth(port: 0);

// Reset to power-on defaults (clears all running tasks)
daq.ResetDevice();
```

---

## Error handling

All driver errors surface as `DaqException`, which carries the numeric DAQmx status code and extended diagnostic text from the driver.

```csharp
try
{
    double v = daq.ReadAnalogInput(channel: 0, minValue: 0.0, maxValue: 5.0);
}
catch (DaqException ex)
{
    Console.WriteLine($"DAQmx error {ex.ErrorCode}: {ex.Message}");
    Console.WriteLine(ex.ExtendedInfo);  // driver-supplied causes & solutions
}
```

---

## Unit testing without hardware

`IDaqDevice` is the testable interface. Implement it with a simulation class so tests run without a physical device:

```csharp
public class SimulatedDaq : IDaqDevice
{
    public string DeviceAlias => "Sim0";
    public bool SupportsHardwareTiming() => false;
    public void ResetDevice() { }
    public uint GetPortWidth(byte port) => 8;

    public double ReadAnalogInput(uint channel, double min, double max,
        double timeout = DaqDefaults.TimeoutSeconds) => (min + max) / 2.0;

    public double ReadAnalogInput(uint channel, double min, double max,
        double[] buffer, double timeout = DaqDefaults.TimeoutSeconds)
    {
        Array.Fill(buffer, (min + max) / 2.0);
        return buffer.Average();
    }

    // ... implement remaining members
}
```

The xUnit integration tests in `NetDaqmx.Tests` skip automatically when no device is present using `xunit.SkippableFact`:

```csharp
Skip.If(NIDAQ.GetSystemDevices().Length == 0, "No NI-DAQ devices connected.");
```

---

## USB-6008 OEM pin reference

| Signal | Channel string | Range | Notes |
|---|---|---|---|
| AI0–AI7 | `Dev1/ai0`–`Dev1/ai7` | ±10 V RSE / ±20 V Diff | 8 SE or 4 differential inputs |
| AO0–AO1 | `Dev1/ao0`–`Dev1/ao1` | 0–5 V | ~150 S/s, software-timed |
| P0.0–P0.7 | port 0, line 0–7 | 3.3/5 V TTL | 8-line bidirectional |
| P1.0–P1.3 | port 1, line 0–3 | 3.3/5 V TTL | 4-line bidirectional |
| PFI 0 / CTR0 | `Dev1/ctr0` | 5 V TTL | 32-bit counter, up to 5 MHz |

> NI USB-6008 does **not** support hardware sample clocking on AI or AO. `SupportsHardwareTiming()` will always return `false` for this device.

---

## Developer notes

- `nicaiu.dll` is installed under `C:\Windows\System32\` by the NI-DAQmx driver.
- Function signatures in `DllWrapper.cs` are verified against `NIDAQmx.h`, which NI installs at:
  - `C:\Program Files (x86)\National Instruments\NI-DAQ\DAQmx ANSI C Dev\include\NIDAQmx.h`
- The library must be compiled as **x86** — `nicaiu.dll` is a 32-bit DLL.

---

## Project structure

```
NetDAQmx/                   # Class library (nicaiu.dll wrapper)
  NIDAQ.cs                  # IDaqDevice implementation — real hardware
  Interfaces/IDaqDevice.cs  # Testable interface
  DllWrapper.cs             # P/Invoke declarations (nicaiu.dll)
  DaqException.cs           # Typed DAQmx error with ErrorCode + ExtendedInfo
  DaqDefaults.cs            # Shared constants (default timeout = 10 s)
  Helpers/DaqTask.cs        # IDisposable task-handle wrapper

NetDaqmx.Tests/             # xUnit integration tests (hardware-optional)
NetDAQmx_TestingPlatform/   # Console app for manual device verification
```
