namespace Caskr.server.Models;

using System.Text.Json.Serialization;

public partial class CompletedTask
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string Name { get; set; } = null!;

    [JsonIgnore]
    public virtual Order Order { get; set; } = null!;
}
