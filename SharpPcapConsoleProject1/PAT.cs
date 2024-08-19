internal class PAT
{
    public int TableID { get; set; }
    public int SectionSyntaxIndicator { get; set; }
    public int SectionLength { get; set; }
    public int TransportStreamID { get; set; }
    public int VersionNumber { get; set; }
    public bool CurrentNextIndicator { get; set; }
    public int SectionNumber { get; set; }
    public int LastSectionNumber { get; set; }
    public List<PMT> PMTs { get; set; } = new List<PMT>(); // List of PMT objects

    public PAT(int tableID, int sectionSyntaxIndicator, int sectionLength, int transportStreamID, int versionNumber, bool currentNextIndicator, int sectionNumber, int lastSectionNumber)
    {
        TableID = tableID;
        SectionSyntaxIndicator = sectionSyntaxIndicator;
        SectionLength = sectionLength;
        TransportStreamID = transportStreamID;
        VersionNumber = versionNumber;
        CurrentNextIndicator = currentNextIndicator;
        SectionNumber = sectionNumber;
        LastSectionNumber = lastSectionNumber;
    }

    public void AddPMT(PMT pmt)
    {
        PMTs.Add(pmt);
    }

    public override string ToString()
    {
        var pmtInfo = string.Join("\n", PMTs.Select(p => p.ToString()));
        return $"TableID: {TableID}, SectionSyntaxIndicator: {SectionSyntaxIndicator}, SectionLength: {SectionLength}, TransportStreamID: {TransportStreamID}, VersionNumber: {VersionNumber}, CurrentNextIndicator: {CurrentNextIndicator}, SectionNumber: {SectionNumber}, LastSectionNumber: {LastSectionNumber}, PMTs: \n{pmtInfo}";
    }
}
