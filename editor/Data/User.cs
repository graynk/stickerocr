namespace editor.Data
{
  public class User
  {
    public long Id { get; set; }
    
    public string FirstName { get; set; }
    
    public string Username { get; set; }
    
    public string PhotoUrl { get; set; }
    
    public long AuthDate { get; set; }
    
    public string Hash { get; set; }
  }
}