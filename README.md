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
