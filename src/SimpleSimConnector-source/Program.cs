// Copyright (c) 2026 Alex Nord.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// Commercial use is prohibited without written permission.
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace SimpleSimConnector
{
    enum DEFINITIONS
    {
        Identity,
        CoreFlight,
        Weather,
        Radio,
        Fuel,
        Engine,
        Autopilot
    }

    enum REQUESTS
    {
        Identity,
        CoreFlight,
        Weather,
        Radio,
        Fuel,
        Engine,
        Autopilot
    }

    enum EVENTS
    {
        Frame
    }

    enum PMDG777_CLIENT_DATA
    {
        DataArea = 0x504D4447,
        DataDefinition = 0x504D4448,
        DataRequest = 0x504D4449
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct IdentityData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string title;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string atcModel;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string atcType;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct CoreFlightData
    {
        public double latitude;
        public double longitude;
        public double altitudeMeters;
        public double groundSpeedKnots;
        public double headingTrueDegrees;
        public double headingMagneticDegrees;
        public double onGround;
        public double verticalSpeedFeetPerSecond;
        public double pitchDegrees;
        public double bankDegrees;
        public double gForce;
        public double groundElevationMeters;
        public double landingRateMetersPerSecond;
        public double indicatedAirspeedKnots;
        public double trueAirspeedKnots;
        public double barberPoleAirspeedKnots;
        public double parkingBrake;
        public double numFlapPositions;
        public double gearDown;
        public double lightNavigation;
        public double lightBeacon;
        public double lightStrobes;
        public double lightInstruments;
        public double lightLogo;
        public double lightCabin;
        public double cabinAltitudeMeters;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct WeatherData
    {
        public double outsideAirTemperatureCelsius;
        public double ambientPressureInchesHg;
        public double seaLevelPressureMillibars;
        public double visibilityMeters;
        public double windSpeedKnots;
        public double windDirectionDegrees;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct RadioData
    {
        public double com1Available;
        public double com2Available;
        public double nav1Available;
        public double nav2Available;
        public double com1Frequency;
        public double com2Frequency;
        public double nav1Frequency;
        public double nav2Frequency;
        public double transponderCode;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct FuelData
    {
        public double fuelWeightPerGallon;
        public double fuelTankCenterCapacityGallons;
        public double fuelTankCenterLevel;
        public double fuelTankCenter2CapacityGallons;
        public double fuelTankCenter2Level;
        public double fuelTankCenter3CapacityGallons;
        public double fuelTankCenter3Level;
        public double fuelTankLeftMainCapacityGallons;
        public double fuelTankLeftMainLevel;
        public double fuelTankLeftAuxCapacityGallons;
        public double fuelTankLeftAuxLevel;
        public double fuelTankLeftTipCapacityGallons;
        public double fuelTankLeftTipLevel;
        public double fuelTankRightMainCapacityGallons;
        public double fuelTankRightMainLevel;
        public double fuelTankRightAuxCapacityGallons;
        public double fuelTankRightAuxLevel;
        public double fuelTankRightTipCapacityGallons;
        public double fuelTankRightTipLevel;
        public double fuelTankExternal1CapacityGallons;
        public double fuelTankExternal1Level;
        public double fuelTankExternal2CapacityGallons;
        public double fuelTankExternal2Level;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct EngineData
    {
        public double engineType;
        public double itt1DegreesCelsius;
        public double itt2DegreesCelsius;
        public double antiIce1Enabled;
        public double antiIce2Enabled;
        public double exitOpen;
        public double apuPctRpm;
        public double apuSwitch;
        public double apuGeneratorActive;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct AutopilotData
    {
        public double barometerSettingMillibars;
        public double yawDamperEnabled;
        public double flightDirectorEnabled;
        public double autopilotAirspeedHoldKnots;
        public double autopilotMachHoldMach;
        public double autopilotAltitudeHoldFeet;
        public double autopilotHeadingLockDegrees;
        public double autopilotPitchHoldRadians;
        public double autopilotVerticalSpeedHoldFeetPerMinute;
        public double autopilotAltitudeHoldActive;
        public double autopilotHeadingLockActive;
        public double autopilotAirspeedHoldActive;
        public double autopilotMachHoldActive;
        public double autopilotVerticalSpeedHoldActive;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct Pmdg777Data
    {
        public byte elecApuGenSwitchOn;
        public byte elecApuSelector;
        public byte airApuBleedAirSwitchAuto;
        public float mcpIasMach;
        public byte mcpIasBlank;
        public ushort mcpHeading;
        public ushort mcpAltitude;
        public short mcpVertSpeed;
        public byte mcpVertSpeedBlank;
        public byte mcpFdSwitchOnLeft;
        public byte mcpFdSwitchOnRight;
        public byte mcpAtArmSwitchOnLeft;
        public byte mcpAtArmSwitchOnRight;
        public byte mcpAnnunApLeft;
        public byte mcpAnnunApRight;
        public byte mcpAnnunAt;
        public byte mcpAnnunLnav;
        public byte mcpAnnunVnav;
        public byte mcpAnnunFlch;
        public byte mcpAnnunHdgHold;
        public byte mcpAnnunVsFpa;
        public byte mcpAnnunAltHold;
        public byte mcpAnnunLoc;
        public byte mcpAnnunApp;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct SingleValueData
    {
        public double value;
    }

    class ConnectorSettings
    {
        public string BackendUrl = "http://127.0.0.1:5000/api/telemetry";
        public bool LocalApiEnabled = true;
        public int LocalApiPort = 4789;
        public bool WriteLocalTelemetryFile = true;

        public bool WaitForSim = true;
        public bool AutoExitWithSim = true;
        public int AutoExitDelaySeconds = 10;

        public string[] SimProcessNames = new string[]
        {
            "FlightSimulator2024",
            "FlightSimulator"
        };
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0)
            {
                string command = args[0].Trim().ToLowerInvariant();

                try
                {
                    if (command == "--install-autostart")
                    {
                        string changed = MsfsAutostartManager.Install(Application.ExecutablePath);

                        MessageBox.Show(
                            "Autostart installed." +
                            Environment.NewLine + Environment.NewLine +
                            changed,
                            "Simple Sim Connector",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        return;
                    }

                    if (command == "--uninstall-autostart")
                    {
                        string changed = MsfsAutostartManager.Uninstall();

                        MessageBox.Show(
                            "Autostart removed." +
                            Environment.NewLine + Environment.NewLine +
                            changed,
                            "Simple Sim Connector",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Simple Sim Connector",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    return;
                }
            }

            Application.Run(new ConnectorForm());
        }
    }

    public class ConnectorForm : Form
    {
        private const int WM_USER_SIMCONNECT = 0x0402;

        private SimConnect simconnect;

        private Label statusLabel;
        private Label latestLabel;
        private Label configLabel;

        private Button installAutostartButton;
        private Button removeAutostartButton;

        private ConnectorSettings settings;

        private TcpListener localApiServer;
        private CancellationTokenSource localApiCancellation;

        private System.Windows.Forms.Timer simWatcherTimer;
        private DateTime? simMissingSinceUtc;
        private bool hasEverConnectedToSim = false;

        private readonly object latestJsonLock = new object();
        private string latestJson;
        private bool backendConnected = false;
        private double latestFrameRate = double.NaN;
        private double latestSimulationRate = double.NaN;
        private string latestAircraftTitle = "";
        private IdentityData? latestIdentityData;
        private CoreFlightData? latestCoreFlightData;
        private WeatherData? latestWeatherData;
        private RadioData? latestRadioData;
        private FuelData? latestFuelData;
        private EngineData? latestEngineData;
        private AutopilotData? latestAutopilotData;
        private readonly List<string> identityDefinitionNames = new List<string>();
        private readonly List<string> coreFlightDefinitionNames = new List<string>();
        private readonly List<string> weatherDefinitionNames = new List<string>();
        private readonly List<string> radioDefinitionNames = new List<string>();
        private readonly List<string> fuelDefinitionNames = new List<string>();
        private readonly List<string> engineDefinitionNames = new List<string>();
        private readonly List<string> autopilotDefinitionNames = new List<string>();
        private readonly Dictionary<int, string> fenixRequestIdToVarName = new Dictionary<int, string>();
        private readonly Dictionary<int, string> fenixDefinitionIdToVarName = new Dictionary<int, string>();
        private readonly Dictionary<string, double?> fenixLvarValues = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> fenixReadableVarNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> fenixDiscoveredVarSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> fenixDiscoveredVars = new List<string>();
        private readonly Dictionary<string, double?> pmdg777SdkValues = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);
        private IAircraftAdapter activeAircraftAdapter = new GenericAircraftAdapter();
        private AircraftIdentityInfo latestAircraftIdentity = AircraftAdapterFactory.ResolveIdentity("", "", "", "");
        private string fenixCockpitBehaviorPath = "";
        private string fenixPackagePath = "";
        private bool pmdg777ClientDataRequested = false;
        private bool pmdg777ClientDataAvailable = false;

        private const int FenixDefinitionBase = 1000;
        private const int FenixRequestBase = 2000;

        private static readonly SimVarDefinition[] CoreFlightDefinitions =
        {
            new SimVarDefinition("latitude", "PLANE LATITUDE", null, "degrees", "FLOAT64", "degrees", "degrees", "identity", "-90..90"),
            new SimVarDefinition("longitude", "PLANE LONGITUDE", null, "degrees", "FLOAT64", "degrees", "degrees", "identity", "-180..180"),
            new SimVarDefinition("altitudeMeters", "PLANE ALTITUDE", null, "meters", "FLOAT64", "meters", "meters", "identity", "n/a"),
            new SimVarDefinition("groundSpeedKnots", "GROUND VELOCITY", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", "0..150"),
            new SimVarDefinition("headingTrueDegrees", "PLANE HEADING DEGREES TRUE", null, "degrees", "FLOAT64", "degrees", "degrees", "normalize heading", "0..360"),
            new SimVarDefinition("headingMagneticDegrees", "PLANE HEADING DEGREES MAGNETIC", null, "degrees", "FLOAT64", "degrees", "degrees", "normalize heading", "0..360"),
            new SimVarDefinition("onGround", "SIM ON GROUND", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("verticalSpeedFeetPerSecond", "VERTICAL SPEED", null, "feet per second", "FLOAT64", "ft/s", "m/s", "ft/s * 0.3048", "n/a"),
            new SimVarDefinition("pitchDegrees", "PLANE PITCH DEGREES", null, "degrees", "FLOAT64", "degrees", "degrees", "identity", "n/a"),
            new SimVarDefinition("bankDegrees", "PLANE BANK DEGREES", null, "degrees", "FLOAT64", "degrees", "degrees", "identity", "n/a"),
            new SimVarDefinition("gForce", "G FORCE", null, "gforce", "FLOAT64", "g", "g", "identity", "n/a"),
            new SimVarDefinition("groundElevationMeters", "GROUND ALTITUDE", null, "meters", "FLOAT64", "meters", "meters", "identity", "n/a"),
            new SimVarDefinition("landingRateMetersPerSecond", "PLANE TOUCHDOWN NORMAL VELOCITY", null, "meters per second", "FLOAT64", "m/s", "m/s", "identity", "n/a"),
            new SimVarDefinition("indicatedAirspeedKnots", "AIRSPEED INDICATED", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", "0..250"),
            new SimVarDefinition("trueAirspeedKnots", "AIRSPEED TRUE", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", "0..350"),
            new SimVarDefinition("barberPoleAirspeedKnots", "AIRSPEED BARBER POLE", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", "0..400"),
            new SimVarDefinition("parkingBrake", "BRAKE PARKING POSITION", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("numFlapPositions", "TRAILING EDGE FLAPS NUM HANDLE POSITIONS", null, "number", "FLOAT64", "count", "count", "identity", "0..20"),
            new SimVarDefinition("gearDown", "GEAR HANDLE POSITION", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("lightNavigation", "LIGHT NAV", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("lightBeacon", "LIGHT BEACON", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("lightStrobes", "LIGHT STROBE", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("lightInstruments", "LIGHT PANEL", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("lightLogo", "LIGHT LOGO", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("lightCabin", "LIGHT CABIN", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("cabinAltitudeMeters", "PRESSURIZATION CABIN ALTITUDE", null, "meters", "FLOAT64", "meters", "meters", "identity", "n/a")
        };

        private static readonly SimVarDefinition[] WeatherDefinitions =
        {
            new SimVarDefinition("outsideAirTemperatureCelsius", "AMBIENT TEMPERATURE", null, "celsius", "FLOAT64", "celsius", "celsius", "identity", "-100..70"),
            new SimVarDefinition("ambientPressureInchesHg", "AMBIENT PRESSURE", null, "inHg", "FLOAT64", "inHg", "pascal", "inHg * 3386.389", "15000..110000"),
            new SimVarDefinition("seaLevelPressureMillibars", "SEA LEVEL PRESSURE", null, "millibars", "FLOAT64", "millibars", "pascal", "mbar * 100", "80000..110000"),
            new SimVarDefinition("visibilityMeters", "AMBIENT VISIBILITY", null, "meters", "FLOAT64", "meters", "km", "meters / 1000", ">= 0"),
            new SimVarDefinition("windSpeedKnots", "AMBIENT WIND VELOCITY", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", "0..150"),
            new SimVarDefinition("windDirectionDegrees", "AMBIENT WIND DIRECTION", null, "degrees", "FLOAT64", "degrees", "degrees", "normalize heading", "0..360")
        };

        private static readonly SimVarDefinition[] RadioDefinitions =
        {
            new SimVarDefinition("com1Available", "COM AVAILABLE:1", 1, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("com2Available", "COM AVAILABLE:2", 2, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("nav1Available", "NAV AVAILABLE:1", 1, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("nav2Available", "NAV AVAILABLE:2", 2, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("com1Frequency", "COM ACTIVE FREQUENCY:1", 1, "Frequency BCD16", "FLOAT64", "BCD16", "MHz string", "decode BCD16", "118.00..136.99"),
            new SimVarDefinition("com2Frequency", "COM ACTIVE FREQUENCY:2", 2, "Frequency BCD16", "FLOAT64", "BCD16", "MHz string", "decode BCD16", "118.00..136.99"),
            new SimVarDefinition("nav1Frequency", "NAV ACTIVE FREQUENCY:1", 1, "MHz", "FLOAT64", "MHz", "MHz string", "identity format", "108.00..117.95"),
            new SimVarDefinition("nav2Frequency", "NAV ACTIVE FREQUENCY:2", 2, "MHz", "FLOAT64", "MHz", "MHz string", "identity format", "108.00..117.95"),
            new SimVarDefinition("transponderCode", "TRANSPONDER CODE:1", 1, "number", "FLOAT64", "number", "octal string", "format 0000", "0000..7777")
        };

        private static readonly SimVarDefinition[] FuelDefinitions =
        {
            new SimVarDefinition("fuelWeightPerGallon", "FUEL WEIGHT PER GALLON", null, "pounds", "FLOAT64", "lb/gal", "kg factor", "lb * 0.45359237", "4.0..8.5"),
            new SimVarDefinition("fuelTankCenterCapacityGallons", "FUEL TANK CENTER CAPACITY", null, "gallons", "FLOAT64", "gallons", "kg input", "gal * density", "0..300000"),
            new SimVarDefinition("fuelTankCenterLevel", "FUEL TANK CENTER LEVEL", null, "percent over 100", "FLOAT64", "0..1", "percent", "raw*100", "0..100"),
            new SimVarDefinition("fuelTankCenter2CapacityGallons", "FUEL TANK CENTER2 CAPACITY", null, "gallons", "FLOAT64", "gallons", "kg input", "gal * density", "0..300000"),
            new SimVarDefinition("fuelTankCenter2Level", "FUEL TANK CENTER2 LEVEL", null, "percent over 100", "FLOAT64", "0..1", "percent", "raw*100", "0..100"),
            new SimVarDefinition("fuelTankCenter3CapacityGallons", "FUEL TANK CENTER3 CAPACITY", null, "gallons", "FLOAT64", "gallons", "kg input", "gal * density", "0..300000"),
            new SimVarDefinition("fuelTankCenter3Level", "FUEL TANK CENTER3 LEVEL", null, "percent over 100", "FLOAT64", "0..1", "percent", "raw*100", "0..100"),
            new SimVarDefinition("fuelTankLeftMainCapacityGallons", "FUEL TANK LEFT MAIN CAPACITY", null, "gallons", "FLOAT64", "gallons", "kg input", "gal * density", "0..300000"),
            new SimVarDefinition("fuelTankLeftMainLevel", "FUEL TANK LEFT MAIN LEVEL", null, "percent over 100", "FLOAT64", "0..1", "percent", "raw*100", "0..100"),
            new SimVarDefinition("fuelTankLeftAuxCapacityGallons", "FUEL TANK LEFT AUX CAPACITY", null, "gallons", "FLOAT64", "gallons", "kg input", "gal * density", "0..300000"),
            new SimVarDefinition("fuelTankLeftAuxLevel", "FUEL TANK LEFT AUX LEVEL", null, "percent over 100", "FLOAT64", "0..1", "percent", "raw*100", "0..100"),
            new SimVarDefinition("fuelTankLeftTipCapacityGallons", "FUEL TANK LEFT TIP CAPACITY", null, "gallons", "FLOAT64", "gallons", "kg input", "gal * density", "0..300000"),
            new SimVarDefinition("fuelTankLeftTipLevel", "FUEL TANK LEFT TIP LEVEL", null, "percent over 100", "FLOAT64", "0..1", "percent", "raw*100", "0..100"),
            new SimVarDefinition("fuelTankRightMainCapacityGallons", "FUEL TANK RIGHT MAIN CAPACITY", null, "gallons", "FLOAT64", "gallons", "kg input", "gal * density", "0..300000"),
            new SimVarDefinition("fuelTankRightMainLevel", "FUEL TANK RIGHT MAIN LEVEL", null, "percent over 100", "FLOAT64", "0..1", "percent", "raw*100", "0..100"),
            new SimVarDefinition("fuelTankRightAuxCapacityGallons", "FUEL TANK RIGHT AUX CAPACITY", null, "gallons", "FLOAT64", "gallons", "kg input", "gal * density", "0..300000"),
            new SimVarDefinition("fuelTankRightAuxLevel", "FUEL TANK RIGHT AUX LEVEL", null, "percent over 100", "FLOAT64", "0..1", "percent", "raw*100", "0..100"),
            new SimVarDefinition("fuelTankRightTipCapacityGallons", "FUEL TANK RIGHT TIP CAPACITY", null, "gallons", "FLOAT64", "gallons", "kg input", "gal * density", "0..300000"),
            new SimVarDefinition("fuelTankRightTipLevel", "FUEL TANK RIGHT TIP LEVEL", null, "percent over 100", "FLOAT64", "0..1", "percent", "raw*100", "0..100"),
            new SimVarDefinition("fuelTankExternal1CapacityGallons", "FUEL TANK EXTERNAL1 CAPACITY", null, "gallons", "FLOAT64", "gallons", "kg input", "gal * density", "0..300000"),
            new SimVarDefinition("fuelTankExternal1Level", "FUEL TANK EXTERNAL1 LEVEL", null, "percent over 100", "FLOAT64", "0..1", "percent", "raw*100", "0..100"),
            new SimVarDefinition("fuelTankExternal2CapacityGallons", "FUEL TANK EXTERNAL2 CAPACITY", null, "gallons", "FLOAT64", "gallons", "kg input", "gal * density", "0..300000"),
            new SimVarDefinition("fuelTankExternal2Level", "FUEL TANK EXTERNAL2 LEVEL", null, "percent over 100", "FLOAT64", "0..1", "percent", "raw*100", "0..100")
        };

        private static readonly SimVarDefinition[] EngineDefinitions =
        {
            new SimVarDefinition("engineType", "ENGINE TYPE", null, "enum", "FLOAT64", "enum", "enum string", "enum mapping", "known enum"),
            new SimVarDefinition("itt1DegreesCelsius", "TURB ENG ITT:1", 1, "celsius", "FLOAT64", "celsius", "celsius", "identity", "n/a"),
            new SimVarDefinition("itt2DegreesCelsius", "TURB ENG ITT:2", 2, "celsius", "FLOAT64", "celsius", "celsius", "identity", "n/a"),
            new SimVarDefinition("antiIce1Enabled", "ENG ANTI ICE:1", 1, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("antiIce2Enabled", "ENG ANTI ICE:2", 2, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("exitOpen", "EXIT OPEN", null, "percent over 100", "FLOAT64", "0..1", "bool", "> 0", "0..1"),
            new SimVarDefinition("apuPctRpm", "APU PCT RPM", null, "percent over 100", "FLOAT64", "0..100", "status input", "APU status", "0..100"),
            new SimVarDefinition("apuSwitch", "APU SWITCH", null, "bool", "FLOAT64", "bool", "status input", "APU status", "0|1"),
            new SimVarDefinition("apuGeneratorActive", "APU GENERATOR ACTIVE:1", 1, "bool", "FLOAT64", "bool", "status input", "APU status", "0|1")
        };

        private static readonly SimVarDefinition[] AutopilotDefinitions =
        {
            new SimVarDefinition("barometerSettingMillibars", "KOHLSMAN SETTING MB:1", 1, "millibars", "FLOAT64", "millibars", "pascal", "mbar * 100", "80000..110000"),
            new SimVarDefinition("yawDamperEnabled", "AUTOPILOT YAW DAMPER", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("flightDirectorEnabled", "AUTOPILOT FLIGHT DIRECTOR ACTIVE", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("autopilotAirspeedHoldKnots", "AUTOPILOT AIRSPEED HOLD VAR", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", "0..250"),
            new SimVarDefinition("autopilotMachHoldMach", "AUTOPILOT MACH HOLD VAR", null, "mach", "FLOAT64", "mach", "mach", "identity", "0..1.2"),
            new SimVarDefinition("autopilotAltitudeHoldFeet", "AUTOPILOT ALTITUDE LOCK VAR", null, "feet", "FLOAT64", "feet", "meters", "feet * 0.3048", "n/a"),
            new SimVarDefinition("autopilotHeadingLockDegrees", "AUTOPILOT HEADING LOCK DIR", null, "degrees", "FLOAT64", "degrees", "degrees", "normalize heading", "0..360"),
            new SimVarDefinition("autopilotPitchHoldRadians", "AUTOPILOT PITCH HOLD REF", null, "radians", "FLOAT64", "radians", "degrees", "rad * 57.2957795", "n/a"),
            new SimVarDefinition("autopilotVerticalSpeedHoldFeetPerMinute", "AUTOPILOT VERTICAL HOLD VAR", null, "feet per minute", "FLOAT64", "ft/min", "m/s", "ft/min * 0.00508", "n/a"),
            new SimVarDefinition("autopilotAltitudeHoldActive", "AUTOPILOT ALTITUDE LOCK", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("autopilotHeadingLockActive", "AUTOPILOT HEADING LOCK", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("autopilotAirspeedHoldActive", "AUTOPILOT AIRSPEED HOLD", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("autopilotMachHoldActive", "AUTOPILOT MACH HOLD", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("autopilotVerticalSpeedHoldActive", "AUTOPILOT VERTICAL HOLD", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1")
        };

        private static readonly HttpClient http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        private readonly string appDataFolder =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SimpleSimConnector"
            );

        private string ExeFolder
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        private string ConfigPath
        {
            get { return Path.Combine(ExeFolder, "connector.ini"); }
        }

        private string LogPath
        {
            get { return Path.Combine(appDataFolder, "connector.log"); }
        }

        private string TelemetryPath
        {
            get { return Path.Combine(appDataFolder, "telemetry.ndjson"); }
        }

        public ConnectorForm()
        {
            Text = "Simple Sim Connector";
            Width = 760;
            Height = 230;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ShowIcon = true;

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                // Keep default icon if Windows cannot extract the embedded one.
            }

            statusLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 18,
                Width = 700,
                Height = 24,
                Text = "Starting..."
            };

            latestLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 52,
                Width = 700,
                Height = 52,
                Text = "Waiting for telemetry..."
            };

            configLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 112,
                Width = 700,
                Height = 42,
                Text = ""
            };

            installAutostartButton = new Button
            {
                Left = 20,
                Top = 160,
                Width = 190,
                Height = 30,
                Text = "Install MSFS autostart"
            };

            removeAutostartButton = new Button
            {
                Left = 225,
                Top = 160,
                Width = 190,
                Height = 30,
                Text = "Remove MSFS autostart"
            };

            installAutostartButton.Click += InstallAutostartButton_Click;
            removeAutostartButton.Click += RemoveAutostartButton_Click;

            Controls.Add(statusLabel);
            Controls.Add(latestLabel);
            Controls.Add(configLabel);
            Controls.Add(installAutostartButton);
            Controls.Add(removeAutostartButton);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Directory.CreateDirectory(appDataFolder);

            settings = LoadSettings();

            Log("Simple Sim Connector started.");
            Log("Backend URL: " + settings.BackendUrl);
            Log("Local API enabled: " + settings.LocalApiEnabled);
            Log("Local API port: " + settings.LocalApiPort);
            Log("Wait for sim: " + settings.WaitForSim);
            Log("Auto exit with sim: " + settings.AutoExitWithSim);

            configLabel.Text =
                "POST: " + settings.BackendUrl + Environment.NewLine +
                "Local API: " + (settings.LocalApiEnabled
                    ? "http://0.0.0.0:" + settings.LocalApiPort + "/telemetry"
                    : "disabled");

            if (settings.LocalApiEnabled)
            {
                StartLocalApi();
            }

            if (settings.WaitForSim)
            {
                SetStatus("Waiting for Microsoft Flight Simulator 2024...");
                StartSimWatcher();
            }
            else
            {
                SetStatus("Connecting to Microsoft Flight Simulator...");
                ConnectToSim();
            }
        }

        private ConnectorSettings LoadSettings()
        {
            var loaded = new ConnectorSettings();

            if (!File.Exists(ConfigPath))
            {
                string defaultConfig =
                    "# Simple Sim Connector settings" + Environment.NewLine +
                    "backend_url=http://127.0.0.1:5000/api/telemetry" + Environment.NewLine +
                    Environment.NewLine +
                    "local_api_enabled=true" + Environment.NewLine +
                    "local_api_port=4789" + Environment.NewLine +
                    Environment.NewLine +
                    "write_local_telemetry_file=true" + Environment.NewLine +
                    Environment.NewLine +
                    "wait_for_sim=true" + Environment.NewLine +
                    "auto_exit_with_sim=true" + Environment.NewLine +
                    "auto_exit_delay_seconds=10" + Environment.NewLine +
                    "sim_process_names=FlightSimulator2024,FlightSimulator" + Environment.NewLine;

                File.WriteAllText(ConfigPath, defaultConfig);
            }

            foreach (string rawLine in File.ReadAllLines(ConfigPath))
            {
                string line = rawLine.Trim();

                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                int equalsIndex = line.IndexOf('=');

                if (equalsIndex <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, equalsIndex).Trim().ToLowerInvariant();
                string value = line.Substring(equalsIndex + 1).Trim();

                if (key == "backend_url" && value.Length > 0)
                {
                    loaded.BackendUrl = value;
                }
                else if (key == "local_api_enabled")
                {
                    loaded.LocalApiEnabled = ParseBool(value, loaded.LocalApiEnabled);
                }
                else if (key == "local_api_port")
                {
                    int port;
                    if (int.TryParse(value, out port) && port > 0 && port <= 65535)
                    {
                        loaded.LocalApiPort = port;
                    }
                }
                else if (key == "write_local_telemetry_file")
                {
                    loaded.WriteLocalTelemetryFile = ParseBool(value, loaded.WriteLocalTelemetryFile);
                }
                else if (key == "wait_for_sim")
                {
                    loaded.WaitForSim = ParseBool(value, loaded.WaitForSim);
                }
                else if (key == "auto_exit_with_sim")
                {
                    loaded.AutoExitWithSim = ParseBool(value, loaded.AutoExitWithSim);
                }
                else if (key == "auto_exit_delay_seconds")
                {
                    int seconds;
                    if (int.TryParse(value, out seconds) && seconds >= 0 && seconds <= 300)
                    {
                        loaded.AutoExitDelaySeconds = seconds;
                    }
                }
                else if (key == "sim_process_names")
                {
                    string[] parts = value.Split(',');
                    var names = new System.Collections.Generic.List<string>();

                    foreach (string part in parts)
                    {
                        string cleaned = (part ?? "").Trim();

                        if (cleaned.Length > 0)
                        {
                            names.Add(cleaned);
                        }
                    }

                    if (names.Count > 0)
                    {
                        loaded.SimProcessNames = names.ToArray();
                    }
                }
            }

            return loaded;
        }

        private static bool ParseBool(string value, bool fallback)
        {
            string normalized = (value ?? "").Trim().ToLowerInvariant();

            if (normalized == "true" || normalized == "yes" || normalized == "1" || normalized == "on")
            {
                return true;
            }

            if (normalized == "false" || normalized == "no" || normalized == "0" || normalized == "off")
            {
                return false;
            }

            return fallback;
        }

        private void StartSimWatcher()
        {
            simWatcherTimer = new System.Windows.Forms.Timer();
            simWatcherTimer.Interval = 2000;
            simWatcherTimer.Tick += SimWatcherTick;
            simWatcherTimer.Start();

            Log("Sim watcher started.");
            SimWatcherTick(null, EventArgs.Empty);
        }

        private void SimWatcherTick(object sender, EventArgs e)
        {
            bool simRunning = IsSimulatorRunning();

            if (simRunning)
            {
                simMissingSinceUtc = null;

                if (simconnect == null)
                {
                    SetStatus("MSFS detected. Connecting...");
                    ConnectToSim();
                }

                return;
            }

            if (simconnect != null)
            {
                Log("MSFS process no longer detected. Closing SimConnect.");
                SetStatus("MSFS closed. Disconnecting...");

                CloseSimConnect();
            }

            if (settings.AutoExitWithSim && hasEverConnectedToSim)
            {
                if (simMissingSinceUtc == null)
                {
                    simMissingSinceUtc = DateTime.UtcNow;
                    return;
                }

                double missingSeconds = (DateTime.UtcNow - simMissingSinceUtc.Value).TotalSeconds;

                if (missingSeconds >= settings.AutoExitDelaySeconds)
                {
                    Log("MSFS closed. Auto exiting connector.");
                    Close();
                    return;
                }

                SetStatus("MSFS closed. Exiting in " + Math.Ceiling(settings.AutoExitDelaySeconds - missingSeconds) + "s...");
            }
            else
            {
                SetStatus("Waiting for Microsoft Flight Simulator 2024...");
            }
        }

        private bool IsSimulatorRunning()
        {
            try
            {
                Process[] processes = Process.GetProcesses();

                foreach (Process process in processes)
                {
                    string processName = "";

                    try
                    {
                        processName = process.ProcessName;
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (string configuredName in settings.SimProcessNames)
                    {
                        string wanted = (configuredName ?? "").Trim();

                        if (wanted.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            wanted = wanted.Substring(0, wanted.Length - 4);
                        }

                        if (string.Equals(processName, wanted, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Failed to check simulator process: " + ex.Message);
            }

            return false;
        }

        private void StartLocalApi()
        {
            try
            {
                localApiCancellation = new CancellationTokenSource();
                localApiServer = new TcpListener(IPAddress.Any, settings.LocalApiPort);
                localApiServer.Start();

                Log("Local API listening on 0.0.0.0:" + settings.LocalApiPort);

                Task.Run(() => LocalApiLoop(localApiCancellation.Token));
            }
            catch (Exception ex)
            {
                string message = "Failed to start local API: " + ex.Message;
                Log(message);
                SetStatus(message);
            }
        }

        private async Task LocalApiLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client = null;

                try
                {
                    client = await localApiServer.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleLocalApiClient(client));
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log("Local API accept error: " + ex.Message);

                    try
                    {
                        if (client != null)
                        {
                            client.Close();
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void HandleLocalApiClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    NetworkStream stream = client.GetStream();

                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    string firstLine = "";
                    using (StringReader reader = new StringReader(request))
                    {
                        firstLine = reader.ReadLine() ?? "";
                    }

                    string body;
                    string status = "200 OK";
                    string contentType = "application/json";

                    if (firstLine.StartsWith("GET /telemetry ") || firstLine.StartsWith("GET /telemetry?"))
                    {
                        body = GetLatestJsonOrOfflineStatus();
                    }
                    else if (firstLine.StartsWith("GET /health "))
                    {
                        body =
                            "{" +
                                "\"online\":true," +
                                "\"name\":\"simple-sim-connector\"," +
                                "\"source\":\"simconnect-bridge\"" +
                            "}";
                    }
                    else if (firstLine.StartsWith("GET / "))
                    {
                        contentType = "text/plain; charset=utf-8";
                        body =
                            "Simple Sim Connector" + Environment.NewLine +
                            "GET /telemetry" + Environment.NewLine +
                            "GET /health" + Environment.NewLine;
                    }
                    else
                    {
                        status = "404 Not Found";
                        body =
                            "{" +
                                "\"error\":\"not_found\"," +
                                "\"message\":\"Use GET /telemetry or GET /health\"" +
                            "}";
                    }

                    WriteHttpResponse(stream, status, contentType, body);
                }
                catch (Exception ex)
                {
                    Log("Local API client error: " + ex.Message);
                }
            }
        }

        private void WriteHttpResponse(NetworkStream stream, string status, string contentType, string body)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

            string headers =
                "HTTP/1.1 " + status + "\r\n" +
                "Content-Type: " + contentType + "\r\n" +
                "Content-Length: " + bodyBytes.Length + "\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Connection: close\r\n" +
                "\r\n";

            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);

            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(bodyBytes, 0, bodyBytes.Length);
        }

        private string GetLatestJsonOrOfflineStatus()
        {
            lock (latestJsonLock)
            {
                if (!string.IsNullOrWhiteSpace(latestJson))
                {
                    return latestJson;
                }
            }

            return BuildStatusJson(false, "No telemetry received yet");
        }

        private void ConnectToSim()
        {
            try
            {
                simconnect = new SimConnect(
                    "Simple Sim Connector",
                    Handle,
                    WM_USER_SIMCONNECT,
                    null,
                    0
                );

                simconnect.OnRecvOpen += OnSimConnected;
                simconnect.OnRecvQuit += OnSimQuit;
                simconnect.OnRecvException += OnSimException;
                simconnect.OnRecvEventFrame += OnFrameEvent;
                simconnect.OnRecvSimobjectData += OnTelemetryReceived;
                simconnect.OnRecvClientData += OnClientDataReceived;

                Log("SimConnect object created.");
                SetStatus("SimConnect object created. Waiting for MSFS...");
            }
            catch (COMException ex)
            {
                simconnect = null;

                string message = "Could not connect to MSFS yet. " + ex.Message;
                Log(message);
                SetStatus("Waiting for SimConnect...");
            }
            catch (Exception ex)
            {
                simconnect = null;

                string message = "Unexpected startup error: " + ex.Message;
                Log(message);
                SetStatus(message);

                SendStatusPayload(false, message);
            }
        }

        private void OnSimConnected(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            hasEverConnectedToSim = true;

            Log("Connected to MSFS.");
            SetStatus("Connected to MSFS. Requesting telemetry...");
            identityDefinitionNames.Clear();
            coreFlightDefinitionNames.Clear();
            weatherDefinitionNames.Clear();
            radioDefinitionNames.Clear();
            fuelDefinitionNames.Clear();
            engineDefinitionNames.Clear();
            autopilotDefinitionNames.Clear();
            latestAircraftTitle = "";
            latestIdentityData = null;
            latestAircraftIdentity = AircraftAdapterFactory.ResolveIdentity("", "", "", "");
            activeAircraftAdapter = new GenericAircraftAdapter();
            latestCoreFlightData = null;
            latestWeatherData = null;
            latestRadioData = null;
            latestFuelData = null;
            latestEngineData = null;
            latestAutopilotData = null;
            fenixRequestIdToVarName.Clear();
            fenixDefinitionIdToVarName.Clear();
            fenixLvarValues.Clear();
            fenixReadableVarNames.Clear();
            fenixDiscoveredVarSet.Clear();
            fenixDiscoveredVars.Clear();
            pmdg777SdkValues.Clear();
            pmdg777ClientDataRequested = false;
            pmdg777ClientDataAvailable = false;
            fenixCockpitBehaviorPath = "";
            fenixPackagePath = "";

            TelemetryBridgeCatalog.ValidateStructOrder<IdentityData>(TelemetryBridgeCatalog.IdentityDefinitions);
            TelemetryBridgeCatalog.ValidateStructOrder<CoreFlightData>(CoreFlightDefinitions);
            TelemetryBridgeCatalog.ValidateStructOrder<WeatherData>(WeatherDefinitions);
            TelemetryBridgeCatalog.ValidateStructOrder<RadioData>(RadioDefinitions);
            TelemetryBridgeCatalog.ValidateStructOrder<FuelData>(FuelDefinitions);
            TelemetryBridgeCatalog.ValidateStructOrder<EngineData>(EngineDefinitions);
            TelemetryBridgeCatalog.ValidateStructOrder<AutopilotData>(AutopilotDefinitions);

            foreach (SimVarDefinition definition in TelemetryBridgeCatalog.IdentityDefinitions)
            {
                AddDefinition(DEFINITIONS.Identity, identityDefinitionNames, definition);
            }

            simconnect.RegisterDataDefineStruct<IdentityData>(DEFINITIONS.Identity);
            RegisterDefinitionGroup(DEFINITIONS.CoreFlight, CoreFlightDefinitions, coreFlightDefinitionNames, typeof(CoreFlightData));
            RegisterDefinitionGroup(DEFINITIONS.Weather, WeatherDefinitions, weatherDefinitionNames, typeof(WeatherData));
            RegisterDefinitionGroup(DEFINITIONS.Radio, RadioDefinitions, radioDefinitionNames, typeof(RadioData));
            RegisterDefinitionGroup(DEFINITIONS.Fuel, FuelDefinitions, fuelDefinitionNames, typeof(FuelData));
            RegisterDefinitionGroup(DEFINITIONS.Engine, EngineDefinitions, engineDefinitionNames, typeof(EngineData));
            RegisterDefinitionGroup(DEFINITIONS.Autopilot, AutopilotDefinitions, autopilotDefinitionNames, typeof(AutopilotData));
            simconnect.SubscribeToSystemEvent(EVENTS.Frame, "Frame");

            simconnect.RequestDataOnSimObject(
                REQUESTS.Identity,
                DEFINITIONS.Identity,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.ONCE,
                SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                0,
                0,
                0
            );

            RequestDefinitionGroup(REQUESTS.CoreFlight, DEFINITIONS.CoreFlight, SIMCONNECT_PERIOD.SECOND);
            RequestDefinitionGroup(REQUESTS.Weather, DEFINITIONS.Weather, SIMCONNECT_PERIOD.SECOND);
            RequestDefinitionGroup(REQUESTS.Radio, DEFINITIONS.Radio, SIMCONNECT_PERIOD.SECOND);
            RequestDefinitionGroup(REQUESTS.Fuel, DEFINITIONS.Fuel, SIMCONNECT_PERIOD.SECOND);
            RequestDefinitionGroup(REQUESTS.Engine, DEFINITIONS.Engine, SIMCONNECT_PERIOD.SECOND);
            RequestDefinitionGroup(REQUESTS.Autopilot, DEFINITIONS.Autopilot, SIMCONNECT_PERIOD.SECOND);

            Log("Telemetry request started.");
            SetStatus("Connected. Telemetry request started.");
        }

        private void AddDefinition(DEFINITIONS target, List<string> definitionNames, SimVarDefinition definition)
        {
            definitionNames.Add(definition.SimVarName);

            simconnect.AddToDataDefinition(
                target,
                definition.SimVarName,
                definition.SimConnectUnit,
                definition.IsString ? SIMCONNECT_DATATYPE.STRING256 : SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED
            );
        }

        private void RegisterDefinitionGroup(
            DEFINITIONS target,
            SimVarDefinition[] definitions,
            List<string> definitionNames,
            Type structType)
        {
            foreach (SimVarDefinition definition in definitions)
            {
                AddDefinition(target, definitionNames, definition);
            }

            if (structType == typeof(CoreFlightData))
            {
                simconnect.RegisterDataDefineStruct<CoreFlightData>(target);
            }
            else if (structType == typeof(WeatherData))
            {
                simconnect.RegisterDataDefineStruct<WeatherData>(target);
            }
            else if (structType == typeof(RadioData))
            {
                simconnect.RegisterDataDefineStruct<RadioData>(target);
            }
            else if (structType == typeof(FuelData))
            {
                simconnect.RegisterDataDefineStruct<FuelData>(target);
            }
            else if (structType == typeof(EngineData))
            {
                simconnect.RegisterDataDefineStruct<EngineData>(target);
            }
            else if (structType == typeof(AutopilotData))
            {
                simconnect.RegisterDataDefineStruct<AutopilotData>(target);
            }
            else
            {
                throw new InvalidOperationException("Unsupported SimConnect struct type: " + structType.FullName);
            }
        }

        private void RequestDefinitionGroup(REQUESTS request, DEFINITIONS definition, SIMCONNECT_PERIOD period)
        {
            simconnect.RequestDataOnSimObject(
                request,
                definition,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                period,
                SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                0,
                0,
                0
            );
        }

        private void OnFrameEvent(SimConnect sender, SIMCONNECT_RECV_EVENT_FRAME data)
        {
            latestFrameRate = data.fFrameRate;
            latestSimulationRate = data.fSimSpeed;
        }

        private async void OnTelemetryReceived(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            int rawRequestId = unchecked((int)data.dwRequestID);

            try
            {
                string fenixVarName;
                if (fenixRequestIdToVarName.TryGetValue(rawRequestId, out fenixVarName))
                {
                    var lvarValue = (SingleValueData)data.dwData[0];
                    fenixLvarValues[fenixVarName] = lvarValue.value;
                    if (TelemetryMath.IsFinite(lvarValue.value))
                    {
                        fenixReadableVarNames.Add(fenixVarName);
                    }
                    return;
                }

                REQUESTS request = (REQUESTS)data.dwRequestID;

                if (request == REQUESTS.Identity)
                {
                    var identity = (IdentityData)data.dwData[0];
                    latestIdentityData = identity;
                    latestAircraftTitle = Clean(identity.title);
                    latestAircraftIdentity = BuildAircraftIdentity(identity);
                    activeAircraftAdapter = AircraftAdapterFactory.ResolveAdapter(latestAircraftIdentity);
                    EnsureCustomLvarRequests();
                    return;
                }

                if (request == REQUESTS.CoreFlight)
                {
                    latestCoreFlightData = (CoreFlightData)data.dwData[0];
                }
                else if (request == REQUESTS.Weather)
                {
                    latestWeatherData = (WeatherData)data.dwData[0];
                    return;
                }
                else if (request == REQUESTS.Radio)
                {
                    latestRadioData = (RadioData)data.dwData[0];
                    return;
                }
                else if (request == REQUESTS.Fuel)
                {
                    latestFuelData = (FuelData)data.dwData[0];
                    return;
                }
                else if (request == REQUESTS.Engine)
                {
                    latestEngineData = (EngineData)data.dwData[0];
                    return;
                }
                else if (request == REQUESTS.Autopilot)
                {
                    latestAutopilotData = (AutopilotData)data.dwData[0];
                    return;
                }
                else
                {
                    return;
                }

                CoreFlightData coreFlight = latestCoreFlightData.Value;
                WeatherData weather = latestWeatherData ?? CreateUnavailableWeatherData();
                RadioData radios = latestRadioData ?? CreateUnavailableRadioData();
                FuelData fuel = latestFuelData ?? CreateUnavailableFuelData();
                EngineData engine = latestEngineData ?? CreateUnavailableEngineData();
                AutopilotData autopilot = latestAutopilotData ?? CreateUnavailableAutopilotData();

                string json = BuildBackendJson(
                    coreFlight,
                    weather,
                    radios,
                    fuel,
                    engine,
                    autopilot,
                    latestIdentityData ?? new IdentityData(),
                    connected: true,
                    lastError: null
                );

                lock (latestJsonLock)
                {
                    latestJson = json;
                }

                if (settings.WriteLocalTelemetryFile)
                {
                    AppendTelemetry(json);
                }

                UpdateLatestLabel(coreFlight);

                await PostToBackend(json);
            }
            catch (Exception ex)
            {
                string message = "Telemetry handling error: " + ex.Message;
                Log(message);
                SetStatus(message);

                SendStatusPayload(false, message);
            }
        }

        private async Task PostToBackend(string json)
        {
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await http.PostAsync(settings.BackendUrl, content);
                backendConnected = response.IsSuccessStatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    Log("Backend returned HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                backendConnected = false;
                Log("Backend upload failed: " + ex.Message);
            }
        }

        private void OnClientDataReceived(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA data)
        {
            try
            {
                if ((PMDG777_CLIENT_DATA)data.dwRequestID != PMDG777_CLIENT_DATA.DataRequest)
                {
                    return;
                }

                var pmdgData = (Pmdg777Data)data.dwData[0];
                pmdg777ClientDataAvailable = true;
                UpdatePmdg777SdkValues(pmdgData);
            }
            catch (Exception ex)
            {
                Log("PMDG 777 client data error: " + ex.Message);
            }
        }

        private async void SendStatusPayload(bool connected, string lastError)
        {
            string json = BuildStatusJson(connected, lastError);

            lock (latestJsonLock)
            {
                latestJson = json;
            }

            if (settings != null && settings.WriteLocalTelemetryFile)
            {
                AppendTelemetry(json);
            }

            if (settings != null)
            {
                await PostToBackend(json);
            }
        }

        private void OnSimQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Log("MSFS quit.");
            SetStatus("MSFS quit. Connector disconnected.");

            SendStatusPayload(false, "MSFS quit");

            CloseSimConnect();
        }

        private void OnSimException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            string message = "SimConnect exception: " + data.dwException;
            if (data.dwIndex > 0)
            {
                string definitionName = FindDefinitionName((int)data.dwIndex);
                message += definitionName != null
                    ? " at telemetry definition #" + data.dwIndex + " (" + definitionName + ")"
                    : " at telemetry definition #" + data.dwIndex;
            }

            if (data.dwSendID > 0)
            {
                message += " sendId=" + data.dwSendID;
            }

            Log(message);
            SetStatus(message);

            SendStatusPayload(false, message);
        }

        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == WM_USER_SIMCONNECT)
            {
                try
                {
                    simconnect?.ReceiveMessage();
                }
                catch (Exception ex)
                {
                    Log("SimConnect receive error: " + ex.Message);
                    CloseSimConnect();
                }
            }
            else
            {
                base.DefWndProc(ref m);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Log("Connector closed.");

            if (simWatcherTimer != null)
            {
                simWatcherTimer.Stop();
                simWatcherTimer.Dispose();
                simWatcherTimer = null;
            }

            StopLocalApi();
            CloseSimConnect();

            base.OnClosed(e);
        }

        private void StopLocalApi()
        {
            try
            {
                if (localApiCancellation != null)
                {
                    localApiCancellation.Cancel();
                }

                if (localApiServer != null)
                {
                    localApiServer.Stop();
                }
            }
            catch
            {
            }
        }

        private void CloseSimConnect()
        {
            try
            {
                simconnect?.Dispose();
            }
            catch
            {
            }

            simconnect = null;
        }

        private AircraftIdentityInfo BuildAircraftIdentity(IdentityData identityData)
        {
            string provisionalPackagePath = ResolveAircraftPackagePath(identityData);
            AircraftIdentityInfo identity = AircraftAdapterFactory.ResolveIdentity(
                Clean(identityData.title),
                Clean(identityData.atcModel),
                Clean(identityData.atcType),
                provisionalPackagePath);

            if (string.IsNullOrWhiteSpace(identity.PackagePath))
            {
                identity.PackagePath = provisionalPackagePath;
            }

            latestAircraftIdentity = identity;
            return identity;
        }

        private string ResolveAircraftPackagePath(IdentityData identityData)
        {
            AircraftIdentityInfo baseIdentity = AircraftAdapterFactory.ResolveIdentity(
                Clean(identityData.title),
                Clean(identityData.atcModel),
                Clean(identityData.atcType),
                "");

            string appDataPackagesRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft Flight Simulator 2024",
                "Packages",
                "Community");

            if (string.Equals(baseIdentity.DetectedFamily, "IniBuilds A350", StringComparison.OrdinalIgnoreCase))
            {
                string a350Package = Path.Combine(appDataPackagesRoot, "inibuilds-aircraft-a350");
                if (Directory.Exists(a350Package))
                {
                    return a350Package;
                }

                return "";
            }

            if (string.Equals(baseIdentity.DetectedFamily, "IniBuilds A340", StringComparison.OrdinalIgnoreCase))
            {
                string a340Package = Path.Combine(appDataPackagesRoot, "inibuilds-aircraft-a340");
                if (Directory.Exists(a340Package))
                {
                    return a340Package;
                }

                string legacyA340Package = Path.Combine(appDataPackagesRoot, "ini-builds-airbus-a340-v1.0.7-2024");
                if (Directory.Exists(legacyA340Package))
                {
                    return legacyA340Package;
                }

                return "";
            }

            if (!string.Equals(baseIdentity.DetectedFamily, "Fenix A32x", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            string preferredPackage = "";
            if (string.Equals(baseIdentity.DetectedVariant, "A320", StringComparison.OrdinalIgnoreCase))
            {
                preferredPackage = Path.Combine(appDataPackagesRoot, "fnx-aircraft-320");
            }
            else if (string.Equals(baseIdentity.DetectedVariant, "A319", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(baseIdentity.DetectedVariant, "A321", StringComparison.OrdinalIgnoreCase))
            {
                preferredPackage = Path.Combine(appDataPackagesRoot, "fnx-aircraft-319-321");
            }

            if (preferredPackage.Length > 0 && Directory.Exists(preferredPackage))
            {
                return preferredPackage;
            }

            string fallback320 = Path.Combine(appDataPackagesRoot, "fnx-aircraft-320");
            if (Directory.Exists(fallback320))
            {
                return fallback320;
            }

            string fallback319321 = Path.Combine(appDataPackagesRoot, "fnx-aircraft-319-321");
            if (Directory.Exists(fallback319321))
            {
                return fallback319321;
            }

            return "";
        }

        private void EnsureCustomLvarRequests()
        {
            if (simconnect == null)
            {
                return;
            }

            fenixPackagePath = latestAircraftIdentity != null ? Clean(latestAircraftIdentity.PackagePath) : "";

            if (activeAircraftAdapter is Pmdg777Adapter)
            {
                EnsurePmdg777SdkRequest();
                return;
            }

            if (fenixRequestIdToVarName.Count > 0)
            {
                return;
            }

            string adapterName = activeAircraftAdapter != null ? activeAircraftAdapter.Name : "GenericAircraftAdapter";
            string discoveryLabel;
            FenixVariableDiscoveryResult discovery;
            IList<string> requestedVariables;

            if (activeAircraftAdapter is FenixA32xAdapter)
            {
                fenixCockpitBehaviorPath = FindFenixCockpitBehaviorPath(latestAircraftIdentity);
                discovery = FenixVariableDiscovery.DiscoverFromXml(fenixCockpitBehaviorPath);
                requestedVariables = FenixA32xAdapter.GetRequestedVariables(discovery.CandidateVariableSet);
                discoveryLabel = "Fenix";
            }
            else if (activeAircraftAdapter is IniBuildsA350Adapter)
            {
                fenixCockpitBehaviorPath = FindIniBuildsA350BehaviorPath(latestAircraftIdentity);
                discovery = IniBuildsVariableDiscovery.DiscoverLvarsFromBehaviorXml(
                    fenixCockpitBehaviorPath,
                    IniBuildsA350Adapter.GetDiscoveryKeywords());
                requestedVariables = IniBuildsA350Adapter.GetRequestedVariables(discovery.CandidateVariableSet);
                discoveryLabel = "IniBuilds A350";
            }
            else if (activeAircraftAdapter is IniBuildsA340Adapter)
            {
                fenixCockpitBehaviorPath = FindIniBuildsA340BehaviorPath(latestAircraftIdentity);
                discovery = IniBuildsVariableDiscovery.DiscoverLvarsFromBehaviorXml(
                    fenixCockpitBehaviorPath,
                    IniBuildsA340Adapter.GetDiscoveryKeywords());
                requestedVariables = IniBuildsA340Adapter.GetRequestedVariables(discovery.CandidateVariableSet);
                discoveryLabel = "IniBuilds A340";
            }
            else
            {
                return;
            }

            fenixDiscoveredVars.Clear();
            fenixDiscoveredVarSet.Clear();

            foreach (string variable in discovery.CandidateVariables)
            {
                fenixDiscoveredVars.Add(variable);
                fenixDiscoveredVarSet.Add(variable);
            }

            Log(adapterName + " behavior path: " + (discovery.CockpitBehaviorPath ?? ""));
            if (fenixDiscoveredVars.Count > 0)
            {
                Log(discoveryLabel + " candidate variable list (" + fenixDiscoveredVars.Count.ToString(CultureInfo.InvariantCulture) + "): " + string.Join(", ", fenixDiscoveredVars.ToArray()));
            }
            else
            {
                Log(discoveryLabel + " candidate variable list is empty.");
            }

            for (int i = 0; i < requestedVariables.Count; i++)
            {
                RegisterFenixLvarDefinition(requestedVariables[i], i);
            }

            Log(discoveryLabel + " requested LVars (" + requestedVariables.Count.ToString(CultureInfo.InvariantCulture) + "): " + string.Join(", ", requestedVariables));
        }

        private void EnsurePmdg777SdkRequest()
        {
            if (simconnect == null || pmdg777ClientDataRequested)
            {
                return;
            }

            simconnect.MapClientDataNameToID("PMDG_777X_Data", PMDG777_CLIENT_DATA.DataArea);

            AddPmdg777ClientDataField(40, 1);
            AddPmdg777ClientDataField(41, 1);
            AddPmdg777ClientDataField(194, 1);
            AddPmdg777ClientDataField(308, 4);
            AddPmdg777ClientDataField(312, 1);
            AddPmdg777ClientDataField(314, 2);
            AddPmdg777ClientDataField(316, 2);
            AddPmdg777ClientDataField(318, 2);
            AddPmdg777ClientDataField(354, 1);
            AddPmdg777ClientDataField(325, 1);
            AddPmdg777ClientDataField(326, 1);
            AddPmdg777ClientDataField(327, 1);
            AddPmdg777ClientDataField(328, 1);
            AddPmdg777ClientDataField(356, 1);
            AddPmdg777ClientDataField(357, 1);
            AddPmdg777ClientDataField(358, 1);
            AddPmdg777ClientDataField(359, 1);
            AddPmdg777ClientDataField(360, 1);
            AddPmdg777ClientDataField(361, 1);
            AddPmdg777ClientDataField(362, 1);
            AddPmdg777ClientDataField(363, 1);
            AddPmdg777ClientDataField(364, 1);
            AddPmdg777ClientDataField(365, 1);
            AddPmdg777ClientDataField(366, 1);

            simconnect.RegisterDataDefineStruct<Pmdg777Data>(PMDG777_CLIENT_DATA.DataDefinition);
            simconnect.RequestClientData(
                PMDG777_CLIENT_DATA.DataArea,
                PMDG777_CLIENT_DATA.DataRequest,
                PMDG777_CLIENT_DATA.DataDefinition,
                SIMCONNECT_CLIENT_DATA_PERIOD.VISUAL_FRAME,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0);

            pmdg777ClientDataRequested = true;
            Log("PMDG 777 SDK client data request registered.");
        }

        private void AddPmdg777ClientDataField(uint offset, uint size)
        {
            simconnect.AddToClientDataDefinition(
                PMDG777_CLIENT_DATA.DataDefinition,
                offset,
                size,
                0,
                SimConnect.SIMCONNECT_UNUSED);
        }

        private string FindFenixCockpitBehaviorPath(AircraftIdentityInfo identity)
        {
            var candidates = new List<string>();

            if (identity != null && !string.IsNullOrWhiteSpace(identity.PackagePath))
            {
                candidates.Add(Path.Combine(
                    identity.PackagePath,
                    "SimObjects",
                    "Airplanes",
                    "FNX_32X",
                    "attachments",
                    "fnx",
                    "Part_Interior_Cockpit",
                    "model",
                    "Cockpit_Behavior.xml"));
            }

            string appDataPackagesRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft Flight Simulator 2024",
                "Packages",
                "Community");

            candidates.Add(Path.Combine(appDataPackagesRoot, "fnx-aircraft-320", "SimObjects", "Airplanes", "FNX_32X", "attachments", "fnx", "Part_Interior_Cockpit", "model", "Cockpit_Behavior.xml"));
            candidates.Add(Path.Combine(appDataPackagesRoot, "fnx-aircraft-319-321", "SimObjects", "Airplanes", "FNX_32X", "attachments", "fnx", "Part_Interior_Cockpit", "model", "Cockpit_Behavior.xml"));

            for (int i = 0; i < candidates.Count; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return "";
        }

        private string FindIniBuildsA350BehaviorPath(AircraftIdentityInfo identity)
        {
            var candidates = new List<string>();

            if (identity != null && !string.IsNullOrWhiteSpace(identity.PackagePath))
            {
                candidates.Add(Path.Combine(
                    identity.PackagePath,
                    "SimObjects",
                    "Airplanes",
                    "A350",
                    "attachments",
                    "inibuilds",
                    "Function_Interior_A350",
                    "model",
                    "A350_Interior.behavior.xml"));
            }

            string appDataPackagesRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft Flight Simulator 2024",
                "Packages",
                "Community");

            candidates.Add(Path.Combine(
                appDataPackagesRoot,
                "inibuilds-aircraft-a350",
                "SimObjects",
                "Airplanes",
                "A350",
                "attachments",
                "inibuilds",
                "Function_Interior_A350",
                "model",
                "A350_Interior.behavior.xml"));

            for (int i = 0; i < candidates.Count; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return "";
        }

        private string FindIniBuildsA340BehaviorPath(AircraftIdentityInfo identity)
        {
            var candidates = new List<string>();

            if (identity != null && !string.IsNullOrWhiteSpace(identity.PackagePath))
            {
                candidates.Add(Path.Combine(
                    identity.PackagePath,
                    "SimObjects",
                    "Airplanes",
                    "inibuilds-a340",
                    "attachments",
                    "inibuilds",
                    "Function_A343_Interior_EIS1",
                    "model",
                    "A343_Interior_EIS1.behavior.xml"));
            }

            string appDataPackagesRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft Flight Simulator 2024",
                "Packages",
                "Community");

            candidates.Add(Path.Combine(
                appDataPackagesRoot,
                "inibuilds-aircraft-a340",
                "SimObjects",
                "Airplanes",
                "inibuilds-a340",
                "attachments",
                "inibuilds",
                "Function_A343_Interior_EIS1",
                "model",
                "A343_Interior_EIS1.behavior.xml"));

            candidates.Add(Path.Combine(
                appDataPackagesRoot,
                "ini-builds-airbus-a340-v1.0.7-2024",
                "SimObjects",
                "Airplanes",
                "inibuilds-a340",
                "attachments",
                "inibuilds",
                "Function_A343_Interior_EIS1",
                "model",
                "A343_Interior_EIS1.behavior.xml"));

            for (int i = 0; i < candidates.Count; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return "";
        }

        private void RegisterFenixLvarDefinition(string varName, int index)
        {
            int definitionId = FenixDefinitionBase + index;
            int requestId = FenixRequestBase + index;

            fenixDefinitionIdToVarName[definitionId] = varName;
            fenixRequestIdToVarName[requestId] = varName;

            simconnect.AddToDataDefinition(
                (DEFINITIONS)definitionId,
                "L:" + varName,
                "number",
                SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.RegisterDataDefineStruct<SingleValueData>((DEFINITIONS)definitionId);
            simconnect.RequestDataOnSimObject(
                (REQUESTS)requestId,
                (DEFINITIONS)definitionId,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.SECOND,
                SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                0,
                0,
                0);
        }

        private void UpdatePmdg777SdkValues(Pmdg777Data data)
        {
            pmdg777SdkValues["ELEC_APUGen_Sw_ON"] = data.elecApuGenSwitchOn;
            pmdg777SdkValues["ELEC_APU_Selector"] = data.elecApuSelector;
            pmdg777SdkValues["AIR_APUBleedAir_Sw_AUTO"] = data.airApuBleedAirSwitchAuto;
            pmdg777SdkValues["MCP_IASMach"] = data.mcpIasMach;
            pmdg777SdkValues["MCP_IASBlank"] = data.mcpIasBlank;
            pmdg777SdkValues["MCP_Heading"] = data.mcpHeading;
            pmdg777SdkValues["MCP_Altitude"] = data.mcpAltitude;
            pmdg777SdkValues["MCP_VertSpeed"] = data.mcpVertSpeed;
            pmdg777SdkValues["MCP_VertSpeedBlank"] = data.mcpVertSpeedBlank;
            pmdg777SdkValues["MCP_FD_Sw_On_L"] = data.mcpFdSwitchOnLeft;
            pmdg777SdkValues["MCP_FD_Sw_On_R"] = data.mcpFdSwitchOnRight;
            pmdg777SdkValues["MCP_ATArm_Sw_On_L"] = data.mcpAtArmSwitchOnLeft;
            pmdg777SdkValues["MCP_ATArm_Sw_On_R"] = data.mcpAtArmSwitchOnRight;
            pmdg777SdkValues["MCP_annunAP_L"] = data.mcpAnnunApLeft;
            pmdg777SdkValues["MCP_annunAP_R"] = data.mcpAnnunApRight;
            pmdg777SdkValues["MCP_annunAT"] = data.mcpAnnunAt;
            pmdg777SdkValues["MCP_annunLNAV"] = data.mcpAnnunLnav;
            pmdg777SdkValues["MCP_annunVNAV"] = data.mcpAnnunVnav;
            pmdg777SdkValues["MCP_annunFLCH"] = data.mcpAnnunFlch;
            pmdg777SdkValues["MCP_annunHDG_HOLD"] = data.mcpAnnunHdgHold;
            pmdg777SdkValues["MCP_annunVS_FPA"] = data.mcpAnnunVsFpa;
            pmdg777SdkValues["MCP_annunALT_HOLD"] = data.mcpAnnunAltHold;
            pmdg777SdkValues["MCP_annunLOC"] = data.mcpAnnunLoc;
            pmdg777SdkValues["MCP_annunAPP"] = data.mcpAnnunApp;
        }

        private string BuildStatusJson(bool connectedToSimulator, string lastError)
        {
            var sb = new StringBuilder(2048);
            sb.Append("{");
            sb.Append("\"online\":").Append(Bool(connectedToSimulator)).Append(",");
            sb.Append("\"connected\":").Append(Bool(connectedToSimulator)).Append(",");
            sb.Append("\"latitude\":null,");
            sb.Append("\"longitude\":null,");
            sb.Append("\"altitude\":null,");
            sb.Append("\"groundspeed\":null,");
            sb.Append("\"heading\":null,");
            sb.Append("\"callsign\":\"SIMCONNECT\",");
            sb.Append("\"flight_plan\":{\"departure\":null,\"arrival\":null,\"aircraft_short\":\"UNKNOWN\"},");
            sb.Append("\"source\":\"simconnect-bridge\",");
            sb.Append("\"last_error\":").Append(JsonStringOrNull(lastError)).Append(",");
            sb.Append("\"Connected to Simulator\":").Append(Bool(connectedToSimulator)).Append(",");
            sb.Append("\"Connected to Backend\":").Append(Bool(backendConnected)).Append(",");
            sb.Append("\"simulator\":\"Microsoft Flight Simulator\",");
            sb.Append("\"aircraftType\":\"UNKNOWN\",");
            sb.Append("\"aircraftPath\":null,");
            sb.Append("\"fps\":").Append(Num(latestFrameRate)).Append(",");
            sb.Append("\"simulationRate\":").Append(Num(latestSimulationRate)).Append(",");
            sb.Append("\"position\":{\"latitude\":null,\"longitude\":null},");
            sb.Append("\"headingTrueDegrees\":null,");
            sb.Append("\"headingMagneticDegrees\":null,");
            sb.Append("\"gForce\":null,");
            sb.Append("\"altitudeFeet\":null,");
            sb.Append("\"altitudeMeters\":null,");
            sb.Append("\"pitchDegrees\":null,");
            sb.Append("\"bankDegrees\":null,");
            sb.Append("\"groundElevationFeet\":null,");
            sb.Append("\"groundElevationMeters\":null,");
            sb.Append("\"landingRateFeetPerMinute\":null,");
            sb.Append("\"landingRateMetersPerSecond\":null,");
            sb.Append("\"onGround\":null,");
            sb.Append("\"indicatedAirspeedKnots\":null,");
            sb.Append("\"indicatedAirspeedMetersPerSecond\":null,");
            sb.Append("\"trueAirspeedKnots\":null,");
            sb.Append("\"trueAirspeedMetersPerSecond\":null,");
            sb.Append("\"barberPoleAirspeedKnots\":null,");
            sb.Append("\"barberPoleAirspeedMetersPerSecond\":null,");
            sb.Append("\"groundSpeedKnots\":null,");
            sb.Append("\"groundSpeedMetersPerSecond\":null,");
            sb.Append("\"verticalSpeedFeetPerMinute\":null,");
            sb.Append("\"verticalSpeedMetersPerSecond\":null,");
            sb.Append("\"parkingBrake\":null,");
            sb.Append("\"numFlapPositions\":null,");
            sb.Append("\"gearDown\":null,");
            sb.Append("\"doorsOpen\":null,");
            sb.Append("\"lights\":{\"navigation\":null,\"beacon\":null,\"strobes\":null,\"instruments\":null,\"logo\":null,\"cabin\":null},");
            sb.Append("\"com1\":null,");
            sb.Append("\"com2\":null,");
            sb.Append("\"nav1\":null,");
            sb.Append("\"nav2\":null,");
            sb.Append("\"transponder\":null,");
            sb.Append("\"engineType\":null,");
            sb.Append("\"engines\":[{\"ittDegreesCelsius\":null,\"antiIce\":{\"antiIceEnabled\":null}},{\"ittDegreesCelsius\":null,\"antiIce\":{\"antiIceEnabled\":null}}],");
            sb.Append("\"fuelTanks\":[");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_CENTER\",\"capacityKgs\":null,\"percentageFilled\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_CENTER_2\",\"capacityKgs\":null,\"percentageFilled\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_CENTER_3\",\"capacityKgs\":null,\"percentageFilled\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_LEFT_MAIN\",\"capacityKgs\":null,\"percentageFilled\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_LEFT_AUX\",\"capacityKgs\":null,\"percentageFilled\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_LEFT_TIP\",\"capacityKgs\":null,\"percentageFilled\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_RIGHT_MAIN\",\"capacityKgs\":null,\"percentageFilled\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_RIGHT_AUX\",\"capacityKgs\":null,\"percentageFilled\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_RIGHT_TIP\",\"capacityKgs\":null,\"percentageFilled\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_EXTERNAL_1\",\"capacityKgs\":null,\"percentageFilled\":null},");
            sb.Append("{\"position\":\"FUEL_TANK_POSITION_EXTERNAL_2\",\"capacityKgs\":null,\"percentageFilled\":null}");
            sb.Append("],");
            sb.Append("\"outsideAirTemperatureCelsius\":null,");
            sb.Append("\"visibilityKm\":null,");
            sb.Append("\"windSpeedKnots\":null,");
            sb.Append("\"windSpeedMetersPerSecond\":null,");
            sb.Append("\"windDirectionDegrees\":null,");
            sb.Append("\"ambientPressurePascal\":null,");
            sb.Append("\"seaLevelPressurePascal\":null,");
            sb.Append("\"barometerSettingPascal\":null,");
            sb.Append("\"apu\":{\"status\":null,\"source\":null},");
            sb.Append("\"pressurization\":{\"cabinAltitudeFeet\":null,\"cabinAltitudeMeters\":null},");
            sb.Append("\"flightControls\":{\"yawDamperEnabled\":[null]},");
            sb.Append("\"autopilot\":{\"source\":null,\"flightDirectorEnabled\":null,\"flightDirector1Enabled\":null,\"flightDirector2Enabled\":null,\"modes\":[],\"airspeedHoldKnots\":null,\"airspeedHoldMetersPerSecond\":null,\"machHoldMach\":null,\"altitudeHoldFeet\":null,\"altitudeHoldMeters\":null,\"altitudeArmFeet\":null,\"altitudeArmMeters\":null,\"headingLockDegrees\":null,\"pitchHoldDegrees\":null,\"verticalSpeedHoldFeetPerMinute\":null,\"verticalSpeedHoldMetersPerSecond\":null,\"ap1Engaged\":null,\"ap2Engaged\":null,\"autoThrottleArmed\":null,\"autoThrottleActive\":null,\"selectedSpeedKnots\":null,\"selectedSpeedMetersPerSecond\":null,\"selectedMach\":null,\"selectedHeadingDegrees\":null,\"selectedAltitudeFeet\":null,\"selectedAltitudeMeters\":null,\"selectedVerticalSpeedFeetPerMinute\":null,\"selectedVerticalSpeedMetersPerSecond\":null,\"lateralMode\":null,\"verticalMode\":null,\"managedSpeed\":null,\"managedLateral\":null,\"managedVertical\":null},");
            sb.Append("\"aircraftInformation\":{\"simulatorPath\":\"\",\"packagePath\":\"\",\"version\":\"1.1.0\"},");
            sb.Append("\"diagnostics\":{\"rejected\":[],\"warnings\":[],\"aircraftAdapter\":\"GenericAircraftAdapter\",\"fenixDetected\":false,\"fenixLvarSource\":\"unavailable\",\"fenixVariablesDiscovered\":0,\"fenixVariablesReadable\":0,\"identity\":{\"title\":null,\"atcModel\":null,\"atcType\":null,\"simObjectTitle\":null,\"packagePath\":null,\"detectedFamily\":\"Generic\",\"detectedVariant\":\"UNKNOWN\"},\"systems\":{\"apu\":{\"genericValue\":null,\"fenixRaw\":{},\"selected\":null,\"reason\":null},\"flightDirector\":{\"genericValue\":null,\"fenixRaw\":{\"fd1\":null,\"fd2\":null},\"selected\":null,\"reason\":null},\"autopilot\":{\"genericModes\":[],\"fenixRaw\":{},\"selectedSource\":null}}}");
            sb.Append("}");
            return sb.ToString();
        }

        private string BuildBackendJson(
            CoreFlightData core,
            WeatherData weather,
            RadioData radios,
            FuelData fuel,
            EngineData engine,
            AutopilotData autopilot,
            IdentityData identityData,
            bool connected,
            string lastError)
        {
            var rejected = new List<TelemetryRejectedValue>();
            var warnings = new List<TelemetryDiagnosticWarning>();

            AircraftIdentityInfo identity = BuildAircraftIdentity(identityData);
            string callsign = BuildCallsign();
            string aircraftShort = BuildAircraftShort(identity);
            string simulator = "Microsoft Flight Simulator";

            double? latitude = TelemetryMath.ValidateNumeric("latitude", core.latitude, core.latitude, rejected);
            double? longitude = TelemetryMath.ValidateNumeric("longitude", core.longitude, core.longitude, rejected);
            double? altitudeMeters = TelemetryMath.ValidateNumeric("altitudeMeters", core.altitudeMeters, core.altitudeMeters, rejected);
            double? altitudeFeet = altitudeMeters.HasValue ? TelemetryMath.MetersToFeet(altitudeMeters.Value) : (double?)null;
            double? groundSpeedKnots = TelemetryMath.ValidateNumeric("groundSpeedKnots", core.groundSpeedKnots, ZeroNoise(core.groundSpeedKnots, 0.01), rejected);
            double? groundspeed = TelemetryMath.ValidateNumeric("groundSpeedMetersPerSecond", core.groundSpeedKnots, ZeroNoise(TelemetryMath.KnotsToMetersPerSecond(core.groundSpeedKnots), 0.01), rejected);
            double? heading = TelemetryMath.ValidateNumeric("headingTrueDegrees", core.headingTrueDegrees, NormalizeHeading(core.headingTrueDegrees), rejected);
            double? headingMagnetic = TelemetryMath.ValidateNumeric("headingMagneticDegrees", core.headingMagneticDegrees, NormalizeHeading(core.headingMagneticDegrees), rejected);
            double? verticalSpeed = TelemetryMath.ValidateNumeric("verticalSpeedMetersPerSecond", core.verticalSpeedFeetPerSecond, ZeroNoise(TelemetryMath.FeetPerSecondToMetersPerSecond(core.verticalSpeedFeetPerSecond), 0.001), rejected);
            double? verticalSpeedFeetPerMinute = TelemetryMath.IsFinite(core.verticalSpeedFeetPerSecond)
                ? ZeroNoise(TelemetryMath.FeetPerSecondToFeetPerMinute(core.verticalSpeedFeetPerSecond), 0.1)
                : (double?)null;
            double? indicatedAirspeedMetersPerSecond = TelemetryMath.ValidateNumeric("indicatedAirspeedMetersPerSecond", core.indicatedAirspeedKnots, TelemetryMath.KnotsToMetersPerSecond(core.indicatedAirspeedKnots), rejected);
            double? indicatedAirspeedKnots = TelemetryMath.ValidateNumeric("indicatedAirspeedKnots", core.indicatedAirspeedKnots, core.indicatedAirspeedKnots, rejected);
            double? trueAirspeedMetersPerSecond = TelemetryMath.ValidateNumeric("trueAirspeedMetersPerSecond", core.trueAirspeedKnots, TelemetryMath.KnotsToMetersPerSecond(core.trueAirspeedKnots), rejected);
            double? trueAirspeedKnots = TelemetryMath.ValidateNumeric("trueAirspeedKnots", core.trueAirspeedKnots, core.trueAirspeedKnots, rejected);
            double? barberPoleAirspeedMetersPerSecond = TelemetryMath.ValidateNumeric("barberPoleAirspeedMetersPerSecond", core.barberPoleAirspeedKnots, TelemetryMath.KnotsToMetersPerSecond(core.barberPoleAirspeedKnots), rejected);
            double? barberPoleAirspeedKnots = TelemetryMath.ValidateNumeric("barberPoleAirspeedKnots", core.barberPoleAirspeedKnots, core.barberPoleAirspeedKnots, rejected);
            double? groundElevationFeet = TelemetryMath.IsFinite(core.groundElevationMeters)
                ? TelemetryMath.MetersToFeet(core.groundElevationMeters)
                : (double?)null;
            double? landingRateFeetPerMinute = TelemetryMath.IsFinite(core.landingRateMetersPerSecond)
                ? TelemetryMath.MetersPerSecondToFeetPerMinute(core.landingRateMetersPerSecond)
                : (double?)null;

            double? outsideAirTemperatureCelsius = TelemetryMath.ValidateNumeric("outsideAirTemperatureCelsius", weather.outsideAirTemperatureCelsius, weather.outsideAirTemperatureCelsius, rejected);
            double? ambientPressurePascal = TelemetryMath.ValidateNumeric("ambientPressurePascal", weather.ambientPressureInchesHg, TelemetryMath.InchesHgToPascals(weather.ambientPressureInchesHg), rejected);
            double? seaLevelPressurePascal = TelemetryMath.ValidateNumeric("seaLevelPressurePascal", weather.seaLevelPressureMillibars, TelemetryMath.MillibarsToPascals(weather.seaLevelPressureMillibars), rejected);
            double? barometerSettingPascal = TelemetryMath.ValidateNumeric("barometerSettingPascal", autopilot.barometerSettingMillibars, TelemetryMath.MillibarsToPascals(autopilot.barometerSettingMillibars), rejected);
            double? visibilityKm = TelemetryMath.ValidateNumeric("visibilityKm", weather.visibilityMeters, TelemetryMath.MetersToKilometers(weather.visibilityMeters), rejected);
            double? windSpeedMetersPerSecond = TelemetryMath.ValidateNumeric("windSpeedMetersPerSecond", weather.windSpeedKnots, TelemetryMath.KnotsToMetersPerSecond(weather.windSpeedKnots), rejected);
            double? windSpeedKnots = TelemetryMath.ValidateNumeric("windSpeedKnots", weather.windSpeedKnots, weather.windSpeedKnots, rejected);
            double? windDirectionDegrees = TelemetryMath.ValidateNumeric("windDirectionDegrees", weather.windDirectionDegrees, NormalizeHeading(weather.windDirectionDegrees), rejected);

            if (!outsideAirTemperatureCelsius.HasValue && TelemetryMath.IsFinite(weather.outsideAirTemperatureCelsius))
            {
                warnings.Add(new TelemetryDiagnosticWarning
                {
                    Code = "weather_struct_suspect",
                    Message =
                        "AMBIENT TEMPERATURE raw=" + TelemetryMath.FormatNumeric(weather.outsideAirTemperatureCelsius) +
                        " ambientPressureRaw=" + TelemetryMath.FormatNumeric(weather.ambientPressureInchesHg) +
                        " seaLevelPressureRaw=" + TelemetryMath.FormatNumeric(weather.seaLevelPressureMillibars) +
                        " visibilityRaw=" + TelemetryMath.FormatNumeric(weather.visibilityMeters) +
                        " windSpeedRaw=" + TelemetryMath.FormatNumeric(weather.windSpeedKnots) +
                        " windDirectionRaw=" + TelemetryMath.FormatNumeric(weather.windDirectionDegrees)
                });

                Log("weather_struct_suspect raw packet: temp=" + TelemetryMath.FormatNumeric(weather.outsideAirTemperatureCelsius) +
                    " ambientPressure=" + TelemetryMath.FormatNumeric(weather.ambientPressureInchesHg) +
                    " seaLevelPressure=" + TelemetryMath.FormatNumeric(weather.seaLevelPressureMillibars) +
                    " visibility=" + TelemetryMath.FormatNumeric(weather.visibilityMeters) +
                    " windSpeed=" + TelemetryMath.FormatNumeric(weather.windSpeedKnots) +
                    " windDirection=" + TelemetryMath.FormatNumeric(weather.windDirectionDegrees));
            }

            if (altitudeMeters.HasValue && altitudeMeters.Value > 100 &&
                ambientPressurePascal.HasValue && seaLevelPressurePascal.HasValue &&
                ambientPressurePascal.Value > seaLevelPressurePascal.Value)
            {
                warnings.Add(new TelemetryDiagnosticWarning
                {
                    Code = "pressure_mapping_suspicious",
                    Message =
                        "altitudeMeters=" + Num(altitudeMeters) +
                        " ambientPressurePascal=" + Num(ambientPressurePascal) +
                        " seaLevelPressurePascal=" + Num(seaLevelPressurePascal)
                });
                Log("pressure_mapping_suspicious altitude=" + Num(altitudeMeters) +
                    " ambient=" + Num(ambientPressurePascal) +
                    " seaLevel=" + Num(seaLevelPressurePascal));
            }

            string com1 = ResolveComFrequency("com1", "COM ACTIVE FREQUENCY:1", radios.com1Available, radios.com1Frequency, rejected);
            string com2 = ResolveComFrequency("com2", "COM ACTIVE FREQUENCY:2", radios.com2Available, radios.com2Frequency, rejected);
            string nav1 = ResolveNavFrequency("nav1", "NAV ACTIVE FREQUENCY:1", radios.nav1Available, radios.nav1Frequency, rejected);
            string nav2 = ResolveNavFrequency("nav2", "NAV ACTIVE FREQUENCY:2", radios.nav2Available, radios.nav2Frequency, rejected);

            double? validFuelWeightPerGallon = TelemetryMath.ValidateFuelWeightPerGallon(fuel.fuelWeightPerGallon, new List<TelemetryRejectedValue>());
            if (!validFuelWeightPerGallon.HasValue)
            {
                int fuelDensityIndex = GetDefinitionIndex(FuelDefinitions, "fuelWeightPerGallon");
                warnings.Add(new TelemetryDiagnosticWarning
                {
                    Code = "fuel_density_invalid",
                    Message =
                        "raw fuelWeightPerGallonLb=" + TelemetryMath.FormatNumeric(fuel.fuelWeightPerGallon) +
                        " requestedUnit=pounds sourceField=FUEL WEIGHT PER GALLON structFieldIndex=" + fuelDensityIndex.ToString(CultureInfo.InvariantCulture)
                });
                Log("fuel_density_invalid raw=" + TelemetryMath.FormatNumeric(fuel.fuelWeightPerGallon) +
                    " requestedUnit=pounds sourceField=FUEL WEIGHT PER GALLON structFieldIndex=" + fuelDensityIndex.ToString(CultureInfo.InvariantCulture));
            }

            string engineType = MapEngineType(engine.engineType, aircraftShort);
            List<string> genericAutopilotModes = BuildAutopilotModes(autopilot);
            string genericApuStatus = BuildApuStatus(engine);
            bool? genericFlightDirectorEnabled = ToNullableBool(autopilot.flightDirectorEnabled);
            double? genericAutopilotAirspeedHoldMetersPerSecond =
                IsTruthy(autopilot.autopilotAirspeedHoldActive)
                    ? TelemetryMath.ValidateNumeric("autopilot.airspeedHoldMetersPerSecond", autopilot.autopilotAirspeedHoldKnots, TelemetryMath.KnotsToMetersPerSecond(autopilot.autopilotAirspeedHoldKnots), rejected)
                    : (double?)null;
            double? genericAutopilotMachHoldMach =
                IsTruthy(autopilot.autopilotMachHoldActive)
                    ? TelemetryMath.ValidateNumeric("autopilot.machHoldMach", autopilot.autopilotMachHoldMach, autopilot.autopilotMachHoldMach, rejected)
                    : (double?)null;
            double? genericAutopilotAltitudeHoldMeters =
                IsTruthy(autopilot.autopilotAltitudeHoldActive)
                    ? TelemetryMath.ValidateNumeric("autopilot.altitudeHoldMeters", autopilot.autopilotAltitudeHoldFeet, TelemetryMath.FeetToMeters(autopilot.autopilotAltitudeHoldFeet), rejected)
                    : (double?)null;
            double? genericAutopilotHeadingLockDegrees =
                IsTruthy(autopilot.autopilotHeadingLockActive)
                    ? TelemetryMath.ValidateNumeric("autopilot.headingLockDegrees", autopilot.autopilotHeadingLockDegrees, NormalizeHeading(autopilot.autopilotHeadingLockDegrees), rejected)
                    : (double?)null;
            double? genericAutopilotPitchHoldDegrees =
                IsTruthy(autopilot.autopilotVerticalSpeedHoldActive)
                    ? TelemetryMath.ValidateNumeric("autopilot.pitchHoldDegrees", autopilot.autopilotPitchHoldRadians, TelemetryMath.RadiansToDegrees(autopilot.autopilotPitchHoldRadians), rejected)
                    : (double?)null;
            double? genericAutopilotVerticalSpeedHoldMetersPerSecond =
                IsTruthy(autopilot.autopilotVerticalSpeedHoldActive)
                    ? TelemetryMath.ValidateNumeric("autopilot.verticalSpeedHoldMetersPerSecond", autopilot.autopilotVerticalSpeedHoldFeetPerMinute, TelemetryMath.FeetPerMinuteToMetersPerSecond(autopilot.autopilotVerticalSpeedHoldFeetPerMinute), rejected)
                    : (double?)null;

            var genericSystems = new GenericSystemsData
            {
                ApuStatus = genericApuStatus,
                YawDamperEnabled = ToNullableBool(autopilot.yawDamperEnabled),
                FlightDirectorEnabled = genericFlightDirectorEnabled,
                AutopilotModes = genericAutopilotModes,
                AirspeedHoldMetersPerSecond = genericAutopilotAirspeedHoldMetersPerSecond,
                MachHoldMach = genericAutopilotMachHoldMach,
                AltitudeHoldMeters = genericAutopilotAltitudeHoldMeters,
                HeadingLockDegrees = genericAutopilotHeadingLockDegrees,
                PitchHoldDegrees = genericAutopilotPitchHoldDegrees,
                VerticalSpeedHoldMetersPerSecond = genericAutopilotVerticalSpeedHoldMetersPerSecond
            };

            IReadOnlyDictionary<string, double?> customValues = fenixLvarValues;
            ISet<string> discoveredVariables = fenixDiscoveredVarSet;
            int discoveredVariableCount = fenixDiscoveredVars.Count;
            int readableVariableCount = fenixReadableVarNames.Count;
            string customVariableSource = fenixReadableVarNames.Count > 0 ? "direct-simconnect" : "unavailable";

            if (activeAircraftAdapter is Pmdg777Adapter)
            {
                customValues = pmdg777SdkValues;
                discoveredVariables = new HashSet<string>(pmdg777SdkValues.Keys, StringComparer.OrdinalIgnoreCase);
                discoveredVariableCount = 24;
                readableVariableCount = pmdg777ClientDataAvailable ? pmdg777SdkValues.Count : 0;
                customVariableSource = pmdg777ClientDataAvailable ? "pmdg-777-sdk" : "pmdg-777-sdk-unavailable";
            }

            var adapterContext = new AircraftAdapterContext
            {
                Identity = identity,
                Generic = genericSystems,
                CustomVariableValues = customValues,
                DiscoveredVariables = discoveredVariables,
                DiscoveredVariableCount = discoveredVariableCount,
                ReadableVariableCount = readableVariableCount,
                CustomVariableSource = customVariableSource
            };

            AircraftAdapterResult adapterResult = activeAircraftAdapter.Evaluate(adapterContext);
            string apuStatus = adapterResult.Apu != null ? adapterResult.Apu.Status : genericApuStatus;
            bool? yawDamperEnabled = adapterResult.Autopilot != null ? adapterResult.Autopilot.YawDamperEnabled : genericSystems.YawDamperEnabled;
            bool? flightDirectorEnabled = adapterResult.Autopilot != null ? adapterResult.Autopilot.FlightDirectorEnabled : genericFlightDirectorEnabled;
            bool? flightDirector1Enabled = adapterResult.Autopilot != null ? adapterResult.Autopilot.FlightDirector1Enabled : null;
            bool? flightDirector2Enabled = adapterResult.Autopilot != null ? adapterResult.Autopilot.FlightDirector2Enabled : null;
            IList<string> autopilotModes = adapterResult.Autopilot != null ? adapterResult.Autopilot.Modes : genericAutopilotModes;
            bool useCustomAutopilotTelemetry = ShouldUseCustomAutopilotTelemetry(adapterResult);
            double? autopilotAirspeedHoldMetersPerSecond = useCustomAutopilotTelemetry
                ? adapterResult.Autopilot.SelectedSpeedMetersPerSecond
                : genericAutopilotAirspeedHoldMetersPerSecond;
            double? autopilotMachHoldMach = useCustomAutopilotTelemetry
                ? adapterResult.Autopilot.SelectedMach
                : genericAutopilotMachHoldMach;
            double? autopilotAltitudeHoldMeters = useCustomAutopilotTelemetry
                ? adapterResult.Autopilot.SelectedAltitudeMeters
                : genericAutopilotAltitudeHoldMeters;
            double? autopilotHeadingLockDegrees = useCustomAutopilotTelemetry
                ? adapterResult.Autopilot.SelectedHeadingDegrees
                : genericAutopilotHeadingLockDegrees;
            double? autopilotPitchHoldDegrees = useCustomAutopilotTelemetry
                ? null
                : genericAutopilotPitchHoldDegrees;
            double? autopilotVerticalSpeedHoldMetersPerSecond = useCustomAutopilotTelemetry
                ? adapterResult.Autopilot.SelectedVerticalSpeedMetersPerSecond
                : genericAutopilotVerticalSpeedHoldMetersPerSecond;
            double? cabinAltitudeFeet = TelemetryMath.IsFinite(core.cabinAltitudeMeters)
                ? TelemetryMath.MetersToFeet(core.cabinAltitudeMeters)
                : (double?)null;
            double? autopilotAirspeedHoldKnots = autopilotAirspeedHoldMetersPerSecond.HasValue
                ? TelemetryMath.MetersPerSecondToKnots(autopilotAirspeedHoldMetersPerSecond.Value)
                : (double?)null;
            double? autopilotAltitudeHoldFeet = autopilotAltitudeHoldMeters.HasValue
                ? TelemetryMath.MetersToFeet(autopilotAltitudeHoldMeters.Value)
                : (double?)null;
            double? autopilotVerticalSpeedHoldFeetPerMinute = autopilotVerticalSpeedHoldMetersPerSecond.HasValue
                ? TelemetryMath.MetersPerSecondToFeetPerMinute(autopilotVerticalSpeedHoldMetersPerSecond.Value)
                : (double?)null;
            double? selectedSpeedKnots = adapterResult.Autopilot != null && adapterResult.Autopilot.SelectedSpeedMetersPerSecond.HasValue
                ? TelemetryMath.MetersPerSecondToKnots(adapterResult.Autopilot.SelectedSpeedMetersPerSecond.Value)
                : (double?)null;
            double? selectedAltitudeFeet = adapterResult.Autopilot != null && adapterResult.Autopilot.SelectedAltitudeMeters.HasValue
                ? TelemetryMath.MetersToFeet(adapterResult.Autopilot.SelectedAltitudeMeters.Value)
                : (double?)null;
            double? selectedVerticalSpeedFeetPerMinute = adapterResult.Autopilot != null && adapterResult.Autopilot.SelectedVerticalSpeedMetersPerSecond.HasValue
                ? TelemetryMath.MetersPerSecondToFeetPerMinute(adapterResult.Autopilot.SelectedVerticalSpeedMetersPerSecond.Value)
                : (double?)null;

            var sb = new StringBuilder(4096);
            sb.Append("{");
            sb.Append("\"online\":").Append(Bool(connected)).Append(",");
            sb.Append("\"connected\":").Append(Bool(connected)).Append(",");
            sb.Append("\"latitude\":").Append(Num(latitude)).Append(",");
            sb.Append("\"longitude\":").Append(Num(longitude)).Append(",");
            sb.Append("\"altitude\":").Append(Num(altitudeFeet)).Append(",");
            sb.Append("\"groundspeed\":").Append(Num(groundSpeedKnots)).Append(",");
            sb.Append("\"heading\":").Append(Num(heading)).Append(",");
            sb.Append("\"callsign\":\"").Append(Escape(callsign)).Append("\",");
            sb.Append("\"flight_plan\":{");
            sb.Append("\"departure\":null,");
            sb.Append("\"arrival\":null,");
            sb.Append("\"aircraft_short\":\"").Append(Escape(aircraftShort)).Append("\"");
            sb.Append("},");
            sb.Append("\"source\":\"simconnect-bridge\",");
            sb.Append("\"last_error\":").Append(JsonStringOrNull(lastError)).Append(",");

            sb.Append("\"Connected to Simulator\":").Append(Bool(connected)).Append(",");
            sb.Append("\"Connected to Backend\":").Append(Bool(backendConnected)).Append(",");
            sb.Append("\"simulator\":\"").Append(Escape(simulator)).Append("\",");
            sb.Append("\"aircraftType\":\"").Append(Escape(aircraftShort)).Append("\",");
            sb.Append("\"aircraftPath\":null,");
            sb.Append("\"fps\":").Append(Num(latestFrameRate)).Append(",");
            sb.Append("\"simulationRate\":").Append(Num(latestSimulationRate)).Append(",");
            sb.Append("\"position\":{");
            sb.Append("\"latitude\":").Append(Num(latitude)).Append(",");
            sb.Append("\"longitude\":").Append(Num(longitude));
            sb.Append("},");
            sb.Append("\"headingTrueDegrees\":").Append(Num(heading)).Append(",");
            sb.Append("\"headingMagneticDegrees\":").Append(Num(headingMagnetic)).Append(",");
            sb.Append("\"gForce\":").Append(Num(core.gForce)).Append(",");
            sb.Append("\"altitudeFeet\":").Append(Num(altitudeFeet)).Append(",");
            sb.Append("\"altitudeMeters\":").Append(Num(altitudeMeters)).Append(",");
            sb.Append("\"pitchDegrees\":").Append(Num(core.pitchDegrees)).Append(",");
            sb.Append("\"bankDegrees\":").Append(Num(core.bankDegrees)).Append(",");
            sb.Append("\"groundElevationFeet\":").Append(Num(groundElevationFeet)).Append(",");
            sb.Append("\"groundElevationMeters\":").Append(Num(core.groundElevationMeters)).Append(",");
            sb.Append("\"landingRateFeetPerMinute\":").Append(Num(landingRateFeetPerMinute)).Append(",");
            sb.Append("\"landingRateMetersPerSecond\":").Append(Num(core.landingRateMetersPerSecond)).Append(",");
            sb.Append("\"onGround\":").Append(JsonBoolOrNull(core.onGround)).Append(",");
            sb.Append("\"indicatedAirspeedKnots\":").Append(Num(indicatedAirspeedKnots)).Append(",");
            sb.Append("\"indicatedAirspeedMetersPerSecond\":").Append(Num(indicatedAirspeedMetersPerSecond)).Append(",");
            sb.Append("\"trueAirspeedKnots\":").Append(Num(trueAirspeedKnots)).Append(",");
            sb.Append("\"trueAirspeedMetersPerSecond\":").Append(Num(trueAirspeedMetersPerSecond)).Append(",");
            sb.Append("\"barberPoleAirspeedKnots\":").Append(Num(barberPoleAirspeedKnots)).Append(",");
            sb.Append("\"barberPoleAirspeedMetersPerSecond\":").Append(Num(barberPoleAirspeedMetersPerSecond)).Append(",");
            sb.Append("\"groundSpeedKnots\":").Append(Num(groundSpeedKnots)).Append(",");
            sb.Append("\"groundSpeedMetersPerSecond\":").Append(Num(groundspeed)).Append(",");
            sb.Append("\"verticalSpeedFeetPerMinute\":").Append(Num(verticalSpeedFeetPerMinute)).Append(",");
            sb.Append("\"verticalSpeedMetersPerSecond\":").Append(Num(verticalSpeed)).Append(",");
            sb.Append("\"parkingBrake\":").Append(JsonBoolOrNull(core.parkingBrake)).Append(",");
            sb.Append("\"numFlapPositions\":").Append(Num(core.numFlapPositions)).Append(",");
            sb.Append("\"gearDown\":").Append(JsonBoolOrNull(core.gearDown)).Append(",");
            sb.Append("\"doorsOpen\":").Append(JsonBoolOrNull(engine.exitOpen)).Append(",");
            sb.Append("\"lights\":{");
            sb.Append("\"navigation\":").Append(JsonBoolOrNull(core.lightNavigation)).Append(",");
            sb.Append("\"beacon\":").Append(JsonBoolOrNull(core.lightBeacon)).Append(",");
            sb.Append("\"strobes\":").Append(JsonBoolOrNull(core.lightStrobes)).Append(",");
            sb.Append("\"instruments\":").Append(JsonBoolOrNull(core.lightInstruments)).Append(",");
            sb.Append("\"logo\":").Append(JsonBoolOrNull(core.lightLogo)).Append(",");
            sb.Append("\"cabin\":").Append(JsonBoolOrNull(core.lightCabin));
            sb.Append("},");
            sb.Append("\"com1\":").Append(JsonStringOrNull(com1)).Append(",");
            sb.Append("\"com2\":").Append(JsonStringOrNull(com2)).Append(",");
            sb.Append("\"nav1\":").Append(JsonStringOrNull(nav1)).Append(",");
            sb.Append("\"nav2\":").Append(JsonStringOrNull(nav2)).Append(",");
            sb.Append("\"transponder\":").Append(JsonStringOrNull(FormatTransponder(radios.transponderCode))).Append(",");
            sb.Append("\"engineType\":").Append(JsonStringOrNull(engineType)).Append(",");
            sb.Append("\"engines\":[");
            sb.Append("{\"ittDegreesCelsius\":").Append(Num(engine.itt1DegreesCelsius)).Append(",\"antiIce\":{\"antiIceEnabled\":").Append(JsonBoolOrNull(engine.antiIce1Enabled)).Append("}},");
            sb.Append("{\"ittDegreesCelsius\":").Append(Num(engine.itt2DegreesCelsius)).Append(",\"antiIce\":{\"antiIceEnabled\":").Append(JsonBoolOrNull(engine.antiIce2Enabled)).Append("}}");
            sb.Append("],");
            sb.Append("\"fuelTanks\":[");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_CENTER", fuel.fuelTankCenterCapacityGallons, fuel.fuelTankCenterLevel, validFuelWeightPerGallon, rejected, warnings)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_CENTER_2", fuel.fuelTankCenter2CapacityGallons, fuel.fuelTankCenter2Level, validFuelWeightPerGallon, rejected, warnings)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_CENTER_3", fuel.fuelTankCenter3CapacityGallons, fuel.fuelTankCenter3Level, validFuelWeightPerGallon, rejected, warnings)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_LEFT_MAIN", fuel.fuelTankLeftMainCapacityGallons, fuel.fuelTankLeftMainLevel, validFuelWeightPerGallon, rejected, warnings)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_LEFT_AUX", fuel.fuelTankLeftAuxCapacityGallons, fuel.fuelTankLeftAuxLevel, validFuelWeightPerGallon, rejected, warnings)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_LEFT_TIP", fuel.fuelTankLeftTipCapacityGallons, fuel.fuelTankLeftTipLevel, validFuelWeightPerGallon, rejected, warnings)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_RIGHT_MAIN", fuel.fuelTankRightMainCapacityGallons, fuel.fuelTankRightMainLevel, validFuelWeightPerGallon, rejected, warnings)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_RIGHT_AUX", fuel.fuelTankRightAuxCapacityGallons, fuel.fuelTankRightAuxLevel, validFuelWeightPerGallon, rejected, warnings)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_RIGHT_TIP", fuel.fuelTankRightTipCapacityGallons, fuel.fuelTankRightTipLevel, validFuelWeightPerGallon, rejected, warnings)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_EXTERNAL_1", fuel.fuelTankExternal1CapacityGallons, fuel.fuelTankExternal1Level, validFuelWeightPerGallon, rejected, warnings)).Append(",");
            sb.Append(BuildFuelTankJson("FUEL_TANK_POSITION_EXTERNAL_2", fuel.fuelTankExternal2CapacityGallons, fuel.fuelTankExternal2Level, validFuelWeightPerGallon, rejected, warnings));
            sb.Append("],");
            sb.Append("\"outsideAirTemperatureCelsius\":").Append(Num(outsideAirTemperatureCelsius)).Append(",");
            sb.Append("\"visibilityKm\":").Append(Num(visibilityKm)).Append(",");
            sb.Append("\"windSpeedKnots\":").Append(Num(windSpeedKnots)).Append(",");
            sb.Append("\"windSpeedMetersPerSecond\":").Append(Num(windSpeedMetersPerSecond)).Append(",");
            sb.Append("\"windDirectionDegrees\":").Append(Num(windDirectionDegrees)).Append(",");
            sb.Append("\"ambientPressurePascal\":").Append(Num(ambientPressurePascal)).Append(",");
            sb.Append("\"seaLevelPressurePascal\":").Append(Num(seaLevelPressurePascal)).Append(",");
            sb.Append("\"barometerSettingPascal\":").Append(Num(barometerSettingPascal)).Append(",");
            sb.Append("\"apu\":{\"status\":").Append(JsonStringOrNull(apuStatus)).Append(",\"source\":").Append(JsonStringOrNull(adapterResult.Apu != null ? adapterResult.Apu.Source : "generic-simvar")).Append("},");
            sb.Append("\"pressurization\":{\"cabinAltitudeFeet\":").Append(Num(cabinAltitudeFeet)).Append(",\"cabinAltitudeMeters\":").Append(Num(core.cabinAltitudeMeters)).Append("},");
            sb.Append("\"flightControls\":{\"yawDamperEnabled\":[").Append(JsonBoolOrNull(yawDamperEnabled)).Append("]},");
            sb.Append("\"autopilot\":{");
            sb.Append("\"source\":").Append(JsonStringOrNull(adapterResult.Autopilot != null ? adapterResult.Autopilot.Source : "generic-simvar")).Append(",");
            sb.Append("\"flightDirectorEnabled\":").Append(JsonBoolOrNull(flightDirectorEnabled)).Append(",");
            sb.Append("\"flightDirector1Enabled\":").Append(JsonBoolOrNull(flightDirector1Enabled)).Append(",");
            sb.Append("\"flightDirector2Enabled\":").Append(JsonBoolOrNull(flightDirector2Enabled)).Append(",");
            sb.Append("\"modes\":").Append(JsonStringArray(autopilotModes != null ? new List<string>(autopilotModes) : new List<string>())).Append(",");
            sb.Append("\"airspeedHoldKnots\":").Append(Num(autopilotAirspeedHoldKnots)).Append(",");
            sb.Append("\"airspeedHoldMetersPerSecond\":").Append(Num(autopilotAirspeedHoldMetersPerSecond)).Append(",");
            sb.Append("\"machHoldMach\":").Append(Num(autopilotMachHoldMach)).Append(",");
            sb.Append("\"altitudeHoldFeet\":").Append(Num(autopilotAltitudeHoldFeet)).Append(",");
            sb.Append("\"altitudeHoldMeters\":").Append(Num(autopilotAltitudeHoldMeters)).Append(",");
            sb.Append("\"altitudeArmFeet\":").Append(Num(autopilotAltitudeHoldFeet)).Append(",");
            sb.Append("\"altitudeArmMeters\":").Append(Num(autopilotAltitudeHoldMeters)).Append(",");
            sb.Append("\"headingLockDegrees\":").Append(Num(autopilotHeadingLockDegrees)).Append(",");
            sb.Append("\"pitchHoldDegrees\":").Append(Num(autopilotPitchHoldDegrees)).Append(",");
            sb.Append("\"verticalSpeedHoldFeetPerMinute\":").Append(Num(autopilotVerticalSpeedHoldFeetPerMinute)).Append(",");
            sb.Append("\"verticalSpeedHoldMetersPerSecond\":").Append(Num(autopilotVerticalSpeedHoldMetersPerSecond)).Append(",");
            sb.Append("\"ap1Engaged\":").Append(JsonBoolOrNull(adapterResult.Autopilot != null ? adapterResult.Autopilot.Ap1Engaged : null)).Append(",");
            sb.Append("\"ap2Engaged\":").Append(JsonBoolOrNull(adapterResult.Autopilot != null ? adapterResult.Autopilot.Ap2Engaged : null)).Append(",");
            sb.Append("\"autoThrottleArmed\":").Append(JsonBoolOrNull(adapterResult.Autopilot != null ? adapterResult.Autopilot.AutoThrottleArmed : null)).Append(",");
            sb.Append("\"autoThrottleActive\":").Append(JsonBoolOrNull(adapterResult.Autopilot != null ? adapterResult.Autopilot.AutoThrottleActive : null)).Append(",");
            sb.Append("\"selectedSpeedKnots\":").Append(Num(selectedSpeedKnots)).Append(",");
            sb.Append("\"selectedSpeedMetersPerSecond\":").Append(Num(adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectedSpeedMetersPerSecond : null)).Append(",");
            sb.Append("\"selectedMach\":").Append(Num(adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectedMach : null)).Append(",");
            sb.Append("\"selectedHeadingDegrees\":").Append(Num(adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectedHeadingDegrees : null)).Append(",");
            sb.Append("\"selectedAltitudeFeet\":").Append(Num(selectedAltitudeFeet)).Append(",");
            sb.Append("\"selectedAltitudeMeters\":").Append(Num(adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectedAltitudeMeters : null)).Append(",");
            sb.Append("\"selectedVerticalSpeedFeetPerMinute\":").Append(Num(selectedVerticalSpeedFeetPerMinute)).Append(",");
            sb.Append("\"selectedVerticalSpeedMetersPerSecond\":").Append(Num(adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectedVerticalSpeedMetersPerSecond : null)).Append(",");
            sb.Append("\"lateralMode\":").Append(JsonStringOrNull(adapterResult.Autopilot != null ? adapterResult.Autopilot.LateralMode : null)).Append(",");
            sb.Append("\"verticalMode\":").Append(JsonStringOrNull(adapterResult.Autopilot != null ? adapterResult.Autopilot.VerticalMode : null)).Append(",");
            sb.Append("\"managedSpeed\":").Append(JsonBoolOrNull(adapterResult.Autopilot != null ? adapterResult.Autopilot.ManagedSpeed : null)).Append(",");
            sb.Append("\"managedLateral\":").Append(JsonBoolOrNull(adapterResult.Autopilot != null ? adapterResult.Autopilot.ManagedLateral : null)).Append(",");
            sb.Append("\"managedVertical\":").Append(JsonBoolOrNull(adapterResult.Autopilot != null ? adapterResult.Autopilot.ManagedVertical : null));
            sb.Append("},");
            sb.Append("\"aircraftInformation\":{");
            sb.Append("\"simulatorPath\":\"\",");
            sb.Append("\"packagePath\":").Append(JsonStringOrNull(identity.PackagePath)).Append(",");
            sb.Append("\"version\":\"1.1.0\"");
            sb.Append("},");
            sb.Append("\"diagnostics\":").Append(BuildDiagnosticsJson(rejected, warnings, identity, genericSystems, adapterResult));
            sb.Append("}");
            LogRejectedTelemetry(rejected);
            LogDiagnosticWarnings(warnings);
            return sb.ToString();
        }

        private static bool ShouldUseCustomAutopilotTelemetry(AircraftAdapterResult adapterResult)
        {
            if (adapterResult == null || adapterResult.Autopilot == null)
            {
                return false;
            }

            string source = adapterResult.Autopilot.Source ?? "";
            if (source.Length == 0)
            {
                return false;
            }

            if (string.Equals(source, "generic-simvar", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (source.EndsWith("-unavailable", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private string BuildCallsign()
        {
            return "SIMCONNECT";
        }

        private string BuildAircraftShort(AircraftIdentityInfo identity)
        {
            if (identity != null && !string.IsNullOrWhiteSpace(identity.DetectedVariant) && !string.Equals(identity.DetectedVariant, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                return identity.DetectedVariant;
            }

            return BuildAircraftShort(identity != null ? identity.Title : "");
        }

        private string BuildAircraftShort(string aircraftTitle)
        {
            string title = Clean(aircraftTitle);
            string combined = title.ToUpperInvariant();

            if (combined.Contains("A20N")) return "A20N";
            if (combined.Contains("A320")) return "A320";
            if (combined.Contains("A319")) return "A319";
            if (combined.Contains("A321")) return "A321";
            if (combined.Contains("A339") || combined.Contains("A330")) return "A339";
            if (combined.Contains("A359") || combined.Contains("A350")) return "A359";

            if (combined.Contains("B738") || combined.Contains("737-800") || combined.Contains("737")) return "B738";
            if (combined.Contains("B739") || combined.Contains("737-900")) return "B739";
            if (combined.Contains("B789") || combined.Contains("787-9")) return "B789";
            if (combined.Contains("B788") || combined.Contains("787-8")) return "B788";
            if (combined.Contains("B77W") || combined.Contains("777-300")) return "B77W";
            if (combined.Contains("B772") || combined.Contains("777-200")) return "B772";
            if (combined.Contains("B748") || combined.Contains("747-8")) return "B748";
            if (combined.Contains("B744") || combined.Contains("747-400")) return "B744";

            if (combined.Contains("C172") || combined.Contains("172")) return "C172";
            if (combined.Contains("TBM")) return "TBM9";
            if (combined.Contains("CJ4")) return "C25C";
            if (combined.Contains("DA40")) return "DA40";
            if (combined.Contains("DA62")) return "DA62";

            return "UNKNOWN";
        }

        private void AppendTelemetry(string json)
        {
            File.AppendAllText(TelemetryPath, json + Environment.NewLine);
        }

        private void Log(string message)
        {
            Directory.CreateDirectory(appDataFolder);

            File.AppendAllText(
                LogPath,
                DateTime.UtcNow.ToString("o") + " " + message + Environment.NewLine
            );
        }

        private void SetStatus(string message)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.BeginInvoke(new Action(() => statusLabel.Text = message));
            }
            else
            {
                statusLabel.Text = message;
            }
        }

        private void UpdateLatestLabel(CoreFlightData t)
        {
            string callsign = BuildCallsign();
            string aircraftShort = BuildAircraftShort(latestAircraftIdentity);
            double altitudeFeet = TelemetryMath.MetersToFeet(t.altitudeMeters);
            double groundspeedKnots = ZeroNoise(t.groundSpeedKnots, 0.01);

            string text =
                callsign + " / " + aircraftShort + Environment.NewLine +
                "Lat " + Num(t.latitude) +
                " Lon " + Num(t.longitude) +
                " Alt " + Math.Round(altitudeFeet).ToString(CultureInfo.InvariantCulture) + " ft" +
                " GS " + Math.Round(groundspeedKnots).ToString(CultureInfo.InvariantCulture) + " kt" +
                " HDG " + Math.Round(NormalizeHeading(t.headingTrueDegrees)).ToString(CultureInfo.InvariantCulture);

            if (latestLabel.InvokeRequired)
            {
                latestLabel.BeginInvoke(new Action(() => latestLabel.Text = text));
            }
            else
            {
                latestLabel.Text = text;
            }
        }

        private void InstallAutostartButton_Click(object sender, EventArgs e)
        {
            try
            {
                string exePath = Application.ExecutablePath;
                string changedFiles = MsfsAutostartManager.Install(exePath);

                MessageBox.Show(
                    "Simple Sim Connector autostart has been installed." +
                    Environment.NewLine + Environment.NewLine +
                    "Updated:" +
                    Environment.NewLine +
                    changedFiles +
                    Environment.NewLine + Environment.NewLine +
                    "MSFS 2024 should now launch the connector automatically.",
                    "Autostart installed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                Log("Autostart installed: " + changedFiles.Replace(Environment.NewLine, " | "));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Autostart install failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                Log("Autostart install failed: " + ex.Message);
            }
        }

        private void RemoveAutostartButton_Click(object sender, EventArgs e)
        {
            try
            {
                string changedFiles = MsfsAutostartManager.Uninstall();

                MessageBox.Show(
                    "Autostart removal complete." +
                    Environment.NewLine + Environment.NewLine +
                    changedFiles,
                    "Autostart removed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                Log("Autostart removed: " + changedFiles.Replace(Environment.NewLine, " | "));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Autostart removal failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                Log("Autostart removal failed: " + ex.Message);
            }
        }

        private static string Clean(string value)
        {
            return (value ?? "").Trim();
        }

        private static double ZeroNoise(double value, double threshold)
        {
            return Math.Abs(value) < threshold ? 0 : value;
        }

        private static double NormalizeHeading(double heading)
        {
            if (double.IsNaN(heading) || double.IsInfinity(heading))
            {
                return heading;
            }

            heading = heading % 360.0;

            if (heading < 0)
            {
                heading += 360.0;
            }

            return heading;
        }

        private static string Num(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "null";
            }

            return value.ToString("G17", CultureInfo.InvariantCulture);
        }

        private static string Num(double? value)
        {
            return value.HasValue ? Num(value.Value) : "null";
        }

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }

        private static bool IsTruthy(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0.5;
        }

        private static bool? ToNullableBool(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return null;
            }

            return IsTruthy(value);
        }

        private static string JsonBoolOrNull(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "null";
            }

            return IsTruthy(value) ? "true" : "false";
        }

        private static string JsonBoolOrNull(bool? value)
        {
            if (!value.HasValue)
            {
                return "null";
            }

            return value.Value ? "true" : "false";
        }

        private static double CombineAnyTrue(params double[] values)
        {
            bool sawFinite = false;

            foreach (double value in values)
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    continue;
                }

                sawFinite = true;
                if (IsTruthy(value))
                {
                    return 1.0;
                }
            }

            return sawFinite ? 0.0 : double.NaN;
        }

        private string BuildDiagnosticsJson(
            IList<TelemetryRejectedValue> rejected,
            IList<TelemetryDiagnosticWarning> warnings,
            AircraftIdentityInfo identity,
            GenericSystemsData genericSystems,
            AircraftAdapterResult adapterResult)
        {
            var sb = new StringBuilder();
            sb.Append("{\"rejected\":[");

            for (int i = 0; i < rejected.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }

                TelemetryRejectedValue item = rejected[i];
                sb.Append("{");
                sb.Append("\"jsonField\":").Append(JsonStringOrNull(item.JsonField)).Append(",");
                sb.Append("\"simVarName\":").Append(JsonStringOrNull(item.SimVarName)).Append(",");
                sb.Append("\"requestedUnit\":").Append(JsonStringOrNull(item.RequestedUnit)).Append(",");
                sb.Append("\"rawValue\":").Append(JsonStringOrNull(item.RawValue)).Append(",");
                sb.Append("\"convertedValue\":").Append(JsonStringOrNull(item.ConvertedValue)).Append(",");
                sb.Append("\"sanityRange\":").Append(JsonStringOrNull(item.SanityRange)).Append(",");
                sb.Append("\"reason\":").Append(JsonStringOrNull(item.Reason)).Append(",");
                sb.Append("\"action\":").Append(JsonStringOrNull(item.Action));
                sb.Append("}");
            }

            sb.Append("],\"warnings\":[");

            for (int i = 0; i < warnings.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }

                TelemetryDiagnosticWarning item = warnings[i];
                sb.Append("{");
                sb.Append("\"code\":").Append(JsonStringOrNull(item.Code)).Append(",");
                sb.Append("\"message\":").Append(JsonStringOrNull(item.Message));
                sb.Append("}");
            }

            sb.Append("],");
            sb.Append("\"aircraftAdapter\":").Append(JsonStringOrNull(adapterResult != null ? adapterResult.AdapterName : "GenericAircraftAdapter")).Append(",");
            sb.Append("\"fenixDetected\":").Append(Bool(adapterResult != null && adapterResult.FenixDetected)).Append(",");
            sb.Append("\"fenixLvarSource\":").Append(JsonStringOrNull(adapterResult != null ? adapterResult.FenixLvarSource : "unavailable")).Append(",");
            sb.Append("\"fenixVariablesDiscovered\":").Append(adapterResult != null ? adapterResult.FenixVariablesDiscovered.ToString(CultureInfo.InvariantCulture) : "0").Append(",");
            sb.Append("\"fenixVariablesReadable\":").Append(adapterResult != null ? adapterResult.FenixVariablesReadable.ToString(CultureInfo.InvariantCulture) : "0").Append(",");
            sb.Append("\"identity\":{");
            sb.Append("\"title\":").Append(JsonStringOrNull(identity != null ? identity.Title : null)).Append(",");
            sb.Append("\"atcModel\":").Append(JsonStringOrNull(identity != null ? identity.AtcModel : null)).Append(",");
            sb.Append("\"atcType\":").Append(JsonStringOrNull(identity != null ? identity.AtcType : null)).Append(",");
            sb.Append("\"simObjectTitle\":").Append(JsonStringOrNull(identity != null ? identity.SimObjectTitle : null)).Append(",");
            sb.Append("\"packagePath\":").Append(JsonStringOrNull(identity != null ? identity.PackagePath : null)).Append(",");
            sb.Append("\"detectedFamily\":").Append(JsonStringOrNull(identity != null ? identity.DetectedFamily : null)).Append(",");
            sb.Append("\"detectedVariant\":").Append(JsonStringOrNull(identity != null ? identity.DetectedVariant : null));
            sb.Append("},");
            sb.Append("\"systems\":{");
            sb.Append("\"apu\":{");
            sb.Append("\"genericValue\":").Append(JsonStringOrNull(genericSystems != null ? genericSystems.ApuStatus : null)).Append(",");
            sb.Append("\"fenixRaw\":").Append(JsonNullableDoubleMap(adapterResult != null && adapterResult.Apu != null ? adapterResult.Apu.RawValues : null)).Append(",");
            sb.Append("\"selected\":").Append(JsonStringOrNull(adapterResult != null && adapterResult.Apu != null ? adapterResult.Apu.Status : null)).Append(",");
            sb.Append("\"reason\":").Append(JsonStringOrNull(adapterResult != null && adapterResult.Apu != null ? adapterResult.Apu.SelectionReason : null));
            sb.Append("},");
            sb.Append("\"flightDirector\":{");
            sb.Append("\"genericValue\":").Append(JsonBoolOrNull(genericSystems != null ? genericSystems.FlightDirectorEnabled : null)).Append(",");
            sb.Append("\"fenixRaw\":{");
            sb.Append("\"fd1\":").Append(Num(adapterResult != null && adapterResult.Autopilot != null ? GetRawValue(adapterResult.Autopilot.RawValues, "I_FCU_EFIS1_FD", "S_FCU_EFIS1_FD") : null)).Append(",");
            sb.Append("\"fd2\":").Append(Num(adapterResult != null && adapterResult.Autopilot != null ? GetRawValue(adapterResult.Autopilot.RawValues, "I_FCU_EFIS2_FD", "S_FCU_EFIS2_FD") : null));
            sb.Append("},");
            sb.Append("\"selected\":").Append(JsonBoolOrNull(adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.FlightDirectorEnabled : null)).Append(",");
            sb.Append("\"reason\":").Append(JsonStringOrNull(adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null));
            sb.Append("},");
            sb.Append("\"autopilot\":{");
            sb.Append("\"genericModes\":").Append(JsonStringArray(genericSystems != null && genericSystems.AutopilotModes != null ? new List<string>(genericSystems.AutopilotModes) : new List<string>())).Append(",");
            sb.Append("\"fenixRaw\":").Append(JsonNullableDoubleMap(adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.RawValues : null)).Append(",");
            sb.Append("\"selectedSource\":").Append(JsonStringOrNull(adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.Source : null));
            sb.Append("}");
            sb.Append("}}");
            return sb.ToString();
        }

        private static double? GetRawValue(IDictionary<string, double?> values, params string[] keys)
        {
            if (values == null)
            {
                return null;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                double? value;
                if (values.TryGetValue(keys[i], out value))
                {
                    return value;
                }
            }

            return null;
        }

        private static string JsonNullableDoubleMap(IDictionary<string, double?> values)
        {
            if (values == null || values.Count == 0)
            {
                return "{}";
            }

            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (KeyValuePair<string, double?> pair in values)
            {
                if (!first)
                {
                    sb.Append(",");
                }

                first = false;
                sb.Append("\"").Append(Escape(pair.Key)).Append("\":").Append(Num(pair.Value));
            }
            sb.Append("}");
            return sb.ToString();
        }

        private void LogRejectedTelemetry(IList<TelemetryRejectedValue> rejected)
        {
            foreach (TelemetryRejectedValue item in rejected)
            {
                Log(
                    "Rejected telemetry field " +
                    (item.JsonField ?? "unknown") +
                    " simvar=" + (item.SimVarName ?? "") +
                    " requestedUnit=" + (item.RequestedUnit ?? "") +
                    " raw=" + (item.RawValue ?? "null") +
                    " converted=" + (item.ConvertedValue ?? "null") +
                    " sanity=" + (item.SanityRange ?? "") +
                    " reason=" + (item.Reason ?? "") +
                    " action=" + (item.Action ?? ""));
            }
        }

        private void LogDiagnosticWarnings(IList<TelemetryDiagnosticWarning> warnings)
        {
            foreach (TelemetryDiagnosticWarning item in warnings)
            {
                Log("Telemetry warning code=" + (item.Code ?? "") + " message=" + (item.Message ?? ""));
            }
        }

        private string ResolveComFrequency(
            string jsonField,
            string simVarName,
            double available,
            double rawFrequencyBcd16,
            IList<TelemetryRejectedValue> rejected)
        {
            if (!IsTruthy(available))
            {
                rejected.Add(TelemetryMath.CreateRejectedValue(
                    jsonField,
                    TelemetryMath.FormatNumeric(rawFrequencyBcd16),
                    null,
                    "unavailable",
                    "null"));
                return null;
            }

            string formattedFrequency = FormatComFrequencyBcd16(rawFrequencyBcd16);
            return TelemetryMath.ValidateFrequencyString(jsonField, rawFrequencyBcd16, formattedFrequency, rejected);
        }

        private string ResolveNavFrequency(
            string jsonField,
            string simVarName,
            double available,
            double rawFrequencyMhz,
            IList<TelemetryRejectedValue> rejected)
        {
            if (!IsTruthy(available))
            {
                rejected.Add(TelemetryMath.CreateRejectedValue(
                    jsonField,
                    TelemetryMath.FormatNumeric(rawFrequencyMhz),
                    null,
                    "unavailable",
                    "null"));
                return null;
            }

            return TelemetryMath.ValidateFrequencyString(jsonField, rawFrequencyMhz, FormatFrequency(rawFrequencyMhz), rejected);
        }

        private static int GetDefinitionIndex(IReadOnlyList<SimVarDefinition> definitions, string structFieldName)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                if (string.Equals(definitions[i].StructFieldName, structFieldName, StringComparison.Ordinal))
                {
                    return i + 1;
                }
            }

            return -1;
        }

        private string FindDefinitionName(int oneBasedIndex)
        {
            var matches = new List<string>();
            AddDefinitionMatch(matches, identityDefinitionNames, oneBasedIndex, "Identity");
            AddDefinitionMatch(matches, coreFlightDefinitionNames, oneBasedIndex, "CoreFlight");
            AddDefinitionMatch(matches, weatherDefinitionNames, oneBasedIndex, "Weather");
            AddDefinitionMatch(matches, radioDefinitionNames, oneBasedIndex, "Radio");
            AddDefinitionMatch(matches, fuelDefinitionNames, oneBasedIndex, "Fuel");
            AddDefinitionMatch(matches, engineDefinitionNames, oneBasedIndex, "Engine");
            AddDefinitionMatch(matches, autopilotDefinitionNames, oneBasedIndex, "Autopilot");

            if (matches.Count == 0)
            {
                return null;
            }

            return string.Join(" | ", matches.ToArray());
        }

        private static void AddDefinitionMatch(List<string> matches, List<string> definitions, int oneBasedIndex, string groupName)
        {
            int zeroBasedIndex = oneBasedIndex - 1;
            if (zeroBasedIndex >= 0 && zeroBasedIndex < definitions.Count)
            {
                matches.Add(groupName + ":" + definitions[zeroBasedIndex]);
            }
        }

        private static CoreFlightData CreateUnavailableCoreFlightData()
        {
            return new CoreFlightData();
        }

        private static WeatherData CreateUnavailableWeatherData()
        {
            return new WeatherData();
        }

        private static RadioData CreateUnavailableRadioData()
        {
            return new RadioData();
        }

        private static FuelData CreateUnavailableFuelData()
        {
            return new FuelData();
        }

        private static EngineData CreateUnavailableEngineData()
        {
            return new EngineData();
        }

        private static AutopilotData CreateUnavailableAutopilotData()
        {
            return new AutopilotData();
        }

        private static string FormatFrequency(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                return null;
            }

            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string FormatComFrequencyBcd16(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                return null;
            }

            int bcd = (int)Math.Round(value);
            int nibble1 = (bcd >> 12) & 0xF;
            int nibble2 = (bcd >> 8) & 0xF;
            int nibble3 = (bcd >> 4) & 0xF;
            int nibble4 = bcd & 0xF;

            double mhz = 100.0 + (nibble1 * 10.0) + nibble2 + (nibble3 / 10.0) + (nibble4 / 100.0);
            return mhz.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string FormatTransponder(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            {
                return null;
            }

            int rounded = (int)Math.Round(value);
            return rounded.ToString("0000", CultureInfo.InvariantCulture);
        }

        private static string MapEngineType(double value, string aircraftShort)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return InferEngineTypeFromAircraft(aircraftShort);
            }

            switch ((int)Math.Round(value))
            {
                case 0: return "ENGINE_TYPE_PISTON";
                case 1: return "ENGINE_TYPE_JET";
                case 2: return "ENGINE_TYPE_NONE";
                case 3: return "ENGINE_TYPE_HELO_TURBINE";
                case 4: return "ENGINE_TYPE_UNSUPPORTED";
                case 5: return "ENGINE_TYPE_TURBOPROP";
                case 6: return "ENGINE_TYPE_ELECTRIC";
                default: return InferEngineTypeFromAircraft(aircraftShort) ?? "ENGINE_TYPE_UNKNOWN";
            }
        }

        private static string InferEngineTypeFromAircraft(string aircraftShort)
        {
            string upper = Clean(aircraftShort).ToUpperInvariant();

            if (upper.StartsWith("A3") || upper.StartsWith("B7") || upper.StartsWith("B78") || upper.StartsWith("B77") || upper.StartsWith("B74"))
            {
                return "ENGINE_TYPE_JET";
            }

            if (upper.StartsWith("C172") || upper.StartsWith("DA40") || upper.StartsWith("DA62"))
            {
                return "ENGINE_TYPE_PISTON";
            }

            if (upper.StartsWith("TBM"))
            {
                return "ENGINE_TYPE_TURBOPROP";
            }

            return null;
        }

        private static string BuildApuStatus(EngineData t)
        {
            if (double.IsNaN(t.apuPctRpm) || double.IsInfinity(t.apuPctRpm))
            {
                return null;
            }

            if (IsTruthy(t.apuGeneratorActive) || t.apuPctRpm >= 95.0)
            {
                return "running";
            }

            if (IsTruthy(t.apuSwitch) || t.apuPctRpm > 1.0)
            {
                return "starting";
            }

            return "off";
        }

        private string BuildFuelTankJson(
            string position,
            double capacityGallons,
            double rawLevel,
            double? validFuelWeightPerGallon,
            IList<TelemetryRejectedValue> rejected,
            IList<TelemetryDiagnosticWarning> warnings)
        {
            var sb = new StringBuilder();
            sb.Append("{\"position\":\"").Append(Escape(position)).Append("\"");

            double? capacityKgs = null;
            if (validFuelWeightPerGallon.HasValue)
            {
                capacityKgs = TelemetryMath.ValidateNumeric(
                    "fuelTanks[*].capacityKgs",
                    capacityGallons,
                    TelemetryMath.GallonsToKilograms(capacityGallons, validFuelWeightPerGallon.Value),
                    rejected);
            }
            else
            {
                rejected.Add(TelemetryMath.CreateRejectedValue(
                    "fuelTanks[*].capacityKgs",
                    TelemetryMath.FormatNumeric(capacityGallons),
                    null,
                    "invalid_fuel_density",
                    "null"));
            }

            double? percentageFilled = TelemetryMath.ConvertFuelLevelToPercent(rawLevel, warnings);
            if (!percentageFilled.HasValue && TelemetryMath.IsFinite(rawLevel))
            {
                rejected.Add(TelemetryMath.CreateRejectedValue(
                    "fuelTanks[*].percentageFilled",
                    TelemetryMath.FormatNumeric(rawLevel),
                    null,
                    "invalid_fuel_level",
                    "null"));
            }

            sb.Append(",\"capacityKgs\":").Append(Num(capacityKgs));
            sb.Append(",\"percentageFilled\":").Append(Num(percentageFilled));
            sb.Append("}");
            return sb.ToString();
        }

        private static double GallonsToKilograms(double gallons, double poundsPerGallon)
        {
            if (double.IsNaN(gallons) || double.IsInfinity(gallons) || gallons < 0)
            {
                return double.NaN;
            }

            if (double.IsNaN(poundsPerGallon) || double.IsInfinity(poundsPerGallon) || poundsPerGallon <= 0)
            {
                return double.NaN;
            }

            return gallons * poundsPerGallon * 0.45359237;
        }

        private static double QuantityToPercent(double quantityGallons, double capacityGallons)
        {
            if (double.IsNaN(quantityGallons) || double.IsInfinity(quantityGallons) || quantityGallons < 0)
            {
                return double.NaN;
            }

            if (double.IsNaN(capacityGallons) || double.IsInfinity(capacityGallons) || capacityGallons <= 0)
            {
                return double.NaN;
            }

            return (quantityGallons / capacityGallons) * 100.0;
        }

        private static double MetersToFeet(double meters)
        {
            if (double.IsNaN(meters) || double.IsInfinity(meters))
            {
                return meters;
            }

            return meters * 3.280839895;
        }

        private static double FeetToMeters(double feet)
        {
            if (double.IsNaN(feet) || double.IsInfinity(feet))
            {
                return feet;
            }

            return feet / 3.280839895;
        }

        private static double MetersPerSecondToKnots(double metersPerSecond)
        {
            if (double.IsNaN(metersPerSecond) || double.IsInfinity(metersPerSecond))
            {
                return metersPerSecond;
            }

            return metersPerSecond * 1.94384449;
        }

        private static double KnotsToMetersPerSecond(double knots)
        {
            if (double.IsNaN(knots) || double.IsInfinity(knots))
            {
                return knots;
            }

            return knots / 1.94384449;
        }

        private static double FeetPerMinuteToMetersPerSecond(double feetPerMinute)
        {
            if (double.IsNaN(feetPerMinute) || double.IsInfinity(feetPerMinute))
            {
                return feetPerMinute;
            }

            return feetPerMinute * 0.00508;
        }

        private static double RadiansToDegrees(double radians)
        {
            if (double.IsNaN(radians) || double.IsInfinity(radians))
            {
                return radians;
            }

            return radians * (180.0 / Math.PI);
        }

        private static double MetersToKilometers(double meters)
        {
            if (double.IsNaN(meters) || double.IsInfinity(meters))
            {
                return meters;
            }

            return meters / 1000.0;
        }

        private static double InchesHgToPascals(double inchesHg)
        {
            if (double.IsNaN(inchesHg) || double.IsInfinity(inchesHg))
            {
                return inchesHg;
            }

            return inchesHg * 3386.389;
        }

        private static double MillibarsToPascals(double millibars)
        {
            if (double.IsNaN(millibars) || double.IsInfinity(millibars))
            {
                return millibars;
            }

            return millibars * 100.0;
        }

        private static List<string> BuildAutopilotModes(AutopilotData t)
        {
            var modes = new List<string>();

            if (IsTruthy(t.autopilotVerticalSpeedHoldActive))
            {
                modes.Add("AUTOPILOT_MODE_VERTICAL_SPEED_HOLD");
            }

            if (IsTruthy(t.autopilotAltitudeHoldActive))
            {
                modes.Add("AUTOPILOT_MODE_ALTITUDE_HOLD");
            }

            if (IsTruthy(t.autopilotHeadingLockActive))
            {
                modes.Add("AUTOPILOT_MODE_HEADING_HOLD");
            }

            if (IsTruthy(t.autopilotAirspeedHoldActive))
            {
                modes.Add("AUTOPILOT_MODE_AIRSPEED_HOLD");
            }

            if (IsTruthy(t.autopilotMachHoldActive))
            {
                modes.Add("AUTOPILOT_MODE_MACH_HOLD");
            }

            return modes;
        }

        private static string JsonStringArray(List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "[]";
            }

            var sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }
                sb.Append("\"").Append(Escape(values[i])).Append("\"");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string JsonStringOrNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "null";
            }

            return "\"" + Escape(value) + "\"";
        }

        private static string Escape(string value)
        {
            return (value ?? "")
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }

    class MsfsAutostartManager
    {
        private const string AppName = "Simple Sim Connector";

        public static string Install(string exePath)
        {
            string[] baseFolders = GetCandidateBaseFolders();
            int changedCount = 0;
            string result = "";

            foreach (string baseFolder in baseFolders)
            {
                if (!Directory.Exists(baseFolder))
                {
                    continue;
                }

                string exeXmlPath = Path.Combine(baseFolder, "EXE.xml");
                InstallIntoExeXml(exeXmlPath, exePath);
                changedCount++;

                result += exeXmlPath + Environment.NewLine;
            }

            if (changedCount == 0)
            {
                throw new Exception(
                    "Could not find the MSFS 2024 LocalCache folder. Start MSFS 2024 once, then try again."
                );
            }

            return result.Trim();
        }

        public static string Uninstall()
        {
            string[] baseFolders = GetCandidateBaseFolders();
            int changedCount = 0;
            string result = "";

            foreach (string baseFolder in baseFolders)
            {
                if (!Directory.Exists(baseFolder))
                {
                    continue;
                }

                string exeXmlPath = Path.Combine(baseFolder, "EXE.xml");

                if (!File.Exists(exeXmlPath))
                {
                    continue;
                }

                bool removed = RemoveFromExeXml(exeXmlPath);

                if (removed)
                {
                    changedCount++;
                    result += exeXmlPath + Environment.NewLine;
                }
            }

            if (changedCount == 0)
            {
                return "No Simple Sim Connector autostart entry was found.";
            }

            return result.Trim();
        }

        private static string[] GetCandidateBaseFolders()
        {
            return new string[]
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Packages\Microsoft.Limitless_8wekyb3d8bbwe\LocalCache"
                ),

                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft Flight Simulator 2024"
                )
            };
        }

        private static void InstallIntoExeXml(string exeXmlPath, string exePath)
        {
            string directory = Path.GetDirectoryName(exeXmlPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            XmlDocument doc = LoadOrCreateExeXml(exeXmlPath);
            XmlElement root = doc.DocumentElement;

            if (File.Exists(exeXmlPath))
            {
                string backupPath = exeXmlPath + ".backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                File.Copy(exeXmlPath, backupPath, true);
            }

            XmlElement addon = FindAddonByName(root, AppName);

            if (addon == null)
            {
                addon = doc.CreateElement("Launch.Addon");
                root.AppendChild(addon);
            }
            else
            {
                addon.RemoveAll();
            }

            AppendElement(doc, addon, "Name", AppName);
            AppendElement(doc, addon, "Disabled", "False");
            AppendElement(doc, addon, "ManualLoad", "False");
            AppendElement(doc, addon, "Path", exePath);
            AppendElement(doc, addon, "CommandLine", "");
            AppendElement(doc, addon, "NewConsole", "False");

            SaveExeXml(doc, exeXmlPath);
        }

        private static bool RemoveFromExeXml(string exeXmlPath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(exeXmlPath);

            XmlElement root = doc.DocumentElement;
            XmlElement addon = FindAddonByName(root, AppName);

            if (addon == null)
            {
                return false;
            }

            string backupPath = exeXmlPath + ".backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(exeXmlPath, backupPath, true);

            root.RemoveChild(addon);
            SaveExeXml(doc, exeXmlPath);

            return true;
        }

        private static XmlDocument LoadOrCreateExeXml(string exeXmlPath)
        {
            XmlDocument doc = new XmlDocument();

            if (File.Exists(exeXmlPath))
            {
                doc.Load(exeXmlPath);
                return doc;
            }

            string xml =
                "<?xml version=\"1.0\" encoding=\"Windows-1252\"?>" +
                "<SimBase.Document Type=\"Launch\" version=\"1,0\">" +
                "<Descr>Auto launch external applications on MSFS start</Descr>" +
                "<Filename>EXE.xml</Filename>" +
                "<Disabled>False</Disabled>" +
                "<Launch.ManualLoad>False</Launch.ManualLoad>" +
                "</SimBase.Document>";

            doc.LoadXml(xml);
            return doc;
        }

        private static XmlElement FindAddonByName(XmlElement root, string name)
        {
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (node.Name != "Launch.Addon")
                {
                    continue;
                }

                XmlElement element = (XmlElement)node;
                string addonName = GetChildText(element, "Name");

                if (string.Equals(addonName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return element;
                }
            }

            return null;
        }

        private static string GetChildText(XmlElement parent, string childName)
        {
            foreach (XmlNode node in parent.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element && node.Name == childName)
                {
                    return node.InnerText;
                }
            }

            return "";
        }

        private static void AppendElement(XmlDocument doc, XmlElement parent, string name, string value)
        {
            XmlElement element = doc.CreateElement(name);
            element.InnerText = value ?? "";
            parent.AppendChild(element);
        }

        private static void SaveExeXml(XmlDocument doc, string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = Encoding.GetEncoding("Windows-1252");

            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                doc.Save(writer);
            }
        }
    }
}
