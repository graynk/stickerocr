namespace editor.Data
{
  public class UserEditDisplay : UserEdit, ICardable
  {
    public UserEditDisplay() {}
    public UserEditDisplay(string uniqueId, long userId, string text) : base(uniqueId, userId, text)
    {
    }

    public byte[] Image { get; set; }
    public string? Link { get; }
  }
}