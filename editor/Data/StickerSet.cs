using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace editor.Data
{
  public class StickerSet : ICardable
  {
    [Key] 
    public string Name { get; set; }
    public string Title { get; set; }
    [NotMapped]
    public string Text
    {
      get => Title;
      set => Title = value;
    }
    
    public byte[] Thumb { get; set; }

    [NotMapped]
    public byte[] Image => Thumb;
    
    [NotMapped]
    public string Link => $"/stickerSet/{Name}";
  }
}