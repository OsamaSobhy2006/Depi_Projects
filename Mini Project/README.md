# Library Management System

A console-based library management application built with **C# and .NET 10**. It supports basic book management, borrowing and returning books, late-fee calculation, and online late-fee payments through **Stripe Checkout**.

## Features

- **Create books:** Add a book with an ID, title, and author.
- **List books:** Display all books and their borrowing status.
- **Search by title:** Find a book by title without case-sensitive matching.
- **Update books:** Edit a book's title or author.
- **Delete books:** Prevent deletion of a book that is currently borrowed.
- **Borrow books:** Set a borrowing period in days and calculate the due date.
- **Return books:** Return a book when it is not overdue.
- **Calculate late fees:** Charge **EGP 50 for each overdue day**.
- **Online payments:** Create a Stripe Checkout session and verify the payment status before completing an overdue return.

## Technologies

- C#
- .NET 10
- Stripe.net 53.0.0
- Object-Oriented Programming (OOP)
- Console-based user interface

## Project Structure

```text
LibraryManagementSystem/
├── Models/
│   └── Book.cs
├── Repositories/
│   └── BookRepository.cs
├── Services/
│   ├── BorrowBookService.cs
│   ├── CreateBookService.cs
│   ├── DeleteBookService.cs
│   ├── GetAllBooksService.cs
│   ├── GetBookByTitleService.cs
│   ├── LateFeeService.cs
│   ├── PaymentService.cs
│   ├── ReturnBookService.cs
│   └── UpdateBookService.cs
├── LibraryMenu.cs
├── Program.cs
└── LibraryManagementSystem.csproj
```

### Main Components

- **`Models/Book.cs`** — Represents a book and its properties, including `Id`, `Title`, `Author`, `IsBorrowed`, and `DueDate`.
- **`Repositories/BookRepository.cs`** — Handles adding, retrieving, updating, and deleting books. The current implementation stores books in an in-memory `List<Book>` rather than a database.
- **`Services/`** — Contains the individual services for book operations, borrowing, returning, late-fee calculation, and payments.
- **`LibraryMenu.cs`** — Displays the interactive menu and reads the user's selections.
- **`Program.cs`** — Creates and connects the repository and services, then starts the menu.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An internet connection for Stripe payments
- A Stripe account and a valid API secret key (use a test key during development)

## Getting Started

1. Clone the repository:

   ```bash
   git clone https://github.com/OsamaSobhy2006/Depi_Projects.git
   ```

2. Navigate to the directory containing `LibraryManagementSystem.csproj`:

   ```bash
   cd LibraryManagementSystem
   ```

3. Set the `STRIPE_SECRET_KEY` environment variable. Never hard-code your secret key or commit it to source control.

   **PowerShell (current session):**

   ```powershell
   $env:STRIPE_SECRET_KEY="sk_test_your_key_here"
   ```

   **Windows Command Prompt (current session):**

   ```cmd
   set STRIPE_SECRET_KEY=sk_test_your_key_here
   ```

   Replace the placeholder with your own Stripe test secret key.

4. Restore dependencies and run the application:

   ```bash
   dotnet restore
   dotnet run
   ```

## Using the Application

When the application starts, the console menu provides options to:

| Option | Operation |
|---|---|
| 1 | Create a book |
| 2 | List all books |
| 3 | Search for a book by title |
| 4 | Update a book |
| 5 | Delete a book |
| 6 | Borrow a book and set the borrowing period in days |
| 7 | Return a book |
| 8 | Exit the application |

If a book is overdue, the application calculates the late fee and opens a Stripe Checkout page in the browser. After completing the payment, return to the console and press Enter so the application can check the Checkout Session status. If Stripe confirms the payment, the application attempts to complete the return.

## Late-Fee Calculation

- No late fee is charged when a book is returned on or before its due date.
- Overdue time is calculated by calendar date rather than by the number of hours.
- **Formula:** `Late fee = Number of overdue days × EGP 50`

For example, a book returned 3 days late incurs a fee of `3 × 50 = EGP 150`.

## Important Notes

- **Data is stored in memory:** Books are kept in an in-memory list and are lost when the application closes or restarts.
- **Stripe configuration is required:** The `PaymentService` requires the `STRIPE_SECRET_KEY` environment variable. If it is missing, service initialization will fail.
- **Checkout redirect URLs:** The success URL currently points to a GitHub Pages page, while the cancellation URL uses `example.com`. Update these URLs in `Services/PaymentService.cs` for your own deployment.
- **Payment verification:** The application checks the Stripe Checkout Session status after the user returns to the console; it does not currently use a Stripe webhook.
- **Input validation:** Some numeric inputs use `int.Parse`, so invalid input may cause an exception.
- **Secret management:** Never publish your Stripe secret key. Use test keys during development.


## Contributors

- **Osama Sobhy El-Shahat**
- **Mohamed Reda Khater**
- **Noran Fawzy**
- **Eman Bendary**
- **Yousef Mohamed**

