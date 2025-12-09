using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Muestra.Hubs
{
    public class WhatsappHub : Hub
    {
        public async Task EnviarMensaje(string usuario, string mensaje)
        {
            await Clients.All.SendAsync("RecibirMensaje", usuario, mensaje);
        }
    }
}