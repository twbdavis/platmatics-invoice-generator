using PlatmaticsInvoice.Forms;

namespace PlatmaticsInvoice;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
