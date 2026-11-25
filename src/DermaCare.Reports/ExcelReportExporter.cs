using DermaCare.Core.Interfaces;
using OfficeOpenXml;

namespace DermaCare.Reports;

public class ExcelReportExporter
{
    public void ExportSummary(ClinicReportSummary summary, string outputPath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();

        var overview = package.Workbook.Worksheets.Add("Summary");
        overview.Cells["A1"].Value = "DermaCare Clinic Report";
        overview.Cells["A1"].Style.Font.Bold = true;
        overview.Cells["A3"].Value = "Total Appointments";
        overview.Cells["B3"].Value = summary.TotalAppointments;
        overview.Cells["A4"].Value = "Completed Appointments";
        overview.Cells["B4"].Value = summary.CompletedAppointments;
        overview.Cells["A5"].Value = "New Patients";
        overview.Cells["B5"].Value = summary.NewPatients;
        overview.Cells["A6"].Value = "Total Billed";
        overview.Cells["B6"].Value = summary.TotalBilled;
        overview.Cells["A7"].Value = "Total Collected";
        overview.Cells["B7"].Value = summary.TotalCollected;
        overview.Cells["B6:B7"].Style.Numberformat.Format = "Rs. #,##0.00";

        var appts = package.Workbook.Worksheets.Add("Appointments");
        appts.Cells["A1"].Value = "Date";
        appts.Cells["B1"].Value = "Time";
        appts.Cells["C1"].Value = "Patient";
        appts.Cells["D1"].Value = "Doctor";
        appts.Cells["E1"].Value = "Status";
        appts.Row(1).Style.Font.Bold = true;

        for (var i = 0; i < summary.Appointments.Count; i++)
        {
            var r = i + 2;
            var a = summary.Appointments[i];
            appts.Cells[r, 1].Value = a.AppointmentDate.ToString("dd/MM/yyyy");
            appts.Cells[r, 2].Value = a.AppointmentTime.ToString();
            appts.Cells[r, 3].Value = a.Patient?.FullName ?? string.Empty;
            appts.Cells[r, 4].Value = a.Doctor?.User?.FullName ?? string.Empty;
            appts.Cells[r, 5].Value = a.Status.ToString();
        }

        package.SaveAs(new FileInfo(outputPath));
    }
}

