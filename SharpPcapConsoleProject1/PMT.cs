using SharpPcapConsoleProject1;

internal class PMT
{
    public int TableID { get; set; }
    public int SectionSyntaxIndicator { get; set; }
    public int SectionLength { get; set; }
    public int ProgramNumber { get; set; }
    public int VersionNumber { get; set; }
    public bool CurrentNextIndicator { get; set; }
    public int SectionNumber { get; set; }
    public int LastSectionNumber { get; set; }
    public int PCR_PID { get; set; }
    public int ProgramInfoLength { get; set; }
    public byte[] ProgramInfoDescriptors { get; set; }
    public List<PMTinfo> ElementaryStreams { get; set; } = new List<PMTinfo>();

    public PMT(int tableID, int sectionSyntaxIndicator, int sectionLength, int programNumber, int versionNumber, bool currentNextIndicator, int sectionNumber, int lastSectionNumber, int pcrPID, int programInfoLength, byte[] programInfoDescriptors)
    {
        TableID = tableID;
        SectionSyntaxIndicator = sectionSyntaxIndicator;
        SectionLength = sectionLength;
        ProgramNumber = programNumber;
        VersionNumber = versionNumber;
        CurrentNextIndicator = currentNextIndicator;
        SectionNumber = sectionNumber;
        LastSectionNumber = lastSectionNumber;
        PCR_PID = pcrPID;
        ProgramInfoLength = programInfoLength;
        ProgramInfoDescriptors = programInfoDescriptors;
    }

    public void AddElementaryStream(PMTinfo pmtInfo)
    {
        ElementaryStreams.Add(pmtInfo);
    }

    public override string ToString()
    {
        var streamsInfo = string.Join("\n", ElementaryStreams.Select(e => e.ToString()));
        return $"TableID: {TableID}, SectionSyntaxIndicator: {SectionSyntaxIndicator}, SectionLength: {SectionLength}, ProgramNumber: {ProgramNumber}, VersionNumber: {VersionNumber}, CurrentNextIndicator: {CurrentNextIndicator}, SectionNumber: {SectionNumber}, LastSectionNumber: {LastSectionNumber}, PCR_PID: {PCR_PID}, ProgramInfoLength: {ProgramInfoLength}, ProgramInfoDescriptors: {BitConverter.ToString(ProgramInfoDescriptors)}, ElementaryStreams: \n{streamsInfo}";
    }
}
