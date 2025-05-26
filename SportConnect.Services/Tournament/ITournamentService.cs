using SportConnect.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SportConnect.Models;

namespace SportConnect.Services.Tournament
{
    public interface ITournamentService : IRepository<SportConnect.Models.Tournament>
    {
        public Task<IEnumerable<Models.Tournament>> AllParticipatedTournaments(string currentUser);
        public Task<IEnumerable<Models.Tournament>> TournamentsOfSport(string id);
        public Task<IEnumerable<Models.Tournament>> TournamentsOfUser(string id);
        public Task<IEnumerable<Models.Tournament>> AllOtherParticipatedTournaments(string currentUserId);
    }
}