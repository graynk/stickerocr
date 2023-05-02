using System.Runtime.InteropServices;

namespace editor.Data
{
  public interface ICardable
  {
    public string Text { get; set; }
    public byte[] Image { get; }
    public string? Link { get; }
  }
}