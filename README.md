# Restaurant Reservation System

A modern, production-ready restaurant reservation web application built with ASP.NET Core 8.0 (LTS). Features multi-restaurant support, admin panel with role-based authentication, real-time notifications, and a complete booking flow with QR code confirmations.

## Features

### Multi-Restaurant & Branch Management
- Support for multiple restaurants with multiple branches
- Comprehensive branch details: photos, cuisine, location (lat/lng), hours, capacity
- Table management with min/max capacity, location types (indoor/outdoor/terrace/private)
- Visual table layout editor

### Booking System
- Guest-facing booking widget with party size, date, and time slot selection
- Dynamic availability filtering based on table capacity
- Table location preference selection
- Special occasion and request handling
- QR code generation for booking confirmation
- Booking lookup by reference number

### Authentication & Authorization
- Role-based access control:
  - **SuperAdmin**: Full system access
  - **RestaurantManager**: Manage assigned restaurant and branches
  - **BranchManager**: Manage assigned branch
  - **Guest**: Book tables and manage own bookings
- OTP verification via phone/email (Twilio/SendGrid integration points)
- ASP.NET Core Identity with secure password policies

### Admin Panel
- Dashboard with key metrics and recent activity
- Booking management with calendar view
- Review moderation
- Menu management with categories and dietary flags
- Offers and coupon management

### Additional Features
- Real-time notifications via SignalR
- Payment integration interface (Stripe)
- Loyalty program with points
- Review and rating system
- File uploads with cloud storage abstraction
- Concurrency-safe booking allocation

## Technology Stack

- **Backend**: ASP.NET Core 8.0 (LTS)
- **Database**: SQLite (dev) / PostgreSQL (prod)
- **ORM**: Entity Framework Core 8.0
- **Authentication**: ASP.NET Core Identity
- **Real-time**: SignalR
- **Payments**: Stripe.net
- **QR Codes**: QRCoder
- **Frontend**: Bootstrap 5, Bootstrap Icons
- **Testing**: xUnit, Moq

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- (Optional) Docker and Docker Compose
- (Optional) PostgreSQL for production

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-repo/restaurant-reservation.git
   cd restaurant-reservation
   ```

2. **Configure environment** (optional)
   ```bash
   cp .env.sample .env
   # Edit .env with your API keys
   ```

3. **Run the application**
   ```bash
   cd RestaurantReservation.Web
   dotnet run
   ```

4. **Access the application**
   - Main site: https://localhost:5001
   - Admin panel: https://localhost:5001/Admin

### Default Accounts

The application seeds the following demo accounts:

| Role | Username | Email | Password |
|------|----------|-------|----------|
| SuperAdmin | superadmin | admin@restaurant-reservation.com | Admin@123! |
| RestaurantManager | restaurantmanager | manager@gourmetparadise.example.com | Manager@123! |
| BranchManager | branchmanager | branch@gourmetparadise.example.com | Branch@123! |
| Guest | guest | guest@example.com | Guest@123! |

## Docker Setup

### Development with SQLite

```bash
docker-compose up web
```

Access at: http://localhost:8080

### Production with PostgreSQL

```bash
docker-compose --profile postgres up web-postgres db
```

Access at: http://localhost:8081

### Build only

```bash
docker build -t restaurant-reservation .
```

## Configuration

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `DatabaseProvider` | `Sqlite` or `PostgreSQL` | No (default: Sqlite) |
| `ConnectionStrings__DefaultConnection` | Database connection string | No |
| `Stripe__SecretKey` | Stripe secret API key | No |
| `Stripe__PublishableKey` | Stripe publishable key | No |
| `SendGrid__ApiKey` | SendGrid API key for emails | No |
| `Twilio__AccountSid` | Twilio account SID | No |
| `Twilio__AuthToken` | Twilio auth token | No |
| `Twilio__PhoneNumber` | Twilio phone number | No |
| `GoogleMaps__ApiKey` | Google Maps API key | No |

### Switching Databases

**SQLite (default):**
```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=RestaurantReservation.db"
  }
}
```

**PostgreSQL:**
```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=restaurant_reservation;Username=postgres;Password=your_password"
  }
}
```

## Project Structure

```
RestaurantReservation/
├── RestaurantReservation.sln
├── Dockerfile
├── docker-compose.yml
├── .env.sample
├── README.md
├── RestaurantReservation.Web/
│   ├── Areas/
│   │   └── Admin/          # Admin area controllers and views
│   ├── Controllers/        # Main controllers
│   ├── Data/               # DbContext and seed data
│   ├── Hubs/               # SignalR hubs
│   ├── Models/
│   │   ├── Entities/       # EF Core entities
│   │   └── ViewModels/     # View models
│   ├── Services/
│   │   └── Interfaces/     # Service interfaces and implementations
│   ├── Views/              # Razor views
│   └── wwwroot/            # Static files
└── RestaurantReservation.Tests/
    ├── BookingServiceTests.cs
    └── AvailabilityServiceTests.cs
```

## Running Tests

```bash
dotnet test
```

## API Endpoints

### Booking API (AJAX)

- `GET /Bookings/GetAvailableTimeSlots?branchId={id}&date={date}&partySize={size}`
- `GET /Bookings/GetAvailableTables?branchId={id}&date={date}&time={time}&partySize={size}`
- `GET /Bookings/GetAvailabilityCalendar?branchId={id}&partySize={size}`
- `POST /Bookings/ValidateCoupon`

### Admin API

- `GET /Admin/Bookings/GetCalendarEvents?branchId={id}&start={date}&end={date}`

## Integrations

### Stripe Payment

The application includes a complete Stripe PaymentIntent flow for deposits. Configure your Stripe keys in environment variables to enable payments.

### SendGrid/Twilio

Email and SMS notifications are implemented with placeholder integration points. Add your API keys to enable:
- Booking confirmations
- Booking reminders
- OTP verification

### Google Maps

Add your Google Maps API key to enable location maps on branch pages.

## Security Considerations

- All API keys should be stored as environment variables, never in source code
- Password requirements: 8+ characters with uppercase, lowercase, number, and symbol
- CSRF protection enabled on all forms
- Role-based authorization on admin endpoints
- Concurrency-safe booking allocation with optimistic locking

## License

MIT License

## Support

For issues and feature requests, please use the GitHub issue tracker.
