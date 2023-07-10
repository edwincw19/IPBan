﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using DigitalRuby.IPBanCore;

namespace DigitalRuby.IPBanProShared
{
    /// <summary>
    /// Linux firewall using firewalld.
    /// </summary>
    [RequiredOperatingSystem(OSUtility.Linux, Priority = 5, FallbackFirewallType = typeof(IPBanLinuxFirewallIPTables))]
    [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
    public class IPBanLinuxFirewallD : IPBanBaseFirewall
    {
        private const string zoneFile = "/etc/firewalld/zones/public.xml";
        private const string defaultZoneFileContents = @"<?xml version=""1.0"" encoding=""utf-8""?>
        private const int allowPriority = 10;
        private const int dropPriority = 20;

        private readonly string allowRuleName;
        private readonly string allowRuleName6;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rulePrefix">Rule prefix</param>
        public IPBanLinuxFirewallD(string rulePrefix) : base(rulePrefix)
        {
            var pm = OSUtility.UsesYumPackageManager ? "yum" : "apt";
            int exitCode = IPBanLinuxBaseFirewallIPTables.RunProcess(pm, true, "install -q -y firewalld && systemctl start firewalld && systemctl enable firewalld");
            if (exitCode != 0)
            {
                throw new System.IO.IOException("Failed to initialize firewalld with code: " + exitCode);
            }
            IPBanLinuxBaseFirewallIPTables.RunProcess("ufw", false, "disable");
            allowRuleName = AllowRulePrefix + "0_4";
            allowRuleName = AllowRulePrefix + "0_6";
            if (!File.Exists(zoneFile))
            {
                File.WriteAllText(zoneFile, defaultZoneFileContents, ExtensionMethods.Utf8EncodingNoPrefix);
            }
        }

        /// <inheritdoc />
        public override Task<bool> AllowIPAddresses(IEnumerable<string> ipAddresses, CancellationToken cancelToken = default)
        {
            // create or update sets
            var ranges = ipAddresses.Select(i => IPAddressRange.Parse(i));
            var ip4s = ranges.Where(i => i.Begin.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            var ip6s = ranges.Where(i => i.Begin.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
            var result = IPBanLinuxIPSetFirewallD.UpsertSet(allowRuleName, IPBanLinuxIPSetIPTables.HashTypeSingleIP, IPBanLinuxIPSetIPTables.INetFamilyIPV4,
                ip4s, cancelToken);
            result |= IPBanLinuxIPSetFirewallD.UpsertSet(allowRuleName6, IPBanLinuxIPSetIPTables.HashTypeSingleIP, IPBanLinuxIPSetIPTables.INetFamilyIPV6,
                ip6s, cancelToken);

            // create or update rule
            result |= CreateOrUpdateRule(false, allowPriority, allowRuleName, allowRuleName6, Array.Empty<PortRange>());

            // done
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public override Task<bool> AllowIPAddresses(string ruleNamePrefix, IEnumerable<IPAddressRange> ipAddresses, IEnumerable<PortRange> allowedPorts = null, CancellationToken cancelToken = default)
        {
            // create or update sets
            string set = string.IsNullOrWhiteSpace(ruleNamePrefix) ? AllowRulePrefix : RulePrefix + ruleNamePrefix;
            var set4 = set + "_4";
            var set6 = set + "_6";
            var ip4s = ipAddresses.Where(i => i.Begin.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            var ip6s = ipAddresses.Where(i => i.Begin.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
            var result = IPBanLinuxIPSetFirewallD.UpsertSet(set4, IPBanLinuxIPSetIPTables.HashTypeNetwork, IPBanLinuxIPSetIPTables.INetFamilyIPV4,
                ip4s, cancelToken);
            result |= IPBanLinuxIPSetFirewallD.UpsertSet(set6, IPBanLinuxIPSetIPTables.HashTypeNetwork, IPBanLinuxIPSetIPTables.INetFamilyIPV6,
                ip6s, cancelToken);

            // create or update rule
            result |= CreateOrUpdateRule(false, allowPriority, set4, set6, allowedPorts);

            // done
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public override Task<bool> BlockIPAddresses(string ruleNamePrefix, IEnumerable<string> ipAddresses, IEnumerable<PortRange> allowedPorts = null, CancellationToken cancelToken = default)
        {
            // create or update sets
            string set = string.IsNullOrWhiteSpace(ruleNamePrefix) ? BlockRulePrefix : RulePrefix + ruleNamePrefix;
            var set4 = set + "_4";
            var set6 = set + "_6";
            var ranges = ipAddresses.Select(i => IPAddressRange.Parse(i));
            var ip4s = ranges.Where(i => i.Begin.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            var ip6s = ranges.Where(i => i.Begin.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
            var result = IPBanLinuxIPSetFirewallD.UpsertSet(set4, IPBanLinuxIPSetIPTables.HashTypeSingleIP, IPBanLinuxIPSetIPTables.INetFamilyIPV4,
                ip4s, cancelToken);
            result |= IPBanLinuxIPSetFirewallD.UpsertSet(set6, IPBanLinuxIPSetIPTables.HashTypeSingleIP, IPBanLinuxIPSetIPTables.INetFamilyIPV6,
                ip6s, cancelToken);

            // create or update rule
            result |= CreateOrUpdateRule(true, dropPriority, set4, set6, allowedPorts);

            // done
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public override Task<bool> BlockIPAddresses(string ruleNamePrefix, IEnumerable<IPAddressRange> ipAddresses, IEnumerable<PortRange> allowedPorts = null, CancellationToken cancelToken = default)
        {
            // create or update sets
            string set = string.IsNullOrWhiteSpace(ruleNamePrefix) ? BlockRulePrefix : RulePrefix + ruleNamePrefix;
            var set4 = set + "_4";
            var set6 = set + "_6";
            var ip4s = ipAddresses.Where(i => i.Begin.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            var ip6s = ipAddresses.Where(i => i.Begin.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
            var result = IPBanLinuxIPSetFirewallD.UpsertSet(set4, IPBanLinuxIPSetIPTables.HashTypeNetwork, IPBanLinuxIPSetIPTables.INetFamilyIPV4,
                ip4s, cancelToken);
            result |= IPBanLinuxIPSetFirewallD.UpsertSet(set6, IPBanLinuxIPSetIPTables.HashTypeNetwork, IPBanLinuxIPSetIPTables.INetFamilyIPV6,
                ip6s, cancelToken);

            // create or update rule
            result |= CreateOrUpdateRule(true, dropPriority, set4, set6, allowedPorts);

            // done
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public override Task<bool> BlockIPAddressesDelta(string ruleNamePrefix, IEnumerable<IPBanFirewallIPAddressDelta> ipAddresses, IEnumerable<PortRange> allowedPorts = null, CancellationToken cancelToken = default)
        {
            // create or update sets
            string set = string.IsNullOrWhiteSpace(ruleNamePrefix) ? BlockRulePrefix : RulePrefix + ruleNamePrefix;
            var set4 = set + "_4";
            var set6 = set + "_6";
            var result = IPBanLinuxIPSetFirewallD.UpsertSetDelta(set4, IPBanLinuxIPSetIPTables.HashTypeNetwork, IPBanLinuxIPSetIPTables.INetFamilyIPV4,
                ipAddresses.Where(i => i.IsIPV4), cancelToken);
            result |= IPBanLinuxIPSetFirewallD.UpsertSetDelta(set6, IPBanLinuxIPSetIPTables.HashTypeNetwork, IPBanLinuxIPSetIPTables.INetFamilyIPV6,
                ipAddresses.Where(i => !i.IsIPV4), cancelToken);

            // create or update rule
            result |= CreateOrUpdateRule(true, dropPriority, set4, set6, allowedPorts);

            // done
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public override bool DeleteRule(string ruleName)
        {
            var result = IPBanLinuxIPSetFirewallD.DeleteSet(ruleName);
            result |= DeleteRuleInternal(ruleName);
            ReloadFirewallD();
            return result;
        }

        /// <inheritdoc />
        public override IEnumerable<string> EnumerateAllowedIPAddresses()
        {
            var ruleTypes = GetRuleTypes();
            var ruleNames = GetRuleNames(RulePrefix);
            foreach (var rule in ruleNames.Where(r => ruleTypes.TryGetValue(r, out var accept) && accept))
            {
                var entries = IPBanLinuxIPSetFirewallD.ReadSet(rule);
                foreach (var entry in entries)
                {
                    yield return entry;
                }
            }
        }

        /// <inheritdoc />
        public override IEnumerable<string> EnumerateBannedIPAddresses()
        {
            var ruleTypes = GetRuleTypes();
            var ruleNames = GetRuleNames(RulePrefix);
            foreach (var rule in ruleNames.Where(r => ruleTypes.TryGetValue(r, out var accept) && !accept))
            {
                var entries = IPBanLinuxIPSetFirewallD.ReadSet(rule);
                foreach (var entry in entries)
                {
                    yield return entry;
                }
            }
        }

        /// <inheritdoc />
        public override IEnumerable<IPAddressRange> EnumerateIPAddresses(string ruleNamePrefix = null)
        {
            var ruleNames = GetRuleNames(ruleNamePrefix);
            foreach (var rule in ruleNames)
            {
                var entries = IPBanLinuxIPSetFirewallD.ReadSet(rule);
                foreach (var entry in entries)
                {
                    yield return entry;
                }
            }
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetRuleNames(string ruleNamePrefix = null)
        {
            var rules = IPBanLinuxIPSetFirewallD.GetSetNames(ruleNamePrefix);
            return rules;
        }

        /// <inheritdoc />
        public override bool IsIPAddressAllowed(string ipAddress, int port = -1)
        {
            if (System.Net.IPAddress.TryParse(ipAddress, out var ipObj))
            {
                foreach (var ip in EnumerateAllowedIPAddresses())
                {
                    if (IPAddressRange.TryParse(ip, out var range) && range.Contains(ipObj))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <inheritdoc />
        public override bool IsIPAddressBlocked(string ipAddress, out string ruleName, int port = -1)
        {
            if (System.Net.IPAddress.TryParse(ipAddress, out var ipObj))
            {
                var ruleTypes = GetRuleTypes();
                foreach (var kv in ruleTypes.Where(r => !r.Value))
                {
                    foreach (var ip in IPBanLinuxIPSetFirewallD.ReadSet(kv.Key))
                    {
                        if (IPAddressRange.TryParse(ip, out var range) && range.Contains(ipObj))
                        {
                            ruleName = kv.Key;
                            return true;
                        }
                    }
                }
            }
            ruleName = null;
            return false;
        }

        /// <inheritdoc />
        public override void Truncate()
        {
            foreach (var ruleName in IPBanLinuxIPSetFirewallD.GetSetNames(RulePrefix))
            {
                IPBanLinuxIPSetFirewallD.DeleteSet(ruleName);
                DeleteRuleInternal(ruleName);
            }
            ReloadFirewallD();
        }

        private static bool CreateOrUpdateRule(bool drop, int priority, string ruleIP4, string ruleIP6, IEnumerable<PortRange> allowedPorts)
        {
            if (!File.Exists(zoneFile))
            {
                File.WriteAllText(zoneFile, defaultZoneFileContents, ExtensionMethods.Utf8EncodingNoPrefix);
            }

            // load zone from file
            XmlDocument doc = new();
            doc.Load(zoneFile);

            // grab rule for ip4 and ip6
            if (doc.SelectSingleNode($"//rule/source[@ipset='{ruleIP4}']") is not XmlElement xmlElement4)
            {
                xmlElement4 = doc.CreateElement("rule");
                doc.ParentNode.AppendChild(xmlElement4);
            }
            else
            {
                xmlElement4.IsEmpty = true;
            }
            if (doc.SelectSingleNode($"//rule/source[@ipset='{ruleIP6}']") is not XmlElement xmlElement6)
            {
                xmlElement6 = doc.CreateElement("rule");
                doc.ParentNode.AppendChild(xmlElement6);
                xmlElement6.IsEmpty = true;
            }

            // assign rule attributes
            var action = drop ? "drop" : "accept";
            var priorityString = priority.ToString();
            xmlElement4.SetAttribute("priority", priorityString);
            xmlElement6.SetAttribute("priority", priorityString);

            // create and add source element
            var source4 = doc.CreateElement("source");
            source4.SetAttribute("ipset", ruleIP4);
            var source6 = doc.CreateElement("source");
            source6.SetAttribute("ipset", ruleIP4);
            xmlElement4.AppendChild(source4);
            xmlElement6.AppendChild(source6);

            // create and add port elements for each port entry
            var ports = allowedPorts;
            if (drop)
            {
                ports = IPBanFirewallUtility.GetBlockPortRanges(ports);
            }
            foreach (var port in ports)
            {
                var port4 = doc.CreateElement("port");
                port4.SetAttribute("port", port.ToString());
                port4.SetAttribute("protocol", "tcp");
                var port6 = doc.CreateElement("port");
                port6.SetAttribute("port", port.ToString());
                port6.SetAttribute("protocol", "tcp");
                xmlElement4.AppendChild(port4);
                xmlElement6.AppendChild(port6);
            }

            // create and add either drop or accept element
            if (drop)
            {
                var drop4 = doc.CreateElement("drop");
                var drop6 = doc.CreateElement("drop");
                xmlElement4.AppendChild(drop4);
                xmlElement6.AppendChild(drop6);
            }
            else
            {
                var accept4 = doc.CreateElement("accept");
                var accept6 = doc.CreateElement("accept");
                xmlElement4.AppendChild(accept4);
                xmlElement6.AppendChild(accept6);
            }

            // write the zone file back out and reload the firewall
            ExtensionMethods.Retry(() => File.WriteAllText(zoneFile, doc.OuterXml, ExtensionMethods.Utf8EncodingNoPrefix));
            ReloadFirewallD();

            return true;
        }

        private static bool ReloadFirewallD()
        {
            return IPBanLinuxBaseFirewallIPTables.RunProcess("firewall-cmd", true, "--reload") == 0;
        }

        private static bool DeleteRuleInternal(string ruleName)
        {
            bool foundOne = false;

            if (!File.Exists(zoneFile))
            {
                return foundOne;
            }

            XmlDocument doc = new();
            doc.LoadXml(zoneFile);
            var xmlElement = doc.SelectSingleNode($"//rule/source[@ipset='{ruleName}']");
            if (xmlElement is not null)
            {
                // remove the rule element which is the source parent
                xmlElement.ParentNode.ParentNode.RemoveChild(xmlElement.ParentNode);
                File.WriteAllText(zoneFile, doc.OuterXml, ExtensionMethods.Utf8EncodingNoPrefix);
                foundOne = true;
            }
            return foundOne;
        }

        private IReadOnlyDictionary<string, bool> GetRuleTypes()
        {
            Dictionary<string, bool> rules = new();
            var setNames = IPBanLinuxIPSetFirewallD.GetSetNames(RulePrefix);
            if (File.Exists(zoneFile))
            {
                XmlDocument doc = new();
                doc.LoadXml(zoneFile);
                var xmlRules = doc.SelectNodes($"//rule");
                if (xmlRules is not null)
                {
                    foreach (var node in xmlRules)
                    {
                        if (node is XmlElement xmlElement)
                        {
                            var sourceNode = xmlElement.SelectSingleNode("source");
                            if (sourceNode is XmlElement sourceElement)
                            {
                                var ipsetName = sourceElement.Attributes["ipset"]?.Value;
                                if (!string.IsNullOrWhiteSpace(ipsetName) &&
                                    setNames.Contains(ipsetName))
                                {
                                    bool accept = xmlElement.SelectSingleNode("//accept") is not null;
                                    rules[ipsetName] = accept;
                                }
                            }

                        }
                    }
                }
            }
            return rules;
        }
    }
}