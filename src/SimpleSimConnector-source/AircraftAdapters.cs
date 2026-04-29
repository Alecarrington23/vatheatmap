using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SimpleSimConnector
{
    public sealed class AircraftIdentityInfo
    {
        public string Title { get; set; }
        public string AtcModel { get; set; }
        public string AtcType { get; set; }
        public string SimObjectTitle { get; set; }
        public string PackagePath { get; set; }
        public string DetectedFamily { get; set; }
        public string DetectedVariant { get; set; }
    }

    public sealed class GenericSystemsData
    {
        public string ApuStatus { get; set; }
        public bool? YawDamperEnabled { get; set; }
        public bool? FlightDirectorEnabled { get; set; }
        public IList<string> AutopilotModes { get; set; }
        public double? AirspeedHoldMetersPerSecond { get; set; }
        public double? MachHoldMach { get; set; }
        public double? AltitudeHoldMeters { get; set; }
        public double? HeadingLockDegrees { get; set; }
        public double? PitchHoldDegrees { get; set; }
        public double? VerticalSpeedHoldMetersPerSecond { get; set; }
    }

    public sealed class AircraftAdapterContext
    {
        public AircraftIdentityInfo Identity { get; set; }
        public GenericSystemsData Generic { get; set; }
        public IReadOnlyDictionary<string, double?> CustomVariableValues { get; set; }
        public ISet<string> DiscoveredVariables { get; set; }
        public int DiscoveredVariableCount { get; set; }
        public int ReadableVariableCount { get; set; }
        public string CustomVariableSource { get; set; }
    }

    public sealed class AdapterApuState
    {
        public string Status { get; set; }
        public string Source { get; set; }
        public string SelectionReason { get; set; }
        public IDictionary<string, double?> RawValues { get; set; }
    }

    public sealed class AdapterAutopilotState
    {
        public string Source { get; set; }
        public string SelectionReason { get; set; }
        public bool? FlightDirectorEnabled { get; set; }
        public bool? FlightDirector1Enabled { get; set; }
        public bool? FlightDirector2Enabled { get; set; }
        public bool? Ap1Engaged { get; set; }
        public bool? Ap2Engaged { get; set; }
        public bool? AutoThrottleArmed { get; set; }
        public bool? AutoThrottleActive { get; set; }
        public double? SelectedSpeedMetersPerSecond { get; set; }
        public double? SelectedMach { get; set; }
        public double? SelectedHeadingDegrees { get; set; }
        public double? SelectedAltitudeMeters { get; set; }
        public double? SelectedVerticalSpeedMetersPerSecond { get; set; }
        public string LateralMode { get; set; }
        public string VerticalMode { get; set; }
        public bool? ManagedSpeed { get; set; }
        public bool? ManagedLateral { get; set; }
        public bool? ManagedVertical { get; set; }
        public bool? YawDamperEnabled { get; set; }
        public IList<string> Modes { get; set; }
        public IDictionary<string, double?> RawValues { get; set; }
    }

    public sealed class AircraftAdapterResult
    {
        public string AdapterName { get; set; }
        public AircraftIdentityInfo Identity { get; set; }
        public AdapterApuState Apu { get; set; }
        public AdapterAutopilotState Autopilot { get; set; }
        public bool FenixDetected { get; set; }
        public string FenixLvarSource { get; set; }
        public int FenixVariablesDiscovered { get; set; }
        public int FenixVariablesReadable { get; set; }
    }

    public sealed class FenixVariableDiscoveryResult
    {
        public string CockpitBehaviorPath { get; set; }
        public IList<string> CandidateVariables { get; set; }
        public ISet<string> CandidateVariableSet { get; set; }
    }

    public interface IAircraftAdapter
    {
        string Name { get; }
        bool Matches(AircraftIdentityInfo identity);
        AircraftAdapterResult Evaluate(AircraftAdapterContext context);
    }

    public static class AircraftAdapterFactory
    {
        private static readonly IAircraftAdapter[] Adapters =
        {
            new FenixA32xAdapter(),
            new Pmdg777Adapter(),
            new Pmdg737Adapter(),
            new IniBuildsA340Adapter(),
            new IniBuildsA350Adapter(),
            new GenericAircraftAdapter()
        };

        public static AircraftIdentityInfo ResolveIdentity(
            string title,
            string atcModel,
            string atcType,
            string packagePath)
        {
            var identity = new AircraftIdentityInfo
            {
                Title = Clean(title),
                AtcModel = Clean(atcModel),
                AtcType = Clean(atcType),
                SimObjectTitle = Clean(title),
                PackagePath = Clean(packagePath)
            };

            string detectedVariant = DetectVariant(identity.AtcModel)
                ?? DetectVariant(identity.AtcType)
                ?? DetectVariant(identity.Title)
                ?? DetectVariant(identity.PackagePath);

            string combined = (identity.Title + " " + identity.AtcModel + " " + identity.AtcType + " " + identity.PackagePath).ToUpperInvariant();

            if (combined.Contains("FENIX") || combined.Contains("FNX_32X") || combined.Contains("FNX-AIRCRAFT"))
            {
                identity.DetectedFamily = "Fenix A32x";
                identity.DetectedVariant = detectedVariant ?? "unknown";
                return identity;
            }

            if (combined.Contains("PMDG 777") || combined.Contains("PMDG777"))
            {
                identity.DetectedFamily = "PMDG 777";
                identity.DetectedVariant = detectedVariant ?? "unknown";
                return identity;
            }

            if (combined.Contains("PMDG 737") || combined.Contains("PMDG737"))
            {
                identity.DetectedFamily = "PMDG 737";
                identity.DetectedVariant = detectedVariant ?? "unknown";
                return identity;
            }

            if (combined.Contains("INIBUILDS") && (combined.Contains("A340") || combined.Contains("A346")))
            {
                identity.DetectedFamily = "IniBuilds A340";
                identity.DetectedVariant = detectedVariant ?? "A340";
                return identity;
            }

            if (combined.Contains("INIBUILDS") && (combined.Contains("A350") || combined.Contains("A359") || combined.Contains("A35K")))
            {
                identity.DetectedFamily = "IniBuilds A350";
                identity.DetectedVariant = detectedVariant ?? "A359";
                return identity;
            }

            identity.DetectedFamily = "Generic";
            identity.DetectedVariant = detectedVariant ?? "UNKNOWN";
            return identity;
        }

        public static IAircraftAdapter ResolveAdapter(AircraftIdentityInfo identity)
        {
            for (int i = 0; i < Adapters.Length; i++)
            {
                if (Adapters[i].Matches(identity))
                {
                    return Adapters[i];
                }
            }

            return new GenericAircraftAdapter();
        }

        private static string DetectVariant(string text)
        {
            string upper = Clean(text).ToUpperInvariant();
            if (upper.Length == 0)
            {
                return null;
            }

            if (upper.Contains("A-319") || upper.Contains("A319")) return "A319";
            if (upper.Contains("A-320") || upper.Contains("A320")) return "A320";
            if (upper.Contains("A-321") || upper.Contains("A321")) return "A321";
            if (upper.Contains("737-600") || upper.Contains("B736")) return "B736";
            if (upper.Contains("737-700") || upper.Contains("B737")) return "B737";
            if (upper.Contains("737-800") || upper.Contains("B738")) return "B738";
            if (upper.Contains("737-900") || upper.Contains("B739")) return "B739";
            if (upper.Contains("777-200LR") || upper.Contains("777 200LR")) return "B77L";
            if (upper.Contains("777-200ER") || upper.Contains("777 200ER")) return "B772";
            if (upper.Contains("777F")) return "B77F";
            if (upper.Contains("A340") || upper.Contains("A346")) return "A340";
            if (upper.Contains("A350") || upper.Contains("A359")) return "A359";
            return null;
        }

        private static string Clean(string value)
        {
            return (value ?? string.Empty).Trim();
        }
    }

    public static class FenixVariableDiscovery
    {
        private static readonly string[] Keywords =
        {
            "APU", "FD", "FLIGHT_DIRECTOR", "AUTOPILOT", "AP1", "AP2", "ATHR",
            "FCU", "HDG", "TRK", "ALT", "VS", "FPA", "LAT", "VERT", "LOC",
            "APPR", "EXPED", "FAC", "YAW"
        };

        public static FenixVariableDiscoveryResult DiscoverFromXml(string xmlPath)
        {
            var variables = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(xmlPath) || !File.Exists(xmlPath))
            {
                return new FenixVariableDiscoveryResult
                {
                    CockpitBehaviorPath = Clean(xmlPath),
                    CandidateVariables = new List<string>(),
                    CandidateVariableSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                };
            }

            var document = new XmlDocument();
            document.Load(xmlPath);

            XmlNodeList nodes = document.GetElementsByTagName("VAR_NAME");
            foreach (XmlNode node in nodes)
            {
                string candidate = Clean(node.InnerText);
                if (candidate.Length == 0)
                {
                    continue;
                }

                string upper = candidate.ToUpperInvariant();
                for (int i = 0; i < Keywords.Length; i++)
                {
                    if (upper.Contains(Keywords[i]))
                    {
                        variables.Add(candidate);
                        break;
                    }
                }
            }

            return new FenixVariableDiscoveryResult
            {
                CockpitBehaviorPath = xmlPath,
                CandidateVariables = new List<string>(variables),
                CandidateVariableSet = new HashSet<string>(variables, StringComparer.OrdinalIgnoreCase)
            };
        }

        private static string Clean(string value)
        {
            return (value ?? string.Empty).Trim();
        }
    }

    public class GenericAircraftAdapter : IAircraftAdapter
    {
        public virtual string Name
        {
            get { return "GenericAircraftAdapter"; }
        }

        public virtual bool Matches(AircraftIdentityInfo identity)
        {
            return true;
        }

        public virtual AircraftAdapterResult Evaluate(AircraftAdapterContext context)
        {
            GenericSystemsData generic = context != null && context.Generic != null
                ? context.Generic
                : new GenericSystemsData();

            return new AircraftAdapterResult
            {
                AdapterName = Name,
                Identity = context != null ? context.Identity : null,
                FenixDetected = false,
                FenixLvarSource = "unavailable",
                FenixVariablesDiscovered = 0,
                FenixVariablesReadable = 0,
                Apu = new AdapterApuState
                {
                    Status = generic.ApuStatus,
                    Source = "generic-simvar",
                    SelectionReason = "generic aircraft uses SimConnect APU telemetry",
                    RawValues = new Dictionary<string, double?>()
                },
                Autopilot = new AdapterAutopilotState
                {
                    Source = "generic-simvar",
                    SelectionReason = "generic aircraft uses SimConnect autoflight telemetry",
                    FlightDirectorEnabled = generic.FlightDirectorEnabled,
                    FlightDirector1Enabled = null,
                    FlightDirector2Enabled = null,
                    Ap1Engaged = null,
                    Ap2Engaged = null,
                    AutoThrottleArmed = null,
                    AutoThrottleActive = null,
                    SelectedSpeedMetersPerSecond = generic.AirspeedHoldMetersPerSecond,
                    SelectedMach = generic.MachHoldMach,
                    SelectedHeadingDegrees = generic.HeadingLockDegrees,
                    SelectedAltitudeMeters = generic.AltitudeHoldMeters,
                    SelectedVerticalSpeedMetersPerSecond = generic.VerticalSpeedHoldMetersPerSecond,
                    LateralMode = null,
                    VerticalMode = null,
                    ManagedSpeed = null,
                    ManagedLateral = null,
                    ManagedVertical = null,
                    YawDamperEnabled = generic.YawDamperEnabled,
                    Modes = generic.AutopilotModes ?? new List<string>(),
                    RawValues = new Dictionary<string, double?>()
                }
            };
        }
    }

    public sealed class FenixA32xAdapter : GenericAircraftAdapter
    {
        private static readonly string[] RequestedVariables =
        {
            "E_FCU_ALTITUDE",
            "E_FCU_HEADING",
            "E_FCU_SPEED",
            "E_FCU_VS",
            "I_ECAM_APU",
            "I_FCU_AP1",
            "I_FCU_AP2",
            "I_FCU_APPR",
            "I_FCU_ATHR",
            "I_FCU_EFIS1_FD",
            "I_FCU_EFIS2_FD",
            "I_FCU_EXPED",
            "I_FCU_LOC",
            "I_OH_ELEC_APU_GENERATOR_L",
            "I_OH_ELEC_APU_GENERATOR_U",
            "I_OH_ELEC_APU_MASTER_L",
            "I_OH_ELEC_APU_MASTER_U",
            "I_OH_ELEC_APU_START_L",
            "I_OH_ELEC_APU_START_U",
            "I_OH_FLT_CTL_FAC_1_L",
            "I_OH_FLT_CTL_FAC_1_U",
            "I_OH_FLT_CTL_FAC_2_L",
            "I_OH_FLT_CTL_FAC_2_U",
            "I_OH_PNEUMATIC_APU_BLEED_L",
            "I_OH_PNEUMATIC_APU_BLEED_U",
            "S_ECAM_APU",
            "S_FCU_AP1",
            "S_FCU_AP2",
            "S_FCU_APPR",
            "S_FCU_ATHR",
            "S_FCU_EFIS1_FD",
            "S_FCU_EFIS2_FD",
            "S_FCU_EXPED",
            "S_FCU_HDGVS_TRKFPA",
            "S_FCU_LOC",
            "S_FCU_METRIC_ALT",
            "S_FCU_SPD_MACH",
            "S_OH_ELEC_APU_GENERATOR",
            "S_OH_ELEC_APU_MASTER",
            "S_OH_ELEC_APU_START",
            "S_OH_FLT_CTL_FAC_1",
            "S_OH_FLT_CTL_FAC_2",
            "S_OH_PNEUMATIC_APU_BLEED"
        };

        public override string Name
        {
            get { return "FenixA32xAdapter"; }
        }

        public override bool Matches(AircraftIdentityInfo identity)
        {
            return identity != null &&
                string.Equals(identity.DetectedFamily, "Fenix A32x", StringComparison.OrdinalIgnoreCase);
        }

        public static IList<string> GetRequestedVariables(ISet<string> discoveredVariables)
        {
            var requested = new List<string>();

            for (int i = 0; i < RequestedVariables.Length; i++)
            {
                if (discoveredVariables != null && discoveredVariables.Contains(RequestedVariables[i]))
                {
                    requested.Add(RequestedVariables[i]);
                }
            }

            return requested;
        }

        public override AircraftAdapterResult Evaluate(AircraftAdapterContext context)
        {
            GenericSystemsData generic = context != null && context.Generic != null
                ? context.Generic
                : new GenericSystemsData();

            bool readable = context != null &&
                context.CustomVariableValues != null &&
                context.ReadableVariableCount > 0;

            var result = new AircraftAdapterResult
            {
                AdapterName = Name,
                Identity = context != null ? context.Identity : null,
                FenixDetected = true,
                FenixLvarSource = readable ? "direct-simconnect" : "unavailable",
                FenixVariablesDiscovered = context != null ? context.DiscoveredVariableCount : 0,
                FenixVariablesReadable = context != null ? context.ReadableVariableCount : 0
            };

            if (!readable)
            {
                result.Apu = new AdapterApuState
                {
                    Status = null,
                    Source = "fenix-lvar-unavailable",
                    SelectionReason = "Fenix aircraft detected but no readable Fenix LVars are available",
                    RawValues = new Dictionary<string, double?>()
                };

                result.Autopilot = new AdapterAutopilotState
                {
                    Source = "fenix-lvar-unavailable",
                    SelectionReason = "Fenix aircraft detected but no readable Fenix autoflight LVars are available",
                    FlightDirectorEnabled = null,
                    FlightDirector1Enabled = null,
                    FlightDirector2Enabled = null,
                    Ap1Engaged = null,
                    Ap2Engaged = null,
                    AutoThrottleArmed = null,
                    AutoThrottleActive = null,
                    SelectedSpeedMetersPerSecond = null,
                    SelectedMach = null,
                    SelectedHeadingDegrees = null,
                    SelectedAltitudeMeters = null,
                    SelectedVerticalSpeedMetersPerSecond = null,
                    LateralMode = null,
                    VerticalMode = null,
                    ManagedSpeed = null,
                    ManagedLateral = null,
                    ManagedVertical = null,
                    YawDamperEnabled = null,
                    Modes = new List<string>(),
                    RawValues = new Dictionary<string, double?>()
                };

                return result;
            }

            IReadOnlyDictionary<string, double?> values = context.CustomVariableValues;
            var apuRaw = Collect(values,
                "S_OH_ELEC_APU_MASTER",
                "I_OH_ELEC_APU_MASTER_L",
                "I_OH_ELEC_APU_MASTER_U",
                "S_OH_ELEC_APU_START",
                "I_OH_ELEC_APU_START_L",
                "I_OH_ELEC_APU_START_U",
                "S_OH_ELEC_APU_GENERATOR",
                "I_OH_ELEC_APU_GENERATOR_L",
                "I_OH_ELEC_APU_GENERATOR_U",
                "S_OH_PNEUMATIC_APU_BLEED",
                "I_OH_PNEUMATIC_APU_BLEED_L",
                "I_OH_PNEUMATIC_APU_BLEED_U",
                "S_ECAM_APU",
                "I_ECAM_APU");

            bool? apuMaster = AnyKnownBoolean(values,
                "S_OH_ELEC_APU_MASTER",
                "I_OH_ELEC_APU_MASTER_L",
                "I_OH_ELEC_APU_MASTER_U");
            bool? apuStart = AnyKnownBoolean(values,
                "S_OH_ELEC_APU_START",
                "I_OH_ELEC_APU_START_L",
                "I_OH_ELEC_APU_START_U");
            bool? apuGenerator = AnyKnownBoolean(values,
                "S_OH_ELEC_APU_GENERATOR",
                "I_OH_ELEC_APU_GENERATOR_L",
                "I_OH_ELEC_APU_GENERATOR_U");
            bool? apuBleed = AnyKnownBoolean(values,
                "S_OH_PNEUMATIC_APU_BLEED",
                "I_OH_PNEUMATIC_APU_BLEED_L",
                "I_OH_PNEUMATIC_APU_BLEED_U");
            bool? apuEcam = AnyKnownBoolean(values, "S_ECAM_APU", "I_ECAM_APU");

            string apuStatus;
            if (apuBleed == true) apuStatus = "bleed_on";
            else if (apuGenerator == true) apuStatus = "available";
            else if (apuStart == true) apuStatus = "starting";
            else if (apuEcam == true) apuStatus = "running";
            else if (apuMaster == true) apuStatus = "unknown";
            else if (AllKnownFalse(apuBleed, apuGenerator, apuStart, apuEcam, apuMaster)) apuStatus = "off";
            else apuStatus = "unknown";

            bool? fd1 = FirstKnownBoolean(values, "I_FCU_EFIS1_FD", "S_FCU_EFIS1_FD");
            bool? fd2 = FirstKnownBoolean(values, "I_FCU_EFIS2_FD", "S_FCU_EFIS2_FD");
            bool? flightDirector = CombineConfirmed(fd1, fd2);
            bool? ap1 = FirstKnownBoolean(values, "I_FCU_AP1", "S_FCU_AP1");
            bool? ap2 = FirstKnownBoolean(values, "I_FCU_AP2", "S_FCU_AP2");
            bool? athr = FirstKnownBoolean(values, "I_FCU_ATHR", "S_FCU_ATHR");
            bool? trackFpaMode = FirstKnownBoolean(values, "S_FCU_HDGVS_TRKFPA");
            bool? speedMachMode = FirstKnownBoolean(values, "S_FCU_SPD_MACH");
            bool? metricAltMode = FirstKnownBoolean(values, "S_FCU_METRIC_ALT");
            bool? fac1 = AnyKnownBoolean(values, "S_OH_FLT_CTL_FAC_1", "I_OH_FLT_CTL_FAC_1_L", "I_OH_FLT_CTL_FAC_1_U");
            bool? fac2 = AnyKnownBoolean(values, "S_OH_FLT_CTL_FAC_2", "I_OH_FLT_CTL_FAC_2_L", "I_OH_FLT_CTL_FAC_2_U");
            bool? yawDamper = CombineConfirmed(fac1, fac2);

            double? selectedSpeedMetersPerSecond = null;
            double? selectedMach = null;
            double? rawSelectedSpeed = FirstFinite(values, "E_FCU_SPEED");
            if (rawSelectedSpeed.HasValue)
            {
                if (speedMachMode == true)
                {
                    double machValue = ScaleFenixMach(rawSelectedSpeed.Value);
                    if (machValue >= 0 && machValue <= 1.2)
                    {
                        selectedMach = machValue;
                    }
                }
                else
                {
                    double speedKnots = ScaleFenixSelectedSpeedKnots(rawSelectedSpeed.Value);
                    if (speedKnots > 0 && speedKnots <= 450)
                    {
                        selectedSpeedMetersPerSecond = TelemetryMath.KnotsToMetersPerSecond(speedKnots);
                    }
                }
            }

            double? selectedHeadingDegrees = null;
            double? rawHeading = FirstFinite(values, "E_FCU_HEADING");
            if (rawHeading.HasValue)
            {
                double headingValue = ScaleFenixHeadingDegrees(rawHeading.Value);
                if (headingValue >= 0 && headingValue <= 360)
                {
                    selectedHeadingDegrees = NormalizeHeading(headingValue);
                }
            }

            double? selectedAltitudeMeters = null;
            double? rawAltitude = FirstFinite(values, "E_FCU_ALTITUDE");
            if (rawAltitude.HasValue && rawAltitude.Value > 0)
            {
                double altitudeFeet = ScaleFenixSelectedAltitudeFeet(rawAltitude.Value, metricAltMode == true);
                if (altitudeFeet > 0)
                {
                    selectedAltitudeMeters = TelemetryMath.FeetToMeters(altitudeFeet);
                }
            }

            double? selectedVerticalSpeedMetersPerSecond = null;
            double? rawVs = FirstFinite(values, "E_FCU_VS");
            if (rawVs.HasValue && trackFpaMode != true)
            {
                selectedVerticalSpeedMetersPerSecond = TelemetryMath.FeetPerMinuteToMetersPerSecond(ScaleFenixSelectedVerticalSpeedFeetPerMinute(rawVs.Value));
            }

            var autopilotRaw = Collect(values,
                "I_FCU_AP1",
                "I_FCU_AP2",
                "I_FCU_APPR",
                "I_FCU_ATHR",
                "I_FCU_EFIS1_FD",
                "I_FCU_EFIS2_FD",
                "I_FCU_EXPED",
                "I_FCU_LOC",
                "I_OH_FLT_CTL_FAC_1_L",
                "I_OH_FLT_CTL_FAC_1_U",
                "I_OH_FLT_CTL_FAC_2_L",
                "I_OH_FLT_CTL_FAC_2_U",
                "S_FCU_AP1",
                "S_FCU_AP2",
                "S_FCU_APPR",
                "S_FCU_ATHR",
                "S_FCU_EFIS1_FD",
                "S_FCU_EFIS2_FD",
                "S_FCU_EXPED",
                "S_FCU_HDGVS_TRKFPA",
                "S_FCU_LOC",
                "S_FCU_METRIC_ALT",
                "S_FCU_SPD_MACH",
                "S_OH_FLT_CTL_FAC_1",
                "S_OH_FLT_CTL_FAC_2",
                "E_FCU_SPEED",
                "E_FCU_HEADING",
                "E_FCU_ALTITUDE",
                "E_FCU_VS");

            result.Apu = new AdapterApuState
            {
                Status = apuStatus,
                Source = "fenix-lvar",
                SelectionReason = "fenix adapter overrides generic APU SimVars",
                RawValues = apuRaw
            };

            result.Autopilot = new AdapterAutopilotState
            {
                Source = "fenix-lvar",
                SelectionReason = "fenix adapter overrides generic autoflight state with discovered LVars",
                FlightDirectorEnabled = flightDirector,
                FlightDirector1Enabled = fd1,
                FlightDirector2Enabled = fd2,
                Ap1Engaged = ap1,
                Ap2Engaged = ap2,
                AutoThrottleArmed = athr,
                AutoThrottleActive = null,
                SelectedSpeedMetersPerSecond = selectedSpeedMetersPerSecond,
                SelectedMach = selectedMach,
                SelectedHeadingDegrees = selectedHeadingDegrees,
                SelectedAltitudeMeters = selectedAltitudeMeters,
                SelectedVerticalSpeedMetersPerSecond = selectedVerticalSpeedMetersPerSecond,
                LateralMode = null,
                VerticalMode = null,
                ManagedSpeed = null,
                ManagedLateral = null,
                ManagedVertical = null,
                YawDamperEnabled = yawDamper,
                Modes = new List<string>(),
                RawValues = autopilotRaw
            };

            return result;
        }

        private static IDictionary<string, double?> Collect(
            IReadOnlyDictionary<string, double?> values,
            params string[] names)
        {
            var collected = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);

            if (values == null)
            {
                return collected;
            }

            for (int i = 0; i < names.Length; i++)
            {
                double? value;
                if (values.TryGetValue(names[i], out value))
                {
                    collected[names[i]] = value;
                }
            }

            return collected;
        }

        private static double? FirstFinite(IReadOnlyDictionary<string, double?> values, params string[] names)
        {
            if (values == null)
            {
                return null;
            }

            for (int i = 0; i < names.Length; i++)
            {
                double? value;
                if (values.TryGetValue(names[i], out value) &&
                    value.HasValue &&
                    TelemetryMath.IsFinite(value.Value))
                {
                    return value.Value;
                }
            }

            return null;
        }

        private static bool? FirstKnownBoolean(IReadOnlyDictionary<string, double?> values, params string[] names)
        {
            double? value = FirstFinite(values, names);
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value >= 0.5;
        }

        private static bool? AnyKnownBoolean(IReadOnlyDictionary<string, double?> values, params string[] names)
        {
            bool sawKnown = false;

            for (int i = 0; i < names.Length; i++)
            {
                double? value = FirstFinite(values, names[i]);
                if (!value.HasValue)
                {
                    continue;
                }

                sawKnown = true;
                if (value.Value >= 0.5)
                {
                    return true;
                }
            }

            return sawKnown ? (bool?)false : null;
        }

        private static bool? CombineConfirmed(bool? left, bool? right)
        {
            if (left == true || right == true)
            {
                return true;
            }

            if (left == false && right == false)
            {
                return false;
            }

            return null;
        }

        private static bool AllKnownFalse(params bool?[] values)
        {
            bool sawKnown = false;

            for (int i = 0; i < values.Length; i++)
            {
                if (!values[i].HasValue)
                {
                    continue;
                }

                sawKnown = true;
                if (values[i].Value)
                {
                    return false;
                }
            }

            return sawKnown;
        }

        private static double ScaleFenixSelectedSpeedKnots(double rawValue)
        {
            if (!TelemetryMath.IsFinite(rawValue))
            {
                return rawValue;
            }

            if (rawValue > 0 && rawValue < 100)
            {
                return rawValue * 10.0;
            }

            return rawValue;
        }

        private static double ScaleFenixMach(double rawValue)
        {
            if (!TelemetryMath.IsFinite(rawValue))
            {
                return rawValue;
            }

            if (rawValue > 1.2 && rawValue < 200)
            {
                return rawValue / 100.0;
            }

            if (rawValue > 200 && rawValue < 2000)
            {
                return rawValue / 1000.0;
            }

            return rawValue;
        }

        private static double ScaleFenixHeadingDegrees(double rawValue)
        {
            if (!TelemetryMath.IsFinite(rawValue))
            {
                return rawValue;
            }

            if (rawValue >= 0 && rawValue <= 36)
            {
                return rawValue * 10.0;
            }

            return rawValue;
        }

        private static double ScaleFenixSelectedAltitudeFeet(double rawValue, bool metricAltMode)
        {
            if (!TelemetryMath.IsFinite(rawValue))
            {
                return rawValue;
            }

            if (metricAltMode && rawValue > 0 && rawValue < 1000)
            {
                return TelemetryMath.MetersToFeet(rawValue * 100.0);
            }

            if (rawValue > 0 && rawValue < 1000)
            {
                return rawValue * 1000.0;
            }

            return rawValue;
        }

        private static double ScaleFenixSelectedVerticalSpeedFeetPerMinute(double rawValue)
        {
            if (!TelemetryMath.IsFinite(rawValue))
            {
                return rawValue;
            }

            if (rawValue != 0 && rawValue > -100 && rawValue < 100)
            {
                return rawValue * 100.0;
            }

            return rawValue;
        }

        private static double NormalizeHeading(double heading)
        {
            heading = heading % 360.0;
            if (heading < 0)
            {
                heading += 360.0;
            }

            return heading;
        }
    }

    public sealed class Pmdg777Adapter : GenericAircraftAdapter
    {
        public override string Name
        {
            get { return "Pmdg777Adapter"; }
        }

        public override bool Matches(AircraftIdentityInfo identity)
        {
            return identity != null &&
                string.Equals(identity.DetectedFamily, "PMDG 777", StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class Pmdg737Adapter : GenericAircraftAdapter
    {
        public override string Name
        {
            get { return "Pmdg737Adapter"; }
        }

        public override bool Matches(AircraftIdentityInfo identity)
        {
            return identity != null &&
                string.Equals(identity.DetectedFamily, "PMDG 737", StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class IniBuildsA340Adapter : GenericAircraftAdapter
    {
        public override string Name
        {
            get { return "IniBuildsA340Adapter"; }
        }

        public override bool Matches(AircraftIdentityInfo identity)
        {
            return identity != null &&
                string.Equals(identity.DetectedFamily, "IniBuilds A340", StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class IniBuildsA350Adapter : GenericAircraftAdapter
    {
        public override string Name
        {
            get { return "IniBuildsA350Adapter"; }
        }

        public override bool Matches(AircraftIdentityInfo identity)
        {
            return identity != null &&
                string.Equals(identity.DetectedFamily, "IniBuilds A350", StringComparison.OrdinalIgnoreCase);
        }
    }
}
