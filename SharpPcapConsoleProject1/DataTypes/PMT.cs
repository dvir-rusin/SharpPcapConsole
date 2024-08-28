using PacketDotNet.Utils;
using SharpPcapConsoleProject1.DataTypes;

public class PMT: TransportPackets
{
    public PMT() { }

    public PMT(TransportPackets packet): base(packet) 
    {
    
    }

    public Dictionary<int, PMTinfo> ElementaryStreams { get; set; } = new();
    public int TableID { get; set; }
    public bool SectionSyntaxIndicator { get; set; }
    public int SectionLength { get; set; }
    public int ProgramNumber { get; set; }
    public int VersionNumber { get; set; }
    public bool CurrentNextIndicator { get; set; }
    public int SectionNumber { get; set; }
    public int LastSectionNumber { get; set; }
    public int PCR_PID { get; set; }
    public int ProgramInfoLength { get; set; }
    public byte[] ProgramInfoDescriptors { get; set; }
    public byte [] CRC32 { get; set; }
    

    public void AddElementaryStream(int id ,PMTinfo pmtInfo)
    {
        ElementaryStreams.Add(id,pmtInfo);
    }

    public override string ToString()
    {//return $"TableID: {TableID}, SectionSyntaxIndicator: {SectionSyntaxIndicator}, SectionLength: {SectionLength}, ProgramNumber: {ProgramNumber}, VersionNumber: {VersionNumber}, CurrentNextIndicator: {CurrentNextIndicator}, SectionNumber: {SectionNumber}, LastSectionNumber: {LastSectionNumber}, PCR_PID: {PCR_PID}, ProgramInfoLength: {ProgramInfoLength}, ProgramInfoDescriptors: {BitConverter.ToString(ProgramInfoDescriptors)}, ElementaryStreams: \n{streamsInfo}";
        var streamsInfo = string.Join("\n", ElementaryStreams.Select(e => e.ToString()));
        return $"TableID: {TableID}, SectionSyntaxIndicator: {SectionSyntaxIndicator}, SectionLength: {SectionLength}, ProgramNumber: {ProgramNumber}, VersionNumber: {VersionNumber}, CurrentNextIndicator: {CurrentNextIndicator}, SectionNumber: {SectionNumber}, LastSectionNumber: {LastSectionNumber}, PCR_PID: {PCR_PID}, ProgramInfoLength: {ProgramInfoLength}, ElementaryStreams: \n{streamsInfo}";
    }
}
