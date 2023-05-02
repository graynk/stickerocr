using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace editor.Data
{
  public class Sticker : ICardable
  {
    [Key]
    public string UniqueId { get; set; }
    public string Text { get; set; }
    public byte[] Image { get; set; }
    [NotMapped]
    public string? Link { get; set; } = null;
  }
}