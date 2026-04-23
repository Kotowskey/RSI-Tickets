using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace FlightReservationAdminWeb.Soap;

public interface ISoapClientFactory
{
    IFlightReservationService CreateClient();
}

public class SoapClientFactory : ISoapClientFactory
{
    private readonly ChannelFactory<IFlightReservationService> _channelFactory;

    public SoapClientFactory(IConfiguration configuration)
    {
        var endpoint = configuration["SoapService:Url"]
            ?? "http://localhost:5000/FlightService.asmx";

        var binding = new CustomBinding();

        binding.Elements.Add(new TextMessageEncodingBindingElement(
            MessageVersion.Soap11WSAddressingAugust2004, Encoding.UTF8));

        var isHttps = endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        HttpTransportBindingElement transport = isHttps
            ? new HttpsTransportBindingElement()
            : new HttpTransportBindingElement();

        transport.MaxReceivedMessageSize = 20 * 1024 * 1024;
        transport.MaxBufferSize = 20 * 1024 * 1024;

        binding.Elements.Add(transport);

        _channelFactory = new ChannelFactory<IFlightReservationService>(
            binding,
            new EndpointAddress(endpoint));
    }

    public IFlightReservationService CreateClient() => _channelFactory.CreateChannel();
}
