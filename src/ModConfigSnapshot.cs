using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace LimitByCraftingSkillMod
{
    internal sealed class ModConfigSnapshot
    {
        private static readonly IReadOnlyDictionary<string, bool> EmptySkills =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public bool DebugMode { get; private set; }
        public IReadOnlyDictionary<string, bool> CraftingSkillEnabled { get; private set; }
        public string Source { get; private set; }
        public string Hash { get; private set; }

        private ModConfigSnapshot()
        {
            CraftingSkillEnabled = EmptySkills;
            Source = "defaults";
            Hash = ComputeHash(ToXmlString(false, EmptySkills));
        }

        public static ModConfigSnapshot Empty(string source)
        {
            var snapshot = new ModConfigSnapshot();
            if (!string.IsNullOrWhiteSpace(source))
                snapshot.Source = source;
            return snapshot;
        }

        public static ModConfigSnapshot FromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return Empty("missing file");

            return FromXml(File.ReadAllText(path), path);
        }

        public static ModConfigSnapshot FromXml(string xml, string source)
        {
            var snapshot = new ModConfigSnapshot();
            if (!string.IsNullOrWhiteSpace(source))
                snapshot.Source = source;

            if (string.IsNullOrWhiteSpace(xml))
                return snapshot;

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var root = doc.DocumentElement;
            if (root == null) return snapshot;

            var debugNode = root.SelectSingleNode("DebugMode");
            if (debugNode != null && bool.TryParse(debugNode.InnerText?.Trim(), out var debug))
                snapshot.DebugMode = debug;

            var skills = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var skillsNode = root.SelectSingleNode("CraftingSkills");
            if (skillsNode != null)
            {
                foreach (XmlNode child in skillsNode.ChildNodes)
                {
                    if (child.NodeType != XmlNodeType.Element) continue;
                    var name = child.Name;
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var enabled = true;
                    if (!string.IsNullOrWhiteSpace(child.InnerText) &&
                        bool.TryParse(child.InnerText.Trim(), out var parsed))
                    {
                        enabled = parsed;
                    }
                    skills[name] = enabled;
                }
            }

            snapshot.CraftingSkillEnabled = skills;
            snapshot.Hash = ComputeHash(snapshot.ToXmlString());
            return snapshot;
        }

        public string ToXmlString()
        {
            return ToXmlString(DebugMode, CraftingSkillEnabled);
        }

        private static string ToXmlString(bool debugMode, IReadOnlyDictionary<string, bool> skills)
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement("LimitByCraftingSkillModConfig");
            doc.AppendChild(root);

            var skillsNode = doc.CreateElement("CraftingSkills");
            root.AppendChild(skillsNode);
            if (skills != null)
            {
                foreach (var pair in skills)
                {
                    var skill = doc.CreateElement(pair.Key);
                    skill.InnerText = pair.Value ? "true" : "false";
                    skillsNode.AppendChild(skill);
                }
            }

            var debug = doc.CreateElement("DebugMode");
            debug.InnerText = debugMode ? "true" : "false";
            root.AppendChild(debug);

            using (var stringWriter = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = false,
            }))
            {
                doc.Save(xmlWriter);
                xmlWriter.Flush();
                return stringWriter.ToString();
            }
        }

        private static string ComputeHash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(value ?? "");
                var hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
