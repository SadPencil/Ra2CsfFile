// See https://aka.ms/new-console-template for more information
using SadPencil.Ra2CsfFile;

// New empty string table.
CsfFile csf = new CsfFile();

// Edit labels.
csf.AddLabel("gui:test-label-1", "This is the first line.\nThis is the second line =-;#!");
csf.AddLabel("GUI:Test-Label-2", "Hi there!");
Console.WriteLine(csf.Labels["GUI:Test-Label-2"]);
csf.RemoveLabel("GUI:Test-Label-2");

// Write as a csf file
string csfFilename = "example.csf";
using (FileStream fs = File.Open(csfFilename, FileMode.Create))
{
    csf.WriteCsfFile(fs);
}

// Load csf file
using (FileStream fs = File.Open(csfFilename, FileMode.Open))
{
    csf = CsfFile.LoadFromCsfFile(fs);
}

Console.WriteLine($"This csf file has {csf.Labels.Count} labels. The language is {csf.Language}.");

// Save as an ini file
string iniFilename = "example.ini";
using (FileStream fs = File.Open(iniFilename, FileMode.Create))
{
    CsfFileIniHelper.WriteIniFile(csf, fs);
}

// Load ini file
using (FileStream fs = File.Open(iniFilename, FileMode.Open))
{
    csf = CsfFileIniHelper.LoadFromIniFile(fs);
}

if (csf.Labels["gui:test-label-1"] == "This is the first line.\nThis is the second line =-;#!")
{
    Console.WriteLine("Yes. It is expected.");
}
else
{
    Console.WriteLine("Unexpected behavior.");
}