using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DermaCare.Core.Models;

namespace DermaCare.UI.Helpers;

public static class ReceiptPrintHelper
{
    public static bool PrintThermalBill(Invoice invoice, ClinicProfile clinic)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return false;

        var doc = new FlowDocument
        {
            PageWidth = 280,
            PagePadding = new Thickness(8),
            ColumnWidth = 264,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10.5
        };

        void AddLine(string text, bool bold = false, TextAlignment align = TextAlignment.Left, double size = 10.5)
        {
            var p = new Paragraph(new Run(text))
            {
                Margin = new Thickness(0, 1, 0, 1),
                TextAlignment = align,
                FontSize = size,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
            };
            doc.Blocks.Add(p);
        }

        AddLine(clinic.ClinicName, bold: true, align: TextAlignment.Center, size: 12);
        if (!string.IsNullOrWhiteSpace(clinic.Address))
            AddLine(clinic.Address, align: TextAlignment.Center, size: 9.5);
        if (!string.IsNullOrWhiteSpace(clinic.Phone))
            AddLine(clinic.Phone, align: TextAlignment.Center, size: 9.5);

        AddLine("--------------------------------", align: TextAlignment.Center);

        var billNo = string.IsNullOrWhiteSpace(invoice.BillNumber) ? $"#{invoice.InvoiceId}" : invoice.BillNumber;
        AddLine($"Bill # : {billNo}", bold: true);
        AddLine($"Date   : {invoice.IssuedAt.ToLocalTime():dd/MM/yyyy HH:mm}");
        AddLine($"Patient: {invoice.Patient?.FullName}");
        if (!string.IsNullOrWhiteSpace(invoice.Patient?.PatientCode))
            AddLine($"Code   : {invoice.Patient.PatientCode}");
        AddLine($"Status : {invoice.Status}");

        AddLine("--------------------------------", align: TextAlignment.Center);
        AddLine("Item               Qty   Total", bold: true);
        AddLine("--------------------------------", align: TextAlignment.Center);

        foreach (var item in invoice.Items)
        {
            var desc = item.Description.Length > 18 ? item.Description[..18] : item.Description.PadRight(18);
            var qty = item.Quantity.ToString().PadLeft(3);
            var total = $"Rs.{item.LineTotal:N0}".PadLeft(8);
            AddLine($"{desc} {qty} {total}");
        }

        AddLine("--------------------------------", align: TextAlignment.Center);
        if (invoice.DiscountAmount > 0)
        {
            AddLine($"Subtotal : Rs. {invoice.Subtotal:N2}", align: TextAlignment.Right);
            AddLine($"Discount : -Rs. {invoice.DiscountAmount:N2}", align: TextAlignment.Right);
        }
        AddLine($"NET TOTAL: Rs. {invoice.TotalAmount:N2}", bold: true, align: TextAlignment.Right, size: 11.5);
        AddLine($"Paid     : Rs. {invoice.PaidAmount:N2}", align: TextAlignment.Right);
        AddLine($"Balance  : Rs. {invoice.BalanceDue:N2}", bold: true, align: TextAlignment.Right);

        if (invoice.Payments.Count > 0)
        {
            AddLine("--------------------------------", align: TextAlignment.Center);
            foreach (var pay in invoice.Payments)
                AddLine($"{pay.Method}: Rs. {pay.Amount:N0}");
        }

        if (!string.IsNullOrWhiteSpace(invoice.Notes))
        {
            AddLine("--------------------------------", align: TextAlignment.Center);
            AddLine($"Note: {invoice.Notes}", size: 9.5);
        }

        AddLine("--------------------------------", align: TextAlignment.Center);
        AddLine("Thank you for visiting!", align: TextAlignment.Center, bold: true);

        dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"DermaCare Bill {billNo}");
        return true;
    }

    public static bool PrintPrescription(MedicalRecord record, Patient patient, ClinicProfile clinic)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return false;

        var doc = new FlowDocument
        {
            PagePadding = new Thickness(40),
            ColumnWidth = 680,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12
        };

        doc.Blocks.Add(new Paragraph(new Run(clinic.ClinicName))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 2)
        });
        doc.Blocks.Add(new Paragraph(new Run($"{clinic.Address} | {clinic.Phone}"))
        {
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Blocks.Add(new Paragraph(new Run("PRESCRIPTION"))
        {
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        doc.Blocks.Add(new Paragraph(new Run(
            $"Patient: {patient.FullName} ({patient.PatientCode})   |   Date: {record.CreatedAt.ToLocalTime():dd/MM/yyyy}" +
            (string.IsNullOrWhiteSpace(patient.CNIC) ? "" : $"   |   CNIC: {patient.CNIC}")))
        {
            Margin = new Thickness(0, 0, 0, 4)
        });
        doc.Blocks.Add(new Paragraph(new Run(
            $"Diagnosis: {record.Diagnosis}" +
            (string.IsNullOrWhiteSpace(record.ICD10Code) ? "" : $" [{record.ICD10Code}]")))
        {
            Margin = new Thickness(0, 0, 0, 12)
        });

        if (record.Prescriptions.Count > 0)
        {
            var table = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) };
            for (var c = 0; c < 5; c++) table.Columns.Add(new TableColumn());
            var rg = new TableRowGroup();

            var hdr = new TableRow { Background = Brushes.LightGray };
            foreach (var h in new[] { "Medication", "Dose", "Frequency", "Duration", "Instructions" })
            {
                hdr.Cells.Add(new TableCell(new Paragraph(new Run(h)) { FontWeight = FontWeights.Bold, Margin = new Thickness(4) })
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0.5)
                });
            }
            rg.Rows.Add(hdr);

            foreach (var rx in record.Prescriptions)
            {
                var row = new TableRow();
                foreach (var val in new[] { rx.DrugName, rx.Dose, rx.Frequency, rx.Duration, rx.Instructions ?? "-" })
                {
                    row.Cells.Add(new TableCell(new Paragraph(new Run(val)) { Margin = new Thickness(4) })
                    {
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0.5)
                    });
                }
                rg.Rows.Add(row);
            }

            table.RowGroups.Add(rg);
            doc.Blocks.Add(table);
        }

        doc.Blocks.Add(new Paragraph(new Run(
            $"\n\nDoctor Signature: ___________________________   ({record.Doctor?.User?.FullName ?? "Attending Dermatologist"})"))
        {
            Margin = new Thickness(0, 24, 0, 0)
        });

        dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"Prescription {patient.PatientCode}");
        return true;
    }
}
