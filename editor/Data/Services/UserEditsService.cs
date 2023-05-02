using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using editor.Data.Services.Shared;

namespace editor.Data.Services
{
  public class UserEditsService : Service
  {
    public UserEditsService(IDbConnection db) : base(db) {}

    public Task<UserEditDisplay> GetNextUserEdit()
    {
      // not sure if LIMIT 1 is needed
      return _db.QueryFirstOrDefaultAsync<UserEditDisplay>(@"
          SELECT user_edits.unique_id, user_id, text, image FROM user_edits
          LEFT JOIN (SELECT unique_id, image from stickers) AS sticker_images on user_edits.unique_id = sticker_images.unique_id
          WHERE rejected = false AND
                NOT EXISTS (SELECT 1 FROM banned_stickers WHERE banned_stickers.unique_id = user_edits.unique_id)
          ORDER BY user_edits.unique_id
          LIMIT 1
        ");
    }

    public void AcceptUserEdit(UserEdit userEdit)
    {
      _db.Open();
      var transaction = _db.BeginTransaction(IsolationLevel.ReadCommitted);
      _db.Execute(@"
          UPDATE stickers SET text = (:text)
          WHERE unique_id = (:uniqueId)",
        new {text = userEdit.Text, uniqueId = userEdit.UniqueId},
        transaction
        );
      
      _db.Execute(@"
          DELETE FROM user_edits 
          WHERE unique_id = (:uniqueId) AND user_id = (:userId)", 
        new {uniqueId = userEdit.UniqueId, userId = userEdit.UserId},
        transaction
      );
      transaction.Commit();
      _db.Close();
    }
    
    public void RejectUserEdit(UserEdit userEdit)
    {
      _db.Execute(@"
          UPDATE user_edits SET rejected = true
          WHERE unique_id = (:uniqueId) and user_id = (:userId)",
        new {uniqueId = userEdit.UniqueId, userId = userEdit.UserId}
      );
    }
    
    public void BanUserEdit(UserEdit userEdit)
    {
      _db.Execute(@"
          INSERT INTO banned_stickers (unique_id) VALUES (:uniqueId)",
        new {uniqueId = userEdit.UniqueId}
      );
    }
    
    public void UpdateStickers(IEnumerable<UserEdit> edits, bool isAdmin)
    {
      //TODO: batch insert?
      _db.Open();
      var transaction = _db.BeginTransaction(IsolationLevel.ReadCommitted);
      
      foreach (var edit in edits)
      {
        if (isAdmin)
        {
          _db.Execute(@"
          UPDATE stickers SET text = (:text)
          WHERE unique_id = (:uniqueId)",
            new {text = edit.Text, uniqueId = edit.UniqueId},
            transaction
          );
          continue;
        }
        _db.Execute(@"
              INSERT INTO 
                  user_edits (unique_id, user_id, text) 
                  VALUES (:uniqueId, :userId, :text) 
                  ON CONFLICT (unique_id, user_id) 
                      DO UPDATE SET text=:text, rejected=false",
          new {uniqueId = edit.UniqueId, userId = edit.UserId, text = edit.Text}, 
          transaction);
      }

      transaction.Commit();
      _db.Close();
    }
  }
}