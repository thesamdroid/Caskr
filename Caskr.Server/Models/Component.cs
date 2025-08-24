namespace Caskr.server.Models;

public partial class Component
{
    public int Id { get; set; }

    public int BatchId { get; set; }

    public string Name { get; set; } = null!;

    public int Percentage { get; set; }
}

