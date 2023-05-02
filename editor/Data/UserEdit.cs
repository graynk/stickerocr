using System;

namespace editor.Data
{
  public class UserEdit
  {
    public string UniqueId { get; set; }
    public long UserId { get; set; }
    public string Text { get; set; }
    
    public UserEdit() {}

    public UserEdit(string uniqueId, long userId, string text)
    {
      this.UniqueId = uniqueId;
      this.UserId = userId;
      this.Text = text;
    }

    public override bool Equals(object? obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != this.GetType()) return false;
      return Equals((UserEdit) obj);
    }

    protected bool Equals(UserEdit other)
    {
      return UniqueId == other.UniqueId && UserId == other.UserId;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(UniqueId, UserId);
    }
  }
}