# System Rezerwacji Biletów Lotniczych

## Opis projektu

System rezerwacji biletów lotniczych oparty o architekturę SOA (Service-Oriented Architecture) z wykorzystaniem protokołu SOAP. Projekt składa się z dwóch komponentów:

1. **Web Serwis SOAP** (.NET 9 / ASP.NET Core + SoapCore) — serwer udostępniający operacje wyszukiwania lotów, zakupu biletów, sprawdzania rezerwacji i generowania potwierdzeń PDF
2. **Aplikacja kliencka** (Python + tkinter) — desktopowa aplikacja okienkowa konsumująca web serwis SOAP za pomocą biblioteki `zeep`

### Wymagania funkcjonalne

| # | Funkcjonalność | Opis |
|---|---|---|
| 1 | Baza lotów | Miasto wylotu, miasto przylotu, data, godzina, cena, wolne miejsca |
| 2 | Wyszukiwanie lotów | Filtrowanie po mieście wylotu, przylotu i dacie |
| 3 | Kupno biletu | Zakup biletu na wybrany lot z podaniem danych pasażera |
| 4 | Potwierdzenie PDF | Odbiór potwierdzenia kupna w formacie PDF (przesyłany jako załącznik binarny) |
| 5 | Sprawdzenie rezerwacji | Weryfikacja rezerwacji na podstawie numeru |

### Zrealizowane wymagania projektowe

| Wymaganie | Status | Opis realizacji |
|---|---|---|
| Web serwis SOAP | ✅ | ASP.NET Core 9 + SoapCore, WSDL auto-generowany |
| Aplikacja kliencka | ✅ | Python 3 + tkinter (GUI) + zeep (SOAP) |
| MTOM / załączniki binarne | ✅ | PDF generowany przez QuestPDF, przesyłany jako `base64Binary` w SOAP |
| Handlers | ✅ | `IServiceOperationTuner` + middleware logujący komunikaty SOAP |
| SSL/TLS (HTTPS) | ✅ | Kestrel nasłuchuje na porcie 5001 (HTTPS) |
| Klient w innym języku | ✅ | Serwis: C# (.NET), Klient: Python |

---

## Architektura

```
┌─────────────────────────────┐         SOAP/HTTP(S)         ┌──────────────────────────────┐
│     Klient Python           │ ◄──────────────────────────► │    Web Serwis .NET           │
│  (tkinter + zeep)           │         WSDL + XML           │  (ASP.NET Core + SoapCore)   │
│                             │                               │                              │
│  • Wyszukiwanie lotów       │                               │  • IFlightReservationService │
│  • Zakup biletu             │                               │  • SQLite (EF Core)          │
│  • Sprawdzenie rezerwacji   │                               │  • QuestPDF (generowanie)    │
│  • Pobranie PDF             │                               │  • SOAP Handlers             │
└─────────────────────────────┘                               └──────────────────────────────┘
```

---

## Opis WSDL

Serwis udostępnia WSDL pod adresem:
- HTTP: `http://localhost:5000/FlightService.asmx?wsdl`
- HTTPS: `https://localhost:5001/FlightService.asmx?wsdl`

### Przestrzenie nazw

| Prefix | URI | Opis |
|---|---|---|
| `tns` | `http://flightreservation.example.com/` | Namespace serwisu |
| `ns2` | `http://schemas.datacontract.org/2004/07/FlightReservationService.Models` | Namespace modeli danych |

### Operacje serwisu

#### 1. `GetAllFlights`
Zwraca listę wszystkich dostępnych lotów.

**Request:** Brak parametrów

**Response:** `ArrayOfFlight` — lista obiektów `Flight`

#### 2. `SearchFlights`
Wyszukuje loty na podstawie podanych kryteriów.

**Request:** `FlightSearchRequest`
- `CityFrom` (string, opcjonalne) — miasto wylotu
- `CityTo` (string, opcjonalne) — miasto przylotu  
- `Date` (dateTime, opcjonalne) — data wylotu

**Response:** `ArrayOfFlight` — lista pasujących lotów

#### 3. `BuyTicket`
Kupuje bilet na wybrany lot.

**Request:** `TicketPurchaseRequest`
- `FlightId` (int) — ID lotu
- `PassengerName` (string) — imię i nazwisko pasażera
- `PassengerEmail` (string) — email pasażera

**Response:** `TicketPurchaseResponse`
- `Success` (boolean)
- `Message` (string)
- `ReservationNumber` (string) — numer rezerwacji

#### 4. `CheckReservation`
Sprawdza szczegóły rezerwacji.

**Request:** `reservationNumber` (string)

**Response:** `ReservationDetails`
- `Found` (boolean)
- `ReservationNumber`, `PassengerName`, `PassengerEmail` (string)
- `FlightNumber`, `CityFrom`, `CityTo` (string)
- `DepartureDate` (dateTime), `DepartureTime`, `SeatNumber` (string)
- `PurchaseDate` (dateTime)

#### 5. `GetReservationPdf`
Generuje i zwraca potwierdzenie rezerwacji w formacie PDF (załącznik binarny).

**Request:** `reservationNumber` (string)

**Response:** `PdfResponse`
- `Success` (boolean)
- `Message` (string)
- `PdfData` (base64Binary) — dane PDF
- `FileName` (string) — nazwa pliku

### Typy danych (XSD)

```xml
<xs:complexType name="Flight">
  <xs:sequence>
    <xs:element name="AvailableSeats" type="xs:int"/>
    <xs:element name="CityFrom" nillable="true" type="xs:string"/>
    <xs:element name="CityTo" nillable="true" type="xs:string"/>
    <xs:element name="DepartureDate" type="xs:dateTime"/>
    <xs:element name="DepartureTime" nillable="true" type="xs:string"/>
    <xs:element name="FlightNumber" nillable="true" type="xs:string"/>
    <xs:element name="Id" type="xs:int"/>
    <xs:element name="Price" type="xs:decimal"/>
  </xs:sequence>
</xs:complexType>

<xs:complexType name="TicketPurchaseRequest">
  <xs:sequence>
    <xs:element name="FlightId" type="xs:int"/>
    <xs:element name="PassengerEmail" nillable="true" type="xs:string"/>
    <xs:element name="PassengerName" nillable="true" type="xs:string"/>
  </xs:sequence>
</xs:complexType>
```

---

## Przykładowe komunikaty SOAP

### Przykład 1: GetAllFlights

**Request:**
```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                  xmlns:ns="http://flightreservation.example.com/">
  <soapenv:Header/>
  <soapenv:Body>
    <ns:GetAllFlights/>
  </soapenv:Body>
</soapenv:Envelope>
```

**Response:**
```xml
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
  <s:Body>
    <GetAllFlightsResponse xmlns="http://flightreservation.example.com/">
      <GetAllFlightsResult xmlns:d="http://schemas.datacontract.org/2004/07/FlightReservationService.Models">
        <d:Flight>
          <d:AvailableSeats>120</d:AvailableSeats>
          <d:CityFrom>Warszawa</d:CityFrom>
          <d:CityTo>Kraków</d:CityTo>
          <d:DepartureDate>2026-05-01T00:00:00</d:DepartureDate>
          <d:DepartureTime>08:00</d:DepartureTime>
          <d:FlightNumber>LO101</d:FlightNumber>
          <d:Id>1</d:Id>
          <d:Price>199.99</d:Price>
        </d:Flight>
        <!-- ... kolejne loty ... -->
      </GetAllFlightsResult>
    </GetAllFlightsResponse>
  </s:Body>
</s:Envelope>
```

### Przykład 2: SearchFlights

**Request:**
```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                  xmlns:ns="http://flightreservation.example.com/"
                  xmlns:mod="http://schemas.datacontract.org/2004/07/FlightReservationService.Models">
  <soapenv:Header/>
  <soapenv:Body>
    <ns:SearchFlights>
      <ns:request>
        <mod:CityFrom>Warszawa</mod:CityFrom>
        <mod:CityTo xsi:nil="true" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"/>
        <mod:Date>2026-05-01T00:00:00</mod:Date>
      </ns:request>
    </ns:SearchFlights>
  </soapenv:Body>
</soapenv:Envelope>
```

### Przykład 3: BuyTicket

**Request:**
```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                  xmlns:ns="http://flightreservation.example.com/"
                  xmlns:mod="http://schemas.datacontract.org/2004/07/FlightReservationService.Models">
  <soapenv:Header/>
  <soapenv:Body>
    <ns:BuyTicket>
      <ns:request>
        <mod:FlightId>1</mod:FlightId>
        <mod:PassengerName>Jan Kowalski</mod:PassengerName>
        <mod:PassengerEmail>jan@example.com</mod:PassengerEmail>
      </ns:request>
    </ns:BuyTicket>
  </soapenv:Body>
</soapenv:Envelope>
```

**Response:**
```xml
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
  <s:Body>
    <BuyTicketResponse xmlns="http://flightreservation.example.com/">
      <BuyTicketResult xmlns:d="http://schemas.datacontract.org/2004/07/FlightReservationService.Models">
        <d:Message>Bilet kupiony pomyślnie. Lot: LO101, Miejsce: C7</d:Message>
        <d:ReservationNumber>RES-20260415-45555</d:ReservationNumber>
        <d:Success>true</d:Success>
      </BuyTicketResult>
    </BuyTicketResponse>
  </s:Body>
</s:Envelope>
```

### Przykład 4: GetReservationPdf (z załącznikiem binarnym)

**Response (fragment z danymi PDF w base64):**
```xml
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
  <s:Body>
    <GetReservationPdfResponse xmlns="http://flightreservation.example.com/">
      <GetReservationPdfResult xmlns:d="http://schemas.datacontract.org/2004/07/FlightReservationService.Models">
        <d:FileName>bilet_RES-20260415-45555.pdf</d:FileName>
        <d:Message>PDF wygenerowany pomyślnie.</d:Message>
        <d:PdfData>JVBERi0xLjcKMSAw...base64...</d:PdfData>
        <d:Success>true</d:Success>
      </GetReservationPdfResult>
    </GetReservationPdfResponse>
  </s:Body>
</s:Envelope>
```

---

## Instrukcja uruchomienia

### Wymagania

- **.NET 9 SDK** (lub nowszy)
- **Python 3.10+** z pip
- (Opcjonalnie) **SoapUI** do monitorowania komunikatów SOAP

### 1. Uruchomienie Web Serwisu

```bash
cd FlightReservationService
dotnet run
```

Serwis uruchomi się na:
- **HTTP:** http://localhost:5000/FlightService.asmx
- **HTTPS:** https://localhost:5001/FlightService.asmx
- **WSDL:** http://localhost:5000/FlightService.asmx?wsdl

Baza danych SQLite (`flights.db`) zostanie automatycznie utworzona i wypełniona przykładowymi lotami.

### 2. Uruchomienie klienta Python

```bash
cd FlightReservationClient
pip install -r requirements.txt
python main.py
```

### 3. Obsługa aplikacji klienckiej

1. **Połączenie** — kliknij „Połącz" (domyślnie HTTP na porcie 5000). Zaznacz „HTTPS" dla SSL/TLS.
2. **Wyszukiwanie lotów** — zakładka „Wyszukiwanie lotów", opcjonalnie wpisz kryteria i kliknij „Szukaj".
3. **Kupno biletu** — zakładka „Kup bilet", wpisz ID lotu, imię i email, kliknij „Kup bilet".
4. **Sprawdzenie rezerwacji** — zakładka „Sprawdź rezerwację", wpisz numer rezerwacji.
5. **Pobranie PDF** — kliknij „Pobierz PDF" aby pobrać i zapisać potwierdzenie.

---

## Instrukcja dla zewnętrznego klienta

### Adres WSDL
```
http://<host>:5000/FlightService.asmx?wsdl
https://<host>:5001/FlightService.asmx?wsdl
```

### Konfiguracja klienta SOAP
- **Protokół:** SOAP 1.1
- **Kodowanie:** UTF-8
- **Binding:** BasicHttpBinding
- **Namespace serwisu:** `http://flightreservation.example.com/`
- **Namespace typów:** `http://schemas.datacontract.org/2004/07/FlightReservationService.Models`

### Typowy scenariusz użycia

1. Zaimportuj WSDL w swoim narzędziu/języku (SoapUI, zeep, Axis2, itp.)
2. Wywołaj `GetAllFlights()` lub `SearchFlights(request)` aby pobrać dostępne loty
3. Wywołaj `BuyTicket(request)` z danymi pasażera i ID wybranego lotu
4. Zapisz zwrócony `ReservationNumber`
5. Użyj `CheckReservation(reservationNumber)` aby sprawdzić szczegóły
6. Użyj `GetReservationPdf(reservationNumber)` aby pobrać potwierdzenie PDF
7. Zdekoduj pole `PdfData` z base64 i zapisz jako plik .pdf

### Przykład w Python (zeep)

```python
from zeep import Client

client = Client("http://localhost:5000/FlightService.asmx?wsdl")

# Pobierz wszystkie loty
flights = client.service.GetAllFlights()

# Kup bilet
ns = "http://schemas.datacontract.org/2004/07/FlightReservationService.Models"
req_type = client.get_type(f"{{{ns}}}TicketPurchaseRequest")
request = req_type(FlightId=1, PassengerName="Jan Kowalski", PassengerEmail="jan@example.com")
result = client.service.BuyTicket(request)
print(result.ReservationNumber)
```

---

## Opis techniczny komponentów

### Handlers (Obsługa komunikatów SOAP)

Zaimplementowano dwa mechanizmy obsługi (handlers):

1. **`SoapLoggingHandler`** (`IServiceOperationTuner`) — handler SoapCore wywoływany przed każdą operacją, loguje nazwę operacji, metodę HTTP, czas i adres IP klienta.

2. **`SoapMessageLoggingMiddleware`** — middleware ASP.NET Core przechwytujący pełne komunikaty SOAP (request i response) i logujący je w konsoli serwera. Umożliwia podgląd przesyłanych komunikatów XML.

### Przesyłanie załączników binarnych (PDF)

Operacja `GetReservationPdf` generuje dokument PDF za pomocą biblioteki **QuestPDF** i zwraca go jako tablicę bajtów (`byte[]`) zakodowaną w base64 wewnątrz komunikatu SOAP (element `PdfData` typu `xsd:base64Binary`). Klient dekoduje base64 i zapisuje plik PDF.

### SSL/TLS

Serwer Kestrel jest skonfigurowany do nasłuchiwania na dwóch portach:
- Port **5000** — HTTP (bez szyfrowania)
- Port **5001** — HTTPS (z certyfikatem deweloperskim .NET)

Dla produkcji należy skonfigurować własny certyfikat SSL w `appsettings.json` lub przez Kestrel.

---

## Monitorowanie komunikatów SOAP

### Za pomocą logów serwera
Middleware automatycznie loguje wszystkie komunikaty SOAP request/response w konsoli serwera.

### Za pomocą SoapUI
1. Otwórz SoapUI i utwórz nowy projekt SOAP z WSDL: `http://localhost:5000/FlightService.asmx?wsdl`
2. Wywołuj operacje i obserwuj komunikaty w zakładce „Raw"

### Za pomocą HTTP Monitor (SoapUI)
1. W SoapUI: Tools → HTTP Monitor
2. Ustaw port proxy (np. 8888)
3. Skonfiguruj klienta aby przechodził przez proxy
4. Obserwuj pełne komunikaty SOAP w monitorze

---

## Struktura projektu

```
rsi-projekt1/
├── FlightReservationService/          # Web Serwis SOAP (.NET 9)
│   ├── Models/                        # Modele danych (DataContract)
│   │   ├── Flight.cs                  # Model lotu
│   │   ├── Reservation.cs            # Model rezerwacji
│   │   ├── FlightSearchRequest.cs    # Request wyszukiwania
│   │   ├── TicketPurchaseRequest.cs  # Request zakupu
│   │   ├── TicketPurchaseResponse.cs # Response zakupu
│   │   ├── ReservationDetails.cs     # Szczegóły rezerwacji
│   │   └── PdfResponse.cs           # Response z PDF
│   ├── Services/
│   │   ├── IFlightReservationService.cs  # Kontrakt serwisu (ServiceContract)
│   │   └── FlightReservationServiceImpl.cs # Implementacja serwisu
│   ├── Data/
│   │   └── AppDbContext.cs           # Entity Framework DbContext + seed
│   ├── Handlers/
│   │   ├── SoapLoggingHandler.cs     # Handler SoapCore (IServiceOperationTuner)
│   │   └── SoapMessageLoggingMiddleware.cs # Middleware logujący SOAP
│   ├── Program.cs                    # Konfiguracja aplikacji
│   └── appsettings.json
├── FlightReservationClient/          # Klient Python
│   ├── main.py                       # Aplikacja okienkowa (tkinter)
│   ├── soap_client.py               # Wrapper SOAP (zeep)
│   └── requirements.txt             # Zależności Python
└── README.md                         # Dokumentacja
```

---

## Prezentacja na 2 komputerach

1. Na **komputerze A** uruchom serwis: `dotnet run` w katalogu `FlightReservationService`
2. Sprawdź adres IP komputera A (np. `ipconfig` → `192.168.1.100`)
3. Na **komputerze B** zmień URL w kliencie na: `http://192.168.1.100:5000/FlightService.asmx?wsdl`
4. Upewnij się, że firewall zezwala na połączenia na porcie 5000/5001
