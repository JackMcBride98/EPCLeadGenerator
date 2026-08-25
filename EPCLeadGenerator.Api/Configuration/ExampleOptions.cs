using System.ComponentModel.DataAnnotations;

namespace EPCLeadGenerator.Api.Configuration;

public class ExampleOptions
{
    public const string Position = "Example";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Example is missing")]
    public string Example { get; set; } = string.Empty;
}
