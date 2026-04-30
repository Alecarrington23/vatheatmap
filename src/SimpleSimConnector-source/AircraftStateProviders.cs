using System;
using System.Collections.Generic;

namespace SimpleSimConnector
{
    public enum AircraftProviderSourceKind
    {
        SimConnect,
        Sdk,
        LVar,
        Wasm,
        Mixed
    }

    public enum AircraftValueAvailability
    {
        Native,
        Derived,
        Estimated,
        Unsupported,
        Unavailable
    }

    public sealed class ProviderDetectionRule
    {
        public ProviderDetectionRule(string identityField, string contains)
        {
            IdentityField = identityField;
            Contains = contains;
        }

        public string IdentityField { get; private set; }
        public string Contains { get; private set; }
    }

    public sealed class ProviderVariableProfile
    {
        public ProviderVariableProfile(
            string outputField,
            string sourceName,
            string sourceType,
            string unit,
            string conversion,
            string notes)
        {
            OutputField = outputField;
            SourceName = sourceName;
            SourceType = sourceType;
            Unit = unit;
            Conversion = conversion;
            Notes = notes;
        }

        public string OutputField { get; private set; }
        public string SourceName { get; private set; }
        public string SourceType { get; private set; }
        public string Unit { get; private set; }
        public string Conversion { get; private set; }
        public string Notes { get; private set; }
    }

    public sealed class ProviderCapabilityDefinition
    {
        public ProviderCapabilityDefinition(string key, string description, params string[] outputFields)
        {
            Key = key;
            Description = description;
            OutputFields = new List<string>(outputFields ?? new string[0]);
        }

        public string Key { get; private set; }
        public string Description { get; private set; }
        public IList<string> OutputFields { get; private set; }
    }

    public sealed class ProviderProfile
    {
        public ProviderProfile(
            string id,
            string displayName,
            AircraftProviderSourceKind primarySourceKind,
            IList<ProviderDetectionRule> detectionRules,
            IList<ProviderVariableProfile> variableProfiles,
            IList<ProviderCapabilityDefinition> capabilityDefinitions,
            IList<string> unsupportedFields)
        {
            Id = id;
            DisplayName = displayName;
            PrimarySourceKind = primarySourceKind;
            DetectionRules = detectionRules ?? new List<ProviderDetectionRule>();
            VariableProfiles = variableProfiles ?? new List<ProviderVariableProfile>();
            CapabilityDefinitions = capabilityDefinitions ?? new List<ProviderCapabilityDefinition>();
            UnsupportedFields = unsupportedFields ?? new List<string>();
        }

        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public AircraftProviderSourceKind PrimarySourceKind { get; private set; }
        public IList<ProviderDetectionRule> DetectionRules { get; private set; }
        public IList<ProviderVariableProfile> VariableProfiles { get; private set; }
        public IList<ProviderCapabilityDefinition> CapabilityDefinitions { get; private set; }
        public IList<string> UnsupportedFields { get; private set; }
    }

    public sealed class AircraftProviderContext
    {
        public AircraftIdentityInfo Identity { get; set; }
        public GenericSystemsData Generic { get; set; }
        public IReadOnlyDictionary<string, double?> CustomVariableValues { get; set; }
        public ISet<string> DiscoveredVariables { get; set; }
        public int DiscoveredVariableCount { get; set; }
        public int ReadableVariableCount { get; set; }
        public string CustomVariableSource { get; set; }

        public AircraftAdapterContext ToAdapterContext()
        {
            return new AircraftAdapterContext
            {
                Identity = Identity,
                Generic = Generic,
                CustomVariableValues = CustomVariableValues,
                DiscoveredVariables = DiscoveredVariables,
                DiscoveredVariableCount = DiscoveredVariableCount,
                ReadableVariableCount = ReadableVariableCount,
                CustomVariableSource = CustomVariableSource
            };
        }
    }

    public sealed class AircraftStateValueMetadata
    {
        public string OutputField { get; set; }
        public string ProviderName { get; set; }
        public AircraftProviderSourceKind SourceKind { get; set; }
        public AircraftValueAvailability Availability { get; set; }
        public string Reason { get; set; }
    }

    public sealed class AircraftCapabilityReport
    {
        public IList<string> Available { get; set; }
        public IList<string> Unavailable { get; set; }
        public IList<string> Unsupported { get; set; }
    }

    public sealed class AircraftApuStateFrame
    {
        public string Status { get; set; }
    }

    public sealed class AircraftAutopilotStateFrame
    {
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
    }

    public sealed class AircraftStateFrame
    {
        public string ProviderId { get; set; }
        public string ProviderName { get; set; }
        public AircraftProviderSourceKind SourceKind { get; set; }
        public AircraftIdentityInfo Identity { get; set; }
        public AircraftApuStateFrame Apu { get; set; }
        public AircraftAutopilotStateFrame Autopilot { get; set; }
        public AircraftCapabilityReport Capabilities { get; set; }
        public IDictionary<string, AircraftStateValueMetadata> FieldSources { get; set; }
    }

    public sealed class AircraftProviderEvaluation
    {
        public ProviderProfile Profile { get; set; }
        public AircraftStateFrame Frame { get; set; }
        public AircraftAdapterResult AdapterResult { get; set; }
        public GenericSystemsData GenericBaseline { get; set; }
    }

    public interface IAircraftStateProvider
    {
        string Name { get; }
        ProviderProfile Profile { get; }
        bool Matches(AircraftIdentityInfo identity);
        AircraftProviderEvaluation Evaluate(AircraftProviderContext context);
    }

    public abstract class AircraftProviderBase : IAircraftStateProvider
    {
        protected AircraftProviderBase(ProviderProfile profile)
        {
            Profile = profile;
        }

        public abstract string Name { get; }

        public ProviderProfile Profile { get; private set; }

        public virtual bool Matches(AircraftIdentityInfo identity)
        {
            return AircraftDetector.MatchesProfile(Profile, identity);
        }

        public abstract AircraftProviderEvaluation Evaluate(AircraftProviderContext context);
    }

    public abstract class AdapterBackedAircraftProviderBase : AircraftProviderBase
    {
        protected AdapterBackedAircraftProviderBase(ProviderProfile profile)
            : base(profile)
        {
        }

        protected abstract IAircraftAdapter Adapter { get; }

        public override AircraftProviderEvaluation Evaluate(AircraftProviderContext context)
        {
            AircraftAdapterResult adapterResult = Adapter.Evaluate(context != null ? context.ToAdapterContext() : new AircraftAdapterContext());
            AircraftStateFrame frame = StateNormalizer.Normalize(Profile, Name, context, adapterResult);

            return new AircraftProviderEvaluation
            {
                Profile = Profile,
                Frame = frame,
                AdapterResult = adapterResult,
                GenericBaseline = context != null ? context.Generic : null
            };
        }
    }

    public abstract class SdkProviderBase : AdapterBackedAircraftProviderBase
    {
        protected SdkProviderBase(ProviderProfile profile)
            : base(profile)
        {
        }
    }

    public abstract class LVarProviderBase : AdapterBackedAircraftProviderBase
    {
        protected LVarProviderBase(ProviderProfile profile)
            : base(profile)
        {
        }
    }

    public abstract class WasmBridgeProviderBase : AdapterBackedAircraftProviderBase
    {
        protected WasmBridgeProviderBase(ProviderProfile profile)
            : base(profile)
        {
        }
    }

    public sealed class GenericSimConnectProvider : AdapterBackedAircraftProviderBase
    {
        private static readonly GenericAircraftAdapter GenericAdapter = new GenericAircraftAdapter();

        public GenericSimConnectProvider(ProviderProfile profile)
            : base(profile)
        {
        }

        public override string Name
        {
            get { return "GenericSimConnectProvider"; }
        }

        protected override IAircraftAdapter Adapter
        {
            get { return GenericAdapter; }
        }

        public override bool Matches(AircraftIdentityInfo identity)
        {
            return true;
        }
    }

    public sealed class FenixA32xProvider : LVarProviderBase
    {
        private static readonly FenixA32xAdapter FenixAdapter = new FenixA32xAdapter();

        public FenixA32xProvider(ProviderProfile profile)
            : base(profile)
        {
        }

        public override string Name
        {
            get { return "FenixA32xProvider"; }
        }

        protected override IAircraftAdapter Adapter
        {
            get { return FenixAdapter; }
        }
    }

    public sealed class Pmdg777Provider : SdkProviderBase
    {
        private static readonly Pmdg777Adapter PmdgAdapter = new Pmdg777Adapter();

        public Pmdg777Provider(ProviderProfile profile)
            : base(profile)
        {
        }

        public override string Name
        {
            get { return "Pmdg777Provider"; }
        }

        protected override IAircraftAdapter Adapter
        {
            get { return PmdgAdapter; }
        }
    }

    public sealed class Pmdg737Provider : SdkProviderBase
    {
        private static readonly Pmdg737Adapter PmdgAdapter = new Pmdg737Adapter();

        public Pmdg737Provider(ProviderProfile profile)
            : base(profile)
        {
        }

        public override string Name
        {
            get { return "Pmdg737Provider"; }
        }

        protected override IAircraftAdapter Adapter
        {
            get { return PmdgAdapter; }
        }
    }

    public abstract class IniBuildsAirbusProviderBase : LVarProviderBase
    {
        protected IniBuildsAirbusProviderBase(ProviderProfile profile)
            : base(profile)
        {
        }
    }

    public sealed class IniBuildsA340Provider : IniBuildsAirbusProviderBase
    {
        private static readonly IniBuildsA340Adapter IniAdapter = new IniBuildsA340Adapter();

        public IniBuildsA340Provider(ProviderProfile profile)
            : base(profile)
        {
        }

        public override string Name
        {
            get { return "IniBuildsA340Provider"; }
        }

        protected override IAircraftAdapter Adapter
        {
            get { return IniAdapter; }
        }
    }

    public sealed class IniBuildsA350Provider : IniBuildsAirbusProviderBase
    {
        private static readonly IniBuildsA350Adapter IniAdapter = new IniBuildsA350Adapter();

        public IniBuildsA350Provider(ProviderProfile profile)
            : base(profile)
        {
        }

        public override string Name
        {
            get { return "IniBuildsA350Provider"; }
        }

        protected override IAircraftAdapter Adapter
        {
            get { return IniAdapter; }
        }
    }

    public sealed class FlyByWireA380XProvider : LVarProviderBase
    {
        private static readonly FlyByWireA380XAdapter FbwAdapter = new FlyByWireA380XAdapter();

        public FlyByWireA380XProvider(ProviderProfile profile)
            : base(profile)
        {
        }

        public override string Name
        {
            get { return "FlyByWireA380XProvider"; }
        }

        protected override IAircraftAdapter Adapter
        {
            get { return FbwAdapter; }
        }
    }

    public static class AircraftDetector
    {
        public static bool MatchesProfile(ProviderProfile profile, AircraftIdentityInfo identity)
        {
            if (profile == null)
            {
                return false;
            }

            if (profile.DetectionRules == null || profile.DetectionRules.Count == 0)
            {
                return true;
            }

            if (identity == null)
            {
                return false;
            }

            for (int i = 0; i < profile.DetectionRules.Count; i++)
            {
                ProviderDetectionRule rule = profile.DetectionRules[i];
                string value = GetIdentityValue(identity, rule.IdentityField);
                if (value.IndexOf(rule.Contains ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetIdentityValue(AircraftIdentityInfo identity, string identityField)
        {
            if (identity == null)
            {
                return string.Empty;
            }

            string field = (identityField ?? string.Empty).Trim();
            if (field.Equals("Title", StringComparison.OrdinalIgnoreCase)) return identity.Title ?? string.Empty;
            if (field.Equals("AtcModel", StringComparison.OrdinalIgnoreCase)) return identity.AtcModel ?? string.Empty;
            if (field.Equals("AtcType", StringComparison.OrdinalIgnoreCase)) return identity.AtcType ?? string.Empty;
            if (field.Equals("SimObjectTitle", StringComparison.OrdinalIgnoreCase)) return identity.SimObjectTitle ?? string.Empty;
            if (field.Equals("PackagePath", StringComparison.OrdinalIgnoreCase)) return identity.PackagePath ?? string.Empty;
            if (field.Equals("DetectedFamily", StringComparison.OrdinalIgnoreCase)) return identity.DetectedFamily ?? string.Empty;
            if (field.Equals("DetectedVariant", StringComparison.OrdinalIgnoreCase)) return identity.DetectedVariant ?? string.Empty;
            return string.Empty;
        }
    }

    public static class ProviderRegistry
    {
        private static readonly ProviderProfile GenericProfile = ProfileFactory.CreateGenericProfile();
        private static readonly ProviderProfile FenixA32xProfile = ProfileFactory.CreateFenixA32xProfile();
        private static readonly ProviderProfile Pmdg777Profile = ProfileFactory.CreatePmdg777Profile();
        private static readonly ProviderProfile Pmdg737Profile = ProfileFactory.CreatePmdg737Profile();
        private static readonly ProviderProfile IniBuildsAirbusProfile = ProfileFactory.CreateIniBuildsAirbusProfile();
        private static readonly ProviderProfile IniBuildsA340Profile = ProfileFactory.CreateIniBuildsVariantProfile("inibuilds-a340", "IniBuilds A340 Provider", "IniBuilds A340", "A340");
        private static readonly ProviderProfile IniBuildsA350Profile = ProfileFactory.CreateIniBuildsVariantProfile("inibuilds-a350", "IniBuilds A350 Provider", "IniBuilds A350", "A359");
        private static readonly ProviderProfile FlyByWireA380XProfile = ProfileFactory.CreateFlyByWireA380XProfile();

        private static readonly IAircraftStateProvider[] Providers =
        {
            new FenixA32xProvider(FenixA32xProfile),
            new Pmdg777Provider(Pmdg777Profile),
            new Pmdg737Provider(Pmdg737Profile),
            new IniBuildsA340Provider(IniBuildsA340Profile),
            new IniBuildsA350Provider(IniBuildsA350Profile),
            new FlyByWireA380XProvider(FlyByWireA380XProfile),
            new GenericSimConnectProvider(GenericProfile)
        };

        public static IList<ProviderProfile> Profiles
        {
            get
            {
                return new List<ProviderProfile>
                {
                    GenericProfile,
                    FenixA32xProfile,
                    Pmdg737Profile,
                    Pmdg777Profile,
                    IniBuildsAirbusProfile,
                    IniBuildsA340Profile,
                    IniBuildsA350Profile,
                    FlyByWireA380XProfile
                };
            }
        }

        public static IAircraftStateProvider ResolveProvider(AircraftIdentityInfo identity)
        {
            for (int i = 0; i < Providers.Length; i++)
            {
                if (Providers[i].Matches(identity))
                {
                    return Providers[i];
                }
            }

            return Providers[Providers.Length - 1];
        }

        public static AircraftProviderEvaluation Evaluate(AircraftProviderContext context)
        {
            IAircraftStateProvider provider = ResolveProvider(context != null ? context.Identity : null);
            return provider.Evaluate(context);
        }
    }

    public static class StateNormalizer
    {
        public static AircraftStateFrame Normalize(
            ProviderProfile profile,
            string providerName,
            AircraftProviderContext context,
            AircraftAdapterResult adapterResult)
        {
            var fieldSources = new Dictionary<string, AircraftStateValueMetadata>(StringComparer.OrdinalIgnoreCase);
            string autopilotSource = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.Source : null;
            string apuSource = adapterResult != null && adapterResult.Apu != null ? adapterResult.Apu.Source : null;

            var frame = new AircraftStateFrame
            {
                ProviderId = profile != null ? profile.Id : "generic",
                ProviderName = providerName ?? "UnknownProvider",
                SourceKind = profile != null ? profile.PrimarySourceKind : AircraftProviderSourceKind.SimConnect,
                Identity = context != null ? context.Identity : null,
                Apu = new AircraftApuStateFrame
                {
                    Status = adapterResult != null && adapterResult.Apu != null ? adapterResult.Apu.Status : null
                },
                Autopilot = new AircraftAutopilotStateFrame
                {
                    FlightDirectorEnabled = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.FlightDirectorEnabled : null,
                    FlightDirector1Enabled = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.FlightDirector1Enabled : null,
                    FlightDirector2Enabled = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.FlightDirector2Enabled : null,
                    Ap1Engaged = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.Ap1Engaged : null,
                    Ap2Engaged = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.Ap2Engaged : null,
                    AutoThrottleArmed = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.AutoThrottleArmed : null,
                    AutoThrottleActive = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.AutoThrottleActive : null,
                    SelectedSpeedMetersPerSecond = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectedSpeedMetersPerSecond : null,
                    SelectedMach = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectedMach : null,
                    SelectedHeadingDegrees = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectedHeadingDegrees : null,
                    SelectedAltitudeMeters = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectedAltitudeMeters : null,
                    SelectedVerticalSpeedMetersPerSecond = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectedVerticalSpeedMetersPerSecond : null,
                    LateralMode = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.LateralMode : null,
                    VerticalMode = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.VerticalMode : null,
                    ManagedSpeed = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.ManagedSpeed : null,
                    ManagedLateral = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.ManagedLateral : null,
                    ManagedVertical = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.ManagedVertical : null,
                    YawDamperEnabled = adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.YawDamperEnabled : null,
                    Modes = adapterResult != null && adapterResult.Autopilot != null && adapterResult.Autopilot.Modes != null
                        ? new List<string>(adapterResult.Autopilot.Modes)
                        : new List<string>()
                },
                FieldSources = fieldSources
            };

            AddField(fieldSources, providerName, profile, "apu.status", frame.Apu.Status, GetSourceKind(apuSource), false, adapterResult != null && adapterResult.Apu != null ? adapterResult.Apu.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.flightDirectorEnabled", frame.Autopilot.FlightDirectorEnabled, GetSourceKind(autopilotSource), false, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.flightDirector1Enabled", frame.Autopilot.FlightDirector1Enabled, GetSourceKind(autopilotSource), false, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.flightDirector2Enabled", frame.Autopilot.FlightDirector2Enabled, GetSourceKind(autopilotSource), false, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.ap1Engaged", frame.Autopilot.Ap1Engaged, GetSourceKind(autopilotSource), false, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.ap2Engaged", frame.Autopilot.Ap2Engaged, GetSourceKind(autopilotSource), false, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.autoThrottleArmed", frame.Autopilot.AutoThrottleArmed, GetSourceKind(autopilotSource), false, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.autoThrottleActive", frame.Autopilot.AutoThrottleActive, GetSourceKind(autopilotSource), false, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.selectedSpeedMetersPerSecond", frame.Autopilot.SelectedSpeedMetersPerSecond, GetSourceKind(autopilotSource), true, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.selectedMach", frame.Autopilot.SelectedMach, GetSourceKind(autopilotSource), true, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.selectedHeadingDegrees", frame.Autopilot.SelectedHeadingDegrees, GetSourceKind(autopilotSource), true, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.selectedAltitudeMeters", frame.Autopilot.SelectedAltitudeMeters, GetSourceKind(autopilotSource), true, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.selectedVerticalSpeedMetersPerSecond", frame.Autopilot.SelectedVerticalSpeedMetersPerSecond, GetSourceKind(autopilotSource), true, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.lateralMode", frame.Autopilot.LateralMode, GetSourceKind(autopilotSource), true, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.verticalMode", frame.Autopilot.VerticalMode, GetSourceKind(autopilotSource), true, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.managedSpeed", frame.Autopilot.ManagedSpeed, GetSourceKind(autopilotSource), true, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.managedLateral", frame.Autopilot.ManagedLateral, GetSourceKind(autopilotSource), true, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "autopilot.managedVertical", frame.Autopilot.ManagedVertical, GetSourceKind(autopilotSource), true, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);
            AddField(fieldSources, providerName, profile, "flightControls.yawDamperEnabled", frame.Autopilot.YawDamperEnabled, GetSourceKind(autopilotSource), false, adapterResult != null && adapterResult.Autopilot != null ? adapterResult.Autopilot.SelectionReason : null);

            frame.Capabilities = CapabilityReporter.Build(profile, frame);
            return frame;
        }

        private static void AddField(
            IDictionary<string, AircraftStateValueMetadata> fieldSources,
            string providerName,
            ProviderProfile profile,
            string outputField,
            object value,
            AircraftProviderSourceKind sourceKind,
            bool derived,
            string reason)
        {
            AircraftValueAvailability availability;

            if (value != null)
            {
                availability = derived ? AircraftValueAvailability.Derived : AircraftValueAvailability.Native;
            }
            else if (IsUnsupported(profile, outputField))
            {
                availability = AircraftValueAvailability.Unsupported;
            }
            else if ((reason ?? string.Empty).IndexOf("unavailable", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                availability = AircraftValueAvailability.Unavailable;
            }
            else
            {
                availability = AircraftValueAvailability.Unsupported;
            }

            fieldSources[outputField] = new AircraftStateValueMetadata
            {
                OutputField = outputField,
                ProviderName = providerName ?? "UnknownProvider",
                SourceKind = sourceKind,
                Availability = availability,
                Reason = reason
            };
        }

        private static bool IsUnsupported(ProviderProfile profile, string outputField)
        {
            if (profile == null || profile.UnsupportedFields == null)
            {
                return false;
            }

            for (int i = 0; i < profile.UnsupportedFields.Count; i++)
            {
                if (string.Equals(profile.UnsupportedFields[i], outputField, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static AircraftProviderSourceKind GetSourceKind(string source)
        {
            string text = source ?? string.Empty;
            if (text.IndexOf("sdk", StringComparison.OrdinalIgnoreCase) >= 0) return AircraftProviderSourceKind.Sdk;
            if (text.IndexOf("lvar", StringComparison.OrdinalIgnoreCase) >= 0) return AircraftProviderSourceKind.LVar;
            if (text.IndexOf("wasm", StringComparison.OrdinalIgnoreCase) >= 0) return AircraftProviderSourceKind.Wasm;
            if (text.IndexOf("mixed", StringComparison.OrdinalIgnoreCase) >= 0) return AircraftProviderSourceKind.Mixed;
            return AircraftProviderSourceKind.SimConnect;
        }
    }

    public static class CapabilityReporter
    {
        public static AircraftCapabilityReport Build(ProviderProfile profile, AircraftStateFrame frame)
        {
            var available = new List<string>();
            var unavailable = new List<string>();
            var unsupported = new List<string>();

            if (profile != null && profile.CapabilityDefinitions != null)
            {
                for (int i = 0; i < profile.CapabilityDefinitions.Count; i++)
                {
                    ProviderCapabilityDefinition capability = profile.CapabilityDefinitions[i];
                    if (HasCapability(frame, capability))
                    {
                        available.Add(capability.Key);
                    }
                    else
                    {
                        unavailable.Add(capability.Key);
                    }
                }
            }

            if (profile != null && profile.UnsupportedFields != null)
            {
                for (int i = 0; i < profile.UnsupportedFields.Count; i++)
                {
                    unsupported.Add(profile.UnsupportedFields[i]);
                }
            }

            return new AircraftCapabilityReport
            {
                Available = available,
                Unavailable = unavailable,
                Unsupported = unsupported
            };
        }

        private static bool HasCapability(AircraftStateFrame frame, ProviderCapabilityDefinition capability)
        {
            if (frame == null || frame.FieldSources == null || capability == null || capability.OutputFields == null)
            {
                return false;
            }

            for (int i = 0; i < capability.OutputFields.Count; i++)
            {
                AircraftStateValueMetadata metadata;
                if (frame.FieldSources.TryGetValue(capability.OutputFields[i], out metadata) &&
                    metadata != null &&
                    metadata.Availability != AircraftValueAvailability.Unavailable &&
                    metadata.Availability != AircraftValueAvailability.Unsupported)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class ProfileFactory
    {
        public static ProviderProfile CreateGenericProfile()
        {
            return new ProviderProfile(
                "generic-simconnect",
                "Generic SimConnect",
                AircraftProviderSourceKind.SimConnect,
                new List<ProviderDetectionRule>(),
                new List<ProviderVariableProfile>
                {
                    new ProviderVariableProfile("apu.status", "APU PCT RPM/APU SWITCH/APU GENERATOR ACTIVE", "simconnect", "status", "derived", "Generic SimConnect APU baseline."),
                    new ProviderVariableProfile("autopilot.flightDirectorEnabled", "AUTOPILOT FLIGHT DIRECTOR ACTIVE", "simconnect", "bool", "identity", "Generic flight director baseline."),
                    new ProviderVariableProfile("autopilot.selectedAltitudeMeters", "AUTOPILOT ALTITUDE LOCK VAR", "simconnect", "feet", "feet * 0.3048", "Generic selected altitude baseline.")
                },
                CreateCommonCapabilities(),
                new List<string>());
        }

        public static ProviderProfile CreateFenixA32xProfile()
        {
            return new ProviderProfile(
                "fenix-a32x",
                "Fenix A32x",
                AircraftProviderSourceKind.LVar,
                new List<ProviderDetectionRule>
                {
                    new ProviderDetectionRule("DetectedFamily", "Fenix A32x"),
                    new ProviderDetectionRule("PackagePath", "fnx-aircraft")
                },
                new List<ProviderVariableProfile>
                {
                    new ProviderVariableProfile("apu.status", "S_OH_ELEC_APU_MASTER, S_OH_ELEC_APU_START, S_OH_ELEC_APU_GENERATOR, S_OH_PNEUMATIC_APU_BLEED, S_ECAM_APU", "lvar", "mixed", "derived", "Fenix APU state derived from discovered LVars."),
                    new ProviderVariableProfile("autopilot.flightDirector1Enabled", "I_FCU_EFIS1_FD", "lvar", "bool", "identity", "Fenix FD1 state."),
                    new ProviderVariableProfile("autopilot.flightDirector2Enabled", "I_FCU_EFIS2_FD", "lvar", "bool", "identity", "Fenix FD2 state."),
                    new ProviderVariableProfile("autopilot.ap1Engaged", "I_FCU_AP1", "lvar", "bool", "identity", "Fenix AP1 state."),
                    new ProviderVariableProfile("autopilot.ap2Engaged", "I_FCU_AP2", "lvar", "bool", "identity", "Fenix AP2 state."),
                    new ProviderVariableProfile("autopilot.autoThrottleArmed", "I_FCU_ATHR", "lvar", "bool", "identity", "Fenix ATHR armed state."),
                    new ProviderVariableProfile("autopilot.selectedSpeedMetersPerSecond", "E_FCU_SPEED", "lvar", "knots", "scaled then knots * 0.514444", "Fenix FCU speed display."),
                    new ProviderVariableProfile("autopilot.selectedHeadingDegrees", "E_FCU_HEADING", "lvar", "degrees", "scaled", "Fenix FCU heading display."),
                    new ProviderVariableProfile("autopilot.selectedAltitudeMeters", "E_FCU_ALTITUDE", "lvar", "feet", "scaled then feet * 0.3048", "Fenix FCU altitude display."),
                    new ProviderVariableProfile("autopilot.selectedVerticalSpeedMetersPerSecond", "E_FCU_VS", "lvar", "feet/min", "scaled then feet/min * 0.00508", "Fenix FCU vertical speed display.")
                },
                CreateCommonCapabilities(),
                new List<string>());
        }

        public static ProviderProfile CreatePmdg777Profile()
        {
            return new ProviderProfile(
                "pmdg-777-sdk",
                "PMDG 777",
                AircraftProviderSourceKind.Sdk,
                new List<ProviderDetectionRule>
                {
                    new ProviderDetectionRule("DetectedFamily", "PMDG 777"),
                    new ProviderDetectionRule("Title", "PMDG 777")
                },
                new List<ProviderVariableProfile>
                {
                    new ProviderVariableProfile("apu.status", "PMDG_777X_Data.ELEC_APUGen_Sw_ON / ELEC_APU_Selector / AIR_APUBleedAir_Sw_AUTO", "sdk", "mixed", "derived", "PMDG 777 SDK client-data APU state."),
                    new ProviderVariableProfile("autopilot.flightDirector1Enabled", "PMDG_777X_Data.MCP_FD_Sw_On[0]", "sdk", "bool", "identity", "PMDG 777 FD left switch."),
                    new ProviderVariableProfile("autopilot.flightDirector2Enabled", "PMDG_777X_Data.MCP_FD_Sw_On[1]", "sdk", "bool", "identity", "PMDG 777 FD right switch."),
                    new ProviderVariableProfile("autopilot.ap1Engaged", "PMDG_777X_Data.MCP_annunAP[0]", "sdk", "bool", "identity", "PMDG 777 AP left annunciator."),
                    new ProviderVariableProfile("autopilot.ap2Engaged", "PMDG_777X_Data.MCP_annunAP[1]", "sdk", "bool", "identity", "PMDG 777 AP right annunciator."),
                    new ProviderVariableProfile("autopilot.autoThrottleArmed", "PMDG_777X_Data.MCP_ATArm_Sw_On[0/1]", "sdk", "bool", "identity", "PMDG 777 A/T ARM switches."),
                    new ProviderVariableProfile("autopilot.autoThrottleActive", "PMDG_777X_Data.MCP_annunAT", "sdk", "bool", "identity", "PMDG 777 A/T annunciator."),
                    new ProviderVariableProfile("autopilot.selectedSpeedMetersPerSecond", "PMDG_777X_Data.MCP_IASMach", "sdk", "knots or mach", "if >= 10 knots else mach", "PMDG 777 MCP IAS/Mach window."),
                    new ProviderVariableProfile("autopilot.selectedHeadingDegrees", "PMDG_777X_Data.MCP_Heading", "sdk", "degrees", "identity", "PMDG 777 MCP heading window."),
                    new ProviderVariableProfile("autopilot.selectedAltitudeMeters", "PMDG_777X_Data.MCP_Altitude", "sdk", "feet", "feet * 0.3048", "PMDG 777 MCP altitude window."),
                    new ProviderVariableProfile("autopilot.selectedVerticalSpeedMetersPerSecond", "PMDG_777X_Data.MCP_VertSpeed", "sdk", "feet/min", "feet/min * 0.00508", "PMDG 777 MCP vertical speed window.")
                },
                CreateCommonCapabilities(),
                new List<string>());
        }

        public static ProviderProfile CreatePmdg737Profile()
        {
            return new ProviderProfile(
                "pmdg-737-sdk",
                "PMDG 737",
                AircraftProviderSourceKind.Sdk,
                new List<ProviderDetectionRule>
                {
                    new ProviderDetectionRule("DetectedFamily", "PMDG 737"),
                    new ProviderDetectionRule("Title", "PMDG 737")
                },
                new List<ProviderVariableProfile>
                {
                    new ProviderVariableProfile("apu.status", "TODO_PMDG737_SDK_APU", "sdk", "mixed", "TODO", "TODO: wire real PMDG 737 SDK/client-data fields when the 737 package/SDK is available locally."),
                    new ProviderVariableProfile("autopilot.flightDirector1Enabled", "TODO_PMDG737_SDK_FD_LEFT", "sdk", "bool", "TODO", "TODO: wire PMDG 737 FD left source."),
                    new ProviderVariableProfile("autopilot.flightDirector2Enabled", "TODO_PMDG737_SDK_FD_RIGHT", "sdk", "bool", "TODO", "TODO: wire PMDG 737 FD right source."),
                    new ProviderVariableProfile("autopilot.selectedSpeedMetersPerSecond", "TODO_PMDG737_SDK_SPEED", "sdk", "knots", "TODO", "TODO: wire PMDG 737 MCP speed.")
                },
                CreateCommonCapabilities(),
                new List<string>
                {
                    "apu.status",
                    "autopilot.flightDirectorEnabled",
                    "autopilot.flightDirector1Enabled",
                    "autopilot.flightDirector2Enabled",
                    "autopilot.ap1Engaged",
                    "autopilot.ap2Engaged",
                    "autopilot.autoThrottleArmed",
                    "autopilot.autoThrottleActive",
                    "autopilot.selectedSpeedMetersPerSecond",
                    "autopilot.selectedMach",
                    "autopilot.selectedHeadingDegrees",
                    "autopilot.selectedAltitudeMeters",
                    "autopilot.selectedVerticalSpeedMetersPerSecond"
                });
        }

        public static ProviderProfile CreateIniBuildsAirbusProfile()
        {
            return new ProviderProfile(
                "inibuilds-airbus-lvar",
                "IniBuilds Airbus-style",
                AircraftProviderSourceKind.LVar,
                new List<ProviderDetectionRule>
                {
                    new ProviderDetectionRule("DetectedFamily", "IniBuilds A340"),
                    new ProviderDetectionRule("DetectedFamily", "IniBuilds A350"),
                    new ProviderDetectionRule("Title", "iniBuilds Airbus")
                },
                new List<ProviderVariableProfile>
                {
                    new ProviderVariableProfile("apu.status", "INI_APU_AVAILABLE / INI_APU_MASTER_SWITCH / INI_APU_START_BUTTON / INI_GEN_APU_GEN_SWITCH / INI_AIR_BLEED_APU", "lvar", "mixed", "derived", "IniBuilds Airbus reusable APU pattern."),
                    new ProviderVariableProfile("autopilot.flightDirector1Enabled", "INI_FD1_ON or INI_FD_ON", "lvar", "bool", "identity", "IniBuilds Airbus reusable FD source."),
                    new ProviderVariableProfile("autopilot.flightDirector2Enabled", "INI_FD2_ON or INI_FD_ON", "lvar", "bool", "identity", "IniBuilds Airbus reusable FD source."),
                    new ProviderVariableProfile("autopilot.ap1Engaged", "INI_ap1_on", "lvar", "bool", "identity", "IniBuilds Airbus reusable AP1 source."),
                    new ProviderVariableProfile("autopilot.ap2Engaged", "INI_ap2_on", "lvar", "bool", "identity", "IniBuilds Airbus reusable AP2 source."),
                    new ProviderVariableProfile("autopilot.autoThrottleArmed", "INI_AUTOTHROTTLE_ARMED", "lvar", "bool", "identity", "IniBuilds Airbus reusable ATHR armed source.")
                },
                CreateCommonCapabilities(),
                new List<string>());
        }

        public static ProviderProfile CreateIniBuildsVariantProfile(string id, string displayName, string family, string variant)
        {
            ProviderProfile baseProfile = CreateIniBuildsAirbusProfile();

            return new ProviderProfile(
                id,
                displayName,
                baseProfile.PrimarySourceKind,
                new List<ProviderDetectionRule>
                {
                    new ProviderDetectionRule("DetectedFamily", family)
                },
                new List<ProviderVariableProfile>(baseProfile.VariableProfiles),
                new List<ProviderCapabilityDefinition>(baseProfile.CapabilityDefinitions),
                new List<string>(baseProfile.UnsupportedFields));
        }

        public static ProviderProfile CreateFlyByWireA380XProfile()
        {
            return new ProviderProfile(
                "flybywire-a380x",
                "FlyByWire A380X",
                AircraftProviderSourceKind.LVar,
                new List<ProviderDetectionRule>
                {
                    new ProviderDetectionRule("DetectedFamily", "FlyByWire A380X"),
                    new ProviderDetectionRule("PackagePath", "flybywire-aircraft-a380-842"),
                    new ProviderDetectionRule("Title", "FlyByWire A380")
                },
                new List<ProviderVariableProfile>
                {
                    new ProviderVariableProfile("apu.status", "A32NX_OVHD_APU_MASTER_SW_PB_IS_ON, A32NX_OVHD_APU_START_PB_IS_AVAILABLE, A32NX_OVHD_APU_START_PB_IS_ON, A32NX_OVHD_PNEU_APU_BLEED_PB_IS_ON, A32NX_APU_BLEED_AIR_VALVE_OPEN, A32NX_APU_N", "lvar", "mixed", "derived", "FlyByWire A380X APU state derived from confirmed custom vars."),
                    new ProviderVariableProfile("autopilot.ap1Engaged", "A32NX_AUTOPILOT_1_ACTIVE", "lvar", "bool", "identity", "FlyByWire A380X AP1 state."),
                    new ProviderVariableProfile("autopilot.ap2Engaged", "A32NX_AUTOPILOT_2_ACTIVE", "lvar", "bool", "identity", "FlyByWire A380X AP2 state."),
                    new ProviderVariableProfile("autopilot.autoThrottleArmed", "A32NX_AUTOTHRUST_STATUS", "lvar", "enum", "> 0 => armed", "FlyByWire A380X autothrust armed state."),
                    new ProviderVariableProfile("autopilot.autoThrottleActive", "A32NX_AUTOTHRUST_STATUS", "lvar", "enum", ">= 2 => active", "FlyByWire A380X autothrust active state."),
                    new ProviderVariableProfile("autopilot.selectedSpeedMetersPerSecond", "A32NX_AUTOPILOT_SPEED_SELECTED", "lvar", "knots", "knots * 0.514444", "FlyByWire A380X selected speed."),
                    new ProviderVariableProfile("autopilot.selectedHeadingDegrees", "A32NX_AUTOPILOT_HEADING_SELECTED", "lvar", "degrees", "normalize heading", "FlyByWire A380X selected heading."),
                    new ProviderVariableProfile("autopilot.selectedVerticalSpeedMetersPerSecond", "A32NX_AUTOPILOT_VS_SELECTED", "lvar", "feet/min", "feet/min * 0.00508", "FlyByWire A380X selected vertical speed."),
                    new ProviderVariableProfile("autopilot.managedSpeed", "A32NX_SPEEDS_MANAGED_PFD", "lvar", "bool", "identity", "FlyByWire A380X managed speed indication."),
                    new ProviderVariableProfile("autopilot.verticalMode", "A32NX_FCU_ALT_MANAGED, A32NX_FCU_VS_MANAGED, A32NX_TRK_FPA_MODE_ACTIVE", "lvar", "mixed", "derived", "FlyByWire A380X vertical mode hints."),
                    new ProviderVariableProfile("autopilot.lateralMode", "A32NX_FCU_LOC_MODE_ACTIVE, A32NX_FCU_APPR_MODE_ACTIVE, A32NX_TRK_FPA_MODE_ACTIVE", "lvar", "mixed", "derived", "FlyByWire A380X lateral mode hints.")
                },
                CreateCommonCapabilities(),
                new List<string>());
        }

        private static IList<ProviderCapabilityDefinition> CreateCommonCapabilities()
        {
            return new List<ProviderCapabilityDefinition>
            {
                new ProviderCapabilityDefinition("apu", "APU state", "apu.status"),
                new ProviderCapabilityDefinition("flight_director", "Flight director state", "autopilot.flightDirectorEnabled", "autopilot.flightDirector1Enabled", "autopilot.flightDirector2Enabled"),
                new ProviderCapabilityDefinition("autopilot_engagement", "Autopilot engagement", "autopilot.ap1Engaged", "autopilot.ap2Engaged"),
                new ProviderCapabilityDefinition("autothrottle", "Autothrottle or ATHR", "autopilot.autoThrottleArmed", "autopilot.autoThrottleActive"),
                new ProviderCapabilityDefinition("selected_speed", "Selected speed", "autopilot.selectedSpeedMetersPerSecond", "autopilot.selectedMach"),
                new ProviderCapabilityDefinition("selected_heading", "Selected heading", "autopilot.selectedHeadingDegrees"),
                new ProviderCapabilityDefinition("selected_altitude", "Selected altitude", "autopilot.selectedAltitudeMeters"),
                new ProviderCapabilityDefinition("selected_vertical_speed", "Selected vertical speed", "autopilot.selectedVerticalSpeedMetersPerSecond"),
                new ProviderCapabilityDefinition("lateral_mode", "Lateral mode", "autopilot.lateralMode", "autopilot.managedLateral"),
                new ProviderCapabilityDefinition("vertical_mode", "Vertical mode", "autopilot.verticalMode", "autopilot.managedVertical"),
                new ProviderCapabilityDefinition("yaw_damper", "Yaw damper or equivalent", "flightControls.yawDamperEnabled")
            };
        }
    }
}
