using System.Threading.Tasks;
using StockPilot.Sessions.Dto;

namespace StockPilot.Web.Session
{
	public interface IPerRequestSessionCache
	{
		Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformationsAsync();
	}
}
