using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces;

public interface IAccountVerificationClient
{
    Task<bool> SendVerificationEmailAsync(string token);
}
