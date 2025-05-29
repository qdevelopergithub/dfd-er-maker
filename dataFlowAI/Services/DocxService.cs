using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace dataFlowAI.Services
{
    public class DocxService
    {
        private readonly ILogger<DocxService> _logger;

        public DocxService(ILogger<DocxService> logger)
        {
            _logger = logger;
        }

        private T GetPropertySafe<T>(JsonElement element, string propertyName, T defaultValue = default)
        {
            try
            {
                if (element.TryGetProperty(propertyName, out JsonElement property))
                {
                    if (typeof(T) == typeof(string))
                    {
                        var stringValue = property.GetString();
                        return stringValue != null ? (T)(object)stringValue : defaultValue;
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        return (T)(object)property.GetBoolean();
                    }
                    else if (typeof(T) == typeof(JsonElement))
                    {
                        return (T)(object)property;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting property {PropertyName}", propertyName);
            }
            return defaultValue;
        }

        private IEnumerable<JsonElement> EnumerateArraySafe(JsonElement element, string propertyName)
        {
            try
            {
                if (element.TryGetProperty(propertyName, out JsonElement arrayElement) && 
                    arrayElement.ValueKind == JsonValueKind.Array)
                {
                    return arrayElement.EnumerateArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enumerating array {PropertyName}", propertyName);
            }
            return Enumerable.Empty<JsonElement>();
        }

        public byte[] GenerateDFDDocumentation(JsonElement dfdDoc)
        {
            try
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    using (WordprocessingDocument doc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document))
                    {
                        MainDocumentPart mainPart = doc.AddMainDocumentPart();
                        mainPart.Document = new Document();
                        Body body = mainPart.Document.AppendChild(new Body());

                        // Title
                        body.AppendChild(CreateHeading("Data Flow Diagram Documentation", 1, true));
                        body.AppendChild(new Paragraph());

                        // System Overview
                        body.AppendChild(CreateHeading("1. System Overview", 2));
                        body.AppendChild(CreateParagraph(GetPropertySafe<string>(dfdDoc, "systemOverview", "No system overview available")));
                        body.AppendChild(new Paragraph());

                        // Level 2 DFD Description
                        body.AppendChild(CreateHeading("2. Level 2 DFD Description", 2));
                        body.AppendChild(CreateParagraph(GetPropertySafe<string>(dfdDoc, "level2DFD", "No Level 2 DFD description available")));
                        body.AppendChild(new Paragraph());

                        // External Entities
                        body.AppendChild(CreateHeading("3. External Entities", 2));
                        foreach (var entity in EnumerateArraySafe(dfdDoc, "externalEntities"))
                        {
                            body.AppendChild(CreateHeading(GetPropertySafe<string>(entity, "name", "Unnamed Entity"), 3));
                            body.AppendChild(CreateParagraph(GetPropertySafe<string>(entity, "description", "No description available")));
                            body.AppendChild(CreateParagraph($"Interactions: {GetPropertySafe<string>(entity, "interactions", "No interactions specified")}"));
                            body.AppendChild(new Paragraph());
                        }

                        // Processes
                        body.AppendChild(CreateHeading("4. Processes", 2));
                        foreach (var process in EnumerateArraySafe(dfdDoc, "processes"))
                        {
                            body.AppendChild(CreateHeading(GetPropertySafe<string>(process, "name", "Unnamed Process"), 3));
                            body.AppendChild(CreateParagraph(GetPropertySafe<string>(process, "description", "No description available")));
                            body.AppendChild(CreateParagraph($"Inputs: {GetPropertySafe<string>(process, "inputs", "No inputs specified")}"));
                            body.AppendChild(CreateParagraph($"Outputs: {GetPropertySafe<string>(process, "outputs", "No outputs specified")}"));
                            body.AppendChild(new Paragraph());
                        }

                        // Data Stores
                        body.AppendChild(CreateHeading("5. Data Stores", 2));
                        foreach (var store in EnumerateArraySafe(dfdDoc, "dataStores"))
                        {
                            body.AppendChild(CreateHeading(GetPropertySafe<string>(store, "name", "Unnamed Data Store"), 3));
                            body.AppendChild(CreateParagraph(GetPropertySafe<string>(store, "description", "No description available")));
                            body.AppendChild(CreateParagraph($"Data: {GetPropertySafe<string>(store, "data", "No data specified")}"));
                            body.AppendChild(new Paragraph());
                        }

                        // Data Flows
                        body.AppendChild(CreateHeading("6. Data Flows", 2));
                        foreach (var flow in EnumerateArraySafe(dfdDoc, "dataFlows"))
                        {
                            var from = GetPropertySafe<string>(flow, "from", "Unknown");
                            var to = GetPropertySafe<string>(flow, "to", "Unknown");
                            body.AppendChild(CreateHeading($"{from} → {to}", 3));
                            body.AppendChild(CreateParagraph(GetPropertySafe<string>(flow, "description", "No description available")));
                            body.AppendChild(CreateParagraph($"Data: {GetPropertySafe<string>(flow, "data", "No data specified")}"));
                            body.AppendChild(new Paragraph());
                        }

                        // System Boundaries
                        body.AppendChild(CreateHeading("7. System Boundaries", 2));
                        body.AppendChild(CreateParagraph(GetPropertySafe<string>(dfdDoc, "systemBoundaries", "No system boundaries specified")));

                        mainPart.Document.Save();
                    }

                    return mem.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating DFD documentation");
                throw;
            }
        }

        public byte[] GenerateERDocumentation(JsonElement erDoc)
        {
            try
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    using (WordprocessingDocument doc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document))
                    {
                        MainDocumentPart mainPart = doc.AddMainDocumentPart();
                        mainPart.Document = new Document();
                        Body body = mainPart.Document.AppendChild(new Body());

                        // Title
                        body.AppendChild(CreateHeading("Entity Relationship Diagram Documentation", 1, true));
                        body.AppendChild(new Paragraph());

                        // System Overview
                        body.AppendChild(CreateHeading("1. System Overview", 2));
                        body.AppendChild(CreateParagraph(GetPropertySafe<string>(erDoc, "systemOverview", "No system overview available")));
                        body.AppendChild(new Paragraph());

                        // Entities
                        body.AppendChild(CreateHeading("2. Entities", 2));
                        foreach (var entity in EnumerateArraySafe(erDoc, "entities"))
                        {
                            body.AppendChild(CreateHeading(GetPropertySafe<string>(entity, "name", "Unnamed Entity"), 3));
                            body.AppendChild(CreateParagraph(GetPropertySafe<string>(entity, "description", "No description available")));
                            
                            // Attributes
                            body.AppendChild(CreateHeading("Attributes:", 4));
                            foreach (var attr in EnumerateArraySafe(entity, "attributes"))
                            {
                                var name = GetPropertySafe<string>(attr, "name", "Unnamed");
                                var type = GetPropertySafe<string>(attr, "type", "unknown");
                                var isPrimary = GetPropertySafe<bool>(attr, "isPrimary");
                                var description = GetPropertySafe<string>(attr, "description", "No description");

                                var attrDesc = $"{name} ({type})";
                                if (isPrimary) attrDesc += " - Primary Key";
                                attrDesc += $"\n{description}";
                                body.AppendChild(CreateParagraph(attrDesc));
                            }
                            body.AppendChild(new Paragraph());
                        }

                        // Relationships
                        body.AppendChild(CreateHeading("3. Relationships", 2));
                        foreach (var rel in EnumerateArraySafe(erDoc, "relationships"))
                        {
                            var from = GetPropertySafe<string>(rel, "from", "Unknown");
                            var to = GetPropertySafe<string>(rel, "to", "Unknown");
                            var title = $"{from} → {to}";
                            
                            body.AppendChild(CreateHeading(title, 3));
                            body.AppendChild(CreateParagraph(GetPropertySafe<string>(rel, "description", "No description available")));
                            body.AppendChild(CreateParagraph($"Type: {GetPropertySafe<string>(rel, "type", "Unknown")}"));
                            body.AppendChild(CreateParagraph($"Cardinality: {GetPropertySafe<string>(rel, "cardinality", "Unknown")}"));
                            body.AppendChild(new Paragraph());
                        }

                        mainPart.Document.Save();
                    }

                    return mem.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ER documentation");
                throw;
            }
        }

        public byte[] GenerateSchemaDocumentation(JsonElement schemaDoc)
        {
            try
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    using (WordprocessingDocument doc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document))
                    {
                        MainDocumentPart mainPart = doc.AddMainDocumentPart();
                        mainPart.Document = new Document();
                        Body body = mainPart.Document.AppendChild(new Body());

                        // Title
                        body.AppendChild(CreateHeading("Database Schema Documentation", 1, true));
                        body.AppendChild(new Paragraph());

                        // Database Overview
                        body.AppendChild(CreateHeading("1. Database Overview", 2));
                        body.AppendChild(CreateParagraph(GetPropertySafe<string>(schemaDoc, "databaseOverview", "No database overview available")));
                        body.AppendChild(new Paragraph());

                        // Tables
                        body.AppendChild(CreateHeading("2. Tables", 2));
                        foreach (var table in EnumerateArraySafe(schemaDoc, "tables"))
                        {
                            body.AppendChild(CreateHeading(GetPropertySafe<string>(table, "name", "Unnamed Table"), 3));
                            body.AppendChild(CreateParagraph(GetPropertySafe<string>(table, "description", "No description available")));
                            
                            // Columns
                            body.AppendChild(CreateHeading("Columns:", 4));
                            foreach (var col in EnumerateArraySafe(table, "columns"))
                            {
                                var name = GetPropertySafe<string>(col, "name", "Unnamed");
                                var type = GetPropertySafe<string>(col, "type", "unknown");
                                var constraints = GetPropertySafe<string>(col, "constraints", "");
                                
                                var colDesc = $"{name} ({type})";
                                if (!string.IsNullOrWhiteSpace(constraints))
                                {
                                    colDesc += $" - {constraints}";
                                }
                                body.AppendChild(CreateParagraph(colDesc));
                            }

                            // Primary Key
                            var primaryKey = GetPropertySafe<string>(table, "primaryKey", null);
                            if (!string.IsNullOrWhiteSpace(primaryKey))
                            {
                                body.AppendChild(CreateHeading("Primary Key:", 4));
                                body.AppendChild(CreateParagraph(primaryKey));
                            }

                            // Foreign Keys
                            var foreignKeys = EnumerateArraySafe(table, "foreignKeys").ToList();
                            if (foreignKeys.Any())
                            {
                                body.AppendChild(CreateHeading("Foreign Keys:", 4));
                                foreach (var fk in foreignKeys)
                                {
                                    var column = GetPropertySafe<string>(fk, "column", "Unknown");
                                    var references = GetPropertySafe<string>(fk, "references", "Unknown");
                                    body.AppendChild(CreateParagraph($"{column} → {references}"));
                                }
                            }

                            body.AppendChild(new Paragraph());
                        }

                        mainPart.Document.Save();
                    }

                    return mem.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating schema documentation");
                throw;
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