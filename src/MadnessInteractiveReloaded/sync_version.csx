using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using System.Text.RegularExpressions;

const string csProj = "MIR.csproj";
const string versionFilePath = "Shared/GameVersion.cs";

var newVersion = new Version(0,1,0);

if (File.Exists(versionFilePath))
{
    string pattern = @"\(\s*(\d+),\s*(\d+),\s*(\d+)\)";
    var content = File.ReadAllText(versionFilePath);
    var match = Regex.Match(content, pattern);
    if (match.Success)
    {
        int major = int.Parse(match.Groups[1].Value);
        int minor = int.Parse(match.Groups[2].Value);
        int build = int.Parse(match.Groups[3].Value);
        newVersion = new(major, minor, build);
        Console.WriteLine("Read {0} from version file", newVersion.ToString());
    }
    else
    {
        Console.Error.WriteLine("failed to parse version from version file: {0}", content);
        return;
    }
}
else
{
    Console.Error.WriteLine("version file does not exist: {0}", versionFilePath);
    return;
}

if (File.Exists(csProj))
{
    var doc = XDocument.Load(csProj);
    var versionElement = doc.Root.Element("PropertyGroup").Element("Version");
    if (versionElement == null)
    {
        versionElement = new XElement("Version", "0.1.0");
        doc.Root.Element("PropertyGroup").Add(versionElement);
    }

    var version = Version.Parse((string)versionElement);
    Console.WriteLine("{0} >> {1}", version, newVersion);
    versionElement.SetValue(newVersion.ToString());
    doc.Save(csProj);
}
else
{
    Console.Error.WriteLine("csproj does not exist: {0}", csProj);
    return;
}