using System;
using NuGet;

namespace Microsoft.Framework.Runtime
{
    public class SemanticVersion2
    {
        private string _originalString;

        public SemanticVersion SemanticVersion { get; private set; }

        public bool IsSnapshot { get; private set; }

        public SemanticVersion SpecifySnapshot(string snapshotValue)
        {
            if (!IsSnapshot)
            {
                return SemanticVersion;
            }

            var specificString = _originalString.Trim();
            specificString = specificString.Substring(0, specificString.Length - 2);
            if (!string.IsNullOrEmpty(snapshotValue))
            {
                specificString += "-" + snapshotValue;
            }

            return new SemanticVersion(specificString);
        }

        public static SemanticVersion2 Parse(string version)
        {
            var snapshotVersion = new SemanticVersion2();
            snapshotVersion._originalString = version;

            if (version.Trim().EndsWith("-*"))
            {
                snapshotVersion.IsSnapshot = true;
                version = version.Substring(0, version.Length - 2);
            }

            snapshotVersion.SemanticVersion = SemanticVersion.Parse(version);
            return snapshotVersion;
        }

        public static bool TryParse(string version, out SemanticVersion2 result)
        {
            var snapshotVersion = new SemanticVersion2();
            snapshotVersion._originalString = version;

            if (version.Trim().EndsWith("-*"))
            {
                snapshotVersion.IsSnapshot = true;
                version = version.Substring(0, version.Length - 2);
            }

            SemanticVersion semVer;
            if (!SemanticVersion.TryParse(version, out semVer))
            {
                result = null;
                return false;
            }

            snapshotVersion.SemanticVersion = semVer;
            result = snapshotVersion;

            return true;
        }

        public bool EqualsSnapshot(SemanticVersion2 seekingVersion)
        {
            if (seekingVersion.IsSnapshot)
            {
                return SemanticVersion.Version == seekingVersion.SemanticVersion.Version &&
                   SemanticVersion.SpecialVersion.StartsWith(seekingVersion.SemanticVersion.SpecialVersion, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return SemanticVersion.Equals(seekingVersion.SemanticVersion) && IsSnapshot == seekingVersion.IsSnapshot;
            }
        }


        public static bool ShouldUseConsidering(
            SemanticVersion2 current,
            SemanticVersion2 considering,
            SemanticVersion2 ideal)
        {
            if (considering == null)
            {
                // skip nulls
                return false;
            }
            if (!considering.EqualsSnapshot(ideal) && considering.SemanticVersion < ideal.SemanticVersion)
            {
                // don't use anything that's less than the requested version
                return false;
            }
            if (current == null)
            {
                // always use version when it's the first valid
                return true;
            }
            if (current.EqualsSnapshot(ideal) &&
                considering.EqualsSnapshot(ideal))
            {
                // favor higher version when they both match a snapshot patter
                return current.SemanticVersion < considering.SemanticVersion;
            }
            else
            {
                // otherwise favor lower version
                return current.SemanticVersion > considering.SemanticVersion;
            }
        }

        public override string ToString()
        {
            return SemanticVersion.ToString();
        }

        public static implicit operator SemanticVersion2(SemanticVersion v)
        {
            return new SemanticVersion2
            {
                SemanticVersion = v
            };
        }
    }
}