using SharpPcapConsoleProject1;

internal class PAT: TransportPackets
{
    public PAT(TransportPackets packet): base (packet) 
    {
        
    }
    public Dictionary<int, PMT?> PMTs { get; set; } = new(); // Dictionary of PMT objects, keyed by ProgramNumber
    public int TableID { get; set; }
    public int SectionSyntaxIndicator { get; set; }
    public int SectionLength { get; set; }
    public int TransportStreamID { get; set; }
    public int VersionNumber { get; set; }
    public int CurrentNextIndicator { get; set; }
    public int SectionNumber { get; set; }
    public int LastSectionNumber { get; set; }
    public int NetworkPID { get; set; }
    public byte [] CRC32 { get; set; }
    


    public PAT(int tableID, int sectionSyntaxIndicator, int sectionLength, int transportStreamID, int versionNumber, int currentNextIndicator, int sectionNumber, int lastSectionNumber,byte [] cRC32)
    {
        TableID = tableID;
        SectionSyntaxIndicator = sectionSyntaxIndicator;
        SectionLength = sectionLength;
        TransportStreamID = transportStreamID;
        VersionNumber = versionNumber;
        CurrentNextIndicator = currentNextIndicator;
        SectionNumber = sectionNumber;
        LastSectionNumber = lastSectionNumber;
        CRC32 = cRC32;
    }

    public void AddPMT(int programNumber, PMT pmt)
    {
        PMTs[programNumber] = pmt;
    }

    public override string ToString()
    {
        if (PMTs[1]!=null)
        {
            //PMTs[1]?.ToString();
            var pmtInfo = string.Join("\n", PMTs.Select(p => p.ToString()));
            return $"TableID: {TableID}, SectionSyntaxIndicator: {SectionSyntaxIndicator}, SectionLength: {SectionLength}, TransportStreamID: {TransportStreamID}, VersionNumber: {VersionNumber}, CurrentNextIndicator: {CurrentNextIndicator}, SectionNumber: {SectionNumber}, LastSectionNumber: {LastSectionNumber}, PMTs: \n{pmtInfo}";

        }
        else
        {
            return null;
        }
    }
}
