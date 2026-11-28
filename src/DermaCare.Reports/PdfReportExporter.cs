using DermaCare.Core.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

namespace DermaCare.Reports;

public class PdfReportExporter
{
    public void ExportPrescription(MedicalRecord record, Patient patient, ClinicProfile clinic, string outputPath)
    {
        var doc = new Document();
        var section = doc.AddSection();

        var header = section.AddParagraph(clinic.ClinicName);
        header.Format.Font.Size = 18;
        header.Format.Font.Bold = true;

        section.AddParagraph($"{clinic.Address} | {clinic.Phone}");
        section.AddParagraph(" ").Format.SpaceAfter = 10;

        var title = section.AddParagraph("PRESCRIPTION");
        title.Format.Font.Size = 14;
        title.Format.Font.Bold = true;
        title.Format.SpaceAfter = 8;

        section.AddParagraph($"Patient: {patient.FullName} ({patient.PatientCode})");
        section.AddParagraph($"Date: {record.CreatedAt:dd/MM/yyyy}");
        section.AddParagraph($"Diagnosis: {record.Diagnosis} {(string.IsNullOrWhiteSpace(record.ICD10Code) ? "" : $"[{record.ICD10Code}]")}");
        section.AddParagraph(" ").Format.SpaceAfter = 8;

        if (record.Prescriptions.Count > 0)
        {
            var table = section.AddTable();
            table.Borders.Width = 0.5;
            table.AddColumn("4.5cm");
            table.AddColumn("2.5cm");
            table.AddColumn("3cm");
            table.AddColumn("2.5cm");
            table.AddColumn("4cm");

            var hdr = table.AddRow();
            hdr.Shading.Color = Colors.LightGray;
            hdr.Cells[0].AddParagraph("Medication").Format.Font.Bold = true;
            hdr.Cells[1].AddParagraph("Dose").Format.Font.Bold = true;
            hdr.Cells[2].AddParagraph("Frequency").Format.Font.Bold = true;
            hdr.Cells[3].AddParagraph("Duration").Format.Font.Bold = true;
            hdr.Cells[4].AddParagraph("Instructions").Format.Font.Bold = true;

            foreach (var rx in record.Prescriptions)
            {
                var row = table.AddRow();
                row.Cells[0].AddParagraph(rx.DrugName);
                row.Cells[1].AddParagraph(rx.Dose);
                row.Cells[2].AddParagraph(rx.Frequency);
                row.Cells[3].AddParagraph(rx.Duration);
                row.Cells[4].AddParagraph(rx.Instructions ?? "-");
            }
        }

        section.AddParagraph(" ").Format.SpaceAfter = 24;
        section.AddParagraph($"Doctor Signature: ___________________________   ({record.Doctor?.User?.FullName ?? "Attending Dermatologist"})");

        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();
        renderer.PdfDocument.Save(outputPath);
    }

    public void ExportInvoice(Invoice invoice, ClinicProfile clinic, string outputPath)
    {
        var doc = new Document();
        var section = doc.AddSection();

        var header = section.AddParagraph(clinic.ClinicName);
        header.Format.Font.Size = 18;
        header.Format.Font.Bold = true;

        section.AddParagraph($"{clinic.Address} | {clinic.Phone}");
        section.AddParagraph(" ").Format.SpaceAfter = 10;

        var billLabel = string.IsNullOrWhiteSpace(invoice.BillNumber) ? $"#{invoice.InvoiceId}" : invoice.BillNumber;
        var title = section.AddParagraph($"BILL RECEIPT — {billLabel}");
        title.Format.Font.Size = 14;
        title.Format.Font.Bold = true;
        title.Format.SpaceAfter = 6;

        var custCode = string.IsNullOrWhiteSpace(invoice.Patient?.PatientCode) ? string.Empty : $" ({invoice.Patient.PatientCode})";
        section.AddParagraph($"Patient: {invoice.DisplayCustomerName}{custCode}");
        section.AddParagraph($"Date: {invoice.IssuedAt.ToLocalTime():dd/MM/yyyy HH:mm} | Status: {invoice.Status}");
        if (!string.IsNullOrWhiteSpace(invoice.Notes))
            section.AddParagraph($"Notes: {invoice.Notes}");
        section.AddParagraph(" ").Format.SpaceAfter = 8;

        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.AddColumn("2.5cm");
        table.AddColumn("6.5cm");
        table.AddColumn("1.8cm");
        table.AddColumn("2.8cm");
        table.AddColumn("2.9cm");

        var hdr = table.AddRow();
        hdr.Shading.Color = Colors.LightGray;
        hdr.Cells[0].AddParagraph("Type").Format.Font.Bold = true;
        hdr.Cells[1].AddParagraph("Description").Format.Font.Bold = true;
        hdr.Cells[2].AddParagraph("Qty").Format.Font.Bold = true;
        hdr.Cells[3].AddParagraph("Unit Price").Format.Font.Bold = true;
        hdr.Cells[4].AddParagraph("Line Total").Format.Font.Bold = true;

        foreach (var item in invoice.Items)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(item.ItemType.ToString());
            var descText = item.EffectiveDiscount > 0
                ? $"{item.Description} (Disc: -Rs. {item.EffectiveDiscount:N0})"
                : item.Description;
            row.Cells[1].AddParagraph(descText);
            row.Cells[2].AddParagraph(item.Quantity.ToString());
            row.Cells[3].AddParagraph($"Rs. {item.UnitPrice:N2}");
            row.Cells[4].AddParagraph($"Rs. {item.LineTotal:N2}");
        }

        section.AddParagraph(" ").Format.SpaceAfter = 8;
        if (invoice.DiscountAmount > 0)
        {
            section.AddParagraph($"Subtotal: Rs. {invoice.Subtotal:N2}");
            section.AddParagraph($"Discount: -Rs. {invoice.DiscountAmount:N2}");
        }
        section.AddParagraph($"Total Amount: Rs. {invoice.TotalAmount:N2}").Format.Font.Bold = true;
        section.AddParagraph($"Paid Amount: Rs. {invoice.PaidAmount:N2}");
        section.AddParagraph($"Balance Due: Rs. {invoice.BalanceDue:N2}").Format.Font.Bold = true;

        if (invoice.Payments.Count > 0)
        {
            section.AddParagraph(" ").Format.SpaceAfter = 6;
            var payTitle = section.AddParagraph("Payments Recorded:");
            payTitle.Format.Font.Bold = true;
            foreach (var p in invoice.Payments)
                section.AddParagraph($"  - {p.PaidAt.ToLocalTime():dd/MM/yyyy HH:mm} | {p.Method} | Rs. {p.Amount:N2}");
        }

        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();
        renderer.PdfDocument.Save(outputPath);
    }

    public void ExportSummaryReport(
        DermaCare.Core.Interfaces.ClinicReportSummary summary,
        DateTime from,
        DateTime to,
        ClinicProfile clinic,
        string outputPath)
    {
        var doc = new Document();
        var section = doc.AddSection();

        var header = section.AddParagraph(clinic.ClinicName);
        header.Format.Font.Size = 18;
        header.Format.Font.Bold = true;
        section.AddParagraph($"{clinic.Address} | {clinic.Phone}");
        section.AddParagraph(" ").Format.SpaceAfter = 10;

        var title = section.AddParagraph($"CLINIC REVENUE & CLOSING REPORT ({from:dd/MM/yyyy} - {to:dd/MM/yyyy})");
        title.Format.Font.Size = 13;
        title.Format.Font.Bold = true;
        title.Format.SpaceAfter = 8;

        section.AddParagraph($"Total Appointments: {summary.TotalAppointments} (Completed: {summary.CompletedAppointments})");
        section.AddParagraph($"New Patients: {summary.NewPatients}");
        section.AddParagraph($"Bills Created: {summary.Invoices.Count}");
        section.AddParagraph($"Total Billed: Rs. {summary.TotalBilled:N2}").Format.Font.Bold = true;
        section.AddParagraph($"Total Collected: Rs. {summary.TotalCollected:N2}").Format.Font.Bold = true;
        section.AddParagraph(" ").Format.SpaceAfter = 8;

        if (summary.RevenueByMethod.Count > 0)
        {
            var mTitle = section.AddParagraph("Collection by Payment Method:");
            mTitle.Format.Font.Bold = true;
            mTitle.Format.SpaceAfter = 4;

            var mTable = section.AddTable();
            mTable.Borders.Width = 0.5;
            mTable.AddColumn("6cm");
            mTable.AddColumn("3cm");
            mTable.AddColumn("4cm");

            var mHdr = mTable.AddRow();
            mHdr.Shading.Color = Colors.LightGray;
            mHdr.Cells[0].AddParagraph("Payment Method").Format.Font.Bold = true;
            mHdr.Cells[1].AddParagraph("Transactions").Format.Font.Bold = true;
            mHdr.Cells[2].AddParagraph("Amount").Format.Font.Bold = true;

            foreach (var m in summary.RevenueByMethod)
            {
                var row = mTable.AddRow();
                row.Cells[0].AddParagraph(m.Method.ToString());
                row.Cells[1].AddParagraph(m.Count.ToString());
                row.Cells[2].AddParagraph($"Rs. {m.Amount:N2}");
            }
        }

        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();
        renderer.PdfDocument.Save(outputPath);
    }
}

