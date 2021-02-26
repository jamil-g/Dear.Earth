using static WordFinadReplaceNet.GenerateReport;

namespace WordFinadReplaceNet
{
    public interface IGenerateReport
    {
        string GenerateReportPdf(Marks marks, Coordinates coordinates, ReportExtraInfo reportextrainfo);
    }
}
