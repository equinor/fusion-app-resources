using Fusion.Integration.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers.Utilities
{
    [Authorize]
    [ApiController]
    public class UtilitiesController : ResourceControllerBase
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IFusionTokenProvider tokenProvider;
        private readonly IConfiguration configuration;
        private readonly IOptions<FusionIntegrationOptions> fusionOptions;

        public UtilitiesController(IHttpClientFactory httpClientFactory, IFusionTokenProvider tokenProvider, IConfiguration configuration, IOptions<FusionIntegrationOptions> fusionOptions)
        {
            this.httpClientFactory = httpClientFactory;
            this.tokenProvider = tokenProvider;
            this.configuration = configuration;
            this.fusionOptions = fusionOptions;
        }

        [HttpPost("/utilities/parse-spreadsheet")]
        public async Task<ActionResult<ExcelConversion>> ValidateContractorImportSpreadsheet([FromForm]ConvertSpreadsheetRequest request)
        {
            if (request == null)
                return FusionApiError.InvalidOperation("MissingBody", "Could not locate any body payload");

            var url = $"https://pro-f-utility-{fusionOptions.Value.ServiceDiscovery.Environment}.azurewebsites.net";
            var token = await tokenProvider.GetApplicationTokenAsync();
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            client.BaseAddress = new Uri(url);


            if (string.IsNullOrEmpty(url))
                throw new InvalidOperationException("Missing configuration for fusion utility function");


            using (var streamContent = new StreamContent(request.File!.OpenReadStream()))
            {
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                streamContent.Headers.ContentLength = request.File.Length;

                var response = await client.PostAsync("/api/excel-json-converter", streamContent);

                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return JsonConvert.DeserializeObject<ExcelConversion>(content);

                throw new InvalidOperationException($"Parser function returned non-successfull response ({response.StatusCode}).");
            }
        }

        #region Temporary models - should be moved to integration lib

        public class ExcelConversion
        {
            /// <summary>
            /// The column values of the first row in the spreadsheet.
            /// </summary>
            public List<ExcelHeader> Headers { get; set; } = null!;

            /// <summary>
            /// The data rows in the spreadsheet. 
            /// The first row is interpreted as headers and not included.
            /// </summary>
            public List<ExcelDataRow> Data { get; set; } = null!;

            /// <summary>
            /// Messages created during the parsing process.
            /// </summary>
            public List<ExcelParserMessage> Messages { get; set; } = null!;
        }

        public class ExcelHeader
        {
            /// <summary>
            /// The Excel cell reference (ex. A3)
            /// </summary>
            public string CellRef { get; set; } = null!;

            /// <summary>
            /// Content of cell
            /// </summary>
            public string Title { get; set; } = null!;

            /// <summary>
            /// Column index
            /// </summary>
            public int ColIndex { get; set; }
        }
        public class ExcelDataRow
        {
            /// <summary>
            /// Row index
            /// </summary>
            public int Index { get; set; }

            /// <summary>
            /// Cell content of row in string. Empty cells have empty strings "".
            /// </summary>
            public List<string> Items { get; set; } = null!;
        }

        public class ExcelParserMessage
        {
            public string Message { get; set; } = null!;

            public ExcelParserMessageLevel Level { get; set; }

            /// <summary>
            /// Row index the parser generated the message.
            /// </summary>
            public int Row { get; set; }

            /// <summary>
            /// Cell reference that is connected to the message.
            /// </summary>
            public string Cell { get; set; } = null!;

            public enum ExcelParserMessageLevel { Information, Warning, Error }

        }

        #endregion
    }
}
