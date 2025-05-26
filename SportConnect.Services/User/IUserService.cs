using SportConnect.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SportConnect.Models;

namespace SportConnect.Services.User
{
    public interface IUserService : IRepository<SportConnect.Models.SportConnectUser>
    {
        public Task<IEnumerable<Models.SportConnectUser>> AllParticipants(string id);
    }
}