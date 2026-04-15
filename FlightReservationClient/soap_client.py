"""SOAP client wrapper using zeep for the Flight Reservation Service."""

from datetime import datetime
from zeep import Client
from zeep.transports import Transport
from requests import Session

NS_MODELS = "http://schemas.datacontract.org/2004/07/FlightReservationService.Models"


class FlightSoapClient:
    def __init__(self, wsdl_url: str, verify_ssl: bool = True):
        session = Session()
        session.verify = verify_ssl
        transport = Transport(session=session, timeout=30)
        self._client = Client(wsdl=wsdl_url, transport=transport)
        self._service = self._client.service

    def get_all_flights(self) -> list[dict]:
        result = self._service.GetAllFlights()
        return self._parse_flights(result)

    def search_flights(self, city_from: str | None = None,
                       city_to: str | None = None,
                       date: str | None = None) -> list[dict]:
        request_type = self._client.get_type(f"{{{NS_MODELS}}}FlightSearchRequest")
        parsed_date = None
        if date:
            try:
                parsed_date = datetime.strptime(date, "%Y-%m-%d")
            except ValueError:
                pass
        req = request_type(CityFrom=city_from, CityTo=city_to, Date=parsed_date)
        result = self._service.SearchFlights(req)
        return self._parse_flights(result)

    def buy_ticket(self, flight_id: int, passenger_name: str,
                   passenger_email: str) -> dict:
        request_type = self._client.get_type(f"{{{NS_MODELS}}}TicketPurchaseRequest")
        req = request_type(
            FlightId=flight_id,
            PassengerName=passenger_name,
            PassengerEmail=passenger_email)
        result = self._service.BuyTicket(req)
        return self._serialize(result)

    def check_reservation(self, reservation_number: str) -> dict:
        result = self._service.CheckReservation(reservation_number)
        return self._serialize(result)

    def get_reservation_pdf(self, reservation_number: str) -> dict:
        result = self._service.GetReservationPdf(reservation_number)
        return self._serialize(result)

    @staticmethod
    def _parse_flights(result) -> list[dict]:
        if result is None:
            return []
        flights = result if isinstance(result, list) else [result]
        return [FlightSoapClient._serialize(f) for f in flights]

    @staticmethod
    def _serialize(obj) -> dict:
        if obj is None:
            return {}
        if isinstance(obj, dict):
            return {k: FlightSoapClient._convert(v) for k, v in obj.items()}
        try:
            from zeep.xsd.valueobjects import CompoundValue
            if isinstance(obj, CompoundValue):
                return {key: FlightSoapClient._convert(getattr(obj, key))
                        for key in obj.__class__.__dataclass_fields__}
        except (ImportError, AttributeError):
            pass
        if hasattr(obj, "__values__"):
            return dict(obj.__values__)
        if hasattr(obj, "__dict__"):
            return {k: FlightSoapClient._convert(v)
                    for k, v in obj.__dict__.items() if not k.startswith("_")}
        return {"value": obj}

    @staticmethod
    def _convert(value):
        if isinstance(value, dict):
            return FlightSoapClient._serialize(value)
        return value
