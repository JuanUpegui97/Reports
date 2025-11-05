using System.Collections.Generic;
using AnalizadorAuditoria.Methods;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AnalizadorAuditoria.Reports
{
    public class PdfReportGenerator
    {
        /// <summary>
        /// Genera PDF con los registros guardados en lista
        /// </summary>
        public void Generate(List<AuditRecord> records, string reportTitle, string filePath)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // === CAMBIO 2: Usamos reportTitle para el encabezado ===
                    page.Header()
                        .Text(reportTitle) // <-- Cambio aquí
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken2);

                    page.Content().Column(col =>
                    {
                        foreach (var record in records)
                        {

                            col.Item().PaddingTop(15).Column(c =>
                            {
                                // --- Muestra ID, Operación y Archivo ---
                                c.Item().Text($"Registro ID: {record.Id} | Operación: {record.GetOperationName()} | Archivo: {record.Archivo}").Bold();
                                c.Item().Text($"Fecha y Hora: {record.GetFormattedTimestamp()}").FontSize(9).FontColor(Colors.Grey.Medium).Bold();
                            });

                            // (El resto del código para la tabla y la lista no cambia)
                            if (record.Status == "R")
                            {
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                                    table.Header(h =>
                                    {
                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Campo").Bold();
                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Valor Anterior").Bold();
                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Valor Nuevo").Bold();
                                    });
                                    foreach (var newAttr in record.NewAttributes)
                                        if (record.OldAttributes.TryGetValue(newAttr.Key, out string oldValue) && oldValue != newAttr.Value)
                                        {
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(newAttr.Key);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(oldValue);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(newAttr.Value);
                                        }
                                });
                            }
                            else
                            {
                                var attributes = record.Status == "W" ? record.NewAttributes : record.OldAttributes;
                                foreach (var attr in attributes)
                                {
                                    col.Item().Text($"- {attr.Key}: {attr.Value}");
                                }
                            }
                        }
                    });
                    page.Footer().AlignCenter().Text(x => { x.Span("Página "); x.CurrentPageNumber(); });
                });
            }).GeneratePdf(filePath);
        }
    }
}