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
                _logger.LogInformation("Received request to generate documentation for: {Description}", request.Description);

                // Generate both DFD and DFD documentation
                var dfdTask = await _geminiService.GenerateDFD(request.Description);
                var dfdDocTask = await _geminiService.GenerateDFDDoc(request.Description);

               
                var dfd = dfdTask;
                var dfdDoc = dfdDocTask;

                _logger.LogInformation("Successfully generated DFD and documentation");

                return Ok(new
                {
                    dfd = dfd,
                    dfdDoc = dfdDoc
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
                _logger.LogInformation("Generating DFD documentation for: {Description}", request.Description);
                var dfdDoc = await _geminiService.GenerateDFDDoc(request.Description);
                
                _logger.LogInformation("Raw DFD documentation: {DfdDoc}", dfdDoc);

                // Clean up the response
                var cleanedResponse = dfdDoc
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                _logger.LogInformation("Cleaned DFD documentation: {CleanedDoc}", cleanedResponse);

                // Parse the JSON string into a dynamic object
                JsonElement dfdDocObject;
                try
                {
                    dfdDocObject = JsonSerializer.Deserialize<JsonElement>(cleanedResponse);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse DFD documentation JSON");
                    return BadRequest(new { error = "Invalid JSON format in DFD documentation", details = ex.Message });
                }

                // Generate DOCX
                var docxBytes = _docxService.GenerateDFDDocumentation(dfdDocObject);

                return File(docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "dfd-documentation.docx");
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
    }
} 