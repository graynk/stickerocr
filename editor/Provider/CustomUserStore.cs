using System;
using System.Threading;
using System.Threading.Tasks;
using editor.Data;
using Microsoft.AspNetCore.Identity;

namespace editor.Provider
{
    /// <summary>
    /// This store is only partially implemented. It supports user creation and find methods.
    /// </summary>
    public class CustomUserStore : IUserStore<User>, 
        IUserPasswordStore<User>
    {

      #region createuser
        public async Task<IdentityResult> CreateAsync(User user, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
          throw new NotImplementedException();
        }
        #endregion

        public Task<IdentityResult> DeleteAsync(User user, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
          throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public async Task<User> FindByIdAsync(string userId, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
          throw new NotImplementedException();
        }

        public async Task<User> FindByNameAsync(string userName, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
          throw new NotImplementedException();
        }

        public Task<string> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
        {
          throw new NotImplementedException(); 
        }

        public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        {
          throw new NotImplementedException();
        }

        public Task<string> GetUserNameAsync(User user, CancellationToken cancellationToken)
        {
          throw new NotImplementedException();
        }

        public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetNormalizedUserNameAsync(User user, string normalizedName, CancellationToken cancellationToken)
        {
          throw new NotImplementedException();
        }

        public Task SetPasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken)
        {
          throw new NotImplementedException();

        }

        public Task SetUserNameAsync(User user, string userName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
