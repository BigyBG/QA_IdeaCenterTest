using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QA_IdeaCenterTest.Models;

internal class ApiResponseDTO
{
    [JsonPropertyName("msg")]
    public string? Message { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
