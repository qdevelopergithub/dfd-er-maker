using Microsoft.AspNetCore.Mvc;
using dataFlowAI.Services;
using System.Threading.Tasks;
using System.Text.Json;

namespace dataFlowAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeneratorController : ControllerBase
    {
        private readonly GeminiService _geminiService;
        private readonly DocxService _docxService;
        private readonly ILogger<GeneratorController> _logger;

        public GeneratorController(GeminiService geminiService, DocxService docxService, ILogger<GeneratorController> logger)
        {
            _geminiService = geminiService;
            _docxService = docxService;
            _logger = logger;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateRequest request)
        {
            try
            {
                _logger.LogInformation("Received request to generate {DiagramType} for: {Description}", 
                    request.DiagramType, request.Description);

                string diagram;
                string documentation;

                switch (request.DiagramType?.ToLower())
                {
                    case "er":
                        diagram = await _geminiService.GenerateERD(request.Description);
                        documentation = await _geminiService.GenerateERDDoc(request.Description);
                        break;
                    case "schema":
                        diagram = await _geminiService.GenerateSchemaD(request.Description);
                        documentation = await _geminiService.GenerateSchemaDoc(request.Description);
                        break;
                    case "dfd":
                    default:
                        diagram = await _geminiService.GenerateDFD(request.Description);
                        documentation = await _geminiService.GenerateDFDDoc(request.Description);
                        break;
                }

                _logger.LogInformation("Successfully generated {DiagramType} and documentation", request.DiagramType);

                return Ok(new
                {
                    diagram = diagram,
                    documentation = documentation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating documentation");
                return StatusCode(500, new { error = "Failed to generate documentation", details = ex.Message });
            }
        }

        [HttpPost("download-docx")]
        public async Task<IActionResult> DownloadDocx([FromBody] GenerateRequest request)
        {
            try
            {
                _logger.LogInformation("Generating {DiagramType} documentation for: {Description}", 
                    request.DiagramType, request.Description);

                string documentation;
                string fileName;

                switch (request.DiagramType?.ToLower())
                {
                    case "er":
                        documentation = await _geminiService.GenerateERDDoc(request.Description);
                        fileName = "er-documentation.docx";
                        break;
                    case "schema":
                        documentation = await _geminiService.GenerateSchemaDoc(request.Description);
                        fileName = "schema-documentation.docx";
                        break;
                    case "dfd":
                    default:
                        documentation = await _geminiService.GenerateDFDDoc(request.Description);
                        fileName = "dfd-documentation.docx";
                        break;
                }
                
                _logger.LogInformation("Raw documentation: {Documentation}", documentation);

                // Clean up the response
                var cleanedResponse = documentation
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                _logger.LogInformation("Cleaned documentation: {CleanedDoc}", cleanedResponse);

                // Parse the JSON string into a dynamic object
                JsonElement docObject;
                try
                {
                    docObject = JsonSerializer.Deserialize<JsonElement>(cleanedResponse);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse documentation JSON");
                    return BadRequest(new { error = "Invalid JSON format in documentation", details = ex.Message });
                }

                // Generate DOCX based on diagram type
                byte[] docxBytes;
                switch (request.DiagramType?.ToLower())
                {
                    case "er":
                        docxBytes = _docxService.GenerateERDocumentation(docObject);
                        break;
                    case "schema":
                        docxBytes = _docxService.GenerateSchemaDocumentation(docObject);
                        break;
                    case "dfd":
                    default:
                        docxBytes = _docxService.GenerateDFDDocumentation(docObject);
                        break;
                }

                return File(docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating DOCX file");
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class GenerateRequest
    {
        public string Description { get; set; } = string.Empty;
        public string? DiagramType { get; set; }
    }
} 