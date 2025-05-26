using SportConnect.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SportConnect.Models;

namespace SportConnect.Services.Sport
{
    public interface ISportService : IRepository<SportConnect.Models.Sport>
    {
    }
}