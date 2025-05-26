using SportConnect.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SportConnect.Models;

namespace SportConnect.Services.Participation
{
    public interface IParticipationService : IRepository<SportConnect.Models.Participation>
    {
        public Task<Models.Participation> GetParticipation(string userId, string tournamentId);
    }
}