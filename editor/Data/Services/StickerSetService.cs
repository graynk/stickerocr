using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using editor.Data.Services.Shared;

namespace editor.Data.Services
{
    public class StickerSetService : Service
    {
        public StickerSetService(IDbConnection db) : base(db)
        {
        }

        public Task<IEnumerable<StickerSet>> GetStickersSetsAsync(long? id, int offset, int elementsPerPage)
        {
            if (!id.HasValue || id == int.Parse(Environment.GetEnvironmentVariable("ADMIN_ID")))
            {
                return _db.QueryAsync<StickerSet>(@"
                        SELECT * 
                        FROM sticker_sets 
                        ORDER BY title                    
                        OFFSET :offset 
                        LIMIT :elementsPerPage
                    ",
                    new {offset, elementsPerPage});
            }

            return _db.QueryAsync<StickerSet>(
                @"
                        SELECT * 
                        FROM sticker_sets                 
                        WHERE name IN 
                              (
                                  SELECT set_name 
                                  FROM favorites 
                                  WHERE user_id = :userId
                              )
                        ORDER BY title 
                        OFFSET :offset 
                        LIMIT :elementsPerPage
                    ",
                new {userId = id, offset, elementsPerPage}
            );
        }

        public Task<int> CountStickerSetsAsync(long? id)
        {
            if (!id.HasValue || id == int.Parse(Environment.GetEnvironmentVariable("ADMIN_ID")))
            {
                return _db.QueryFirstAsync<int>("SELECT COUNT(*) FROM sticker_sets");
            }

            return _db.QueryFirstAsync<int>(
                @"
                        SELECT COUNT(*) 
                        FROM sticker_sets 
                        WHERE name IN 
                              (
                                  SELECT set_name 
                                  FROM favorites 
                                  WHERE user_id = :userId
                              )
                    ",
                new {userId = id}
            );
        }
    }
}