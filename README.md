# Library Management System

A full-stack Library Management System developed for the University of Transport and Communications (UTC).

The system provides a digital platform for managing books, users, borrowing and returning processes, e-books, reading progress, and personalized book recommendations.

## Overview

The Library Management System was developed to digitize and simplify library operations for both library staff and users.

The system supports:

- Book management
- User and role management
- Online book search and reservation
- QR code-based borrowing and returning
- E-book reading
- Automatic reading progress saving
- Borrowing history tracking
- Due-date reminders
- Automatic overdue fee calculation
- Book favorites
- Personalized book recommendations
- Authentication and authorization

---

## Features

### Authentication & Authorization

- User registration and login
- Authentication using ASP.NET Core Identity
- Role-based authorization
- Support for multiple user roles:
  - Administrator
  - Librarian
  - Student
  - Teacher
- Account expiration management

---

### Book Management

The system supports multiple types of library documents:

- Physical books
- Articles
- Theses
- E-books

Main features:

- Create, update, and delete books
- Search books by title and other information
- Filter books by category, author, language, and document type
- View detailed book information
- Manage physical book copies
- Track book availability

---

### Online Book Reservation

Users can:

- Search for available books
- View book details
- Reserve books online
- View reservation status
- Manage their reservations

---

### QR Code Borrowing & Returning

The system provides QR code-based borrowing and returning for physical books.

Users can:

1. Find a book
2. Scan the QR code
3. Create a borrowing request
4. Borrow the book
5. Return the book by scanning the QR code

This helps reduce manual processing during borrowing and returning.

---

### E-book Reading

Users can read e-books directly through the web application.

Features include:

- Online PDF reading
- Page navigation
- Automatic reading progress saving
- Resume reading from the last position

The system stores the user's reading progress so they can continue reading later.

---

### Borrowing & Overdue Management

The system automatically manages borrowing deadlines.

Features:

- Borrowing due dates
- Automatic overdue detection
- Due-date reminders
- Overdue fee calculation
- Borrowing history
- Borrowing status tracking

Background jobs are used to process scheduled tasks automatically.

---

### Favorites

Users can:

- Add books to favorites
- Remove books from favorites
- View their favorite books

Favorite interactions can also contribute to the recommendation system.

---

### Book Recommendation System

The system includes a personalized book recommendation feature using Machine Learning.

The recommendation system uses user interaction data such as:

- Borrowing history
- Favorite books

The system applies **Matrix Factorization** using ML.NET to generate personalized recommendations.

When there is insufficient interaction data, the system can fall back to content-based recommendations.

---

## Tech Stack

### Frontend

- Vue.js 3
- Composition API
- JavaScript / TypeScript
- Vite
- Pinia
- Vue Router
- HTML5
- CSS3
- Bootstrap / Tailwind CSS
- Axios

### Backend

- ASP.NET Core
- C#
- Entity Framework Core
- ASP.NET Core Identity
- RESTful API

### Database

- MySQL
- Entity Framework Core
- Pomelo.EntityFrameworkCore.MySql

### Machine Learning

- ML.NET
- Matrix Factorization
- Content-based recommendation fallback

### File & Media

- Cloudinary
- PDF.js

### Development Tools

- Git
- GitHub
- Visual Studio Code
- MySQL Workbench
- Postman

---

## System Architecture

The system follows a client-server architecture.

```text
┌──────────────────────────────┐
│          Vue.js 3            │
│          Frontend            │
│                              │
│  Components                  │
│  Pages                       │
│  Pinia                       │
│  Vue Router                  │
└──────────────┬───────────────┘
               │
               │ REST API
               │ HTTP / JSON
               ▼
┌──────────────────────────────┐
│       ASP.NET Core API       │
│                              │
│ Controllers                  │
│ Services                     │
│ Business Logic               │
│ Authentication              │
│ Authorization               │
└──────────────┬───────────────┘
               │
               │ Entity Framework Core
               ▼
┌──────────────────────────────┐
│            MySQL             │
│                              │
│ Users                        │
│ Books                        │
│ Borrowing                    │
│ Reservations                 │
│ Recommendations              │
└──────────────────────────────┘

               │
               ├──────────────► Cloudinary
               │                File / Image Storage
               │
               └──────────────► ML.NET
                                Recommendation System


📂 Project Structure
LibraryManagementSystem/
│
├── backend/
│   ├── Controllers/
│   ├── Data/
│   ├── DTOs/
│   ├── Models/
│   ├── Services/
│   ├── Migrations/
│   ├── Program.cs
│   └── appsettings.json
│
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   ├── views/
│   │   ├── stores/
│   │   ├── router/
│   │   ├── services/
│   │   └── assets/
│   ├── public/
│   ├── package.json
│   └── vite.config.js
│
└── README.md

The actual project structure may differ depending on the current implementation.

🗄️ Main Database Entities

The system contains several main entities:

ApplicationUser
    │
    ├── BorrowTransactions
    ├── BorrowRequests
    ├── UserFavoriteBooks
    ├── ReadingProgress
    └── Recommendations


Book
    │
    ├── BookAuthors
    ├── BookCategories
    ├── BookLanguages
    └── BookCopies


BookCopy
    │
    └── BorrowTransactions


DocumentType
    ├── Physical Book
    ├── Article
    ├── Thesis
    └── E-book
🔐 Security

The application uses ASP.NET Core Identity for authentication and authorization.

Security-related features include:

Password hashing
Authentication
Role-based authorization
Protected API endpoints
User permission management
Token-based authentication

Sensitive configuration values such as:

Database passwords
API keys
Cloudinary credentials
Authentication secrets

should be stored in environment variables or local configuration files and must not be committed to the repository.

⚙️ Getting Started
Prerequisites

Make sure the following tools are installed:

.NET SDK 8+
Node.js 20+
npm
MySQL 8+
Git
📥 Clone the Repository
git clone https://github.com/HiepNT2003/library-management-system.git


cd library-management-system
🔧 Backend Setup

Go to the backend directory:

cd backend

Restore dependencies:

dotnet restore

Configure the database connection in:

appsettings.Development.json

Example:

{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;database=LibrarySchoolDB;user=root;password=YOUR_PASSWORD"
  }
}

Do not commit real database credentials or API keys to the repository.

🗄️ Database Migration

Apply Entity Framework Core migrations:

dotnet ef database update

If Entity Framework CLI is not installed:

dotnet tool install --global dotnet-ef
▶️ Run Backend
dotnet run

The API will be available at the configured local address.

Example:

https://localhost:5001
🎨 Frontend Setup

Open a new terminal and go to the frontend directory:

cd frontend

Install dependencies:

npm install

Configure the API URL in your environment file:

VITE_API_URL=https://localhost:5001/api

Run the development server:

npm run dev

The frontend will normally be available at:

http://localhost:5173

🧠 Recommendation System

The recommendation system uses ML.NET Matrix Factorization to provide personalized book recommendations.

User Interaction

The system collects implicit user interactions such as:

Borrow book
    ↓
User interaction score


Favorite book
    ↓
Higher interaction score
        ↓
ML.NET Matrix Factorization
        ↓
Personalized recommendations

The model is trained using historical user-book interactions.

When there is insufficient data for a user, the system can use content-based filtering as a fallback strategy.

🔄 Background Jobs

Background jobs are used to automate scheduled tasks such as:

Checking overdue books
Calculating overdue fees
Sending due-date reminders
Updating borrowing status

This reduces manual processing and ensures that time-dependent operations are handled automatically.

🧪 API

The backend exposes RESTful APIs for the frontend.

Example API categories:

/api/auth
/api/books
/api/authors
/api/categories
/api/book-copies
/api/transactions
/api/borrow-requests
/api/users
/api/recommendations

The frontend communicates with the backend using HTTP requests and JSON data.

🚀 Future Improvements

Potential improvements include:

More advanced recommendation algorithms
Improved search with full-text indexing
Performance optimization
Automated testing
Docker containerization
More detailed analytics and reporting
📄 License

This project was developed for educational purposes as a graduation project.
