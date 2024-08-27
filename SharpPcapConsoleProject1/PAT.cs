using SharpPcapConsoleProject1;

internal class PAT: TransportPackets
{
    public PAT(TransportPackets packet): base (packet) 
    {
        
    }
    public Dictionary<int, PMT?> PMTs { get; set; } = new(); // Dictionary of PMT objects, keyed by ProgramNumber
    public int TableID { get; set; }
    public bool SectionSyntaxIndicator { get; set; }
    public int SectionLength { get; set; }
    public int TransportStreamID { get; set; }
    public int VersionNumber { get; set; }
    public bool CurrentNextIndicator { get; set; }
    public int SectionNumber { get; set; }
    public int LastSectionNumber { get; set; }
    public int NetworkPID { get; set; }
    public byte [] CRC32 { get; set; }
    

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
