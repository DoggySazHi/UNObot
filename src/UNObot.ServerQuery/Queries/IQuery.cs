using System.Threading.Tasks;

namespace UNObot.ServerQuery.Queries
{
    public interface IQuery
    {
        public Task FetchData();
    }
}