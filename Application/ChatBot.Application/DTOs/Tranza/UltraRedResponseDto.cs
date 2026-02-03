using System.Text.Json.Serialization;

namespace ChatBot.Application.DTOs.Tranza
{
    public class UltraRedResponseDto
    {
        public PersonDto Person { get; set; } = default!;

        [JsonPropertyName("card")]
        public List<CardDto> Cards { get; set; } = new();

        public string Result { get; set; } = default!;
    }
}
