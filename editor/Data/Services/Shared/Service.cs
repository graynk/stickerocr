using System.Data;

namespace editor.Data.Services.Shared
{
  public abstract class Service
  {
    protected IDbConnection _db;

    protected Service(IDbConnection db)
    {
      this._db = db;
    }
  }
}