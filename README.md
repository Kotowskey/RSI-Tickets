# System Rezerwacji Biletów Lotniczych

## Opis projektu

System rezerwacji biletów lotniczych oparty o architekturę SOA (Service-Oriented Architecture) z wykorzystaniem protokołu SOAP. Projekt składa się z trzech niezależnych modułów:

1. **Web Serwis SOAP** (`FlightReservationService/`, .NET 9 / ASP.NET Core + SoapCore) — serwer udostępniający operacje wyszukiwania lotów, zakupu biletów, sprawdzania rezerwacji, generowania potwierdzeń PDF oraz pełne CRUD dla lotów (admin)
2. **Aplikacja kliencka — okienkowa** (`FlightReservationClient/`, Python + tkinter) — desktopowa aplikacja konsumująca web serwis SOAP za pomocą biblioteki `zeep` (kupno biletów, sprawdzanie rezerwacji, pobieranie PDF)
3. **Panel administracyjny — web** (`FlightReservationAdminWeb/`, .NET 9 / ASP.NET Core MVC) — aplikacja w przeglądarce dla administratora (pełne CRUD lotów, upload zdjęć), konsumująca ten sam web serwis SOAP przez `ChannelFactory<T>` i WCF

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
| Klient okienkowy | ✅ | Python 3 + tkinter (GUI) + zeep (SOAP) |
| Klient w przeglądarce | ✅ | ASP.NET Core 9 MVC + Bootstrap 5, `ChannelFactory<T>` (WCF) |
| 3 niezależne moduły | ✅ | Serwis + klient okienkowy + panel web |
| MTOM / załączniki binarne | ✅ | PDF generowany przez QuestPDF + zdjęcia lotów — `base64Binary` w SOAP |
| Upload plików (zdjęcia) | ✅ | Admin web wysyła zdjęcia JPG/PNG do serwisu SOAP przez `byte[]` |
| Pełny CRUD dla klasy | ✅ | `Flight` — Create / Read / Update / Delete przez panel admina |
| Handlers | ✅ | `IServiceOperationTuner` + middleware logujący komunikaty SOAP |
| SSL/TLS (HTTPS) | ✅ | Kestrel nasłuchuje na porcie 5001 (HTTPS) |
| Klient w innym języku | ✅ | Serwis: C# (.NET), Klient 1: Python (zeep), Klient 2: C# (WCF) |
| Przechowywanie danych | ✅ | SQLite przez Entity Framework Core (plik `flights.db`) |

---

## Architektura

```
┌─────────────────────────────┐                              ┌──────────────────────────────┐
│  Klient Python (tkinter)    │                              │   Panel Web (ASP.NET MVC)    │
│   FlightReservationClient   │                              │   FlightReservationAdminWeb  │
│                             │                              │                              │
│  • Wyszukiwanie lotów       │                              │  • Lista lotów (CRUD)        │
│  • Zakup biletu             │                              │  • Dodawanie / edycja lotów  │
│  • Sprawdzenie rezerwacji   │                              │  • Upload zdjęć (PNG/JPG)    │
│  • Pobranie PDF             │                              │  • Podgląd szczegółów        │
└──────────────┬──────────────┘                              └──────────────┬───────────────┘
               │                                                            │
               │ SOAP/HTTP(S)    WSDL + XML         SOAP/HTTP(S)            │
               │ (zeep)                             (ChannelFactory)        │
               │                                                            │
               └──────────────────────┐      ┌─────────────────────────────┘
                                      ▼      ▼
                    ┌──────────────────────────────────────────┐
                    │     Web Serwis SOAP (ASP.NET + SoapCore) │
                    │       FlightReservationService           │
                    │                                          │
                    │  • IFlightReservationService (kontrakt)  │
                    │  • 10 operacji (5 user + 5 admin CRUD)   │
                    │  • SQLite (EF Core) – flights.db         │
                    │  • QuestPDF (generowanie potwierdzeń)    │
                    │  • Handlers + middleware (logowanie)     │
                    │  • HTTP 5000 / HTTPS 5001                │
                    └──────────────────────────────────────────┘
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

#### 6. `AddFlight` (admin)
Dodaje nowy lot. Obsługuje opcjonalny upload zdjęcia jako `base64Binary`.

**Request:** `FlightAdminRequest`
- `FlightNumber`, `CityFrom`, `CityTo`, `DepartureTime` (string)
- `DepartureDate` (dateTime), `Price` (decimal), `AvailableSeats` (int)
- `PhotoData` (base64Binary, opcjonalne), `PhotoFileName`, `PhotoContentType` (string, opcjonalne)

**Response:** `FlightOperationResponse` — `Success`, `Message`, `FlightId` (ID nowo utworzonego lotu)

#### 7. `UpdateFlight` (admin)
Aktualizuje dane istniejącego lotu. Pozwala wymienić zdjęcie lub usunąć je przez flagę `RemovePhoto`.

**Request:** `FlightAdminRequest` (z polem `Id`)

**Response:** `FlightOperationResponse`

#### 8. `DeleteFlight` (admin)
Usuwa lot. Odmawia usunięcia, jeśli lot ma aktywne rezerwacje.

**Request:** `flightId` (int)

**Response:** `FlightOperationResponse`

#### 9. `GetFlight`
Zwraca pojedynczy lot po ID (bez bajtów zdjęcia — tylko flaga `HasPhoto`).

**Request:** `flightId` (int)

**Response:** `Flight` lub `null` jeśli nie znaleziono

#### 10. `GetFlightPhoto`
Zwraca binarne zdjęcie lotu (załącznik `base64Binary`).

**Request:** `flightId` (int)

**Response:** `FlightPhotoResponse` — `Success`, `Message`, `PhotoData`, `FileName`, `ContentType`

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
- **Docker + Docker Compose** (opcjonalnie, alternatywa dla uruchamiania ręcznego)

### Szybki start przez Docker Compose

W katalogu głównym projektu:

```bash
docker compose up --build -d
```

Jeśli Twoja wersja Dockera nie wspiera `docker compose`, użyj:

```bash
docker-compose up --build -d
```

Dostępne adresy po uruchomieniu:
- SOAP (HTTP): `http://localhost:5000/FlightService.asmx`
- WSDL (HTTP): `http://localhost:5000/FlightService.asmx?wsdl`
- Panel admina (HTTP): `http://localhost:5002`

Zatrzymanie:

```bash
docker compose down
```

Alternatywnie (starszy CLI):

```bash
docker-compose down
```

> W trybie Docker Compose domyślnie włączony jest tylko HTTP (HTTPS wyłączone dla uproszczenia pracy kontenerów bez certyfikatu). Dane SQLite są trzymane w wolumenie `soap-db-data`, więc przetrwają restart kontenerów.

### Docker Compose z HTTPS

1. Wygeneruj certyfikat `.pfx` w katalogu `certs/`:

```bash
mkdir -p certs
dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p "changeit123"
```

2. Uruchom stack z nakładką HTTPS:

```bash
docker compose -f docker-compose.yml -f docker-compose.https.yml up --build -d
```

Jeśli używasz starszego CLI:

```bash
docker-compose -f docker-compose.yml -f docker-compose.https.yml up --build -d
```

Dostępne adresy HTTPS:
- SOAP (HTTPS): `https://localhost:5001/FlightService.asmx`
- WSDL (HTTPS): `https://localhost:5001/FlightService.asmx?wsdl`
- Panel admina (HTTPS): `https://localhost:5003`

Wyłączenie:

```bash
docker compose -f docker-compose.yml -f docker-compose.https.yml down
```

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

### 2. Uruchomienie klienta Python (okienkowego)

```bash
cd FlightReservationClient
pip install -r requirements.txt
python main.py
```

### 3. Uruchomienie panelu administracyjnego (web)

W osobnym terminalu, przy działającym serwisie SOAP:

```bash
cd FlightReservationAdminWeb
dotnet run
```

Panel admina uruchomi się na:
- **HTTP:** http://localhost:5002
- **HTTPS:** https://localhost:5003

Otwórz adres w przeglądarce. Panel komunikuje się z serwisem SOAP przez `ChannelFactory<IFlightReservationService>` (WCF) — URL serwisu można zmienić w `FlightReservationAdminWeb/appsettings.json`:

```json
"SoapService": {
  "Url": "http://localhost:5000/FlightService.asmx"
}
```

### 4. Obsługa aplikacji klienckiej (tkinter)

1. **Połączenie** — kliknij „Połącz" (domyślnie HTTP na porcie 5000). Zaznacz „HTTPS" dla SSL/TLS.
2. **Wyszukiwanie lotów** — zakładka „Wyszukiwanie lotów", opcjonalnie wpisz kryteria i kliknij „Szukaj".
3. **Kupno biletu** — zakładka „Kup bilet", wpisz ID lotu, imię i email, kliknij „Kup bilet".
4. **Sprawdzenie rezerwacji** — zakładka „Sprawdź rezerwację", wpisz numer rezerwacji.
5. **Pobranie PDF** — kliknij „Pobierz PDF" aby pobrać i zapisać potwierdzenie.

### 5. Obsługa panelu administracyjnego (web)

1. **Lista lotów** — strona główna `/Flights` pokazuje tabelę wszystkich lotów z miniaturami zdjęć.
2. **Dodawanie lotu** — przycisk „Dodaj nowy lot" → formularz z polami lotu + opcjonalnym uploadem zdjęcia (JPG/PNG).
3. **Edycja lotu** — przycisk „Edytuj" przy loście → zmiana danych, wymiana lub usunięcie zdjęcia.
4. **Szczegóły lotu** — przycisk „oko" → widok ze zdjęciem w pełnym rozmiarze + tabela szczegółów.
5. **Usuwanie lotu** — przycisk „kosz" → strona potwierdzenia. Lot z aktywnymi rezerwacjami nie zostanie usunięty (zwraca błąd z serwisu).

> **Uwaga:** Przy pierwszym uruchomieniu po aktualizacji serwis automatycznie wykryje stary schemat bazy (bez kolumn `Photo*`) i przebuduje plik `flights.db`. Wszystkie wcześniejsze rezerwacje zostaną utracone — taki koszt uproszczonego podejścia bez migracji EF.

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

### Przesyłanie załączników binarnych (PDF i zdjęcia)

- **Download PDF (serwer → klient)**: `GetReservationPdf` generuje dokument PDF za pomocą biblioteki **QuestPDF** i zwraca go jako `byte[]` w polu `PdfData` (SOAP `xsd:base64Binary`). Klient dekoduje i zapisuje plik.
- **Upload zdjęć (klient → serwer)**: operacje `AddFlight` / `UpdateFlight` przyjmują pole `PhotoData` typu `byte[]` (SOAP `xsd:base64Binary`) wraz z `PhotoFileName` i `PhotoContentType`. Panel admina wczytuje plik obrazka (`IFormFile`), strumień bajtów trafia bezpośrednio do komunikatu SOAP.
- **Download zdjęć (serwer → klient)**: operacja `GetFlightPhoto` zwraca `byte[]` z obrazkiem, a kontroler `FlightsController.Photo(id)` strumieniuje go do przeglądarki jako `<img src="...">`.
- `ReaderQuotas.MaxStringContentLength` ustawiony na 10 MB po stronie serwera + `MaxReceivedMessageSize` 20 MB po stronie klienta WCF pozwalają przesyłać zdjęcia do ~10 MB.

### Panel administracyjny (klient w przeglądarce)

Moduł `FlightReservationAdminWeb` jest drugim klientem SOAP w architekturze — napisanym w C# / ASP.NET Core 9 MVC. W odróżnieniu od klienta Python (zeep) używa **WCF**: `ChannelFactory<IFlightReservationService>` z `CustomBinding` dopasowanym do konfiguracji serwera:

- `TextMessageEncodingBindingElement` z `MessageVersion.Soap11WSAddressingAugust2004`
- `HttpTransportBindingElement` / `HttpsTransportBindingElement` z powiększonym `MaxReceivedMessageSize` (20 MB na upload zdjęć)

Dzięki temu stos prezentuje **interoperacyjność WSDL** z dwóch stron: ten sam serwis SOAP jest konsumowany przez klienta Pythonowego (dynamiczny proxy z WSDL) i klienta .NET (statyczny kontrakt przez interfejs). Zawartość komunikatów XML jest identyczna.

Stos UI admina: **Bootstrap 5** (CDN), **Bootstrap Icons**, walidacja formularzy przez `jQuery Validation Unobtrusive` + atrybuty DataAnnotations w `FlightFormModel`.

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
│   │   ├── Flight.cs                  # Model lotu (+ pola zdjęcia)
│   │   ├── Reservation.cs             # Model rezerwacji
│   │   ├── FlightSearchRequest.cs     # Request wyszukiwania
│   │   ├── TicketPurchaseRequest.cs   # Request zakupu
│   │   ├── TicketPurchaseResponse.cs  # Response zakupu
│   │   ├── ReservationDetails.cs      # Szczegóły rezerwacji
│   │   ├── PdfResponse.cs             # Response z PDF
│   │   ├── FlightAdminRequest.cs      # Request CRUD admina (Add/Update)
│   │   ├── FlightOperationResponse.cs # Response CRUD admina
│   │   └── FlightPhotoResponse.cs     # Response ze zdjęciem lotu
│   ├── Services/
│   │   ├── IFlightReservationService.cs    # Kontrakt serwisu (10 operacji)
│   │   └── FlightReservationServiceImpl.cs # Implementacja serwisu + CRUD
│   ├── Data/
│   │   └── AppDbContext.cs            # Entity Framework DbContext + seed
│   ├── Handlers/
│   │   ├── SoapLoggingHandler.cs             # Handler (IServiceOperationTuner)
│   │   └── SoapMessageLoggingMiddleware.cs   # Middleware logujący SOAP
│   ├── Program.cs                     # Konfiguracja aplikacji + auto-migracja
│   └── appsettings.json
│
├── FlightReservationClient/           # Klient Python (okienkowy)
│   ├── main.py                        # Aplikacja okienkowa (tkinter)
│   ├── soap_client.py                 # Wrapper SOAP (zeep)
│   └── requirements.txt               # Zależności Python
│
├── FlightReservationAdminWeb/         # Panel admina (ASP.NET Core MVC)
│   ├── Soap/
│   │   ├── Contracts.cs               # DTO + IFlightReservationService (proxy)
│   │   └── SoapClientFactory.cs       # ChannelFactory<T> + CustomBinding (WCF)
│   ├── Controllers/
│   │   ├── HomeController.cs          # Redirect do /Flights
│   │   └── FlightsController.cs       # CRUD: Index/Details/Create/Edit/Delete/Photo
│   ├── Models/
│   │   └── FlightFormModel.cs         # ViewModel formularzy + walidacja
│   ├── Views/
│   │   ├── Shared/_Layout.cshtml      # Layout Bootstrap 5
│   │   ├── Home/Error.cshtml
│   │   └── Flights/                   # Widoki Razor
│   │       ├── Index.cshtml           # Lista lotów z miniaturkami
│   │       ├── Details.cshtml         # Szczegóły lotu + zdjęcie
│   │       ├── Create.cshtml          # Formularz dodawania + upload
│   │       ├── Edit.cshtml            # Formularz edycji + wymiana zdjęcia
│   │       └── Delete.cshtml          # Potwierdzenie usunięcia
│   ├── Program.cs                     # Konfiguracja Kestrel (porty 5002/5003)
│   ├── appsettings.json               # Konfiguracja (URL serwisu SOAP)
│   └── FlightReservationAdminWeb.csproj
│
└── README.md                          # Dokumentacja
```

---

## Prezentacja na 2 komputerach

1. Na **komputerze A** uruchom serwis: `dotnet run` w katalogu `FlightReservationService`
2. Sprawdź adres IP komputera A (np. `ipconfig` → `192.168.1.100`)
3. Na **komputerze B**:
   - **Klient Python**: wpisz URL `http://192.168.1.100:5000/FlightService.asmx?wsdl` w polu WSDL
   - **Panel admina (web)**: uruchom `dotnet run` w `FlightReservationAdminWeb/`, edytując wcześniej `appsettings.json` → `SoapService.Url = "http://192.168.1.100:5000/FlightService.asmx"`. Panel będzie dostępny pod `http://localhost:5002` na komputerze B.
4. Upewnij się, że firewall na komputerze A zezwala na połączenia przychodzące na portach **5000/5001**. Porty **5002/5003** (admin) nasłuchują lokalnie na komputerze B.
