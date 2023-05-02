using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using editor.Data.Services.Shared;
using Dapper;

namespace editor.Data.Services
{
  public class StickerService : Service
  {
    public StickerService(IDbConnection db) : base(db) {}
    public Task<string> GetSetTitle(string setName)
    {
      return _db.QueryFirstAsync<string>("SELECT title FROM sticker_sets WHERE name = :setName", new {setName});
    }
    
    public Task<IEnumerable<Sticker>> GetStickersAsync(string setName, long? userId)
    {
      if (!userId.HasValue)
      {
        return _db.QueryAsync<Sticker>(
          @"
          SELECT unique_id, text, image 
          FROM stickers 
          WHERE set_name = :setName
          AND NOT EXISTS (SELECT 1 FROM banned_stickers WHERE banned_stickers.unique_id = stickers.unique_id) 
          ORDER BY order_in_pack",
          new {setName}
          );
      }

      return _db.QueryAsync<Sticker>(@"
            SELECT 
                   stickers.unique_id, coalesce(edits.text, stickers.text) AS text, image
            FROM 
                 stickers 
                     LEFT JOIN (SELECT * FROM user_edits WHERE user_id = :userId) AS edits 
                         ON stickers.unique_id = edits.unique_id 
            WHERE set_name = :setName
            ORDER BY order_in_pack",
        new {userId, setName}
      );
    }
  }
}