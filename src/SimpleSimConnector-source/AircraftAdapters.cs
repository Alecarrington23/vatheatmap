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
            new FlyByWireA380XAdapter(),
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

            if ((combined.Contains("FLYBYWIRE") || combined.Contains("FLYBYWIRE-AIRCRAFT-A380-842")) &&
                (combined.Contains("A380") || combined.Contains("A388")))
            {
                identity.DetectedFamily = "FlyByWire A380X";
                identity.DetectedVariant = detectedVariant ?? "A388";
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
            if (upper.Contains("A380") || upper.Contains("A388")) return "A388";
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

    public static class IniBuildsVariableDiscovery
    {
        public static FenixVariableDiscoveryResult DiscoverLvarsFromBehaviorXml(string xmlPath, string[] keywords)
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

            string content = File.ReadAllText(xmlPath);
            int index = 0;

            while (index >= 0 && index < content.Length)
            {
                index = content.IndexOf("L:", index, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    break;
                }

                int start = index + 2;
                int end = start;
                while (end < content.Length)
                {
                    char current = content[end];
                    if (!(char.IsLetterOrDigit(current) || current == '_' || current == ':' || current == '-'))
                    {
                        break;
                    }

                    end++;
                }

                string candidate = Clean(content.Substring(start, end - start).TrimEnd(','));
                index = end;

                if (candidate.Length == 0)
                {
                    continue;
                }

                string upper = candidate.ToUpperInvariant();
                for (int i = 0; i < keywords.Length; i++)
                {
                    if (upper.Contains(keywords[i]))
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

        public static FenixVariableDiscoveryResult DiscoverLvarsFromDirectory(string directoryPath, string[] keywords)
        {
            var variables = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                return new FenixVariableDiscoveryResult
                {
                    CockpitBehaviorPath = Clean(directoryPath),
                    CandidateVariables = new List<string>(),
                    CandidateVariableSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                };
            }

            string[] files = Directory.GetFiles(directoryPath, "*.xml", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string content;
                try
                {
                    content = File.ReadAllText(files[i]);
                }
                catch
                {
                    continue;
                }

                ExtractLvars(content, keywords, variables);
            }

            return new FenixVariableDiscoveryResult
            {
                CockpitBehaviorPath = directoryPath,
                CandidateVariables = new List<string>(variables),
                CandidateVariableSet = new HashSet<string>(variables, StringComparer.OrdinalIgnoreCase)
            };
        }

        private static void ExtractLvars(string content, string[] keywords, SortedSet<string> variables)
        {
            int index = 0;

            while (index >= 0 && index < content.Length)
            {
                index = content.IndexOf("L:", index, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    break;
                }

                int start = index + 2;
                int end = start;
                while (end < content.Length)
                {
                    char current = content[end];
                    if (!(char.IsLetterOrDigit(current) || current == '_' || current == ':' || current == '-'))
                    {
                        break;
                    }

                    end++;
                }

                string candidate = Clean(content.Substring(start, end - start).TrimEnd(','));
                index = end;

                if (candidate.Length == 0)
                {
                    continue;
                }

                string upper = candidate.ToUpperInvariant();
                for (int i = 0; i < keywords.Length; i++)
                {
                    if (upper.Contains(keywords[i]))
                    {
                        variables.Add(candidate);
                        break;
                    }
                }
            }
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
        private static readonly string[] RequestedFields =
        {
            "ELEC_APUGen_Sw_ON",
            "ELEC_APU_Selector",
            "AIR_APUBleedAir_Sw_AUTO",
            "MCP_IASMach",
            "MCP_IASBlank",
            "MCP_Heading",
            "MCP_Altitude",
            "MCP_VertSpeed",
            "MCP_VertSpeedBlank",
            "MCP_FD_Sw_On_L",
            "MCP_FD_Sw_On_R",
            "MCP_ATArm_Sw_On_L",
            "MCP_ATArm_Sw_On_R",
            "MCP_annunAP_L",
            "MCP_annunAP_R",
            "MCP_annunAT",
            "MCP_annunLNAV",
            "MCP_annunVNAV",
            "MCP_annunFLCH",
            "MCP_annunHDG_HOLD",
            "MCP_annunVS_FPA",
            "MCP_annunALT_HOLD",
            "MCP_annunLOC",
            "MCP_annunAPP"
        };

        public override string Name
        {
            get { return "Pmdg777Adapter"; }
        }

        public override bool Matches(AircraftIdentityInfo identity)
        {
            return identity != null &&
                string.Equals(identity.DetectedFamily, "PMDG 777", StringComparison.OrdinalIgnoreCase);
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
                FenixDetected = false,
                FenixLvarSource = readable ? "pmdg-777-sdk" : "pmdg-777-sdk-unavailable",
                FenixVariablesDiscovered = RequestedFields.Length,
                FenixVariablesReadable = context != null ? context.ReadableVariableCount : 0
            };

            if (!readable)
            {
                result.Apu = new AdapterApuState
                {
                    Status = null,
                    Source = "pmdg-777-sdk-unavailable",
                    SelectionReason = "PMDG 777 detected but no readable PMDG SDK client data is available",
                    RawValues = new Dictionary<string, double?>()
                };

                result.Autopilot = new AdapterAutopilotState
                {
                    Source = "pmdg-777-sdk-unavailable",
                    SelectionReason = "PMDG 777 detected but no readable PMDG SDK autoflight data is available",
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
                    YawDamperEnabled = generic.YawDamperEnabled,
                    Modes = new List<string>(),
                    RawValues = new Dictionary<string, double?>()
                };

                return result;
            }

            IReadOnlyDictionary<string, double?> values = context.CustomVariableValues;
            var apuRaw = Collect(values,
                "ELEC_APUGen_Sw_ON",
                "ELEC_APU_Selector",
                "AIR_APUBleedAir_Sw_AUTO");

            bool? apuGenerator = FirstKnownBoolean(values, "ELEC_APUGen_Sw_ON");
            bool? apuBleed = FirstKnownBoolean(values, "AIR_APUBleedAir_Sw_AUTO");
            double? apuSelectorRaw = FirstFinite(values, "ELEC_APU_Selector");
            string apuStatus;

            if (apuBleed == true) apuStatus = "bleed_on";
            else if (apuSelectorRaw.HasValue && Math.Round(apuSelectorRaw.Value) >= 2.0) apuStatus = "starting";
            else if (apuGenerator == true) apuStatus = "available";
            else if (apuSelectorRaw.HasValue && Math.Round(apuSelectorRaw.Value) >= 1.0) apuStatus = "running";
            else if (apuSelectorRaw.HasValue && Math.Round(apuSelectorRaw.Value) <= 0.0 && apuGenerator == false && apuBleed == false) apuStatus = "off";
            else apuStatus = "unknown";

            bool? fd1 = FirstKnownBoolean(values, "MCP_FD_Sw_On_L");
            bool? fd2 = FirstKnownBoolean(values, "MCP_FD_Sw_On_R");
            bool? flightDirector = CombineConfirmed(fd1, fd2);
            bool? ap1 = FirstKnownBoolean(values, "MCP_annunAP_L");
            bool? ap2 = FirstKnownBoolean(values, "MCP_annunAP_R");
            bool? autoThrottleArmed = AnyKnownBoolean(values, "MCP_ATArm_Sw_On_L", "MCP_ATArm_Sw_On_R");
            bool? autoThrottleActive = FirstKnownBoolean(values, "MCP_annunAT");
            bool? iasBlank = FirstKnownBoolean(values, "MCP_IASBlank");
            bool? verticalSpeedBlank = FirstKnownBoolean(values, "MCP_VertSpeedBlank");

            double? selectedSpeedMetersPerSecond = null;
            double? selectedMach = null;
            double? iasMachRaw = FirstFinite(values, "MCP_IASMach");
            if (iasMachRaw.HasValue && iasBlank != true)
            {
                if (iasMachRaw.Value > 0.0 && iasMachRaw.Value < 10.0)
                {
                    selectedMach = iasMachRaw.Value;
                }
                else
                {
                    selectedSpeedMetersPerSecond = TelemetryMath.KnotsToMetersPerSecond(iasMachRaw.Value);
                }
            }

            double? selectedHeadingDegrees = FirstFinite(values, "MCP_Heading");
            double? selectedAltitudeMeters = null;
            double? altitudeRaw = FirstFinite(values, "MCP_Altitude");
            if (altitudeRaw.HasValue)
            {
                selectedAltitudeMeters = TelemetryMath.FeetToMeters(altitudeRaw.Value);
            }

            double? selectedVerticalSpeedMetersPerSecond = null;
            double? verticalSpeedRaw = FirstFinite(values, "MCP_VertSpeed");
            if (verticalSpeedRaw.HasValue && verticalSpeedBlank != true)
            {
                selectedVerticalSpeedMetersPerSecond = TelemetryMath.FeetPerMinuteToMetersPerSecond(verticalSpeedRaw.Value);
            }

            bool? lnav = FirstKnownBoolean(values, "MCP_annunLNAV");
            bool? vnav = FirstKnownBoolean(values, "MCP_annunVNAV");
            bool? flch = FirstKnownBoolean(values, "MCP_annunFLCH");
            bool? hdgHold = FirstKnownBoolean(values, "MCP_annunHDG_HOLD");
            bool? vsFpa = FirstKnownBoolean(values, "MCP_annunVS_FPA");
            bool? altHold = FirstKnownBoolean(values, "MCP_annunALT_HOLD");
            bool? loc = FirstKnownBoolean(values, "MCP_annunLOC");
            bool? app = FirstKnownBoolean(values, "MCP_annunAPP");

            var modes = new List<string>();
            AddMode(modes, lnav, "AUTOPILOT_MODE_LNAV");
            AddMode(modes, vnav, "AUTOPILOT_MODE_VNAV");
            AddMode(modes, flch, "AUTOPILOT_MODE_FLCH");
            AddMode(modes, hdgHold, "AUTOPILOT_MODE_HEADING_HOLD");
            AddMode(modes, vsFpa, "AUTOPILOT_MODE_VERTICAL_SPEED");
            AddMode(modes, altHold, "AUTOPILOT_MODE_ALTITUDE_HOLD");
            AddMode(modes, loc, "AUTOPILOT_MODE_LOC");
            AddMode(modes, app, "AUTOPILOT_MODE_APPROACH");

            var autopilotRaw = Collect(values, RequestedFields);

            result.Apu = new AdapterApuState
            {
                Status = apuStatus,
                Source = "pmdg-777-sdk",
                SelectionReason = "PMDG 777 adapter overrides generic APU SimVars with PMDG SDK client data",
                RawValues = apuRaw
            };

            result.Autopilot = new AdapterAutopilotState
            {
                Source = "pmdg-777-sdk",
                SelectionReason = "PMDG 777 adapter overrides generic autoflight state with PMDG SDK client data",
                FlightDirectorEnabled = flightDirector,
                FlightDirector1Enabled = fd1,
                FlightDirector2Enabled = fd2,
                Ap1Engaged = ap1,
                Ap2Engaged = ap2,
                AutoThrottleArmed = autoThrottleArmed,
                AutoThrottleActive = autoThrottleActive,
                SelectedSpeedMetersPerSecond = selectedSpeedMetersPerSecond,
                SelectedMach = selectedMach,
                SelectedHeadingDegrees = selectedHeadingDegrees,
                SelectedAltitudeMeters = selectedAltitudeMeters,
                SelectedVerticalSpeedMetersPerSecond = selectedVerticalSpeedMetersPerSecond,
                LateralMode = DetermineLateralMode(app, loc, lnav, hdgHold),
                VerticalMode = DetermineVerticalMode(vnav, flch, altHold, vsFpa),
                ManagedSpeed = vnav,
                ManagedLateral = lnav,
                ManagedVertical = vnav,
                YawDamperEnabled = generic.YawDamperEnabled,
                Modes = modes,
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
                    !double.IsNaN(value.Value) &&
                    !double.IsInfinity(value.Value))
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

            if (!sawKnown)
            {
                return null;
            }

            return false;
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

        private static void AddMode(IList<string> modes, bool? enabled, string mode)
        {
            if (enabled == true)
            {
                modes.Add(mode);
            }
        }

        private static string DetermineLateralMode(bool? app, bool? loc, bool? lnav, bool? hdgHold)
        {
            if (app == true) return "APP";
            if (loc == true) return "LOC";
            if (lnav == true) return "LNAV";
            if (hdgHold == true) return "HDG_HOLD";
            return null;
        }

        private static string DetermineVerticalMode(bool? vnav, bool? flch, bool? altHold, bool? vsFpa)
        {
            if (vnav == true) return "VNAV";
            if (flch == true) return "FLCH";
            if (altHold == true) return "ALT_HOLD";
            if (vsFpa == true) return "VS_FPA";
            return null;
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

        public override AircraftAdapterResult Evaluate(AircraftAdapterContext context)
        {
            return new AircraftAdapterResult
            {
                AdapterName = Name,
                Identity = context != null ? context.Identity : null,
                FenixDetected = false,
                FenixLvarSource = "pmdg-737-sdk-unavailable",
                FenixVariablesDiscovered = 0,
                FenixVariablesReadable = 0,
                Apu = new AdapterApuState
                {
                    Status = null,
                    Source = "pmdg-737-sdk-unavailable",
                    SelectionReason = "PMDG 737 detected but no PMDG 737 SDK integration is installed in this build",
                    RawValues = new Dictionary<string, double?>()
                },
                Autopilot = new AdapterAutopilotState
                {
                    Source = "pmdg-737-sdk-unavailable",
                    SelectionReason = "PMDG 737 detected but no PMDG 737 SDK integration is installed in this build",
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
                    YawDamperEnabled = context != null && context.Generic != null ? context.Generic.YawDamperEnabled : null,
                    Modes = new List<string>(),
                    RawValues = new Dictionary<string, double?>()
                }
            };
        }
    }

    public sealed class IniBuildsA340Adapter : GenericAircraftAdapter
    {
        private static readonly string[] RequestedVariables =
        {
            "INI_AIR_BLEED_APU",
            "INI_AP1_BUTTON",
            "INI_ap1_on",
            "INI_AP2_BUTTON",
            "INI_ap2_on",
            "INI_APU_AVAILABLE",
            "INI_APU_MASTER_SWITCH",
            "INI_APU_START_BUTTON",
            "INI_ATHR_LIGHT",
            "INI_AUTOLAND_LIGHT",
            "INI_AUTOTHROTTLE_ARMED",
            "INI_FCU_ALTITUDE_MODE_COMMAND",
            "INI_FCU_HDG_VS_COMMAND",
            "INI_FCU_MANAGED_HEADING_BUTTON",
            "INI_FCU_MANAGED_SPEED_BUTTON",
            "INI_FCU_METRIC_STATE",
            "INI_FCU_SELECTED_HEADING_BUTTON",
            "INI_FCU_SELECTED_SPEED_BUTTON",
            "INI_FD1_ON",
            "INI_FD2_ON",
            "INI_GEN_APU_GEN_SWITCH",
            "INI_MCU_LOC_LIGHT",
            "INI_SPD_MACH_BUTTON"
        };

        private static readonly string[] DiscoveryKeywords =
        {
            "APU", "AP1", "AP2", "APPR", "ATHR", "AUTOLAND", "AUTOTHROTTLE",
            "FCU", "FD", "HDG", "HEADING", "LOC", "MACH", "MANAGED",
            "SELECTED", "SPD", "SPEED", "TRK", "FPA", "ALT", "VS", "VERT"
        };

        public override string Name
        {
            get { return "IniBuildsA340Adapter"; }
        }

        public override bool Matches(AircraftIdentityInfo identity)
        {
            return identity != null &&
                string.Equals(identity.DetectedFamily, "IniBuilds A340", StringComparison.OrdinalIgnoreCase);
        }

        public static string[] GetDiscoveryKeywords()
        {
            return DiscoveryKeywords;
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
                FenixDetected = false,
                FenixLvarSource = readable ? "direct-simconnect" : "unavailable",
                FenixVariablesDiscovered = context != null ? context.DiscoveredVariableCount : 0,
                FenixVariablesReadable = context != null ? context.ReadableVariableCount : 0
            };

            if (!readable)
            {
                result.Apu = new AdapterApuState
                {
                    Status = null,
                    Source = "inibuilds-a340-lvar-unavailable",
                    SelectionReason = "IniBuilds A340 detected but no readable custom LVars are available",
                    RawValues = new Dictionary<string, double?>()
                };

                result.Autopilot = new AdapterAutopilotState
                {
                    Source = "inibuilds-a340-lvar-unavailable",
                    SelectionReason = "IniBuilds A340 detected but no readable custom autoflight LVars are available",
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
                "INI_APU_AVAILABLE",
                "INI_APU_MASTER_SWITCH",
                "INI_APU_START_BUTTON",
                "INI_GEN_APU_GEN_SWITCH",
                "INI_AIR_BLEED_APU");

            bool? apuAvailable = FirstKnownBoolean(values, "INI_APU_AVAILABLE");
            bool? apuMaster = FirstKnownBoolean(values, "INI_APU_MASTER_SWITCH");
            bool? apuStart = FirstKnownBoolean(values, "INI_APU_START_BUTTON");
            bool? apuGenerator = FirstKnownBoolean(values, "INI_GEN_APU_GEN_SWITCH");
            bool? apuBleed = FirstKnownBoolean(values, "INI_AIR_BLEED_APU");

            string apuStatus;
            if (apuBleed == true) apuStatus = "bleed_on";
            else if (apuAvailable == true) apuStatus = "available";
            else if (apuStart == true) apuStatus = "starting";
            else if (apuGenerator == true) apuStatus = "running";
            else if (apuMaster == true) apuStatus = "unknown";
            else if (AllKnownFalse(apuBleed, apuAvailable, apuStart, apuGenerator, apuMaster)) apuStatus = "off";
            else apuStatus = "unknown";

            bool? flightDirector1 = FirstKnownBoolean(values, "INI_FD1_ON");
            bool? flightDirector2 = FirstKnownBoolean(values, "INI_FD2_ON");
            bool? flightDirector;
            if (flightDirector1 == true || flightDirector2 == true)
            {
                flightDirector = true;
            }
            else if (flightDirector1 == false && flightDirector2 == false)
            {
                flightDirector = false;
            }
            else
            {
                flightDirector = null;
            }

            bool? ap1 = FirstKnownBoolean(values, "INI_ap1_on", "INI_AP1_BUTTON");
            bool? ap2 = FirstKnownBoolean(values, "INI_ap2_on", "INI_AP2_BUTTON");
            bool? autoThrottleArmed = FirstKnownBoolean(values, "INI_AUTOTHROTTLE_ARMED");
            bool? autoThrottleActive = FirstKnownBoolean(values, "INI_ATHR_LIGHT");
            bool? managedSpeed = FirstKnownBoolean(values, "INI_FCU_MANAGED_SPEED_BUTTON");
            bool? selectedSpeedMode = FirstKnownBoolean(values, "INI_FCU_SELECTED_SPEED_BUTTON");
            bool? managedLateral = FirstKnownBoolean(values, "INI_FCU_MANAGED_HEADING_BUTTON");
            bool? selectedHeadingMode = FirstKnownBoolean(values, "INI_FCU_SELECTED_HEADING_BUTTON");
            bool? altitudeModeCommand = FirstKnownBoolean(values, "INI_FCU_ALTITUDE_MODE_COMMAND");
            bool? speedMachMode = FirstKnownBoolean(values, "INI_SPD_MACH_BUTTON");
            bool? locMode = FirstKnownBoolean(values, "INI_MCU_LOC_LIGHT");
            bool? autolandMode = FirstKnownBoolean(values, "INI_AUTOLAND_LIGHT");

            var autopilotRaw = Collect(values,
                "INI_AP1_BUTTON",
                "INI_ap1_on",
                "INI_AP2_BUTTON",
                "INI_ap2_on",
                "INI_AUTOTHROTTLE_ARMED",
                "INI_ATHR_LIGHT",
                "INI_FD1_ON",
                "INI_FD2_ON",
                "INI_MCU_LOC_LIGHT",
                "INI_AUTOLAND_LIGHT",
                "INI_FCU_MANAGED_SPEED_BUTTON",
                "INI_FCU_SELECTED_SPEED_BUTTON",
                "INI_FCU_MANAGED_HEADING_BUTTON",
                "INI_FCU_SELECTED_HEADING_BUTTON",
                "INI_FCU_ALTITUDE_MODE_COMMAND",
                "INI_FCU_HDG_VS_COMMAND",
                "INI_SPD_MACH_BUTTON",
                "INI_FCU_METRIC_STATE");

            result.Apu = new AdapterApuState
            {
                Status = apuStatus,
                Source = "inibuilds-a340-lvar",
                SelectionReason = "IniBuilds A340 adapter overrides generic APU SimVars",
                RawValues = apuRaw
            };

            result.Autopilot = new AdapterAutopilotState
            {
                Source = "inibuilds-a340-lvar",
                SelectionReason = "IniBuilds A340 adapter overrides generic autoflight state with readable custom LVars",
                FlightDirectorEnabled = flightDirector,
                FlightDirector1Enabled = flightDirector1,
                FlightDirector2Enabled = flightDirector2,
                Ap1Engaged = ap1,
                Ap2Engaged = ap2,
                AutoThrottleArmed = autoThrottleArmed,
                AutoThrottleActive = autoThrottleActive,
                SelectedSpeedMetersPerSecond = speedMachMode == true ? null : generic.AirspeedHoldMetersPerSecond,
                SelectedMach = speedMachMode == true ? generic.MachHoldMach : null,
                SelectedHeadingDegrees = generic.HeadingLockDegrees,
                SelectedAltitudeMeters = generic.AltitudeHoldMeters,
                SelectedVerticalSpeedMetersPerSecond = generic.VerticalSpeedHoldMetersPerSecond,
                LateralMode = locMode == true ? "LOC" : null,
                VerticalMode = autolandMode == true ? "AUTOLAND" : null,
                ManagedSpeed = managedSpeed == true ? true : selectedSpeedMode == true ? (bool?)false : null,
                ManagedLateral = managedLateral == true ? true : selectedHeadingMode == true ? (bool?)false : null,
                ManagedVertical = altitudeModeCommand,
                YawDamperEnabled = generic.YawDamperEnabled,
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

        private static bool? FirstKnownBoolean(IReadOnlyDictionary<string, double?> values, params string[] names)
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
                    !double.IsNaN(value.Value) &&
                    !double.IsInfinity(value.Value))
                {
                    return value.Value >= 0.5;
                }
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
    }

    public sealed class IniBuildsA350Adapter : GenericAircraftAdapter
    {
        private static readonly string[] RequestedVariables =
        {
            "INI_AIR_BLEED_APU",
            "INI_AP1_BUTTON",
            "INI_ap1_on",
            "INI_AP2_BUTTON",
            "INI_ap2_on",
            "INI_APU_AVAILABLE",
            "INI_APU_MASTER_SWITCH",
            "INI_APU_START_BUTTON",
            "INI_ATHR_LIGHT",
            "INI_AUTOLAND_LIGHT",
            "INI_AUTOTHROTTLE_ARMED",
            "INI_FCU_ALTITUDE_MODE_COMMAND",
            "INI_FCU_HDG_VS_COMMAND",
            "INI_FCU_MANAGED_HEADING_BUTTON",
            "INI_FCU_MANAGED_SPEED_BUTTON",
            "INI_FCU_METRIC_STATE",
            "INI_FCU_SELECTED_HEADING_BUTTON",
            "INI_FCU_SELECTED_SPEED_BUTTON",
            "INI_FD_ON",
            "INI_GEN_APU_GEN_SWITCH",
            "INI_MCU_LOC_LIGHT",
            "INI_SPD_MACH_BUTTON"
        };

        private static readonly string[] DiscoveryKeywords =
        {
            "APU", "AP1", "AP2", "APPR", "ATHR", "AUTOLAND", "AUTOTHROTTLE",
            "FCU", "FD", "HDG", "HEADING", "LOC", "MACH", "MANAGED",
            "SELECTED", "SPD", "SPEED", "TRK", "FPA", "ALT", "VS", "VERT"
        };

        public override string Name
        {
            get { return "IniBuildsA350Adapter"; }
        }

        public override bool Matches(AircraftIdentityInfo identity)
        {
            return identity != null &&
                string.Equals(identity.DetectedFamily, "IniBuilds A350", StringComparison.OrdinalIgnoreCase);
        }

        public static string[] GetDiscoveryKeywords()
        {
            return DiscoveryKeywords;
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
                FenixDetected = false,
                FenixLvarSource = readable ? "direct-simconnect" : "unavailable",
                FenixVariablesDiscovered = context != null ? context.DiscoveredVariableCount : 0,
                FenixVariablesReadable = context != null ? context.ReadableVariableCount : 0
            };

            if (!readable)
            {
                result.Apu = new AdapterApuState
                {
                    Status = null,
                    Source = "inibuilds-a350-lvar-unavailable",
                    SelectionReason = "IniBuilds A350 detected but no readable custom LVars are available",
                    RawValues = new Dictionary<string, double?>()
                };

                result.Autopilot = new AdapterAutopilotState
                {
                    Source = "inibuilds-a350-lvar-unavailable",
                    SelectionReason = "IniBuilds A350 detected but no readable custom autoflight LVars are available",
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
                "INI_APU_AVAILABLE",
                "INI_APU_MASTER_SWITCH",
                "INI_APU_START_BUTTON",
                "INI_GEN_APU_GEN_SWITCH",
                "INI_AIR_BLEED_APU");

            bool? apuAvailable = FirstKnownBoolean(values, "INI_APU_AVAILABLE");
            bool? apuMaster = FirstKnownBoolean(values, "INI_APU_MASTER_SWITCH");
            bool? apuStart = FirstKnownBoolean(values, "INI_APU_START_BUTTON");
            bool? apuGenerator = FirstKnownBoolean(values, "INI_GEN_APU_GEN_SWITCH");
            bool? apuBleed = FirstKnownBoolean(values, "INI_AIR_BLEED_APU");

            string apuStatus;
            if (apuBleed == true) apuStatus = "bleed_on";
            else if (apuAvailable == true) apuStatus = "available";
            else if (apuStart == true) apuStatus = "starting";
            else if (apuGenerator == true) apuStatus = "running";
            else if (apuMaster == true) apuStatus = "unknown";
            else if (AllKnownFalse(apuBleed, apuAvailable, apuStart, apuGenerator, apuMaster)) apuStatus = "off";
            else apuStatus = "unknown";

            bool? flightDirector = FirstKnownBoolean(values, "INI_FD_ON");
            bool? ap1 = FirstKnownBoolean(values, "INI_ap1_on", "INI_AP1_BUTTON");
            bool? ap2 = FirstKnownBoolean(values, "INI_ap2_on", "INI_AP2_BUTTON");
            bool? autoThrottleArmed = FirstKnownBoolean(values, "INI_AUTOTHROTTLE_ARMED");
            bool? autoThrottleActive = FirstKnownBoolean(values, "INI_ATHR_LIGHT");
            bool? managedSpeed = FirstKnownBoolean(values, "INI_FCU_MANAGED_SPEED_BUTTON");
            bool? selectedSpeedMode = FirstKnownBoolean(values, "INI_FCU_SELECTED_SPEED_BUTTON");
            bool? managedLateral = FirstKnownBoolean(values, "INI_FCU_MANAGED_HEADING_BUTTON");
            bool? selectedHeadingMode = FirstKnownBoolean(values, "INI_FCU_SELECTED_HEADING_BUTTON");
            bool? altitudeModeCommand = FirstKnownBoolean(values, "INI_FCU_ALTITUDE_MODE_COMMAND");
            bool? trkFpaMode = FirstKnownBoolean(values, "INI_FCU_HDG_VS_COMMAND");
            bool? speedMachMode = FirstKnownBoolean(values, "INI_SPD_MACH_BUTTON");
            bool? locMode = FirstKnownBoolean(values, "INI_MCU_LOC_LIGHT");
            bool? autolandMode = FirstKnownBoolean(values, "INI_AUTOLAND_LIGHT");

            var autopilotRaw = Collect(values,
                "INI_AP1_BUTTON",
                "INI_ap1_on",
                "INI_AP2_BUTTON",
                "INI_ap2_on",
                "INI_AUTOTHROTTLE_ARMED",
                "INI_ATHR_LIGHT",
                "INI_FD_ON",
                "INI_MCU_LOC_LIGHT",
                "INI_AUTOLAND_LIGHT",
                "INI_FCU_MANAGED_SPEED_BUTTON",
                "INI_FCU_SELECTED_SPEED_BUTTON",
                "INI_FCU_MANAGED_HEADING_BUTTON",
                "INI_FCU_SELECTED_HEADING_BUTTON",
                "INI_FCU_ALTITUDE_MODE_COMMAND",
                "INI_FCU_HDG_VS_COMMAND",
                "INI_SPD_MACH_BUTTON",
                "INI_FCU_METRIC_STATE");

            result.Apu = new AdapterApuState
            {
                Status = apuStatus,
                Source = "inibuilds-a350-lvar",
                SelectionReason = "IniBuilds A350 adapter overrides generic APU SimVars",
                RawValues = apuRaw
            };

            result.Autopilot = new AdapterAutopilotState
            {
                Source = "inibuilds-a350-lvar",
                SelectionReason = "IniBuilds A350 adapter overrides generic autoflight state with readable custom LVars",
                FlightDirectorEnabled = flightDirector,
                FlightDirector1Enabled = flightDirector,
                FlightDirector2Enabled = flightDirector,
                Ap1Engaged = ap1,
                Ap2Engaged = ap2,
                AutoThrottleArmed = autoThrottleArmed,
                AutoThrottleActive = autoThrottleActive,
                SelectedSpeedMetersPerSecond = speedMachMode == true ? null : generic.AirspeedHoldMetersPerSecond,
                SelectedMach = speedMachMode == true ? generic.MachHoldMach : null,
                SelectedHeadingDegrees = generic.HeadingLockDegrees,
                SelectedAltitudeMeters = generic.AltitudeHoldMeters,
                SelectedVerticalSpeedMetersPerSecond = generic.VerticalSpeedHoldMetersPerSecond,
                LateralMode = locMode == true ? "LOC" : null,
                VerticalMode = autolandMode == true ? "AUTOLAND" : null,
                ManagedSpeed = managedSpeed == true ? true : selectedSpeedMode == true ? (bool?)false : null,
                ManagedLateral = managedLateral == true ? true : selectedHeadingMode == true ? (bool?)false : null,
                ManagedVertical = altitudeModeCommand,
                YawDamperEnabled = generic.YawDamperEnabled,
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

        private static bool? FirstKnownBoolean(IReadOnlyDictionary<string, double?> values, params string[] names)
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
                    !double.IsNaN(value.Value) &&
                    !double.IsInfinity(value.Value))
                {
                    return value.Value >= 0.5;
                }
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
    }

    public sealed class FlyByWireA380XAdapter : GenericAircraftAdapter
    {
        private static readonly string[] RequestedVariables =
        {
            "A32NX_APU_BLEED_AIR_VALVE_OPEN",
            "A32NX_APU_N",
            "A32NX_AUTOPILOT_1_ACTIVE",
            "A32NX_AUTOPILOT_2_ACTIVE",
            "A32NX_AUTOPILOT_FPA_SELECTED",
            "A32NX_AUTOPILOT_HEADING_SELECTED",
            "A32NX_AUTOPILOT_SPEED_SELECTED",
            "A32NX_AUTOPILOT_VS_SELECTED",
            "A32NX_AUTOTHRUST_STATUS",
            "A32NX_FCU_ALT_MANAGED",
            "A32NX_FCU_APPR_MODE_ACTIVE",
            "A32NX_FCU_LOC_MODE_ACTIVE",
            "A32NX_FCU_VS_MANAGED",
            "A32NX_OVHD_APU_MASTER_SW_PB_IS_ON",
            "A32NX_OVHD_APU_START_PB_IS_AVAILABLE",
            "A32NX_OVHD_APU_START_PB_IS_ON",
            "A32NX_OVHD_PNEU_APU_BLEED_PB_IS_ON",
            "A32NX_SPEEDS_MANAGED_PFD",
            "A32NX_TRK_FPA_MODE_ACTIVE"
        };

        private static readonly string[] DiscoveryKeywords =
        {
            "APU", "AUTOPILOT", "AUTOTHRUST", "FCU", "FD", "HEADING", "SPEED",
            "VS", "FPA", "ALT", "MANAGED", "LOC", "APPR", "TRK", "BLEED"
        };

        public override string Name
        {
            get { return "FlyByWireA380XAdapter"; }
        }

        public override bool Matches(AircraftIdentityInfo identity)
        {
            return identity != null &&
                string.Equals(identity.DetectedFamily, "FlyByWire A380X", StringComparison.OrdinalIgnoreCase);
        }

        public static string[] GetDiscoveryKeywords()
        {
            return DiscoveryKeywords;
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
                FenixDetected = false,
                FenixLvarSource = readable ? "direct-simconnect" : "unavailable",
                FenixVariablesDiscovered = context != null ? context.DiscoveredVariableCount : 0,
                FenixVariablesReadable = context != null ? context.ReadableVariableCount : 0
            };

            if (!readable)
            {
                result.Apu = new AdapterApuState
                {
                    Status = null,
                    Source = "flybywire-a380x-lvar-unavailable",
                    SelectionReason = "FlyByWire A380X detected but no readable custom LVars are available",
                    RawValues = new Dictionary<string, double?>()
                };

                result.Autopilot = new AdapterAutopilotState
                {
                    Source = "flybywire-a380x-lvar-unavailable",
                    SelectionReason = "FlyByWire A380X detected but no readable custom autoflight LVars are available",
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

            bool? apuMaster = FirstKnownBoolean(values, "A32NX_OVHD_APU_MASTER_SW_PB_IS_ON");
            bool? apuAvailable = FirstKnownBoolean(values, "A32NX_OVHD_APU_START_PB_IS_AVAILABLE");
            bool? apuStart = FirstKnownBoolean(values, "A32NX_OVHD_APU_START_PB_IS_ON");
            bool? apuBleedButton = FirstKnownBoolean(values, "A32NX_OVHD_PNEU_APU_BLEED_PB_IS_ON");
            bool? apuBleedValve = FirstKnownBoolean(values, "A32NX_APU_BLEED_AIR_VALVE_OPEN");
            double? apuN = FirstFinite(values, "A32NX_APU_N");

            bool? apuBleed = null;
            if (apuBleedButton == true || apuBleedValve == true)
            {
                apuBleed = true;
            }
            else if (apuBleedButton == false && apuBleedValve == false)
            {
                apuBleed = false;
            }

            string apuStatus;
            if (apuBleed == true) apuStatus = "bleed_on";
            else if (apuAvailable == true) apuStatus = "available";
            else if (apuN.HasValue && apuN.Value >= 95.0) apuStatus = "running";
            else if (apuStart == true || (apuN.HasValue && apuN.Value > 1.0)) apuStatus = "starting";
            else if (apuMaster == true) apuStatus = "unknown";
            else if (AllKnownFalse(apuMaster, apuAvailable, apuStart, apuBleedButton, apuBleedValve) && (!apuN.HasValue || apuN.Value <= 1.0)) apuStatus = "off";
            else apuStatus = "unknown";

            bool? ap1 = FirstKnownBoolean(values, "A32NX_AUTOPILOT_1_ACTIVE");
            bool? ap2 = FirstKnownBoolean(values, "A32NX_AUTOPILOT_2_ACTIVE");
            double? autoThrustStatus = FirstFinite(values, "A32NX_AUTOTHRUST_STATUS");
            bool? autoThrottleArmed = autoThrustStatus.HasValue ? autoThrustStatus.Value > 0.0 : (bool?)null;
            bool? autoThrottleActive = autoThrustStatus.HasValue ? (autoThrustStatus.Value >= 2.0) : (bool?)null;
            bool? managedSpeed = FirstKnownBoolean(values, "A32NX_SPEEDS_MANAGED_PFD");
            bool? managedVertical = FirstKnownBoolean(values, "A32NX_FCU_ALT_MANAGED", "A32NX_FCU_VS_MANAGED");
            bool? locMode = FirstKnownBoolean(values, "A32NX_FCU_LOC_MODE_ACTIVE");
            bool? apprMode = FirstKnownBoolean(values, "A32NX_FCU_APPR_MODE_ACTIVE");
            bool? trkFpaMode = FirstKnownBoolean(values, "A32NX_TRK_FPA_MODE_ACTIVE");

            double? selectedSpeedKnots = FirstFinite(values, "A32NX_AUTOPILOT_SPEED_SELECTED");
            double? selectedHeading = FirstFinite(values, "A32NX_AUTOPILOT_HEADING_SELECTED");
            double? selectedVsFeetPerMinute = FirstFinite(values, "A32NX_AUTOPILOT_VS_SELECTED");

            bool? combinedFlightDirector = generic.FlightDirectorEnabled;
            if (!combinedFlightDirector.HasValue && (ap1 == true || ap2 == true))
            {
                combinedFlightDirector = true;
            }

            string lateralMode = null;
            if (apprMode == true) lateralMode = "APPR";
            else if (locMode == true) lateralMode = "LOC";
            else if (trkFpaMode == true) lateralMode = "TRK";

            string verticalMode = null;
            if (trkFpaMode == true) verticalMode = "FPA";
            else if (managedVertical == true) verticalMode = "MANAGED";

            var apuRaw = Collect(values,
                "A32NX_OVHD_APU_MASTER_SW_PB_IS_ON",
                "A32NX_OVHD_APU_START_PB_IS_AVAILABLE",
                "A32NX_OVHD_APU_START_PB_IS_ON",
                "A32NX_OVHD_PNEU_APU_BLEED_PB_IS_ON",
                "A32NX_APU_BLEED_AIR_VALVE_OPEN",
                "A32NX_APU_N");

            var autopilotRaw = Collect(values,
                "A32NX_AUTOPILOT_1_ACTIVE",
                "A32NX_AUTOPILOT_2_ACTIVE",
                "A32NX_AUTOTHRUST_STATUS",
                "A32NX_AUTOPILOT_SPEED_SELECTED",
                "A32NX_AUTOPILOT_HEADING_SELECTED",
                "A32NX_AUTOPILOT_VS_SELECTED",
                "A32NX_AUTOPILOT_FPA_SELECTED",
                "A32NX_FCU_ALT_MANAGED",
                "A32NX_FCU_VS_MANAGED",
                "A32NX_SPEEDS_MANAGED_PFD",
                "A32NX_FCU_LOC_MODE_ACTIVE",
                "A32NX_FCU_APPR_MODE_ACTIVE",
                "A32NX_TRK_FPA_MODE_ACTIVE");

            result.Apu = new AdapterApuState
            {
                Status = apuStatus,
                Source = "flybywire-a380x-lvar",
                SelectionReason = "FlyByWire A380X adapter overrides generic APU SimVars",
                RawValues = apuRaw
            };

            result.Autopilot = new AdapterAutopilotState
            {
                Source = "flybywire-a380x-lvar",
                SelectionReason = "FlyByWire A380X adapter overrides generic autoflight state with readable custom LVars",
                FlightDirectorEnabled = combinedFlightDirector,
                FlightDirector1Enabled = null,
                FlightDirector2Enabled = null,
                Ap1Engaged = ap1,
                Ap2Engaged = ap2,
                AutoThrottleArmed = autoThrottleArmed,
                AutoThrottleActive = autoThrottleActive,
                SelectedSpeedMetersPerSecond = selectedSpeedKnots.HasValue ? TelemetryMath.KnotsToMetersPerSecond(selectedSpeedKnots.Value) : (double?)null,
                SelectedMach = null,
                SelectedHeadingDegrees = selectedHeading.HasValue ? NormalizeHeading(selectedHeading.Value) : (double?)null,
                SelectedAltitudeMeters = generic.AltitudeHoldMeters,
                SelectedVerticalSpeedMetersPerSecond = selectedVsFeetPerMinute.HasValue ? TelemetryMath.FeetPerMinuteToMetersPerSecond(selectedVsFeetPerMinute.Value) : (double?)null,
                LateralMode = lateralMode,
                VerticalMode = verticalMode,
                ManagedSpeed = managedSpeed,
                ManagedLateral = null,
                ManagedVertical = managedVertical,
                YawDamperEnabled = generic.YawDamperEnabled,
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

        private static bool? FirstKnownBoolean(IReadOnlyDictionary<string, double?> values, params string[] names)
        {
            double? value = FirstFinite(values, names);
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value >= 0.5;
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
                    !double.IsNaN(value.Value) &&
                    !double.IsInfinity(value.Value))
                {
                    return value.Value;
                }
            }

            return null;
        }

        private static double NormalizeHeading(double degrees)
        {
            double value = degrees % 360.0;
            if (value < 0.0)
            {
                value += 360.0;
            }

            return value;
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
    }
}
