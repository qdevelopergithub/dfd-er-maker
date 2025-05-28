using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace dataFlowAI.Services
{
    public class DocxService
    {
        public byte[] GenerateDFDDocumentation(JsonElement dfdDoc)
        {
            using (MemoryStream mem = new MemoryStream())
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document))
                {
                    // Add a main document part
                    MainDocumentPart mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    Body body = mainPart.Document.AppendChild(new Body());

                    // Title
                    body.AppendChild(CreateHeading("Data Flow Diagram Documentation", 1, true));
                    body.AppendChild(new Paragraph());

                    // System Overview
                    body.AppendChild(CreateHeading("1. System Overview", 2));
                    body.AppendChild(CreateParagraph(dfdDoc.GetProperty("systemOverview").GetString() ?? "No system overview available"));
                    body.AppendChild(new Paragraph());

                    // Level 0 DFD Description
                    body.AppendChild(CreateHeading("2. Level 0 DFD Description", 2));
                    body.AppendChild(CreateParagraph(dfdDoc.GetProperty("level0DFD").GetString() ?? "No Level 0 DFD description available"));
                    body.AppendChild(new Paragraph());

                    // External Entities
                    body.AppendChild(CreateHeading("3. External Entities", 2));
                    foreach (var entity in dfdDoc.GetProperty("externalEntities").EnumerateArray())
                    {
                        body.AppendChild(CreateHeading(entity.GetProperty("name").GetString() ?? "Unnamed Entity", 3));
                        body.AppendChild(CreateParagraph(entity.GetProperty("description").GetString() ?? "No description available"));
                        body.AppendChild(CreateParagraph($"Interactions: {entity.GetProperty("interactions").GetString() ?? "No interactions specified"}"));
                        body.AppendChild(new Paragraph());
                    }

                    // Processes
                    body.AppendChild(CreateHeading("4. Processes", 2));
                    foreach (var process in dfdDoc.GetProperty("processes").EnumerateArray())
                    {
                        body.AppendChild(CreateHeading(process.GetProperty("name").GetString() ?? "Unnamed Process", 3));
                        body.AppendChild(CreateParagraph(process.GetProperty("description").GetString() ?? "No description available"));
                        body.AppendChild(CreateParagraph($"Inputs: {process.GetProperty("inputs").GetString() ?? "No inputs specified"}"));
                        body.AppendChild(CreateParagraph($"Outputs: {process.GetProperty("outputs").GetString() ?? "No outputs specified"}"));
                        body.AppendChild(new Paragraph());
                    }

                    // Data Stores
                    body.AppendChild(CreateHeading("5. Data Stores", 2));
                    foreach (var store in dfdDoc.GetProperty("dataStores").EnumerateArray())
                    {
                        body.AppendChild(CreateHeading(store.GetProperty("name").GetString() ?? "Unnamed Data Store", 3));
                        body.AppendChild(CreateParagraph(store.GetProperty("description").GetString() ?? "No description available"));
                        body.AppendChild(CreateParagraph($"Data: {store.GetProperty("data").GetString() ?? "No data specified"}"));
                        body.AppendChild(new Paragraph());
                    }

                    // Data Flows
                    body.AppendChild(CreateHeading("6. Data Flows", 2));
                    foreach (var flow in dfdDoc.GetProperty("dataFlows").EnumerateArray())
                    {
                        body.AppendChild(CreateHeading($"{flow.GetProperty("from").GetString() ?? "Unknown"} → {flow.GetProperty("to").GetString() ?? "Unknown"}", 3));
                        body.AppendChild(CreateParagraph(flow.GetProperty("description").GetString() ?? "No description available"));
                        body.AppendChild(CreateParagraph($"Data: {flow.GetProperty("data").GetString() ?? "No data specified"}"));
                        body.AppendChild(new Paragraph());
                    }

                    // System Boundaries
                    body.AppendChild(CreateHeading("7. System Boundaries", 2));
                    body.AppendChild(CreateParagraph(dfdDoc.GetProperty("systemBoundaries").GetString() ?? "No system boundaries specified"));

                    // Save the document
                    mainPart.Document.Save();
                }

                return mem.ToArray();
            }
        }

        private Paragraph CreateHeading(string text, int level, bool center = false)
        {
            Paragraph paragraph = new Paragraph();
            ParagraphProperties paragraphProperties = new ParagraphProperties();
            
            if (center)
            {
                paragraphProperties.Append(new Justification() { Val = JustificationValues.Center });
            }

            Run run = new Run();
            RunProperties runProperties = new RunProperties();
            runProperties.Append(new Bold());
            runProperties.Append(new FontSize() { Val = (level == 1) ? "32" : (level == 2) ? "28" : "24" });
            run.Append(runProperties);
            run.Append(new Text(text));

            paragraph.Append(paragraphProperties);
            paragraph.Append(run);

            return paragraph;
        }

        private Paragraph CreateParagraph(string text)
        {
            Paragraph paragraph = new Paragraph();
            Run run = new Run();
            RunProperties runProperties = new RunProperties();
            runProperties.Append(new FontSize() { Val = "24" });
            run.Append(runProperties);
            run.Append(new Text(text));
            paragraph.Append(run);
            return paragraph;
        }
    }
} 