namespace Domain.Entities;

public class CardLabel
{
    public int CardId { get; set; }
    public int LabelId { get; set; }
    
    // Navigation properties
    public Card Card { get; set; } = null!;
    public Label Label { get; set; } = null!;
} 